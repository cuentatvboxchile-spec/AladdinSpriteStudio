namespace AladdinSpriteStudio.UI.CharacterReplacement.Conversion;

/// <summary>
/// Opciones para reducir una hoja convertida a una paleta conservadora
/// de SNES 4BPP: 16 índices como máximo, con el índice 0 reservado para
/// fondo/transparencia y hasta 15 colores visibles.
/// </summary>
public sealed class SnesSpritePaletteQuantizationOptions
{
    public int MaximumPaletteColors { get; init; } = 16;

    public bool ReserveIndexZeroForTransparency { get; init; } = true;

    public System.Drawing.Color BackgroundColor { get; init; } =
        System.Drawing.Color.Transparent;

    public int BackgroundTolerance { get; init; } = 20;

    public int AlphaThreshold { get; init; } = 16;

    /// <summary>
    /// Permite reducir el costo del análisis en imágenes muy grandes.
    /// 1 analiza todos los píxeles; 2 analiza uno de cada dos, etc.
    /// </summary>
    public int SamplingStride { get; init; } = 1;

    /// <summary>
    /// Mantiene la salida sin tramado para evitar ruido entre tiles.
    /// </summary>
    public bool UseDithering { get; init; } = false;
}

/// <summary>
/// Resultado de la reducción de paleta. Incluye la imagen cuantizada,
/// los índices por píxel y los 32 bytes de paleta SNES BGR555.
/// </summary>
public sealed class SnesSpritePaletteQuantizationResult
    : IDisposable
{
    private readonly List<System.Drawing.Color> _palette;

    internal SnesSpritePaletteQuantizationResult(
        System.Drawing.Bitmap quantizedBitmap,
        byte[,] pixelIndices,
        IReadOnlyList<System.Drawing.Color> palette,
        int originalUniqueColorCount,
        double meanColorError)
    {
        QuantizedBitmap =
            quantizedBitmap ??
            throw new ArgumentNullException(
                nameof(quantizedBitmap));

        PixelIndices =
            pixelIndices ??
            throw new ArgumentNullException(
                nameof(pixelIndices));

        ArgumentNullException.ThrowIfNull(
            palette);

        _palette =
            palette.ToList();

        OriginalUniqueColorCount =
            originalUniqueColorCount;

        MeanColorError =
            meanColorError;
    }

    public System.Drawing.Bitmap QuantizedBitmap { get; }

    /// <summary>
    /// Matriz [x, y] de índices de paleta.
    /// </summary>
    public byte[,] PixelIndices { get; }

    public IReadOnlyList<System.Drawing.Color> Palette =>
        _palette;

    public int OriginalUniqueColorCount { get; }

    public double MeanColorError { get; }

    public byte[] GetSnesBgr555PaletteBytes()
    {
        byte[] result =
            new byte[16 * 2];

        for (int index = 0;
             index < 16;
             index++)
        {
            System.Drawing.Color color =
                index < _palette.Count
                    ? _palette[index]
                    : System.Drawing.Color.Black;

            int red5 =
                color.R * 31 / 255;

            int green5 =
                color.G * 31 / 255;

            int blue5 =
                color.B * 31 / 255;

            ushort packed =
                (ushort)(
                    red5 |
                    green5 << 5 |
                    blue5 << 10);

            result[index * 2] =
                (byte)(packed & 0xFF);

            result[index * 2 + 1] =
                (byte)(packed >> 8);
        }

        return result;
    }

    public void SaveSnesPaletteBinary(
        string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            filePath);

        File.WriteAllBytes(
            filePath,
            GetSnesBgr555PaletteBytes());
    }

    public void SaveJascPalette(
        string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            filePath);

        List<string> lines =
        [
            "JASC-PAL",
            "0100",
            "16"
        ];

        for (int index = 0;
             index < 16;
             index++)
        {
            System.Drawing.Color color =
                index < _palette.Count
                    ? _palette[index]
                    : System.Drawing.Color.Black;

            lines.Add(
                $"{color.R} {color.G} {color.B}");
        }

        File.WriteAllLines(
            filePath,
            lines);
    }

    public void Dispose()
    {
        QuantizedBitmap.Dispose();
    }
}

/// <summary>
/// Cuantizador de paleta única para sprites SNES.
///
/// Esta clase prepara una paleta de 16 entradas y una matriz de índices.
/// No inserta todavía los datos en la ROM ni decide qué subpaleta OAM
/// utiliza el juego.
/// </summary>
public static class SnesSpritePaletteQuantizer
{
    private sealed class ColorSample
    {
        public required int Red { get; init; }

        public required int Green { get; init; }

        public required int Blue { get; init; }

        public required int Count { get; init; }

        public int PackedRgb =>
            Red << 16 |
            Green << 8 |
            Blue;
    }

    private sealed class ColorBox
    {
        public ColorBox(
            IReadOnlyList<ColorSample> samples)
        {
            Samples =
                samples.ToList();

            Recalculate();
        }

        public List<ColorSample> Samples { get; }

        public int TotalCount { get; private set; }

        public int RedMinimum { get; private set; }

        public int RedMaximum { get; private set; }

        public int GreenMinimum { get; private set; }

        public int GreenMaximum { get; private set; }

        public int BlueMinimum { get; private set; }

        public int BlueMaximum { get; private set; }

        public int RedRange =>
            RedMaximum -
            RedMinimum;

        public int GreenRange =>
            GreenMaximum -
            GreenMinimum;

        public int BlueRange =>
            BlueMaximum -
            BlueMinimum;

        public int LargestRange =>
            Math.Max(
                RedRange,
                Math.Max(
                    GreenRange,
                    BlueRange));

        public long SplitPriority =>
            (long)Math.Max(
                1,
                LargestRange) *
            Math.Max(
                1,
                TotalCount);

        public bool CanSplit =>
            Samples.Count > 1 &&
            LargestRange > 0;

        public void Recalculate()
        {
            if (Samples.Count == 0)
            {
                TotalCount = 0;

                RedMinimum =
                    RedMaximum =
                    GreenMinimum =
                    GreenMaximum =
                    BlueMinimum =
                    BlueMaximum =
                    0;

                return;
            }

            TotalCount =
                Samples.Sum(
                    sample => sample.Count);

            RedMinimum =
                Samples.Min(
                    sample => sample.Red);

            RedMaximum =
                Samples.Max(
                    sample => sample.Red);

            GreenMinimum =
                Samples.Min(
                    sample => sample.Green);

            GreenMaximum =
                Samples.Max(
                    sample => sample.Green);

            BlueMinimum =
                Samples.Min(
                    sample => sample.Blue);

            BlueMaximum =
                Samples.Max(
                    sample => sample.Blue);
        }

        public System.Drawing.Color GetWeightedAverage()
        {
            if (Samples.Count == 0 ||
                TotalCount <= 0)
            {
                return System.Drawing.Color.Black;
            }

            long red =
                0;

            long green =
                0;

            long blue =
                0;

            foreach (ColorSample sample
                     in Samples)
            {
                red +=
                    (long)sample.Red *
                    sample.Count;

                green +=
                    (long)sample.Green *
                    sample.Count;

                blue +=
                    (long)sample.Blue *
                    sample.Count;
            }

            return System.Drawing.Color.FromArgb(
                255,
                (int)(red / TotalCount),
                (int)(green / TotalCount),
                (int)(blue / TotalCount));
        }
    }

    public static SnesSpritePaletteQuantizationResult Quantize(
        System.Drawing.Image sourceImage,
        SnesSpritePaletteQuantizationOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(
            sourceImage);

        options ??=
            new SnesSpritePaletteQuantizationOptions();

        ValidateOptions(
            options);

        using System.Drawing.Bitmap sourceBitmap =
            new(
                sourceImage);

        Dictionary<int, int> histogram =
            BuildHistogram(
                sourceBitmap,
                options);

        int originalUniqueColorCount =
            histogram.Count;

        int visibleColorSlots =
            options.MaximumPaletteColors -
            (options.ReserveIndexZeroForTransparency
                ? 1
                : 0);

        List<System.Drawing.Color> visiblePalette =
            BuildVisiblePalette(
                histogram,
                visibleColorSlots);

        List<System.Drawing.Color> palette =
            BuildFinalPalette(
                visiblePalette,
                options);

        byte[,] indices =
            new byte[
                sourceBitmap.Width,
                sourceBitmap.Height];

        System.Drawing.Bitmap quantizedBitmap =
            new(
                sourceBitmap.Width,
                sourceBitmap.Height,
                System.Drawing.Imaging.PixelFormat.Format32bppArgb);

        double totalError =
            0.0;

        long foregroundPixelCount =
            0;

        if (options.UseDithering)
        {
            QuantizeWithFloydSteinberg(
                sourceBitmap,
                quantizedBitmap,
                indices,
                palette,
                options,
                ref totalError,
                ref foregroundPixelCount);
        }
        else
        {
            QuantizeWithoutDithering(
                sourceBitmap,
                quantizedBitmap,
                indices,
                palette,
                options,
                ref totalError,
                ref foregroundPixelCount);
        }

        double meanError =
            foregroundPixelCount == 0
                ? 0.0
                : totalError /
                  foregroundPixelCount;

        return new SnesSpritePaletteQuantizationResult(
            quantizedBitmap,
            indices,
            palette,
            originalUniqueColorCount,
            meanError);
    }

    private static void ValidateOptions(
        SnesSpritePaletteQuantizationOptions options)
    {
        if (options.MaximumPaletteColors < 2 ||
            options.MaximumPaletteColors > 16)
        {
            throw new ArgumentOutOfRangeException(
                nameof(options.MaximumPaletteColors),
                "La paleta debe contener entre 2 y 16 colores.");
        }

        if (options.BackgroundTolerance < 0 ||
            options.BackgroundTolerance > 255)
        {
            throw new ArgumentOutOfRangeException(
                nameof(options.BackgroundTolerance));
        }

        if (options.AlphaThreshold < 0 ||
            options.AlphaThreshold > 255)
        {
            throw new ArgumentOutOfRangeException(
                nameof(options.AlphaThreshold));
        }

        if (options.SamplingStride < 1 ||
            options.SamplingStride > 16)
        {
            throw new ArgumentOutOfRangeException(
                nameof(options.SamplingStride));
        }
    }

    private static Dictionary<int, int> BuildHistogram(
        System.Drawing.Bitmap bitmap,
        SnesSpritePaletteQuantizationOptions options)
    {
        Dictionary<int, int> histogram =
            new();

        int stride =
            options.SamplingStride;

        for (int y = 0;
             y < bitmap.Height;
             y += stride)
        {
            for (int x = 0;
                 x < bitmap.Width;
                 x += stride)
            {
                System.Drawing.Color pixel =
                    bitmap.GetPixel(
                        x,
                        y);

                if (IsBackgroundPixel(
                        pixel,
                        options))
                {
                    continue;
                }

                int rgb =
                    pixel.R << 16 |
                    pixel.G << 8 |
                    pixel.B;

                histogram.TryGetValue(
                    rgb,
                    out int count);

                histogram[rgb] =
                    count + 1;
            }
        }

        return histogram;
    }

    private static List<System.Drawing.Color> BuildVisiblePalette(
        IReadOnlyDictionary<int, int> histogram,
        int maximumVisibleColors)
    {
        if (maximumVisibleColors <= 0 ||
            histogram.Count == 0)
        {
            return new List<System.Drawing.Color>();
        }

        List<ColorSample> samples =
            histogram
                .Select(
                    pair =>
                        new ColorSample
                        {
                            Red =
                                pair.Key >> 16 &
                                0xFF,

                            Green =
                                pair.Key >> 8 &
                                0xFF,

                            Blue =
                                pair.Key &
                                0xFF,

                            Count =
                                pair.Value
                        })
                .ToList();

        if (samples.Count <=
            maximumVisibleColors)
        {
            return samples
                .OrderByDescending(
                    sample => sample.Count)
                .Select(
                    sample =>
                        System.Drawing.Color.FromArgb(
                            255,
                            sample.Red,
                            sample.Green,
                            sample.Blue))
                .ToList();
        }

        List<ColorBox> boxes =
            new()
            {
                new ColorBox(
                    samples)
            };

        while (boxes.Count <
               maximumVisibleColors)
        {
            ColorBox? boxToSplit =
                boxes
                    .Where(box => box.CanSplit)
                    .OrderByDescending(
                        box => box.SplitPriority)
                    .FirstOrDefault();

            if (boxToSplit is null)
            {
                break;
            }

            (ColorBox first,
             ColorBox second) =
                SplitBox(
                    boxToSplit);

            boxes.Remove(
                boxToSplit);

            boxes.Add(
                first);

            boxes.Add(
                second);
        }

        return boxes
            .Select(
                box => box.GetWeightedAverage())
            .OrderBy(
                color =>
                    GetLuminance(color))
            .ToList();
    }

    private static (ColorBox First, ColorBox Second)
        SplitBox(
            ColorBox box)
    {
        Func<ColorSample, int> selector =
            box.RedRange >=
                box.GreenRange &&
            box.RedRange >=
                box.BlueRange
                ? sample => sample.Red
                : box.GreenRange >=
                  box.BlueRange
                    ? sample => sample.Green
                    : sample => sample.Blue;

        List<ColorSample> ordered =
            box.Samples
                .OrderBy(selector)
                .ThenBy(
                    sample =>
                        sample.PackedRgb)
                .ToList();

        int half =
            Math.Max(
                1,
                box.TotalCount / 2);

        int accumulated =
            0;

        int splitIndex =
            1;

        for (int index = 0;
             index <
             ordered.Count - 1;
             index++)
        {
            accumulated +=
                ordered[index].Count;

            splitIndex =
                index + 1;

            if (accumulated >=
                half)
            {
                break;
            }
        }

        splitIndex =
            Math.Clamp(
                splitIndex,
                1,
                ordered.Count - 1);

        return
        (
            new ColorBox(
                ordered
                    .Take(splitIndex)
                    .ToList()),

            new ColorBox(
                ordered
                    .Skip(splitIndex)
                    .ToList())
        );
    }

    private static List<System.Drawing.Color> BuildFinalPalette(
        IReadOnlyList<System.Drawing.Color> visiblePalette,
        SnesSpritePaletteQuantizationOptions options)
    {
        List<System.Drawing.Color> palette =
            new();

        if (options.ReserveIndexZeroForTransparency)
        {
            palette.Add(
                System.Drawing.Color.FromArgb(
                    0,
                    options.BackgroundColor.R,
                    options.BackgroundColor.G,
                    options.BackgroundColor.B));
        }

        palette.AddRange(
            visiblePalette.Take(
                options.MaximumPaletteColors -
                palette.Count));

        while (palette.Count <
               options.MaximumPaletteColors)
        {
            palette.Add(
                System.Drawing.Color.Black);
        }

        return palette;
    }

    private static void QuantizeWithoutDithering(
        System.Drawing.Bitmap source,
        System.Drawing.Bitmap destination,
        byte[,] indices,
        IReadOnlyList<System.Drawing.Color> palette,
        SnesSpritePaletteQuantizationOptions options,
        ref double totalError,
        ref long foregroundPixelCount)
    {
        int firstVisibleIndex =
            options.ReserveIndexZeroForTransparency
                ? 1
                : 0;

        for (int y = 0;
             y < source.Height;
             y++)
        {
            for (int x = 0;
                 x < source.Width;
                 x++)
            {
                System.Drawing.Color pixel =
                    source.GetPixel(
                        x,
                        y);

                if (IsBackgroundPixel(
                        pixel,
                        options))
                {
                    indices[x, y] =
                        0;

                    destination.SetPixel(
                        x,
                        y,
                        options.ReserveIndexZeroForTransparency
                            ? System.Drawing.Color.Transparent
                            : palette[0]);

                    continue;
                }

                int nearestIndex =
                    FindNearestPaletteIndex(
                        pixel.R,
                        pixel.G,
                        pixel.B,
                        palette,
                        firstVisibleIndex);

                indices[x, y] =
                    (byte)nearestIndex;

                System.Drawing.Color mapped =
                    palette[nearestIndex];

                destination.SetPixel(
                    x,
                    y,
                    System.Drawing.Color.FromArgb(
                        255,
                        mapped.R,
                        mapped.G,
                        mapped.B));

                totalError +=
                    CalculateSquaredDistance(
                        pixel.R,
                        pixel.G,
                        pixel.B,
                        mapped.R,
                        mapped.G,
                        mapped.B);

                foregroundPixelCount++;
            }
        }
    }

    private static void QuantizeWithFloydSteinberg(
        System.Drawing.Bitmap source,
        System.Drawing.Bitmap destination,
        byte[,] indices,
        IReadOnlyList<System.Drawing.Color> palette,
        SnesSpritePaletteQuantizationOptions options,
        ref double totalError,
        ref long foregroundPixelCount)
    {
        int width =
            source.Width;

        int height =
            source.Height;

        double[,,] working =
            new double[
                width,
                height,
                3];

        bool[,] isBackground =
            new bool[
                width,
                height];

        for (int y = 0;
             y < height;
             y++)
        {
            for (int x = 0;
                 x < width;
                 x++)
            {
                System.Drawing.Color pixel =
                    source.GetPixel(
                        x,
                        y);

                isBackground[x, y] =
                    IsBackgroundPixel(
                        pixel,
                        options);

                working[x, y, 0] =
                    pixel.R;

                working[x, y, 1] =
                    pixel.G;

                working[x, y, 2] =
                    pixel.B;
            }
        }

        int firstVisibleIndex =
            options.ReserveIndexZeroForTransparency
                ? 1
                : 0;

        for (int y = 0;
             y < height;
             y++)
        {
            for (int x = 0;
                 x < width;
                 x++)
            {
                if (isBackground[x, y])
                {
                    indices[x, y] =
                        0;

                    destination.SetPixel(
                        x,
                        y,
                        options.ReserveIndexZeroForTransparency
                            ? System.Drawing.Color.Transparent
                            : palette[0]);

                    continue;
                }

                int red =
                    ClampByte(
                        working[x, y, 0]);

                int green =
                    ClampByte(
                        working[x, y, 1]);

                int blue =
                    ClampByte(
                        working[x, y, 2]);

                int nearestIndex =
                    FindNearestPaletteIndex(
                        red,
                        green,
                        blue,
                        palette,
                        firstVisibleIndex);

                indices[x, y] =
                    (byte)nearestIndex;

                System.Drawing.Color mapped =
                    palette[nearestIndex];

                destination.SetPixel(
                    x,
                    y,
                    System.Drawing.Color.FromArgb(
                        255,
                        mapped.R,
                        mapped.G,
                        mapped.B));

                double errorRed =
                    red -
                    mapped.R;

                double errorGreen =
                    green -
                    mapped.G;

                double errorBlue =
                    blue -
                    mapped.B;

                DistributeError(
                    working,
                    isBackground,
                    x + 1,
                    y,
                    errorRed,
                    errorGreen,
                    errorBlue,
                    7.0 / 16.0);

                DistributeError(
                    working,
                    isBackground,
                    x - 1,
                    y + 1,
                    errorRed,
                    errorGreen,
                    errorBlue,
                    3.0 / 16.0);

                DistributeError(
                    working,
                    isBackground,
                    x,
                    y + 1,
                    errorRed,
                    errorGreen,
                    errorBlue,
                    5.0 / 16.0);

                DistributeError(
                    working,
                    isBackground,
                    x + 1,
                    y + 1,
                    errorRed,
                    errorGreen,
                    errorBlue,
                    1.0 / 16.0);

                totalError +=
                    CalculateSquaredDistance(
                        red,
                        green,
                        blue,
                        mapped.R,
                        mapped.G,
                        mapped.B);

                foregroundPixelCount++;
            }
        }
    }

    private static void DistributeError(
        double[,,] working,
        bool[,] isBackground,
        int x,
        int y,
        double errorRed,
        double errorGreen,
        double errorBlue,
        double factor)
    {
        int width =
            working.GetLength(0);

        int height =
            working.GetLength(1);

        if (x < 0 ||
            y < 0 ||
            x >= width ||
            y >= height ||
            isBackground[x, y])
        {
            return;
        }

        working[x, y, 0] +=
            errorRed *
            factor;

        working[x, y, 1] +=
            errorGreen *
            factor;

        working[x, y, 2] +=
            errorBlue *
            factor;
    }

    private static int FindNearestPaletteIndex(
        int red,
        int green,
        int blue,
        IReadOnlyList<System.Drawing.Color> palette,
        int firstIndex)
    {
        int bestIndex =
            Math.Clamp(
                firstIndex,
                0,
                palette.Count - 1);

        double bestDistance =
            double.MaxValue;

        for (int index = firstIndex;
             index < palette.Count;
             index++)
        {
            System.Drawing.Color candidate =
                palette[index];

            double distance =
                CalculateSquaredDistance(
                    red,
                    green,
                    blue,
                    candidate.R,
                    candidate.G,
                    candidate.B);

            if (distance <
                bestDistance)
            {
                bestDistance =
                    distance;

                bestIndex =
                    index;
            }
        }

        return bestIndex;
    }

    private static double CalculateSquaredDistance(
        int red1,
        int green1,
        int blue1,
        int red2,
        int green2,
        int blue2)
    {
        double red =
            red1 -
            red2;

        double green =
            green1 -
            green2;

        double blue =
            blue1 -
            blue2;

        return
            red * red * 0.30 +
            green * green * 0.59 +
            blue * blue * 0.11;
    }

    private static bool IsBackgroundPixel(
        System.Drawing.Color pixel,
        SnesSpritePaletteQuantizationOptions options)
    {
        if (pixel.A <
            options.AlphaThreshold)
        {
            return true;
        }

        if (options.BackgroundColor.A == 0)
        {
            return false;
        }

        return ColorDistance(
                   pixel,
                   options.BackgroundColor) <=
               options.BackgroundTolerance;
    }

    private static int ColorDistance(
        System.Drawing.Color first,
        System.Drawing.Color second)
    {
        int red =
            first.R -
            second.R;

        int green =
            first.G -
            second.G;

        int blue =
            first.B -
            second.B;

        return (int)Math.Sqrt(
            red * red +
            green * green +
            blue * blue);
    }

    private static double GetLuminance(
        System.Drawing.Color color)
    {
        return
            color.R * 0.299 +
            color.G * 0.587 +
            color.B * 0.114;
    }

    private static int ClampByte(
        double value)
    {
        return (int)Math.Clamp(
            Math.Round(value),
            0,
            255);
    }
}
