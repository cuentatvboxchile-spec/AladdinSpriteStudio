namespace AladdinSpriteStudio.UI.Graphics;

/// <summary>
/// Resultado de importar una página de tiles
/// dentro de una copia de la ROM.
/// </summary>
public sealed class TilePageImportResult
{
    public TilePageImportResult(
        int tileCount,
        int startOffset,
        int finalOffset,
        string outputRomPath)
    {
        TileCount = tileCount;
        StartOffset = startOffset;
        FinalOffset = finalOffset;
        OutputRomPath = outputRomPath;
    }

    public int TileCount { get; }

    public int StartOffset { get; }

    public int FinalOffset { get; }

    public string OutputRomPath { get; }
}

/// <summary>
/// Convierte una página PNG de 128 × 128 píxeles
/// a tiles SNES 4BPP y la inserta en una copia de la ROM.
/// </summary>
public static class TilePageImporter
{
    private const int TileWidth = 8;
    private const int TileHeight = 8;

    public const int TilesPerRow = 16;
    public const int TilesPerColumn = 16;

    public const int TileCount =
        TilesPerRow * TilesPerColumn;

    public const int PageWidth =
        TilesPerRow * TileWidth;

    public const int PageHeight =
        TilesPerColumn * TileHeight;

    public const int PageByteCount =
        TileCount *
        Encoder4Bpp.BytesPerTile;

    /// <summary>
    /// Convierte una imagen PNG de 128 × 128
    /// a 8192 bytes de gráficos SNES 4BPP.
    /// </summary>
    public static byte[] ConvertPngTo4Bpp(
        string pngFilePath,
        Color[] palette)
    {
        if (string.IsNullOrWhiteSpace(
                pngFilePath))
        {
            throw new ArgumentException(
                "Debe indicar la ruta del archivo PNG.",
                nameof(pngFilePath));
        }

        if (!File.Exists(pngFilePath))
        {
            throw new FileNotFoundException(
                "No se encontró la imagen PNG.",
                pngFilePath);
        }

        ArgumentNullException.ThrowIfNull(
            palette);

        if (palette.Length !=
            SnesPalette.ColorsPerPalette)
        {
            throw new ArgumentException(
                "La paleta debe contener exactamente 16 colores.",
                nameof(palette));
        }

        using Bitmap bitmap =
            new(pngFilePath);

        if (bitmap.Width != PageWidth ||
            bitmap.Height != PageHeight)
        {
            throw new InvalidDataException(
                $"La imagen debe medir exactamente " +
                $"{PageWidth} × {PageHeight} píxeles.\n\n" +
                $"Tamaño encontrado: " +
                $"{bitmap.Width} × {bitmap.Height}.");
        }

        Dictionary<int, byte> paletteIndexes =
            CreatePaletteIndexMap(
                palette);

        byte[] pageData =
            new byte[PageByteCount];

        for (int tileIndex = 0;
             tileIndex < TileCount;
             tileIndex++)
        {
            int tileColumn =
                tileIndex % TilesPerRow;

            int tileRow =
                tileIndex / TilesPerRow;

            int tileX =
                tileColumn * TileWidth;

            int tileY =
                tileRow * TileHeight;

            byte[,] pixels =
                ReadTilePixels(
                    bitmap,
                    tileX,
                    tileY,
                    paletteIndexes);

            byte[] encodedTile =
                Encoder4Bpp.EncodeTile(
                    pixels);

            int destinationOffset =
                tileIndex *
                Encoder4Bpp.BytesPerTile;

            Array.Copy(
                encodedTile,
                0,
                pageData,
                destinationOffset,
                encodedTile.Length);
        }

        return pageData;
    }

    /// <summary>
    /// Inserta la página convertida dentro de una copia
    /// de los datos completos de la ROM.
    /// </summary>
    public static TilePageImportResult
        ImportPageToRomCopy(
            byte[] romData,
            int startOffset,
            string pngFilePath,
            Color[] palette,
            string outputRomPath)
    {
        ArgumentNullException.ThrowIfNull(
            romData);

        if (string.IsNullOrWhiteSpace(
                outputRomPath))
        {
            throw new ArgumentException(
                "Debe indicar dónde guardar la ROM modificada.",
                nameof(outputRomPath));
        }

        if (startOffset < 0 ||
            startOffset %
            Encoder4Bpp.BytesPerTile != 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(startOffset),
                "El offset inicial debe estar alineado " +
                "a bloques de 32 bytes.");
        }

        byte[] pageData =
            ConvertPngTo4Bpp(
                pngFilePath,
                palette);

        if (startOffset >
            romData.Length - pageData.Length)
        {
            throw new ArgumentOutOfRangeException(
                nameof(startOffset),
                "La página no cabe dentro de la ROM " +
                "desde el offset indicado.");
        }

        byte[] modifiedRom =
            (byte[])romData.Clone();

        Array.Copy(
            pageData,
            0,
            modifiedRom,
            startOffset,
            pageData.Length);

        string? outputDirectory =
            Path.GetDirectoryName(
                outputRomPath);

        if (!string.IsNullOrWhiteSpace(
                outputDirectory))
        {
            Directory.CreateDirectory(
                outputDirectory);
        }

        File.WriteAllBytes(
            outputRomPath,
            modifiedRom);

        int finalOffset =
            startOffset +
            pageData.Length -
            1;

        return new TilePageImportResult(
            TileCount,
            startOffset,
            finalOffset,
            outputRomPath);
    }

    private static byte[,] ReadTilePixels(
        Bitmap bitmap,
        int tileX,
        int tileY,
        Dictionary<int, byte> paletteIndexes)
    {
        byte[,] pixels =
            new byte[
                TileHeight,
                TileWidth];

        for (int row = 0;
             row < TileHeight;
             row++)
        {
            for (int column = 0;
                 column < TileWidth;
                 column++)
            {
                int imageX =
                    tileX + column;

                int imageY =
                    tileY + row;

                Color pixelColor =
                    bitmap.GetPixel(
                        imageX,
                        imageY);

                if (pixelColor.A < 128)
                {
                    pixels[row, column] = 0;
                    continue;
                }

                int opaqueArgb =
                    Color.FromArgb(
                        255,
                        pixelColor.R,
                        pixelColor.G,
                        pixelColor.B)
                    .ToArgb();

                if (!paletteIndexes.TryGetValue(
                        opaqueArgb,
                        out byte colorIndex))
                {
                    throw new InvalidDataException(
                        $"La imagen contiene un color que no " +
                        $"pertenece a la paleta.\n\n" +
                        $"Posición: X={imageX}, Y={imageY}\n" +
                        $"Color: " +
                        $"#{pixelColor.R:X2}" +
                        $"{pixelColor.G:X2}" +
                        $"{pixelColor.B:X2}\n\n" +
                        "Utiliza solamente los 16 colores " +
                        "de la paleta importada.");
                }

                pixels[row, column] =
                    colorIndex;
            }
        }

        return pixels;
    }

    private static Dictionary<int, byte>
        CreatePaletteIndexMap(
            Color[] palette)
    {
        Dictionary<int, byte> result =
            new();

        for (byte index = 0;
             index < palette.Length;
             index++)
        {
            Color color =
                palette[index];

            int opaqueArgb =
                Color.FromArgb(
                    255,
                    color.R,
                    color.G,
                    color.B)
                .ToArgb();

            // Si hay colores duplicados,
            // conserva el primer índice.
            if (!result.ContainsKey(
                    opaqueArgb))
            {
                result.Add(
                    opaqueArgb,
                    index);
            }
        }

        return result;
    }
}