using AladdinSpriteStudio.UI.CharacterReplacement.Models;
using AladdinSpriteStudio.UI.CharacterReplacement.Rendering;

namespace AladdinSpriteStudio.UI.CharacterReplacement.Validation;

/// <summary>
/// Nivel de importancia de una observación encontrada durante la
/// validación de una hoja preparada para una futura conversión a ROM.
/// </summary>
public enum RomValidationSeverity
{
    Information,
    Warning,
    Error
}

/// <summary>
/// Una observación individual producida por el validador.
/// </summary>
public sealed class RomValidationIssue
{
    public RomValidationIssue(
        string code,
        RomValidationSeverity severity,
        string title,
        string description,
        int? targetFrameIndex = null)
    {
        Code =
            string.IsNullOrWhiteSpace(code)
                ? throw new ArgumentException(
                    "El código de la observación es obligatorio.",
                    nameof(code))
                : code;

        Severity =
            severity;

        Title =
            string.IsNullOrWhiteSpace(title)
                ? throw new ArgumentException(
                    "El título de la observación es obligatorio.",
                    nameof(title))
                : title;

        Description =
            description ??
            string.Empty;

        TargetFrameIndex =
            targetFrameIndex;
    }

    public string Code { get; }

    public RomValidationSeverity Severity { get; }

    public string Title { get; }

    public string Description { get; }

    /// <summary>
    /// Índice cero basado de la pose objetivo relacionada.
    /// </summary>
    public int? TargetFrameIndex { get; }

    public string SeverityText =>
        Severity switch
        {
            RomValidationSeverity.Error =>
                "ERROR",

            RomValidationSeverity.Warning =>
                "ADVERTENCIA",

            _ =>
                "INFORMACIÓN"
        };

    public override string ToString()
    {
        string poseText =
            TargetFrameIndex is int index
                ? $" | Pose Aladdin {index + 1:000}"
                : string.Empty;

        return
            $"[{SeverityText}] {Code}{poseText} — " +
            $"{Title}: {Description}";
    }
}

/// <summary>
/// Parámetros conservadores para validar la hoja antes de convertirla
/// a paleta SNES, tiles 8 × 8 y datos 4BPP.
/// </summary>
public sealed class RomCompatibilityValidationOptions
{
    /// <summary>
    /// Tamaño esperado de la hoja original usada por el proyecto.
    /// </summary>
    public int ExpectedSheetWidth { get; init; } = 418;

    public int ExpectedSheetHeight { get; init; } = 736;

    /// <summary>
    /// Cantidad de poses detectadas en la hoja de referencia de Aladdin.
    /// </summary>
    public int ExpectedTargetFrameCount { get; init; } = 102;

    /// <summary>
    /// Exige que todas las poses objetivo tengan una asignación.
    /// </summary>
    public bool RequireAllFramesMapped { get; init; } = true;

    /// <summary>
    /// Cantidad máxima de colores visibles permitida por pose antes de
    /// necesitar reducción de paleta. SNES 4BPP dispone de 16 índices;
    /// normalmente uno se reserva para transparencia.
    /// </summary>
    public int MaximumVisibleColorsPerFrame { get; init; } = 15;

    /// <summary>
    /// Límites básicos para detectar iconos, letras o fragmentos pequeños.
    /// </summary>
    public int MinimumSourceFrameWidth { get; init; } = 10;

    public int MinimumSourceFrameHeight { get; init; } = 14;

    public int MinimumSourceVisiblePixels { get; init; } = 80;

    /// <summary>
    /// Tolerancia usada para reconocer el color de fondo del lienzo final.
    /// </summary>
    public int BackgroundTolerance { get; init; } = 20;

    public int AlphaThreshold { get; init; } = 16;

    /// <summary>
    /// Cuando es verdadero, se genera también la hoja convertida para
    /// revisar dimensiones, transparencia, colores y regiones.
    /// </summary>
    public bool RenderConvertedSheet { get; init; } = true;
}

/// <summary>
/// Resultado completo de la validación.
/// </summary>
public sealed class RomCompatibilityValidationResult
{
    private readonly List<RomValidationIssue> _issues = new();

    internal RomCompatibilityValidationResult(
        int targetFrameCount,
        int mappedFrameCount,
        int pendingFrameCount)
    {
        TargetFrameCount =
            targetFrameCount;

        MappedFrameCount =
            mappedFrameCount;

        PendingFrameCount =
            pendingFrameCount;
    }

    public int TargetFrameCount { get; }

    public int MappedFrameCount { get; }

    public int PendingFrameCount { get; }

    public int ErrorCount =>
        _issues.Count(
            issue =>
                issue.Severity ==
                RomValidationSeverity.Error);

    public int WarningCount =>
        _issues.Count(
            issue =>
                issue.Severity ==
                RomValidationSeverity.Warning);

    public int InformationCount =>
        _issues.Count(
            issue =>
                issue.Severity ==
                RomValidationSeverity.Information);

    public IReadOnlyList<RomValidationIssue> Issues =>
        _issues;

    /// <summary>
    /// Indica que la hoja puede pasar a la siguiente etapa técnica.
    /// No significa todavía que la ROM pueda modificarse sin estudiar
    /// sus tablas de animación, OAM, compresión y direcciones de tiles.
    /// </summary>
    public bool CanProceedToTilePreparation =>
        ErrorCount == 0;

    public string StatusText =>
        CanProceedToTilePreparation
            ? WarningCount == 0
                ? "APTA PARA PREPARAR TILES"
                : "APTA CON ADVERTENCIAS"
            : "NO APTA";

    internal void AddIssue(
        RomValidationIssue issue)
    {
        ArgumentNullException.ThrowIfNull(issue);

        _issues.Add(
            issue);
    }

    public string CreateTextReport()
    {
        List<string> lines =
        [
            "ALADDIN SPRITE STUDIO",
            "VALIDACIÓN DE COMPATIBILIDAD GRÁFICA PARA ROM",
            "",
            $"Fecha: {DateTime.Now:yyyy-MM-dd HH:mm:ss}",
            "",
            "RESUMEN",
            "-------",
            $"Poses objetivo: {TargetFrameCount}",
            $"Poses asignadas: {MappedFrameCount}",
            $"Poses pendientes: {PendingFrameCount}",
            $"Errores: {ErrorCount}",
            $"Advertencias: {WarningCount}",
            $"Información: {InformationCount}",
            $"Estado: {StatusText}",
            "",
            "OBSERVACIONES",
            "-------------"
        ];

        if (_issues.Count == 0)
        {
            lines.Add(
                "No se encontraron observaciones.");
        }
        else
        {
            foreach (RomValidationIssue issue
                     in _issues
                         .OrderByDescending(
                             item => item.Severity)
                         .ThenBy(
                             item =>
                                 item.TargetFrameIndex ??
                                 int.MaxValue)
                         .ThenBy(
                             item => item.Code))
            {
                lines.Add(
                    issue.ToString());
            }
        }

        lines.Add("");
        lines.Add("NOTA TÉCNICA");
        lines.Add("------------");
        lines.Add(
            "Este informe valida la preparación visual y estructural " +
            "de la hoja. La inserción real en la ROM requiere todavía " +
            "identificar la paleta utilizada, el orden de tiles, las " +
            "tablas de animación, la composición OAM y cualquier " +
            "compresión aplicada por el juego.");

        return string.Join(
            Environment.NewLine,
            lines);
    }
}

/// <summary>
/// Valida una preparación gráfica conservando la hoja y las regiones
/// objetivo originales de Aladdin.
/// </summary>
public static class RomCompatibilityValidator
{
    public static RomCompatibilityValidationResult Validate(
        CharacterReplacementProject project,
        SpriteSheetDocument targetDocument,
        RomCompatibilityValidationOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(project);
        ArgumentNullException.ThrowIfNull(targetDocument);

        options ??=
            new RomCompatibilityValidationOptions();

        ValidateOptions(
            options);

        int targetFrameCount =
            targetDocument.Frames.Count;

        int mappedFrameCount =
            project.MappingCount;

        int pendingFrameCount =
            project.GetUnmappedTargetCount();

        RomCompatibilityValidationResult result =
            new(
                targetFrameCount,
                mappedFrameCount,
                pendingFrameCount);

        ValidateSheetDimensions(
            targetDocument,
            options,
            result);

        ValidateTargetFrameCount(
            targetDocument,
            options,
            result);

        ValidateMappingCompleteness(
            project,
            targetDocument,
            options,
            result);

        ValidateMappings(
            project,
            targetDocument,
            options,
            result);

        if (options.RenderConvertedSheet &&
            project.MappingCount > 0)
        {
            ValidateRenderedSheet(
                project,
                targetDocument,
                options,
                result);
        }

        AddTechnicalScopeInformation(
            result);

        return result;
    }

    private static void ValidateOptions(
        RomCompatibilityValidationOptions options)
    {
        if (options.ExpectedSheetWidth <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(options.ExpectedSheetWidth));
        }

        if (options.ExpectedSheetHeight <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(options.ExpectedSheetHeight));
        }

        if (options.ExpectedTargetFrameCount <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(options.ExpectedTargetFrameCount));
        }

        if (options.MaximumVisibleColorsPerFrame < 1 ||
            options.MaximumVisibleColorsPerFrame > 255)
        {
            throw new ArgumentOutOfRangeException(
                nameof(options.MaximumVisibleColorsPerFrame));
        }

        if (options.MinimumSourceFrameWidth < 1 ||
            options.MinimumSourceFrameHeight < 1 ||
            options.MinimumSourceVisiblePixels < 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(options.MinimumSourceFrameWidth),
                "Los límites mínimos deben ser mayores que cero.");
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

    private static void ValidateSheetDimensions(
        SpriteSheetDocument targetDocument,
        RomCompatibilityValidationOptions options,
        RomCompatibilityValidationResult result)
    {
        if (targetDocument.Width ==
                options.ExpectedSheetWidth &&
            targetDocument.Height ==
                options.ExpectedSheetHeight)
        {
            result.AddIssue(
                new RomValidationIssue(
                    "SHEET_SIZE_OK",
                    RomValidationSeverity.Information,
                    "Tamaño de hoja",
                    $"La hoja conserva " +
                    $"{targetDocument.Width} × " +
                    $"{targetDocument.Height} píxeles."));

            return;
        }

        result.AddIssue(
            new RomValidationIssue(
                "SHEET_SIZE_MISMATCH",
                RomValidationSeverity.Error,
                "Tamaño de hoja incorrecto",
                $"Se esperaba " +
                $"{options.ExpectedSheetWidth} × " +
                $"{options.ExpectedSheetHeight}, " +
                $"pero la hoja mide " +
                $"{targetDocument.Width} × " +
                $"{targetDocument.Height}."));
    }

    private static void ValidateTargetFrameCount(
        SpriteSheetDocument targetDocument,
        RomCompatibilityValidationOptions options,
        RomCompatibilityValidationResult result)
    {
        if (targetDocument.Frames.Count ==
            options.ExpectedTargetFrameCount)
        {
            result.AddIssue(
                new RomValidationIssue(
                    "FRAME_COUNT_OK",
                    RomValidationSeverity.Information,
                    "Cantidad de poses",
                    $"Se conservaron las " +
                    $"{targetDocument.Frames.Count} " +
                    "poses objetivo esperadas."));

            return;
        }

        result.AddIssue(
            new RomValidationIssue(
                "FRAME_COUNT_MISMATCH",
                RomValidationSeverity.Error,
                "Cantidad de poses alterada",
                $"Se esperaban " +
                $"{options.ExpectedTargetFrameCount} poses, " +
                $"pero actualmente existen " +
                $"{targetDocument.Frames.Count}."));
    }

    private static void ValidateMappingCompleteness(
        CharacterReplacementProject project,
        SpriteSheetDocument targetDocument,
        RomCompatibilityValidationOptions options,
        RomCompatibilityValidationResult result)
    {
        int pendingCount =
            project.GetUnmappedTargetCount();

        if (pendingCount == 0)
        {
            result.AddIssue(
                new RomValidationIssue(
                    "ALL_FRAMES_MAPPED",
                    RomValidationSeverity.Information,
                    "Asignaciones completas",
                    "Todas las poses objetivo tienen una " +
                    "pose fuente asignada."));

            return;
        }

        RomValidationSeverity severity =
            options.RequireAllFramesMapped
                ? RomValidationSeverity.Error
                : RomValidationSeverity.Warning;

        result.AddIssue(
            new RomValidationIssue(
                "PENDING_FRAMES",
                severity,
                "Poses pendientes",
                $"Quedan {pendingCount} poses de Aladdin " +
                "sin reemplazar. En esas animaciones todavía " +
                "aparecería el personaje original."));

        foreach (SpriteFrame frame
                 in targetDocument.Frames
                     .Where(frame =>
                         project.GetMapping(frame) is null)
                     .OrderBy(frame => frame.Index))
        {
            result.AddIssue(
                new RomValidationIssue(
                    "PENDING_FRAME",
                    severity,
                    "Pose sin asignación",
                    "Esta región todavía conserva a Aladdin.",
                    frame.Index));
        }
    }

    private static void ValidateMappings(
        CharacterReplacementProject project,
        SpriteSheetDocument targetDocument,
        RomCompatibilityValidationOptions options,
        RomCompatibilityValidationResult result)
    {
        HashSet<SpriteFrame> seenTargets =
            new();

        foreach (FrameMapping mapping
                 in project.Mappings
                     .OrderBy(
                         item =>
                             item.TargetFrame.Index))
        {
            int targetIndex =
                mapping.TargetFrame.Index;

            if (!ReferenceEquals(
                    mapping.TargetDocument,
                    targetDocument))
            {
                result.AddIssue(
                    new RomValidationIssue(
                        "FOREIGN_TARGET_DOCUMENT",
                        RomValidationSeverity.Error,
                        "Documento objetivo inválido",
                        "La asignación apunta a otra hoja objetivo.",
                        targetIndex));
            }

            if (!targetDocument.Frames.Contains(
                    mapping.TargetFrame))
            {
                result.AddIssue(
                    new RomValidationIssue(
                        "MISSING_TARGET_FRAME",
                        RomValidationSeverity.Error,
                        "Pose objetivo inválida",
                        "La pose objetivo ya no pertenece a la " +
                        "hoja de Aladdin cargada.",
                        targetIndex));
            }

            if (!mapping.SourceDocument.Frames.Contains(
                    mapping.SourceFrame))
            {
                result.AddIssue(
                    new RomValidationIssue(
                        "MISSING_SOURCE_FRAME",
                        RomValidationSeverity.Error,
                        "Pose fuente inválida",
                        "La pose fuente ya no pertenece a su hoja.",
                        targetIndex));
            }

            if (!seenTargets.Add(
                    mapping.TargetFrame))
            {
                result.AddIssue(
                    new RomValidationIssue(
                        "DUPLICATE_TARGET_MAPPING",
                        RomValidationSeverity.Error,
                        "Asignación duplicada",
                        "La misma pose objetivo aparece más de una vez.",
                        targetIndex));
            }

            ValidateSourceFrame(
                mapping,
                options,
                result);

            ValidateDestinationBounds(
                mapping,
                result);

            ValidateOrientation(
                mapping,
                result);
        }
    }

    private static void ValidateSourceFrame(
        FrameMapping mapping,
        RomCompatibilityValidationOptions options,
        RomCompatibilityValidationResult result)
    {
        SpriteFrame sourceFrame =
            mapping.SourceFrame;

        bool suspicious =
            sourceFrame.Bounds.Width <
                options.MinimumSourceFrameWidth ||
            sourceFrame.Bounds.Height <
                options.MinimumSourceFrameHeight ||
            sourceFrame.PixelCount <
                options.MinimumSourceVisiblePixels;

        if (!suspicious)
        {
            return;
        }

        result.AddIssue(
            new RomValidationIssue(
                "SUSPICIOUS_SOURCE_FRAME",
                RomValidationSeverity.Warning,
                "Pose fuente sospechosa",
                $"La fuente mide " +
                $"{sourceFrame.Bounds.Width} × " +
                $"{sourceFrame.Bounds.Height} píxeles y contiene " +
                $"{sourceFrame.PixelCount} píxeles visibles. " +
                "Podría ser un icono, una letra o un fragmento.",
                mapping.TargetFrame.Index));
    }

    private static void ValidateDestinationBounds(
        FrameMapping mapping,
        RomCompatibilityValidationResult result)
    {
        System.Drawing.Rectangle destination =
            mapping.GetDestinationBounds();

        System.Drawing.Rectangle available =
            new(
                0,
                0,
                mapping.TargetFrame.Bounds.Width,
                mapping.TargetFrame.Bounds.Height);

        if (available.Contains(
                destination))
        {
            return;
        }

        result.AddIssue(
            new RomValidationIssue(
                "DESTINATION_OVERFLOW",
                RomValidationSeverity.Error,
                "Pose fuera de su región",
                $"El resultado ocupa X={destination.X}, " +
                $"Y={destination.Y}, " +
                $"{destination.Width} × {destination.Height}, " +
                $"pero la región disponible es " +
                $"{available.Width} × {available.Height}. " +
                "Se perderían píxeles o se invadirían tiles vecinos.",
                mapping.TargetFrame.Index));
    }

    private static void ValidateOrientation(
        FrameMapping mapping,
        RomCompatibilityValidationResult result)
    {
        if (mapping.FlipVertical)
        {
            result.AddIssue(
                new RomValidationIssue(
                    "VERTICAL_FLIP",
                    RomValidationSeverity.Warning,
                    "Volteo vertical activo",
                    "El modo conservador para ROM recomienda no " +
                    "usar volteo vertical salvo que sea intencional.",
                    mapping.TargetFrame.Index));
        }

        if (!mapping.AutoFit)
        {
            result.AddIssue(
                new RomValidationIssue(
                    "MANUAL_SCALE",
                    RomValidationSeverity.Information,
                    "Escala manual",
                    $"La pose utiliza escala manual " +
                    $"{mapping.Scale:0.00}. Revise que no quede " +
                    "cortada ni demasiado pequeña.",
                    mapping.TargetFrame.Index));
        }
    }

    private static void ValidateRenderedSheet(
        CharacterReplacementProject project,
        SpriteSheetDocument targetDocument,
        RomCompatibilityValidationOptions options,
        RomCompatibilityValidationResult result)
    {
        using System.Drawing.Bitmap convertedSheet =
            MappedSpriteSheetRenderer.Render(
                targetDocument,
                project.Mappings,
                preserveUnmappedTargetFrames: true);

        if (convertedSheet.Width !=
                targetDocument.Width ||
            convertedSheet.Height !=
                targetDocument.Height)
        {
            result.AddIssue(
                new RomValidationIssue(
                    "RENDERED_SIZE_MISMATCH",
                    RomValidationSeverity.Error,
                    "Tamaño renderizado incorrecto",
                    $"La imagen convertida mide " +
                    $"{convertedSheet.Width} × " +
                    $"{convertedSheet.Height}, " +
                    "por lo que ya no coincide con la hoja objetivo."));

            return;
        }

        result.AddIssue(
            new RomValidationIssue(
                "RENDERED_SIZE_OK",
                RomValidationSeverity.Information,
                "Tamaño renderizado",
                "La imagen convertida conserva exactamente " +
                "las dimensiones de la hoja objetivo."));

        ValidateRenderedTransparency(
            convertedSheet,
            options,
            result);

        ValidateMappedFrameColors(
            convertedSheet,
            project,
            targetDocument,
            options,
            result);
    }

    private static void ValidateRenderedTransparency(
        System.Drawing.Bitmap convertedSheet,
        RomCompatibilityValidationOptions options,
        RomCompatibilityValidationResult result)
    {
        long transparentPixelCount =
            0;

        for (int y = 0;
             y < convertedSheet.Height;
             y++)
        {
            for (int x = 0;
                 x < convertedSheet.Width;
                 x++)
            {
                if (convertedSheet.GetPixel(
                        x,
                        y).A <
                    options.AlphaThreshold)
                {
                    transparentPixelCount++;
                }
            }
        }

        if (transparentPixelCount == 0)
        {
            result.AddIssue(
                new RomValidationIssue(
                    "OPAQUE_BACKGROUND",
                    RomValidationSeverity.Information,
                    "Fondo opaco",
                    "La hoja no contiene transparencia real. " +
                    "Durante la codificación deberá reservarse " +
                    "el índice 0 de la paleta para el fondo."));

            return;
        }

        result.AddIssue(
            new RomValidationIssue(
                "ALPHA_PRESENT",
                RomValidationSeverity.Warning,
                "Transparencia presente",
                $"La imagen contiene " +
                $"{transparentPixelCount:N0} píxeles transparentes. " +
                "La conversión SNES deberá normalizarlos al índice " +
                "transparente de la paleta."));
    }

    private static void ValidateMappedFrameColors(
        System.Drawing.Bitmap convertedSheet,
        CharacterReplacementProject project,
        SpriteSheetDocument targetDocument,
        RomCompatibilityValidationOptions options,
        RomCompatibilityValidationResult result)
    {
        System.Drawing.Color background =
            targetDocument.BackgroundColor;

        int framesNeedingReduction =
            0;

        foreach (FrameMapping mapping
                 in project.Mappings)
        {
            System.Drawing.Rectangle region =
                System.Drawing.Rectangle.Intersect(
                    new System.Drawing.Rectangle(
                        System.Drawing.Point.Empty,
                        convertedSheet.Size),
                    mapping.TargetFrame.Bounds);

            HashSet<int> colors =
                new();

            for (int y = region.Top;
                 y < region.Bottom;
                 y++)
            {
                for (int x = region.Left;
                     x < region.Right;
                     x++)
                {
                    System.Drawing.Color pixel =
                        convertedSheet.GetPixel(
                            x,
                            y);

                    if (pixel.A <
                        options.AlphaThreshold)
                    {
                        continue;
                    }

                    if (ColorDistance(
                            pixel,
                            background) <=
                        options.BackgroundTolerance)
                    {
                        continue;
                    }

                    colors.Add(
                        pixel.ToArgb());
                }
            }

            if (colors.Count <=
                options.MaximumVisibleColorsPerFrame)
            {
                continue;
            }

            framesNeedingReduction++;

            result.AddIssue(
                new RomValidationIssue(
                    "PALETTE_REDUCTION_REQUIRED",
                    RomValidationSeverity.Warning,
                    "Reducción de colores necesaria",
                    $"La pose contiene {colors.Count} colores " +
                    $"visibles; el objetivo conservador es " +
                    $"{options.MaximumVisibleColorsPerFrame}. " +
                    "Debe cuantizarse antes de generar 4BPP.",
                    mapping.TargetFrame.Index));
        }

        if (framesNeedingReduction == 0)
        {
            result.AddIssue(
                new RomValidationIssue(
                    "FRAME_COLOR_LIMIT_OK",
                    RomValidationSeverity.Information,
                    "Colores por pose",
                    $"Todas las poses asignadas se mantienen en " +
                    $"{options.MaximumVisibleColorsPerFrame} colores " +
                    "visibles o menos."));
        }
        else
        {
            result.AddIssue(
                new RomValidationIssue(
                    "COLOR_REDUCTION_SUMMARY",
                    RomValidationSeverity.Warning,
                    "Resumen de paleta",
                    $"{framesNeedingReduction} poses necesitan " +
                    "reducción de colores antes de convertirse " +
                    "a SNES 4BPP."));
        }
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

    private static void AddTechnicalScopeInformation(
        RomCompatibilityValidationResult result)
    {
        result.AddIssue(
            new RomValidationIssue(
                "ROM_STRUCTURE_PENDING",
                RomValidationSeverity.Information,
                "Estructura interna pendiente",
                "La validación visual no confirma todavía los " +
                "offsets definitivos, el orden de tiles, la paleta, " +
                "las tablas de animación, OAM ni la compresión " +
                "utilizada por la ROM."));
    }
}
