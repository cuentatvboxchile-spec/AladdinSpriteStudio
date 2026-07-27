using AladdinSpriteStudio.UI.CharacterReplacement.Models;

namespace AladdinSpriteStudio.UI.CharacterReplacement.Analysis;

/// <summary>
/// Configuración de la autoasignación diseñada para conservar la
/// estructura gráfica original de Aladdin y facilitar una futura
/// inserción en los mismos espacios de tiles de la ROM.
/// </summary>
public sealed class RomSafeAutoMappingOptions
{
    public int NormalizedGridSize { get; init; } = 16;

    public double MinimumAcceptedScore { get; init; } = 0.66;

    public bool PreferUniqueSourceFrames { get; init; } = true;

    public bool AllowHorizontalFlip { get; init; } = true;

    public bool AllowSourceReuseWhenNecessary { get; init; } = false;

    public int BackgroundTolerance { get; init; } = 34;

    public int AlphaThreshold { get; init; } = 16;

    /// <summary>
    /// Descarta letras, marcas y fragmentos demasiado pequeños.
    /// </summary>
    public int MinimumSourceContentWidth { get; init; } = 10;

    public int MinimumSourceContentHeight { get; init; } = 14;

    public int MinimumSourceForegroundPixels { get; init; } = 80;

    /// <summary>
    /// Diferencia mínima entre la mejor y la segunda coincidencia.
    /// Las coincidencias ambiguas quedan sin asignar.
    /// </summary>
    public double MinimumBestSecondGap { get; init; } = 0.015;
}

/// <summary>
/// Resultado de una coincidencia automática entre una pose de Aladdin
/// y una pose del personaje nuevo.
/// </summary>
public sealed class RomSafeAutoMappingSuggestion
{
    public RomSafeAutoMappingSuggestion(
        SpriteSheetDocument targetDocument,
        SpriteFrame targetFrame,
        SpriteSheetDocument sourceDocument,
        SpriteFrame sourceFrame,
        double score,
        bool flipHorizontal)
    {
        TargetDocument =
            targetDocument ??
            throw new ArgumentNullException(nameof(targetDocument));

        TargetFrame =
            targetFrame ??
            throw new ArgumentNullException(nameof(targetFrame));

        SourceDocument =
            sourceDocument ??
            throw new ArgumentNullException(nameof(sourceDocument));

        SourceFrame =
            sourceFrame ??
            throw new ArgumentNullException(nameof(sourceFrame));

        Score = Math.Clamp(score, 0.0, 1.0);
        FlipHorizontal = flipHorizontal;
    }

    public SpriteSheetDocument TargetDocument { get; }

    public SpriteFrame TargetFrame { get; }

    public SpriteSheetDocument SourceDocument { get; }

    public SpriteFrame SourceFrame { get; }

    public double Score { get; }

    public bool FlipHorizontal { get; }

    public string ConfidenceText =>
        Score >= 0.78
            ? "Alta"
            : Score >= 0.60
                ? "Media"
                : "Baja";
}

/// <summary>
/// Motor de comparación de poses que conserva el rectángulo, orden y
/// cantidad de poses objetivo de Aladdin.
/// </summary>
public static class RomSafeAutoMappingEngine
{
    private sealed class PoseFeatures
    {
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

    private sealed class Candidate
    {
        public required PoseFeatures Target { get; init; }

        public required PoseFeatures Source { get; init; }

        public required double Score { get; init; }

        public required bool FlipHorizontal { get; init; }
    }

    public static IReadOnlyList<RomSafeAutoMappingSuggestion> Analyze(
        SpriteSheetDocument targetDocument,
        SpriteSheetDocument sourceDocument,
        RomSafeAutoMappingOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(targetDocument);
        ArgumentNullException.ThrowIfNull(sourceDocument);

        options ??= new RomSafeAutoMappingOptions();

        ValidateOptions(options);

        if (targetDocument.Frames.Count == 0)
        {
            throw new InvalidOperationException(
                "La hoja de Aladdin no contiene poses detectadas.");
        }

        if (sourceDocument.Frames.Count == 0)
        {
            throw new InvalidOperationException(
                "La hoja del personaje nuevo no contiene poses detectadas.");
        }

        using System.Drawing.Bitmap targetBitmap =
            new(targetDocument.Image);

        using System.Drawing.Bitmap sourceBitmap =
            new(sourceDocument.Image);

        List<PoseFeatures> targetFeatures =
            targetDocument.Frames
                .Where(frame => frame.IsEnabled)
                .Select(frame =>
                    ExtractFeatures(
                        targetBitmap,
                        targetDocument,
                        frame,
                        options))
                .ToList();

        List<PoseFeatures> sourceFeatures =
            sourceDocument.Frames
                .Where(frame => frame.IsEnabled)
                .Select(frame =>
                    ExtractFeatures(
                        sourceBitmap,
                        sourceDocument,
                        frame,
                        options))
                .Where(feature =>
                    IsLikelyCharacterPose(
                        feature,
                        options))
                .ToList();

        if (targetFeatures.Count == 0)
        {
            throw new InvalidOperationException(
                "No existen poses activas en la hoja de Aladdin.");
        }

        if (sourceFeatures.Count == 0)
        {
            throw new InvalidOperationException(
                "No se encontraron poses de personaje utilizables. " +
                "La hoja contiene principalmente texto, fondos o " +
                "fragmentos demasiado pequeños.");
        }

        List<Candidate> candidates =
            BuildCandidates(
                targetFeatures,
                sourceFeatures,
                options);

        return SelectSuggestions(
            targetDocument,
            sourceDocument,
            targetFeatures,
            candidates,
            options);
    }

    public static int ApplySuggestions(
        CharacterReplacementProject project,
        IEnumerable<RomSafeAutoMappingSuggestion> suggestions,
        double minimumScore = 0.52,
        bool replaceExistingMappings = false)
    {
        ArgumentNullException.ThrowIfNull(project);
        ArgumentNullException.ThrowIfNull(suggestions);

        minimumScore =
            Math.Clamp(
                minimumScore,
                0.0,
                1.0);

        int appliedCount = 0;

        foreach (RomSafeAutoMappingSuggestion suggestion
                 in suggestions
                     .Where(item => item.Score >= minimumScore)
                     .OrderBy(item => item.TargetFrame.Index))
        {
            FrameMapping? existing =
                project.GetMapping(
                    suggestion.TargetFrame);

            if (existing is not null &&
                !replaceExistingMappings)
            {
                continue;
            }

            FrameMapping mapping =
                project.AssignFrame(
                    suggestion.TargetFrame,
                    suggestion.SourceDocument,
                    suggestion.SourceFrame);

            mapping.AutoFit = true;
            mapping.Scale = 1.0f;
            mapping.OffsetX = 0;
            mapping.OffsetY = 0;
            mapping.FlipHorizontal =
                suggestion.FlipHorizontal;
            mapping.FlipVertical = false;

            appliedCount++;
        }

        return appliedCount;
    }

    private static void ValidateOptions(
        RomSafeAutoMappingOptions options)
    {
        if (options.NormalizedGridSize < 8 ||
            options.NormalizedGridSize > 64)
        {
            throw new ArgumentOutOfRangeException(
                nameof(options.NormalizedGridSize),
                "La cuadrícula normalizada debe estar entre 8 y 64.");
        }

        if (options.MinimumAcceptedScore < 0.0 ||
            options.MinimumAcceptedScore > 1.0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(options.MinimumAcceptedScore),
                "El puntaje mínimo debe estar entre 0 y 1.");
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

        if (options.MinimumSourceContentWidth < 1 ||
            options.MinimumSourceContentHeight < 1 ||
            options.MinimumSourceForegroundPixels < 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(options.MinimumSourceContentWidth),
                "Los mínimos de la pose fuente deben ser mayores que cero.");
        }

        if (options.MinimumBestSecondGap < 0.0 ||
            options.MinimumBestSecondGap > 1.0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(options.MinimumBestSecondGap));
        }
    }

    private static bool IsLikelyCharacterPose(
        PoseFeatures feature,
        RomSafeAutoMappingOptions options)
    {
        if (feature.ContentWidth <
                options.MinimumSourceContentWidth ||
            feature.ContentHeight <
                options.MinimumSourceContentHeight ||
            feature.ForegroundPixelCount <
                options.MinimumSourceForegroundPixels)
        {
            return false;
        }

        double aspect =
            feature.ContentWidth /
            (double)Math.Max(1, feature.ContentHeight);

        if (aspect < 0.18 || aspect > 4.50)
        {
            return false;
        }

        return feature.FillRatio >= 0.035 &&
               feature.FillRatio <= 0.90;
    }

    private static List<Candidate> BuildCandidates(
        IReadOnlyList<PoseFeatures> targets,
        IReadOnlyList<PoseFeatures> sources,
        RomSafeAutoMappingOptions options)
    {
        List<Candidate> candidates =
            new(targets.Count * sources.Count);

        foreach (PoseFeatures target in targets)
        {
            foreach (PoseFeatures source in sources)
            {
                double normalScore =
                    CompareFeatures(
                        target,
                        source,
                        flipHorizontal: false);

                double selectedScore = normalScore;
                bool selectedFlip = false;

                if (options.AllowHorizontalFlip)
                {
                    double flippedScore =
                        CompareFeatures(
                            target,
                            source,
                            flipHorizontal: true);

                    if (flippedScore > selectedScore)
                    {
                        selectedScore = flippedScore;
                        selectedFlip = true;
                    }
                }

                candidates.Add(
                    new Candidate
                    {
                        Target = target,
                        Source = source,
                        Score = selectedScore,
                        FlipHorizontal = selectedFlip
                    });
            }
        }

        return candidates;
    }

    private static IReadOnlyList<RomSafeAutoMappingSuggestion>
        SelectSuggestions(
            SpriteSheetDocument targetDocument,
            SpriteSheetDocument sourceDocument,
            IReadOnlyList<PoseFeatures> targets,
            IReadOnlyList<Candidate> candidates,
            RomSafeAutoMappingOptions options)
    {
        Dictionary<int, Candidate> selectedByTarget = new();
        HashSet<int> usedSourceIndexes = new();

        HashSet<int> ambiguousTargets =
            candidates
                .GroupBy(item => item.Target.Frame.Index)
                .Where(group =>
                {
                    Candidate[] top = group
                        .OrderByDescending(item => item.Score)
                        .Take(2)
                        .ToArray();

                    return top.Length > 1 &&
                           top[0].Score - top[1].Score <
                           options.MinimumBestSecondGap;
                })
                .Select(group => group.Key)
                .ToHashSet();

        IEnumerable<Candidate> orderedCandidates =
            candidates
                .OrderByDescending(item => item.Score)
                .ThenBy(item => item.Target.Frame.Index)
                .ThenBy(item => item.Source.Frame.Index);

        foreach (Candidate candidate in orderedCandidates)
        {
            int targetIndex = candidate.Target.Frame.Index;
            int sourceIndex = candidate.Source.Frame.Index;

            if (ambiguousTargets.Contains(targetIndex))
            {
                continue;
            }

            if (selectedByTarget.ContainsKey(targetIndex))
            {
                continue;
            }

            if (options.PreferUniqueSourceFrames &&
                usedSourceIndexes.Contains(sourceIndex))
            {
                continue;
            }

            if (candidate.Score <
                options.MinimumAcceptedScore)
            {
                continue;
            }

            selectedByTarget[targetIndex] = candidate;
            usedSourceIndexes.Add(sourceIndex);
        }

        if (options.AllowSourceReuseWhenNecessary)
        {
            foreach (PoseFeatures target
                     in targets.OrderBy(item => item.Frame.Index))
            {
                int targetIndex = target.Frame.Index;

                if (selectedByTarget.ContainsKey(targetIndex) ||
                    ambiguousTargets.Contains(targetIndex))
                {
                    continue;
                }

                Candidate? bestCandidate =
                    candidates
                        .Where(item =>
                            item.Target.Frame.Index == targetIndex)
                        .OrderByDescending(item => item.Score)
                        .ThenBy(item => item.Source.Frame.Index)
                        .FirstOrDefault();

                if (bestCandidate is null ||
                    bestCandidate.Score <
                    options.MinimumAcceptedScore)
                {
                    continue;
                }

                selectedByTarget[targetIndex] = bestCandidate;
            }
        }

        return selectedByTarget
            .Values
            .OrderBy(item => item.Target.Frame.Index)
            .Select(item =>
                new RomSafeAutoMappingSuggestion(
                    targetDocument,
                    item.Target.Frame,
                    sourceDocument,
                    item.Source.Frame,
                    item.Score,
                    item.FlipHorizontal))
            .ToList();
    }

    private static PoseFeatures ExtractFeatures(
        System.Drawing.Bitmap bitmap,
        SpriteSheetDocument document,
        SpriteFrame frame,
        RomSafeAutoMappingOptions options)
    {
        System.Drawing.Rectangle imageBounds =
            new(
                System.Drawing.Point.Empty,
                bitmap.Size);

        System.Drawing.Rectangle bounds =
            System.Drawing.Rectangle.Intersect(
                imageBounds,
                frame.Bounds);

        int gridSize = options.NormalizedGridSize;

        if (bounds.Width <= 0 ||
            bounds.Height <= 0)
        {
            return new PoseFeatures
            {
                Frame = frame,
                Mask = new bool[gridSize, gridSize],
                HorizontalProfile = new double[gridSize],
                VerticalProfile = new double[gridSize],
                AspectRatio = 1.0,
                FillRatio = 0.0,
                CenterX = 0.5,
                CenterY = 0.5,
                ForegroundPixelCount = 0,
                ContentWidth = 0,
                ContentHeight = 0
            };
        }

        System.Drawing.Color[] backgroundCandidates =
            BuildBackgroundCandidates(
                bitmap,
                document,
                bounds);

        int[,] gridHits =
            new int[gridSize, gridSize];

        int[,] gridSamples =
            new int[gridSize, gridSize];

        long foregroundCount = 0;
        double weightedX = 0.0;
        double weightedY = 0.0;

        int minimumForegroundX = bounds.Width;
        int minimumForegroundY = bounds.Height;
        int maximumForegroundX = -1;
        int maximumForegroundY = -1;

        for (int y = 0; y < bounds.Height; y++)
        {
            int imageY = bounds.Y + y;

            int gridY =
                Math.Min(
                    gridSize - 1,
                    y * gridSize /
                    Math.Max(1, bounds.Height));

            for (int x = 0; x < bounds.Width; x++)
            {
                int imageX = bounds.X + x;

                int gridX =
                    Math.Min(
                        gridSize - 1,
                        x * gridSize /
                        Math.Max(1, bounds.Width));

                gridSamples[gridX, gridY]++;

                System.Drawing.Color pixel =
                    bitmap.GetPixel(imageX, imageY);

                if (!IsForegroundPixel(
                        pixel,
                        backgroundCandidates,
                        options))
                {
                    continue;
                }

                gridHits[gridX, gridY]++;
                foregroundCount++;
                weightedX += x + 0.5;
                weightedY += y + 0.5;

                minimumForegroundX =
                    Math.Min(minimumForegroundX, x);
                minimumForegroundY =
                    Math.Min(minimumForegroundY, y);
                maximumForegroundX =
                    Math.Max(maximumForegroundX, x);
                maximumForegroundY =
                    Math.Max(maximumForegroundY, y);
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

                mask[gridX, gridY] = occupied;

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
            horizontalProfile[index] /= gridSize;
            verticalProfile[index] /= gridSize;
        }

        double totalPixels =
            Math.Max(
                1.0,
                bounds.Width * bounds.Height);

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

        int contentWidth =
            maximumForegroundX < minimumForegroundX
                ? 0
                : maximumForegroundX - minimumForegroundX + 1;

        int contentHeight =
            maximumForegroundY < minimumForegroundY
                ? 0
                : maximumForegroundY - minimumForegroundY + 1;

        return new PoseFeatures
        {
            Frame = frame,
            Mask = mask,
            HorizontalProfile = horizontalProfile,
            VerticalProfile = verticalProfile,
            AspectRatio =
                bounds.Width /
                (double)Math.Max(1, bounds.Height),
            FillRatio = foregroundCount / totalPixels,
            CenterX = Math.Clamp(centerX, 0.0, 1.0),
            CenterY = Math.Clamp(centerY, 0.0, 1.0),
            ForegroundPixelCount =
                foregroundCount > int.MaxValue
                    ? int.MaxValue
                    : (int)foregroundCount,
            ContentWidth = contentWidth,
            ContentHeight = contentHeight
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

        foreach (System.Drawing.Point point in points)
        {
            System.Drawing.Color color =
                bitmap.GetPixel(point.X, point.Y);

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
        IReadOnlyList<System.Drawing.Color> backgroundCandidates,
        RomSafeAutoMappingOptions options)
    {
        if (pixel.A < options.AlphaThreshold)
        {
            return false;
        }

        foreach (System.Drawing.Color background
                 in backgroundCandidates)
        {
            if (ColorDistance(pixel, background) <=
                options.BackgroundTolerance)
            {
                return false;
            }

            if (BelongsToBackgroundColorFamily(
                    pixel,
                    background))
            {
                return false;
            }
        }

        return true;
    }

    private static bool BelongsToBackgroundColorFamily(
        System.Drawing.Color pixel,
        System.Drawing.Color background)
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
            pixelValue < 0.08)
        {
            return false;
        }

        double hueDifference =
            Math.Abs(pixelHue - backgroundHue);

        hueDifference =
            Math.Min(
                hueDifference,
                360.0 - hueDifference);

        return hueDifference <= 14.0 &&
               Math.Abs(pixelValue - backgroundValue) <= 0.62;
    }

    private static void RgbToHsv(
        System.Drawing.Color color,
        out double hue,
        out double saturation,
        out double value)
    {
        double red = color.R / 255.0;
        double green = color.G / 255.0;
        double blue = color.B / 255.0;

        double maximum = Math.Max(red, Math.Max(green, blue));
        double minimum = Math.Min(red, Math.Min(green, blue));
        double delta = maximum - minimum;

        value = maximum;
        saturation = maximum <= 0.0 ? 0.0 : delta / maximum;

        if (delta <= 0.000001)
        {
            hue = 0.0;
            return;
        }

        if (maximum == red)
        {
            hue = 60.0 * (((green - blue) / delta) % 6.0);
        }
        else if (maximum == green)
        {
            hue = 60.0 * (((blue - red) / delta) + 2.0);
        }
        else
        {
            hue = 60.0 * (((red - green) / delta) + 4.0);
        }

        if (hue < 0.0)
        {
            hue += 360.0;
        }
    }

    private static int ColorDistance(
        System.Drawing.Color first,
        System.Drawing.Color second)
    {
        int red = first.R - second.R;
        int green = first.G - second.G;
        int blue = first.B - second.B;

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
                ? 1.0 - source.CenterX
                : source.CenterX;

        double centerDistance =
            Math.Sqrt(
                Math.Pow(
                    target.CenterX - sourceCenterX,
                    2) +
                Math.Pow(
                    target.CenterY - source.CenterY,
                    2));

        double centerScore =
            Math.Clamp(
                1.0 - centerDistance / 0.75,
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

        return Math.Clamp(score, 0.0, 1.0);
    }

    private static double CompareMasks(
        bool[,] target,
        bool[,] source,
        bool flipHorizontal)
    {
        int width = target.GetLength(0);
        int height = target.GetLength(1);

        int intersection = 0;
        int union = 0;
        int agreements = 0;
        int total = width * height;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int sourceX =
                    flipHorizontal
                        ? width - 1 - x
                        : x;

                bool targetValue = target[x, y];
                bool sourceValue = source[sourceX, y];

                if (targetValue && sourceValue)
                {
                    intersection++;
                }

                if (targetValue || sourceValue)
                {
                    union++;
                }

                if (targetValue == sourceValue)
                {
                    agreements++;
                }
            }
        }

        double iou =
            union == 0
                ? 0.0
                : (double)intersection / union;

        double agreement =
            total == 0
                ? 0.0
                : (double)agreements / total;

        return iou * 0.78 +
               agreement * 0.22;
    }

    private static double CompareProfiles(
        IReadOnlyList<double> first,
        IReadOnlyList<double> second,
        bool reverseSecond)
    {
        int count =
            Math.Min(first.Count, second.Count);

        if (count == 0)
        {
            return 0.0;
        }

        double difference = 0.0;

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

        difference /= count;

        return Math.Clamp(
            1.0 - difference,
            0.0,
            1.0);
    }

    private static double SimilarityFromRatio(
        double first,
        double second,
        double maximumLogDifference)
    {
        first = Math.Max(first, 0.0001);
        second = Math.Max(second, 0.0001);

        double difference =
            Math.Abs(
                Math.Log(first / second));

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
            Math.Abs(first - second);

        return Math.Clamp(
            1.0 -
            difference /
            maximumDifference,
            0.0,
            1.0);
    }
}
