using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using AladdinSpriteStudio.UI.CharacterReplacement.Models;

namespace AladdinSpriteStudio.UI.CharacterReplacement.Analysis;

/// <summary>
/// Configuración para detectar las poses de una hoja.
/// </summary>
public sealed class SpriteSheetAnalysisOptions
{
    public int BackgroundTolerance { get; set; } =
        18;

    public int AlphaThreshold { get; set; } =
        16;

    public int MinimumPixelCount { get; set; } =
        20;

    public int MinimumWidth { get; set; } =
        3;

    public int MinimumHeight { get; set; } =
        3;

    public int MergeDistance { get; set; } =
        2;

    public int Padding { get; set; } =
        1;
}

/// <summary>
/// Detecta regiones de píxeles diferentes al fondo.
/// </summary>
public static class SpriteSheetAnalyzer
{
    private sealed class DetectedComponent
    {
        public Rectangle Bounds { get; set; }

        public int PixelCount { get; set; }
    }

    public static IReadOnlyList<SpriteFrame> Analyze(
        Bitmap source,
        Color backgroundColor,
        SpriteSheetAnalysisOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(
            source);

        options ??=
            new SpriteSheetAnalysisOptions();

        ValidateOptions(
            options);

        bool[] foreground =
            CreateForegroundMap(
                source,
                backgroundColor,
                options);

        bool[] visited =
            new bool[foreground.Length];

        List<DetectedComponent> components =
            FindComponents(
                foreground,
                visited,
                source.Width,
                source.Height,
                options);

        MergeNearbyComponents(
            components,
            options.MergeDistance,
            source.Width,
            source.Height);

        List<DetectedComponent> orderedComponents =
            components
                .OrderBy(component =>
                    component.Bounds.Top)
                .ThenBy(component =>
                    component.Bounds.Left)
                .ToList();

        List<SpriteFrame> frames =
            new();

        for (int index = 0;
             index < orderedComponents.Count;
             index++)
        {
            DetectedComponent component =
                orderedComponents[index];

            frames.Add(
                new SpriteFrame(
                    index,
                    component.Bounds,
                    component.PixelCount));
        }

        return frames;
    }

    private static bool[] CreateForegroundMap(
        Bitmap source,
        Color backgroundColor,
        SpriteSheetAnalysisOptions options)
    {
        using Bitmap bitmap =
            new(
                source.Width,
                source.Height,
                PixelFormat.Format32bppArgb);

        using (System.Drawing.Graphics graphics =
               System.Drawing.Graphics.FromImage(bitmap))
        {
            graphics.DrawImageUnscaled(
                source,
                0,
                0);
        }

        Rectangle rectangle =
            new(
                0,
                0,
                bitmap.Width,
                bitmap.Height);

        BitmapData bitmapData =
            bitmap.LockBits(
                rectangle,
                ImageLockMode.ReadOnly,
                PixelFormat.Format32bppArgb);

        try
        {
            int stride =
                bitmapData.Stride;

            int absoluteStride =
                Math.Abs(stride);

            byte[] data =
                new byte[
                    absoluteStride *
                    bitmap.Height];

            Marshal.Copy(
                bitmapData.Scan0,
                data,
                0,
                data.Length);

            bool[] foreground =
                new bool[
                    bitmap.Width *
                    bitmap.Height];

            for (int y = 0;
                 y < bitmap.Height;
                 y++)
            {
                int rowOffset =
                    stride >= 0
                        ? y * stride
                        : (bitmap.Height - 1 - y) *
                          absoluteStride;

                for (int x = 0;
                     x < bitmap.Width;
                     x++)
                {
                    int byteOffset =
                        rowOffset +
                        x * 4;

                    int blue =
                        data[byteOffset];

                    int green =
                        data[byteOffset + 1];

                    int red =
                        data[byteOffset + 2];

                    int alpha =
                        data[byteOffset + 3];

                    bool visible =
                        alpha >=
                        options.AlphaThreshold;

                    bool differsFromBackground =
                        Math.Abs(
                            red -
                            backgroundColor.R)
                        >
                        options.BackgroundTolerance
                        ||
                        Math.Abs(
                            green -
                            backgroundColor.G)
                        >
                        options.BackgroundTolerance
                        ||
                        Math.Abs(
                            blue -
                            backgroundColor.B)
                        >
                        options.BackgroundTolerance;

                    foreground[
                        y * bitmap.Width + x] =
                        visible &&
                        differsFromBackground;
                }
            }

            return foreground;
        }
        finally
        {
            bitmap.UnlockBits(
                bitmapData);
        }
    }

    private static List<DetectedComponent>
        FindComponents(
            bool[] foreground,
            bool[] visited,
            int width,
            int height,
            SpriteSheetAnalysisOptions options)
    {
        List<DetectedComponent> components =
            new();

        Queue<int> pending =
            new();

        for (int y = 0;
             y < height;
             y++)
        {
            for (int x = 0;
                 x < width;
                 x++)
            {
                int startIndex =
                    y * width + x;

                if (!foreground[startIndex] ||
                    visited[startIndex])
                {
                    continue;
                }

                visited[startIndex] =
                    true;

                pending.Enqueue(
                    startIndex);

                int minimumX = x;
                int maximumX = x;
                int minimumY = y;
                int maximumY = y;
                int pixelCount = 0;

                while (pending.Count > 0)
                {
                    int currentIndex =
                        pending.Dequeue();

                    int currentX =
                        currentIndex % width;

                    int currentY =
                        currentIndex / width;

                    pixelCount++;

                    minimumX =
                        Math.Min(
                            minimumX,
                            currentX);

                    maximumX =
                        Math.Max(
                            maximumX,
                            currentX);

                    minimumY =
                        Math.Min(
                            minimumY,
                            currentY);

                    maximumY =
                        Math.Max(
                            maximumY,
                            currentY);

                    for (int deltaY = -1;
                         deltaY <= 1;
                         deltaY++)
                    {
                        for (int deltaX = -1;
                             deltaX <= 1;
                             deltaX++)
                        {
                            if (deltaX == 0 &&
                                deltaY == 0)
                            {
                                continue;
                            }

                            int neighborX =
                                currentX +
                                deltaX;

                            int neighborY =
                                currentY +
                                deltaY;

                            if (neighborX < 0 ||
                                neighborX >= width ||
                                neighborY < 0 ||
                                neighborY >= height)
                            {
                                continue;
                            }

                            int neighborIndex =
                                neighborY *
                                width +
                                neighborX;

                            if (!foreground[
                                    neighborIndex] ||
                                visited[
                                    neighborIndex])
                            {
                                continue;
                            }

                            visited[neighborIndex] =
                                true;

                            pending.Enqueue(
                                neighborIndex);
                        }
                    }
                }

                int componentWidth =
                    maximumX -
                    minimumX +
                    1;

                int componentHeight =
                    maximumY -
                    minimumY +
                    1;

                if (pixelCount <
                        options.MinimumPixelCount ||
                    componentWidth <
                        options.MinimumWidth ||
                    componentHeight <
                        options.MinimumHeight)
                {
                    continue;
                }

                Rectangle bounds =
                    ExpandRectangle(
                        Rectangle.FromLTRB(
                            minimumX,
                            minimumY,
                            maximumX + 1,
                            maximumY + 1),
                        options.Padding,
                        width,
                        height);

                components.Add(
                    new DetectedComponent
                    {
                        Bounds =
                            bounds,

                        PixelCount =
                            pixelCount
                    });
            }
        }

        return components;
    }

    private static void MergeNearbyComponents(
        List<DetectedComponent> components,
        int mergeDistance,
        int imageWidth,
        int imageHeight)
    {
        if (mergeDistance <= 0)
        {
            return;
        }

        bool changed;

        do
        {
            changed = false;

            for (int firstIndex = 0;
                 firstIndex < components.Count;
                 firstIndex++)
            {
                for (int secondIndex =
                         firstIndex + 1;

                     secondIndex <
                         components.Count;

                     secondIndex++)
                {
                    Rectangle expandedFirst =
                        ExpandRectangle(
                            components[firstIndex]
                                .Bounds,
                            mergeDistance,
                            imageWidth,
                            imageHeight);

                    if (!expandedFirst.IntersectsWith(
                            components[secondIndex]
                                .Bounds))
                    {
                        continue;
                    }

                    components[firstIndex].Bounds =
                        Rectangle.Union(
                            components[firstIndex]
                                .Bounds,
                            components[secondIndex]
                                .Bounds);

                    components[firstIndex].PixelCount +=
                        components[secondIndex]
                            .PixelCount;

                    components.RemoveAt(
                        secondIndex);

                    changed = true;

                    break;
                }

                if (changed)
                {
                    break;
                }
            }
        }
        while (changed);
    }

    private static Rectangle ExpandRectangle(
        Rectangle rectangle,
        int amount,
        int imageWidth,
        int imageHeight)
    {
        if (amount <= 0)
        {
            return rectangle;
        }

        int left =
            Math.Max(
                0,
                rectangle.Left -
                amount);

        int top =
            Math.Max(
                0,
                rectangle.Top -
                amount);

        int right =
            Math.Min(
                imageWidth,
                rectangle.Right +
                amount);

        int bottom =
            Math.Min(
                imageHeight,
                rectangle.Bottom +
                amount);

        return Rectangle.FromLTRB(
            left,
            top,
            right,
            bottom);
    }

    private static void ValidateOptions(
        SpriteSheetAnalysisOptions options)
    {
        if (options.BackgroundTolerance
            is < 0 or > 255)
        {
            throw new ArgumentOutOfRangeException(
                nameof(
                    options.BackgroundTolerance));
        }

        if (options.AlphaThreshold
            is < 0 or > 255)
        {
            throw new ArgumentOutOfRangeException(
                nameof(
                    options.AlphaThreshold));
        }

        if (options.MinimumPixelCount < 1 ||
            options.MinimumWidth < 1 ||
            options.MinimumHeight < 1 ||
            options.MergeDistance < 0 ||
            options.Padding < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(options),
                "Las opciones de detección no son válidas.");
        }
    }
}