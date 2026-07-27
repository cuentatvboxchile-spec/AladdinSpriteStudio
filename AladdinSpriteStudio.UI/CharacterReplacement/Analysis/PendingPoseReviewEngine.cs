using AladdinSpriteStudio.UI.CharacterReplacement.Models;

namespace AladdinSpriteStudio.UI.CharacterReplacement.Analysis;

/// <summary>
/// Configuración del buscador de candidatos para poses pendientes.
/// El análisis solo propone opciones; el usuario conserva el control final.
/// </summary>
public sealed class RomPendingPoseReviewOptions
{
    public int NormalizedGridSize { get; init; } = 16;

    public int TopCandidateCount { get; init; } = 5;

    public bool AllowHorizontalFlip { get; init; } = true;

    public int BackgroundTolerance { get; init; } = 34;

    public int AlphaThreshold { get; init; } = 16;

    public int MinimumSourceContentWidth { get; init; } = 10;

    public int MinimumSourceContentHeight { get; init; } = 14;

    public int MinimumSourceForegroundPixels { get; init; } = 80;
}

/// <summary>
/// Una pose fuente propuesta para una pose pendiente de Aladdin.
/// </summary>
public sealed class RomPendingPoseCandidate
{
    public required SpriteSheetDocument SourceDocument { get; init; }

    public required SpriteFrame SourceFrame { get; init; }

    public required double Score { get; init; }

    public required bool FlipHorizontal { get; init; }

    public string ConfidenceText =>
        Score >= 0.82
            ? "Alta"
            : Score >= 0.70
                ? "Media"
                : "Baja";

    public override string ToString()
    {
        string flipText =
            FlipHorizontal
                ? " | volteo horizontal"
                : string.Empty;

        return
            $"{SourceDocument.FileName} | " +
            $"Pose {SourceFrame.Index + 1:000} | " +
            $"{SourceFrame.Bounds.Width} × " +
            $"{SourceFrame.Bounds.Height} px | " +
            $"{Score:P0} ({ConfidenceText})" +
            flipText;
    }
}

/// <summary>
/// Una pose de Aladdin pendiente junto con sus mejores candidatos.
/// </summary>
public sealed class RomPendingPoseReviewItem
{
    public required SpriteSheetDocument TargetDocument { get; init; }

    public required SpriteFrame TargetFrame { get; init; }

    public required IReadOnlyList<RomPendingPoseCandidate> Candidates
    {
        get;
        init;
    }
}

/// <summary>
/// Compara las poses todavía no asignadas con las hojas fuente cargadas.
/// Conserva el enfoque seguro para ROM: no crea poses, no cambia el orden
/// de Aladdin y solo permite sugerir volteo horizontal.
/// </summary>
public static class RomPendingPoseReviewEngine
{
    private sealed class PoseFeatures
    {
        public required SpriteSheetDocument Document { get; init; }

        public required SpriteFrame Frame { get; init; }

        public required bool[,] Mask { get; init; }

        public required double[] HorizontalProfile { get; init; }

        public required double[] VerticalProfile { get; init; }

        public required double AspectRatio { get; init; }

        public required double FillRatio { get; init; }

        public required double CenterX { get; init; }

        public required double CenterY { get; init; }

        public required int ForegroundPixelCount { get; init; }

        public required int ContentWidth { get; init; }

        public required int ContentHeight { get; init; }
    }

    public static IReadOnlyList<RomPendingPoseReviewItem> Build(
        CharacterReplacementProject project,
        SpriteSheetDocument targetDocument,
        IReadOnlyList<SpriteSheetDocument> sourceDocuments,
        RomPendingPoseReviewOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(project);
        ArgumentNullException.ThrowIfNull(targetDocument);
        ArgumentNullException.ThrowIfNull(sourceDocuments);

        options ??=
            new RomPendingPoseReviewOptions();

        ValidateOptions(options);

        if (sourceDocuments.Count == 0)
        {
            return Array.Empty<RomPendingPoseReviewItem>();
        }

        List<SpriteFrame> pendingFrames =
            targetDocument.Frames
                .Where(frame =>
                    frame.IsEnabled &&
                    project.GetMapping(frame) is null)
                .OrderBy(frame => frame.Index)
                .ToList();

        if (pendingFrames.Count == 0)
        {
            return Array.Empty<RomPendingPoseReviewItem>();
        }

        using System.Drawing.Bitmap targetBitmap =
            new(targetDocument.Image);

        Dictionary<SpriteSheetDocument, System.Drawing.Bitmap>
            sourceBitmaps =
                new();

        try
        {
            foreach (SpriteSheetDocument sourceDocument
                     in sourceDocuments.Distinct())
            {
                sourceBitmaps[sourceDocument] =
                    new System.Drawing.Bitmap(
                        sourceDocument.Image);
            }

            List<PoseFeatures> sourceFeatures =
                new();

            foreach (SpriteSheetDocument sourceDocument
                     in sourceDocuments)
            {
                System.Drawing.Bitmap sourceBitmap =
                    sourceBitmaps[sourceDocument];

                foreach (SpriteFrame sourceFrame
                         in sourceDocument.Frames
                             .Where(frame => frame.IsEnabled))
                {
                    PoseFeatures features =
                        ExtractFeatures(
                            sourceBitmap,
                            sourceDocument,
                            sourceFrame,
                            options);

                    if (IsLikelyCharacterPose(
                            features,
                            options))
                    {
                        sourceFeatures.Add(features);
                    }
                }
            }

            if (sourceFeatures.Count == 0)
            {
                return Array.Empty<RomPendingPoseReviewItem>();
            }

            List<RomPendingPoseReviewItem> result =
                new();

            foreach (SpriteFrame targetFrame
                     in pendingFrames)
            {
                PoseFeatures targetFeatures =
                    ExtractFeatures(
                        targetBitmap,
                        targetDocument,
                        targetFrame,
                        options);

                List<RomPendingPoseCandidate> candidates =
                    FindBestCandidates(
                        targetFeatures,
                        sourceFeatures,
                        options);

                if (candidates.Count == 0)
                {
                    continue;
                }

                result.Add(
                    new RomPendingPoseReviewItem
                    {
                        TargetDocument =
                            targetDocument,

                        TargetFrame =
                            targetFrame,

                        Candidates =
                            candidates
                    });
            }

            return result;
        }
        finally
        {
            foreach (System.Drawing.Bitmap bitmap
                     in sourceBitmaps.Values)
            {
                bitmap.Dispose();
            }
        }
    }

    private static List<RomPendingPoseCandidate> FindBestCandidates(
        PoseFeatures target,
        IReadOnlyList<PoseFeatures> sources,
        RomPendingPoseReviewOptions options)
    {
        List<RomPendingPoseCandidate> candidates =
            new();

        foreach (PoseFeatures source
                 in sources)
        {
            double normalScore =
                CompareFeatures(
                    target,
                    source,
                    flipHorizontal: false);

            double selectedScore =
                normalScore;

            bool selectedFlip =
                false;

            if (options.AllowHorizontalFlip)
            {
                double flippedScore =
                    CompareFeatures(
                        target,
                        source,
                        flipHorizontal: true);

                if (flippedScore >
                    selectedScore)
                {
                    selectedScore =
                        flippedScore;

                    selectedFlip =
                        true;
                }
            }

            candidates.Add(
                new RomPendingPoseCandidate
                {
                    SourceDocument =
                        source.Document,

                    SourceFrame =
                        source.Frame,

                    Score =
                        selectedScore,

                    FlipHorizontal =
                        selectedFlip
                });
        }

        return candidates
            .OrderByDescending(candidate => candidate.Score)
            .ThenBy(candidate => candidate.SourceDocument.FileName)
            .ThenBy(candidate => candidate.SourceFrame.Index)
            .Take(options.TopCandidateCount)
            .ToList();
    }

    private static bool IsLikelyCharacterPose(
        PoseFeatures features,
        RomPendingPoseReviewOptions options)
    {
        return
            features.ContentWidth >=
                options.MinimumSourceContentWidth &&
            features.ContentHeight >=
                options.MinimumSourceContentHeight &&
            features.ForegroundPixelCount >=
                options.MinimumSourceForegroundPixels;
    }

    private static void ValidateOptions(
        RomPendingPoseReviewOptions options)
    {
        if (options.NormalizedGridSize < 8 ||
            options.NormalizedGridSize > 64)
        {
            throw new ArgumentOutOfRangeException(
                nameof(options.NormalizedGridSize));
        }

        if (options.TopCandidateCount < 1 ||
            options.TopCandidateCount > 20)
        {
            throw new ArgumentOutOfRangeException(
                nameof(options.TopCandidateCount));
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
    }

    private static PoseFeatures ExtractFeatures(
        System.Drawing.Bitmap bitmap,
        SpriteSheetDocument document,
        SpriteFrame frame,
        RomPendingPoseReviewOptions options)
    {
        System.Drawing.Rectangle imageBounds =
            new(
                System.Drawing.Point.Empty,
                bitmap.Size);

        System.Drawing.Rectangle bounds =
            System.Drawing.Rectangle.Intersect(
                imageBounds,
                frame.Bounds);

        int gridSize =
            options.NormalizedGridSize;

        if (bounds.Width <= 0 ||
            bounds.Height <= 0)
        {
            return CreateEmptyFeatures(
                document,
                frame,
                gridSize);
        }

        System.Drawing.Color[] backgrounds =
            BuildBackgroundCandidates(
                bitmap,
                document,
                bounds);

        int[,] gridHits =
            new int[gridSize, gridSize];

        int[,] gridSamples =
            new int[gridSize, gridSize];

        long foregroundCount =
            0;

        double weightedX =
            0.0;

        double weightedY =
            0.0;

        int minX =
            bounds.Width;

        int minY =
            bounds.Height;

        int maxX =
            -1;

        int maxY =
            -1;

        for (int localY = 0;
             localY < bounds.Height;
             localY++)
        {
            int imageY =
                bounds.Y + localY;

            int gridY =
                Math.Min(
                    gridSize - 1,
                    localY * gridSize /
                    Math.Max(1, bounds.Height));

            for (int localX = 0;
                 localX < bounds.Width;
                 localX++)
            {
                int imageX =
                    bounds.X + localX;

                int gridX =
                    Math.Min(
                        gridSize - 1,
                        localX * gridSize /
                        Math.Max(1, bounds.Width));

                gridSamples[gridX, gridY]++;

                System.Drawing.Color pixel =
                    bitmap.GetPixel(
                        imageX,
                        imageY);

                if (!IsForegroundPixel(
                        pixel,
                        backgrounds,
                        options))
                {
                    continue;
                }

                gridHits[gridX, gridY]++;
                foregroundCount++;
                weightedX += localX + 0.5;
                weightedY += localY + 0.5;

                minX = Math.Min(minX, localX);
                minY = Math.Min(minY, localY);
                maxX = Math.Max(maxX, localX);
                maxY = Math.Max(maxY, localY);
            }
        }

        bool[,] mask =
            new bool[gridSize, gridSize];

        double[] horizontalProfile =
            new double[gridSize];

        double[] verticalProfile =
            new double[gridSize];

        for (int gridY = 0;
             gridY < gridSize;
             gridY++)
        {
            for (int gridX = 0;
                 gridX < gridSize;
                 gridX++)
            {
                int samples =
                    gridSamples[gridX, gridY];

                int hits =
                    gridHits[gridX, gridY];

                double coverage =
                    samples == 0
                        ? 0.0
                        : (double)hits / samples;

                bool occupied =
                    coverage >= 0.12 ||
                    hits >= 2;

                mask[gridX, gridY] =
                    occupied;

                if (occupied)
                {
                    horizontalProfile[gridY] += 1.0;
                    verticalProfile[gridX] += 1.0;
                }
            }
        }

        for (int index = 0;
             index < gridSize;
             index++)
        {
            horizontalProfile[index] /=
                gridSize;

            verticalProfile[index] /=
                gridSize;
        }

        int contentWidth =
            maxX >= minX
                ? maxX - minX + 1
                : 0;

        int contentHeight =
            maxY >= minY
                ? maxY - minY + 1
                : 0;

        double totalPixels =
            Math.Max(
                1.0,
                bounds.Width *
                bounds.Height);

        double centerX =
            foregroundCount == 0
                ? 0.5
                : weightedX /
                  foregroundCount /
                  Math.Max(1, bounds.Width);

        double centerY =
            foregroundCount == 0
                ? 0.5
                : weightedY /
                  foregroundCount /
                  Math.Max(1, bounds.Height);

        return new PoseFeatures
        {
            Document =
                document,

            Frame =
                frame,

            Mask =
                mask,

            HorizontalProfile =
                horizontalProfile,

            VerticalProfile =
                verticalProfile,

            AspectRatio =
                contentWidth /
                (double)Math.Max(1, contentHeight),

            FillRatio =
                foregroundCount /
                totalPixels,

            CenterX =
                Math.Clamp(centerX, 0.0, 1.0),

            CenterY =
                Math.Clamp(centerY, 0.0, 1.0),

            ForegroundPixelCount =
                (int)Math.Min(
                    int.MaxValue,
                    foregroundCount),

            ContentWidth =
                contentWidth,

            ContentHeight =
                contentHeight
        };
    }

    private static PoseFeatures CreateEmptyFeatures(
        SpriteSheetDocument document,
        SpriteFrame frame,
        int gridSize)
    {
        return new PoseFeatures
        {
            Document =
                document,

            Frame =
                frame,

            Mask =
                new bool[gridSize, gridSize],

            HorizontalProfile =
                new double[gridSize],

            VerticalProfile =
                new double[gridSize],

            AspectRatio =
                1.0,

            FillRatio =
                0.0,

            CenterX =
                0.5,

            CenterY =
                0.5,

            ForegroundPixelCount =
                0,

            ContentWidth =
                0,

            ContentHeight =
                0
        };
    }

    private static System.Drawing.Color[]
        BuildBackgroundCandidates(
            System.Drawing.Bitmap bitmap,
            SpriteSheetDocument document,
            System.Drawing.Rectangle bounds)
    {
        List<System.Drawing.Color> candidates =
            new()
            {
                document.BackgroundColor
            };

        System.Drawing.Point[] points =
        {
            new(bounds.Left, bounds.Top),
            new(bounds.Right - 1, bounds.Top),
            new(bounds.Left, bounds.Bottom - 1),
            new(bounds.Right - 1, bounds.Bottom - 1)
        };

        foreach (System.Drawing.Point point
                 in points)
        {
            System.Drawing.Color color =
                bitmap.GetPixel(
                    point.X,
                    point.Y);

            if (candidates.All(candidate =>
                    ColorDistance(candidate, color) > 8))
            {
                candidates.Add(color);
            }
        }

        return candidates.ToArray();
    }

    private static bool IsForegroundPixel(
        System.Drawing.Color pixel,
        IReadOnlyList<System.Drawing.Color> backgrounds,
        RomPendingPoseReviewOptions options)
    {
        if (pixel.A <
            options.AlphaThreshold)
        {
            return false;
        }

        foreach (System.Drawing.Color background
                 in backgrounds)
        {
            if (ColorDistance(pixel, background) <=
                options.BackgroundTolerance)
            {
                return false;
            }
        }

        return true;
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

    private static double CompareFeatures(
        PoseFeatures target,
        PoseFeatures source,
        bool flipHorizontal)
    {
        double maskScore =
            CompareMasks(
                target.Mask,
                source.Mask,
                flipHorizontal);

        double aspectScore =
            SimilarityFromRatio(
                target.AspectRatio,
                source.AspectRatio,
                maximumLogDifference: 1.35);

        double fillScore =
            SimilarityFromDifference(
                target.FillRatio,
                source.FillRatio,
                maximumDifference: 0.55);

        double sourceCenterX =
            flipHorizontal
                ? 1.0 -
                  source.CenterX
                : source.CenterX;

        double centerDistance =
            Math.Sqrt(
                Math.Pow(
                    target.CenterX -
                    sourceCenterX,
                    2) +
                Math.Pow(
                    target.CenterY -
                    source.CenterY,
                    2));

        double centerScore =
            Math.Clamp(
                1.0 -
                centerDistance /
                0.75,
                0.0,
                1.0);

        double horizontalProfileScore =
            CompareProfiles(
                target.HorizontalProfile,
                source.HorizontalProfile,
                reverseSecond: false);

        double verticalProfileScore =
            CompareProfiles(
                target.VerticalProfile,
                source.VerticalProfile,
                reverseSecond: flipHorizontal);

        double score =
            maskScore * 0.48 +
            aspectScore * 0.17 +
            centerScore * 0.12 +
            horizontalProfileScore * 0.09 +
            verticalProfileScore * 0.09 +
            fillScore * 0.05;

        return Math.Clamp(
            score,
            0.0,
            1.0);
    }

    private static double CompareMasks(
        bool[,] target,
        bool[,] source,
        bool flipHorizontal)
    {
        int width =
            target.GetLength(0);

        int height =
            target.GetLength(1);

        int intersection =
            0;

        int union =
            0;

        int agreements =
            0;

        int total =
            width *
            height;

        for (int y = 0;
             y < height;
             y++)
        {
            for (int x = 0;
                 x < width;
                 x++)
            {
                int sourceX =
                    flipHorizontal
                        ? width - 1 - x
                        : x;

                bool targetValue =
                    target[x, y];

                bool sourceValue =
                    source[sourceX, y];

                if (targetValue &&
                    sourceValue)
                {
                    intersection++;
                }

                if (targetValue ||
                    sourceValue)
                {
                    union++;
                }

                if (targetValue ==
                    sourceValue)
                {
                    agreements++;
                }
            }
        }

        double iou =
            union == 0
                ? 0.0
                : (double)intersection /
                  union;

        double agreement =
            total == 0
                ? 0.0
                : (double)agreements /
                  total;

        return
            iou * 0.78 +
            agreement * 0.22;
    }

    private static double CompareProfiles(
        IReadOnlyList<double> first,
        IReadOnlyList<double> second,
        bool reverseSecond)
    {
        int count =
            Math.Min(
                first.Count,
                second.Count);

        if (count == 0)
        {
            return 0.0;
        }

        double difference =
            0.0;

        for (int index = 0;
             index < count;
             index++)
        {
            int secondIndex =
                reverseSecond
                    ? count - 1 - index
                    : index;

            difference +=
                Math.Abs(
                    first[index] -
                    second[secondIndex]);
        }

        difference /=
            count;

        return Math.Clamp(
            1.0 -
            difference,
            0.0,
            1.0);
    }

    private static double SimilarityFromRatio(
        double first,
        double second,
        double maximumLogDifference)
    {
        first =
            Math.Max(
                first,
                0.0001);

        second =
            Math.Max(
                second,
                0.0001);

        double difference =
            Math.Abs(
                Math.Log(
                    first /
                    second));

        return Math.Clamp(
            1.0 -
            difference /
            maximumLogDifference,
            0.0,
            1.0);
    }

    private static double SimilarityFromDifference(
        double first,
        double second,
        double maximumDifference)
    {
        double difference =
            Math.Abs(
                first -
                second);

        return Math.Clamp(
            1.0 -
            difference /
            maximumDifference,
            0.0,
            1.0);
    }
}
