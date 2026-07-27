using AladdinSpriteStudio.UI.CharacterReplacement.Models;

namespace AladdinSpriteStudio.UI.CharacterReplacement.Validation;

/// <summary>
/// Parámetros para comprobar una hoja final ya preparada externamente,
/// por ejemplo una hoja generada por una IA copiando la distribución
/// exacta de la hoja de Aladdin.
/// </summary>
public sealed class ReadyReplacementSheetValidationOptions
{
    public int ExpectedTargetFrameCount { get; init; } = 102;

    public int BackgroundTolerance { get; init; } = 24;

    public int AlphaThreshold { get; init; } = 16;

    public int MinimumVisiblePixelsPerFrame { get; init; } = 20;

    /// <summary>
    /// Porcentaje mínimo de píxeles originales prácticamente idénticos
    /// para considerar que una pose de Aladdin no fue reemplazada.
    /// </summary>
    public double UnchangedPixelRatioThreshold { get; init; } = 0.88;

    public int UnchangedColorTolerance { get; init; } = 12;

    public int MaximumVisibleColorsPerFrame { get; init; } = 15;

    /// <summary>
    /// Una cantidad importante de contenido fuera de las regiones
    /// detectadas se informa como advertencia.
    /// </summary>
    public int OutsideRegionWarningPixelCount { get; init; } = 50;
}

/// <summary>
/// Valida directamente una hoja final. No necesita asignaciones ni
/// FrameMapping: compara cada región de Aladdin contra la imagen lista.
/// </summary>
public static class ReadyReplacementSheetValidator
{
    private sealed class FrameCheck
    {
        public required SpriteFrame Frame { get; init; }

        public required int VisiblePixelCount { get; init; }

        public required int VisibleColorCount { get; init; }

        public required double UnchangedPixelRatio { get; init; }

        public bool IsEmpty { get; init; }

        public bool LooksUnchanged { get; init; }
    }

    public static RomCompatibilityValidationResult Validate(
        SpriteSheetDocument targetDocument,
        System.Drawing.Image readySheet,
        ReadyReplacementSheetValidationOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(targetDocument);
        ArgumentNullException.ThrowIfNull(readySheet);

        options ??=
            new ReadyReplacementSheetValidationOptions();

        ValidateOptions(options);

        using System.Drawing.Bitmap readyBitmap =
            new(readySheet);

        using System.Drawing.Bitmap targetBitmap =
            new(targetDocument.Image);

        List<RomValidationIssue> issues =
            new();

        ValidateDimensions(
            targetDocument,
            readyBitmap,
            issues);

        ValidateTargetFrameCount(
            targetDocument,
            options,
            issues);

        bool dimensionsMatch =
            readyBitmap.Width == targetDocument.Width &&
            readyBitmap.Height == targetDocument.Height;

        List<FrameCheck> checks =
            dimensionsMatch
                ? AnalyzeFrames(
                    targetDocument,
                    targetBitmap,
                    readyBitmap,
                    options)
                : new List<FrameCheck>();

        int completedCount =
            checks.Count(check =>
                !check.IsEmpty &&
                !check.LooksUnchanged);

        int pendingCount =
            Math.Max(
                0,
                targetDocument.Frames.Count -
                completedCount);

        RomCompatibilityValidationResult result =
            new(
                targetDocument.Frames.Count,
                completedCount,
                pendingCount);

        foreach (RomValidationIssue issue in issues)
        {
            result.AddIssue(issue);
        }

        if (dimensionsMatch)
        {
            AddFrameIssues(
                checks,
                options,
                result);

            ValidateContentOutsideTargetRegions(
                targetDocument,
                readyBitmap,
                options,
                result);
        }

        if (pendingCount == 0 &&
            dimensionsMatch)
        {
            result.AddIssue(
                new RomValidationIssue(
                    "READY_SHEET_COMPLETE",
                    RomValidationSeverity.Information,
                    "Hoja final completa",
                    "Todas las regiones contienen contenido nuevo " +
                    "y ninguna parece conservar exactamente a Aladdin."));
        }
        else if (dimensionsMatch)
        {
            result.AddIssue(
                new RomValidationIssue(
                    "READY_SHEET_INCOMPLETE",
                    RomValidationSeverity.Error,
                    "Hoja final incompleta",
                    $"Se detectaron {pendingCount} regiones vacías " +
                    "o aparentemente no reemplazadas."));
        }

        result.AddIssue(
            new RomValidationIssue(
                "DIRECT_READY_SHEET_MODE",
                RomValidationSeverity.Information,
                "Modo de hoja final",
                "La imagen fue validada directamente, sin utilizar " +
                "asignaciones ni reconstruir las poses."));

        result.AddIssue(
            new RomValidationIssue(
                "ROM_STRUCTURE_PENDING",
                RomValidationSeverity.Information,
                "Estructura interna pendiente",
                "Aunque la hoja visual sea correcta, todavía deben " +
                "confirmarse el orden real de tiles, OAM, paletas, " +
                "tablas de animación y posible compresión de la ROM."));

        return result;
    }

    private static void ValidateOptions(
        ReadyReplacementSheetValidationOptions options)
    {
        if (options.ExpectedTargetFrameCount <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(options.ExpectedTargetFrameCount));
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

        if (options.MinimumVisiblePixelsPerFrame < 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(options.MinimumVisiblePixelsPerFrame));
        }

        if (options.UnchangedPixelRatioThreshold < 0.0 ||
            options.UnchangedPixelRatioThreshold > 1.0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(options.UnchangedPixelRatioThreshold));
        }

        if (options.UnchangedColorTolerance < 0 ||
            options.UnchangedColorTolerance > 255)
        {
            throw new ArgumentOutOfRangeException(
                nameof(options.UnchangedColorTolerance));
        }

        if (options.MaximumVisibleColorsPerFrame < 1 ||
            options.MaximumVisibleColorsPerFrame > 255)
        {
            throw new ArgumentOutOfRangeException(
                nameof(options.MaximumVisibleColorsPerFrame));
        }
    }

    private static void ValidateDimensions(
        SpriteSheetDocument targetDocument,
        System.Drawing.Bitmap readyBitmap,
        ICollection<RomValidationIssue> issues)
    {
        if (readyBitmap.Width == targetDocument.Width &&
            readyBitmap.Height == targetDocument.Height)
        {
            issues.Add(
                new RomValidationIssue(
                    "READY_SHEET_SIZE_OK",
                    RomValidationSeverity.Information,
                    "Tamaño correcto",
                    $"La hoja final conserva " +
                    $"{readyBitmap.Width} × {readyBitmap.Height} píxeles."));

            return;
        }

        issues.Add(
            new RomValidationIssue(
                "READY_SHEET_SIZE_MISMATCH",
                RomValidationSeverity.Error,
                "Tamaño incorrecto",
                $"La hoja final mide {readyBitmap.Width} × " +
                $"{readyBitmap.Height}, pero debe medir exactamente " +
                $"{targetDocument.Width} × {targetDocument.Height}."));
    }

    private static void ValidateTargetFrameCount(
        SpriteSheetDocument targetDocument,
        ReadyReplacementSheetValidationOptions options,
        ICollection<RomValidationIssue> issues)
    {
        if (targetDocument.Frames.Count ==
            options.ExpectedTargetFrameCount)
        {
            issues.Add(
                new RomValidationIssue(
                    "READY_FRAME_COUNT_OK",
                    RomValidationSeverity.Information,
                    "Regiones de referencia",
                    $"La hoja de Aladdin contiene las " +
                    $"{targetDocument.Frames.Count} regiones esperadas."));

            return;
        }

        issues.Add(
            new RomValidationIssue(
                "READY_FRAME_COUNT_MISMATCH",
                RomValidationSeverity.Error,
                "Cantidad de regiones inesperada",
                $"Se esperaban {options.ExpectedTargetFrameCount} " +
                $"regiones y se detectaron " +
                $"{targetDocument.Frames.Count}."));
    }

    private static List<FrameCheck> AnalyzeFrames(
        SpriteSheetDocument targetDocument,
        System.Drawing.Bitmap targetBitmap,
        System.Drawing.Bitmap readyBitmap,
        ReadyReplacementSheetValidationOptions options)
    {
        List<FrameCheck> result =
            new();

        foreach (SpriteFrame frame
                 in targetDocument.Frames
                     .OrderBy(item => item.Index))
        {
            System.Drawing.Rectangle bounds =
                System.Drawing.Rectangle.Intersect(
                    new System.Drawing.Rectangle(
                        System.Drawing.Point.Empty,
                        readyBitmap.Size),
                    frame.Bounds);

            int visiblePixels =
                0;

            int originalForegroundPixels =
                0;

            int nearlyIdenticalOriginalPixels =
                0;

            HashSet<int> visibleColors =
                new();

            for (int y = bounds.Top;
                 y < bounds.Bottom;
                 y++)
            {
                for (int x = bounds.Left;
                     x < bounds.Right;
                     x++)
                {
                    System.Drawing.Color original =
                        targetBitmap.GetPixel(x, y);

                    System.Drawing.Color ready =
                        readyBitmap.GetPixel(x, y);

                    bool originalIsBackground =
                        IsBackground(
                            original,
                            targetDocument.BackgroundColor,
                            options);

                    bool readyIsBackground =
                        IsBackground(
                            ready,
                            targetDocument.BackgroundColor,
                            options);

                    if (!readyIsBackground)
                    {
                        visiblePixels++;

                        visibleColors.Add(
                            System.Drawing.Color.FromArgb(
                                255,
                                ready.R,
                                ready.G,
                                ready.B).ToArgb());
                    }

                    if (!originalIsBackground)
                    {
                        originalForegroundPixels++;

                        if (!readyIsBackground &&
                            ColorDistance(
                                original,
                                ready) <=
                            options.UnchangedColorTolerance)
                        {
                            nearlyIdenticalOriginalPixels++;
                        }
                    }
                }
            }

            double unchangedRatio =
                originalForegroundPixels == 0
                    ? 0.0
                    : (double)nearlyIdenticalOriginalPixels /
                      originalForegroundPixels;

            bool empty =
                visiblePixels <
                options.MinimumVisiblePixelsPerFrame;

            bool unchanged =
                !empty &&
                unchangedRatio >=
                options.UnchangedPixelRatioThreshold;

            result.Add(
                new FrameCheck
                {
                    Frame =
                        frame,

                    VisiblePixelCount =
                        visiblePixels,

                    VisibleColorCount =
                        visibleColors.Count,

                    UnchangedPixelRatio =
                        unchangedRatio,

                    IsEmpty =
                        empty,

                    LooksUnchanged =
                        unchanged
                });
        }

        return result;
    }

    private static void AddFrameIssues(
        IEnumerable<FrameCheck> checks,
        ReadyReplacementSheetValidationOptions options,
        RomCompatibilityValidationResult result)
    {
        foreach (FrameCheck check in checks)
        {
            int frameIndex =
                check.Frame.Index;

            if (check.IsEmpty)
            {
                result.AddIssue(
                    new RomValidationIssue(
                        "READY_FRAME_EMPTY",
                        RomValidationSeverity.Error,
                        "Región vacía",
                        $"La región contiene únicamente " +
                        $"{check.VisiblePixelCount} píxeles visibles.",
                        frameIndex));

                continue;
            }

            if (check.LooksUnchanged)
            {
                result.AddIssue(
                    new RomValidationIssue(
                        "READY_FRAME_UNCHANGED",
                        RomValidationSeverity.Error,
                        "Aladdin posiblemente no reemplazado",
                        $"{check.UnchangedPixelRatio:P1} de los píxeles " +
                        "originales conservan prácticamente el mismo color.",
                        frameIndex));
            }

            if (check.VisibleColorCount >
                options.MaximumVisibleColorsPerFrame)
            {
                result.AddIssue(
                    new RomValidationIssue(
                        "READY_PALETTE_REDUCTION_REQUIRED",
                        RomValidationSeverity.Warning,
                        "Reducción de colores necesaria",
                        $"La región contiene {check.VisibleColorCount} " +
                        $"colores visibles; el objetivo conservador es " +
                        $"{options.MaximumVisibleColorsPerFrame}.",
                        frameIndex));
            }
        }
    }

    private static void ValidateContentOutsideTargetRegions(
        SpriteSheetDocument targetDocument,
        System.Drawing.Bitmap readyBitmap,
        ReadyReplacementSheetValidationOptions options,
        RomCompatibilityValidationResult result)
    {
        bool[,] covered =
            new bool[
                readyBitmap.Width,
                readyBitmap.Height];

        foreach (SpriteFrame frame
                 in targetDocument.Frames)
        {
            System.Drawing.Rectangle bounds =
                System.Drawing.Rectangle.Intersect(
                    new System.Drawing.Rectangle(
                        System.Drawing.Point.Empty,
                        readyBitmap.Size),
                    frame.Bounds);

            for (int y = bounds.Top;
                 y < bounds.Bottom;
                 y++)
            {
                for (int x = bounds.Left;
                     x < bounds.Right;
                     x++)
                {
                    covered[x, y] =
                        true;
                }
            }
        }

        int outsideVisiblePixels =
            0;

        for (int y = 0;
             y < readyBitmap.Height;
             y++)
        {
            for (int x = 0;
                 x < readyBitmap.Width;
                 x++)
            {
                if (covered[x, y])
                {
                    continue;
                }

                System.Drawing.Color pixel =
                    readyBitmap.GetPixel(x, y);

                if (!IsBackground(
                        pixel,
                        targetDocument.BackgroundColor,
                        options))
                {
                    outsideVisiblePixels++;
                }
            }
        }

        if (outsideVisiblePixels >
            options.OutsideRegionWarningPixelCount)
        {
            result.AddIssue(
                new RomValidationIssue(
                    "READY_CONTENT_OUTSIDE_REGIONS",
                    RomValidationSeverity.Warning,
                    "Contenido fuera de las poses",
                    $"Se encontraron {outsideVisiblePixels:N0} píxeles " +
                    "visibles fuera de las regiones detectadas. " +
                    "Pueden corresponder a texto, créditos, iconos " +
                    "o elementos que no deben convertirse en tiles."));
        }
        else
        {
            result.AddIssue(
                new RomValidationIssue(
                    "READY_OUTSIDE_CONTENT_OK",
                    RomValidationSeverity.Information,
                    "Contenido exterior",
                    "No se detectó una cantidad importante de contenido " +
                    "fuera de las regiones de poses."));
        }
    }

    private static bool IsBackground(
        System.Drawing.Color pixel,
        System.Drawing.Color expectedBackground,
        ReadyReplacementSheetValidationOptions options)
    {
        if (pixel.A <
            options.AlphaThreshold)
        {
            return true;
        }

        return ColorDistance(
                   pixel,
                   expectedBackground) <=
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
}
