using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using AladdinSpriteStudio.UI.CharacterReplacement.Models;

namespace AladdinSpriteStudio.UI.CharacterReplacement.Rendering;

/// <summary>
/// Genera la hoja convertida colocando las poses asignadas sobre las
/// regiones originales de Aladdin. Elimina fondos de varios tonos y
/// recorta el contenido real antes de escalarlo.
/// </summary>
public static class MappedSpriteSheetRenderer
{
    private const int AlphaThreshold = 16;
    private const int BorderColorTolerance = 48;
    private const int DocumentBackgroundTolerance = 28;
    private const int MaximumBorderPaletteColors = 10;

    public static Bitmap Render(
        SpriteSheetDocument targetDocument,
        IEnumerable<FrameMapping> mappings,
        bool preserveUnmappedTargetFrames = true)
    {
        ArgumentNullException.ThrowIfNull(targetDocument);
        ArgumentNullException.ThrowIfNull(mappings);

        Bitmap result = new(
            targetDocument.Width,
            targetDocument.Height,
            PixelFormat.Format32bppArgb);

        using (System.Drawing.Graphics graphics =
               System.Drawing.Graphics.FromImage(result))
        {
            graphics.CompositingMode =
                CompositingMode.SourceCopy;

            if (preserveUnmappedTargetFrames)
            {
                graphics.DrawImageUnscaled(
                    targetDocument.Image,
                    0,
                    0);
            }
            else
            {
                graphics.Clear(
                    targetDocument.BackgroundColor);
            }
        }

        foreach (FrameMapping mapping in mappings
                     .OrderBy(item => item.TargetFrame.Index))
        {
            if (!ReferenceEquals(
                    mapping.TargetDocument,
                    targetDocument))
            {
                continue;
            }

            DrawMapping(
                result,
                mapping);
        }

        return result;
    }

    private static void DrawMapping(
        Bitmap destinationSheet,
        FrameMapping mapping)
    {
        Rectangle targetBounds =
            Rectangle.Intersect(
                new Rectangle(
                    Point.Empty,
                    destinationSheet.Size),
                mapping.TargetFrame.Bounds);

        if (targetBounds.Width <= 0 ||
            targetBounds.Height <= 0)
        {
            return;
        }

        using Bitmap extractedPose =
            ExtractPoseWithTransparentBackground(
                mapping.SourceDocument,
                mapping.SourceFrame);

        using Bitmap sourcePose =
            TrimTransparentBorders(
                extractedPose);

        if (sourcePose.Width <= 1 &&
            sourcePose.Height <= 1 &&
            IsCompletelyTransparent(sourcePose))
        {
            return;
        }

        ApplyFlip(
            sourcePose,
            mapping.FlipHorizontal,
            mapping.FlipVertical);

        Rectangle relativeDestination =
            CalculateDestinationBounds(
                mapping,
                sourcePose.Size,
                targetBounds.Size);

        Rectangle absoluteDestination = new(
            targetBounds.X + relativeDestination.X,
            targetBounds.Y + relativeDestination.Y,
            relativeDestination.Width,
            relativeDestination.Height);

        if (absoluteDestination.Width <= 0 ||
            absoluteDestination.Height <= 0)
        {
            return;
        }

        using System.Drawing.Graphics graphics =
            System.Drawing.Graphics.FromImage(
                destinationSheet);

        graphics.CompositingMode =
            CompositingMode.SourceCopy;

        using SolidBrush backgroundBrush =
            new(mapping.TargetDocument.BackgroundColor);

        graphics.FillRectangle(
            backgroundBrush,
            targetBounds);

        graphics.CompositingMode =
            CompositingMode.SourceOver;

        graphics.CompositingQuality =
            CompositingQuality.HighSpeed;

        graphics.InterpolationMode =
            InterpolationMode.NearestNeighbor;

        graphics.PixelOffsetMode =
            PixelOffsetMode.Half;

        graphics.SmoothingMode =
            SmoothingMode.None;

        GraphicsState state =
            graphics.Save();

        try
        {
            graphics.SetClip(
                targetBounds);

            graphics.DrawImage(
                sourcePose,
                absoluteDestination,
                new Rectangle(
                    Point.Empty,
                    sourcePose.Size),
                GraphicsUnit.Pixel);
        }
        finally
        {
            graphics.Restore(
                state);
        }
    }

    private static Rectangle CalculateDestinationBounds(
        FrameMapping mapping,
        Size sourceSize,
        Size targetSize)
    {
        float scale =
            mapping.AutoFit
                ? Math.Min(
                    targetSize.Width /
                    (float)Math.Max(1, sourceSize.Width),
                    targetSize.Height /
                    (float)Math.Max(1, sourceSize.Height))
                : Math.Max(0.01f, mapping.Scale);

        int width =
            Math.Max(
                1,
                (int)Math.Round(
                    sourceSize.Width * scale));

        int height =
            Math.Max(
                1,
                (int)Math.Round(
                    sourceSize.Height * scale));

        int x =
            (targetSize.Width - width) / 2 +
            mapping.OffsetX;

        int y =
            targetSize.Height - height +
            mapping.OffsetY;

        return new Rectangle(
            x,
            y,
            width,
            height);
    }

    private static Bitmap ExtractPoseWithTransparentBackground(
        SpriteSheetDocument document,
        SpriteFrame frame)
    {
        Rectangle imageBounds = new(
            Point.Empty,
            document.Image.Size);

        Rectangle clippedBounds =
            Rectangle.Intersect(
                imageBounds,
                frame.Bounds);

        Bitmap transparentPose = new(
            Math.Max(1, clippedBounds.Width),
            Math.Max(1, clippedBounds.Height),
            PixelFormat.Format32bppArgb);

        if (clippedBounds.Width <= 0 ||
            clippedBounds.Height <= 0)
        {
            return transparentPose;
        }

        using Bitmap sourceImage =
            new(document.Image);

        Color[,] pixels =
            new Color[
                clippedBounds.Width,
                clippedBounds.Height];

        for (int y = 0;
             y < clippedBounds.Height;
             y++)
        {
            for (int x = 0;
                 x < clippedBounds.Width;
                 x++)
            {
                pixels[x, y] =
                    sourceImage.GetPixel(
                        clippedBounds.X + x,
                        clippedBounds.Y + y);
            }
        }

        IReadOnlyList<Color> borderPalette =
            BuildBorderPalette(
                pixels,
                document.BackgroundColor);

        bool[,] removableBackground =
            FloodFillBackground(
                pixels,
                borderPalette,
                document.BackgroundColor);

        for (int y = 0;
             y < clippedBounds.Height;
             y++)
        {
            for (int x = 0;
                 x < clippedBounds.Width;
                 x++)
            {
                Color pixel =
                    pixels[x, y];

                transparentPose.SetPixel(
                    x,
                    y,
                    removableBackground[x, y] ||
                    pixel.A <= AlphaThreshold
                        ? Color.Transparent
                        : pixel);
            }
        }

        return transparentPose;
    }

    private static IReadOnlyList<Color> BuildBorderPalette(
        Color[,] pixels,
        Color documentBackground)
    {
        int width = pixels.GetLength(0);
        int height = pixels.GetLength(1);

        Dictionary<int, int> counts = new();

        void Add(Color color)
        {
            if (color.A <= AlphaThreshold)
            {
                return;
            }

            int key = color.ToArgb();

            counts.TryGetValue(
                key,
                out int count);

            counts[key] = count + 1;
        }

        for (int x = 0;
             x < width;
             x++)
        {
            Add(pixels[x, 0]);

            if (height > 1)
            {
                Add(pixels[x, height - 1]);
            }
        }

        for (int y = 1;
             y < height - 1;
             y++)
        {
            Add(pixels[0, y]);

            if (width > 1)
            {
                Add(pixels[width - 1, y]);
            }
        }

        List<Color> palette =
            counts
                .OrderByDescending(pair => pair.Value)
                .Take(MaximumBorderPaletteColors)
                .Select(pair => Color.FromArgb(pair.Key))
                .ToList();

        if (palette.All(color =>
                !ColorsAreClose(
                    color,
                    documentBackground,
                    8)))
        {
            palette.Add(
                documentBackground);
        }

        return palette;
    }

    private static bool[,] FloodFillBackground(
        Color[,] pixels,
        IReadOnlyList<Color> borderPalette,
        Color documentBackground)
    {
        int width = pixels.GetLength(0);
        int height = pixels.GetLength(1);

        bool[,] background =
            new bool[width, height];

        Queue<Point> pending =
            new();

        void TryAdd(int x, int y)
        {
            if (x < 0 ||
                y < 0 ||
                x >= width ||
                y >= height ||
                background[x, y])
            {
                return;
            }

            Color pixel =
                pixels[x, y];

            if (!IsBackgroundCandidate(
                    pixel,
                    borderPalette,
                    documentBackground))
            {
                return;
            }

            background[x, y] = true;
            pending.Enqueue(new Point(x, y));
        }

        for (int x = 0;
             x < width;
             x++)
        {
            TryAdd(x, 0);
            TryAdd(x, height - 1);
        }

        for (int y = 0;
             y < height;
             y++)
        {
            TryAdd(0, y);
            TryAdd(width - 1, y);
        }

        while (pending.Count > 0)
        {
            Point point =
                pending.Dequeue();

            for (int offsetY = -1;
                 offsetY <= 1;
                 offsetY++)
            {
                for (int offsetX = -1;
                     offsetX <= 1;
                     offsetX++)
                {
                    if (offsetX == 0 &&
                        offsetY == 0)
                    {
                        continue;
                    }

                    TryAdd(
                        point.X + offsetX,
                        point.Y + offsetY);
                }
            }
        }

        return background;
    }

    private static bool IsBackgroundCandidate(
        Color pixel,
        IReadOnlyList<Color> borderPalette,
        Color documentBackground)
    {
        if (pixel.A <= AlphaThreshold)
        {
            return true;
        }

        if (ColorsAreClose(
                pixel,
                documentBackground,
                DocumentBackgroundTolerance))
        {
            return true;
        }

        foreach (Color background in borderPalette)
        {
            if (ColorsAreClose(
                    pixel,
                    background,
                    BorderColorTolerance))
            {
                return true;
            }
        }

        return BelongsToBackgroundColorFamily(
            pixel,
            documentBackground);
    }

    private static bool BelongsToBackgroundColorFamily(
        Color pixel,
        Color background)
    {
        RgbToHsv(
            pixel,
            out double pixelHue,
            out double pixelSaturation,
            out double pixelValue);

        RgbToHsv(
            background,
            out double backgroundHue,
            out double backgroundSaturation,
            out double backgroundValue);

        if (backgroundSaturation < 0.40 ||
            pixelSaturation < 0.42 ||
            pixelValue < 0.06)
        {
            return false;
        }

        double hueDifference =
            Math.Abs(
                pixelHue - backgroundHue);

        hueDifference =
            Math.Min(
                hueDifference,
                360.0 - hueDifference);

        return hueDifference <= 15.0 &&
               Math.Abs(
                   pixelValue - backgroundValue) <= 0.70;
    }

    private static void RgbToHsv(
        Color color,
        out double hue,
        out double saturation,
        out double value)
    {
        double red = color.R / 255.0;
        double green = color.G / 255.0;
        double blue = color.B / 255.0;

        double maximum =
            Math.Max(
                red,
                Math.Max(green, blue));

        double minimum =
            Math.Min(
                red,
                Math.Min(green, blue));

        double delta =
            maximum - minimum;

        value = maximum;
        saturation =
            maximum <= 0.0
                ? 0.0
                : delta / maximum;

        if (delta <= 0.000001)
        {
            hue = 0.0;
            return;
        }

        if (maximum == red)
        {
            hue =
                60.0 *
                (((green - blue) / delta) % 6.0);
        }
        else if (maximum == green)
        {
            hue =
                60.0 *
                (((blue - red) / delta) + 2.0);
        }
        else
        {
            hue =
                60.0 *
                (((red - green) / delta) + 4.0);
        }

        if (hue < 0.0)
        {
            hue += 360.0;
        }
    }

    private static Bitmap TrimTransparentBorders(
        Bitmap source)
    {
        Rectangle opaqueBounds =
            FindOpaqueBounds(source);

        if (opaqueBounds.Width <= 0 ||
            opaqueBounds.Height <= 0)
        {
            return new Bitmap(
                1,
                1,
                PixelFormat.Format32bppArgb);
        }

        Bitmap trimmed = new(
            opaqueBounds.Width,
            opaqueBounds.Height,
            PixelFormat.Format32bppArgb);

        using System.Drawing.Graphics graphics =
            System.Drawing.Graphics.FromImage(trimmed);

        graphics.CompositingMode =
            CompositingMode.SourceCopy;

        graphics.DrawImage(
            source,
            new Rectangle(
                0,
                0,
                trimmed.Width,
                trimmed.Height),
            opaqueBounds,
            GraphicsUnit.Pixel);

        return trimmed;
    }

    private static Rectangle FindOpaqueBounds(
        Bitmap image)
    {
        int minimumX = image.Width;
        int minimumY = image.Height;
        int maximumX = -1;
        int maximumY = -1;

        for (int y = 0;
             y < image.Height;
             y++)
        {
            for (int x = 0;
                 x < image.Width;
                 x++)
            {
                if (image.GetPixel(x, y).A <=
                    AlphaThreshold)
                {
                    continue;
                }

                minimumX = Math.Min(minimumX, x);
                minimumY = Math.Min(minimumY, y);
                maximumX = Math.Max(maximumX, x);
                maximumY = Math.Max(maximumY, y);
            }
        }

        if (maximumX < minimumX ||
            maximumY < minimumY)
        {
            return Rectangle.Empty;
        }

        return Rectangle.FromLTRB(
            minimumX,
            minimumY,
            maximumX + 1,
            maximumY + 1);
    }

    private static bool IsCompletelyTransparent(
        Bitmap image)
    {
        for (int y = 0;
             y < image.Height;
             y++)
        {
            for (int x = 0;
                 x < image.Width;
                 x++)
            {
                if (image.GetPixel(x, y).A >
                    AlphaThreshold)
                {
                    return false;
                }
            }
        }

        return true;
    }

    private static bool ColorsAreClose(
        Color first,
        Color second,
        int tolerance)
    {
        if (second.A == 0)
        {
            return first.A <= AlphaThreshold;
        }

        int redDifference =
            first.R - second.R;

        int greenDifference =
            first.G - second.G;

        int blueDifference =
            first.B - second.B;

        int distanceSquared =
            redDifference * redDifference +
            greenDifference * greenDifference +
            blueDifference * blueDifference;

        return distanceSquared <=
               tolerance * tolerance;
    }

    private static void ApplyFlip(
        Bitmap image,
        bool flipHorizontal,
        bool flipVertical)
    {
        if (flipHorizontal && flipVertical)
        {
            image.RotateFlip(
                RotateFlipType.RotateNoneFlipXY);
        }
        else if (flipHorizontal)
        {
            image.RotateFlip(
                RotateFlipType.RotateNoneFlipX);
        }
        else if (flipVertical)
        {
            image.RotateFlip(
                RotateFlipType.RotateNoneFlipY);
        }
    }
}
