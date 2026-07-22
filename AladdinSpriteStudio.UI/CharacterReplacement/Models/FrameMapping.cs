namespace AladdinSpriteStudio.UI.CharacterReplacement.Models;

/// <summary>
/// Relaciona una pose objetivo de Aladdin
/// con una pose del personaje sustituto.
/// </summary>
public sealed class FrameMapping
{
    private float _scale = 1f;

    public FrameMapping(
        SpriteSheetDocument targetDocument,
        SpriteFrame targetFrame,
        SpriteSheetDocument sourceDocument,
        SpriteFrame sourceFrame)
    {
        TargetDocument =
            targetDocument ??
            throw new ArgumentNullException(
                nameof(targetDocument));

        TargetFrame =
            targetFrame ??
            throw new ArgumentNullException(
                nameof(targetFrame));

        SourceDocument =
            sourceDocument ??
            throw new ArgumentNullException(
                nameof(sourceDocument));

        SourceFrame =
            sourceFrame ??
            throw new ArgumentNullException(
                nameof(sourceFrame));

        if (!TargetDocument.Frames.Contains(TargetFrame))
        {
            throw new ArgumentException(
                "La pose objetivo no pertenece a la hoja de Aladdin.",
                nameof(targetFrame));
        }

        if (!SourceDocument.Frames.Contains(SourceFrame))
        {
            throw new ArgumentException(
                "La pose fuente no pertenece a la hoja seleccionada.",
                nameof(sourceFrame));
        }
    }

    /// <summary>
    /// Hoja original de Aladdin.
    /// </summary>
    public SpriteSheetDocument TargetDocument { get; }

    /// <summary>
    /// Pose de Aladdin que será reemplazada.
    /// </summary>
    public SpriteFrame TargetFrame { get; }

    /// <summary>
    /// Hoja que contiene la pose del personaje nuevo.
    /// </summary>
    public SpriteSheetDocument SourceDocument { get; }

    /// <summary>
    /// Pose del personaje nuevo.
    /// </summary>
    public SpriteFrame SourceFrame { get; }

    /// <summary>
    /// Ajusta automáticamente la pose nueva
    /// dentro del espacio ocupado por Aladdin.
    /// </summary>
    public bool AutoFit { get; set; } = true;

    /// <summary>
    /// Escala manual, usada cuando AutoFit es falso.
    /// </summary>
    public float Scale
    {
        get => _scale;

        set
        {
            if (value is < 0.05f or > 16f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(value),
                    "La escala debe estar entre 0.05 y 16.");
            }

            _scale = value;
        }
    }

    /// <summary>
    /// Desplazamiento horizontal.
    /// </summary>
    public int OffsetX { get; set; }

    /// <summary>
    /// Desplazamiento vertical.
    /// </summary>
    public int OffsetY { get; set; }

    public bool FlipHorizontal { get; set; }

    public bool FlipVertical { get; set; }

    /// <summary>
    /// Escala que será usada realmente.
    /// </summary>
    public float EffectiveScale
    {
        get
        {
            if (!AutoFit)
            {
                return Scale;
            }

            float horizontalScale =
                TargetFrame.Bounds.Width /
                (float)SourceFrame.Bounds.Width;

            float verticalScale =
                TargetFrame.Bounds.Height /
                (float)SourceFrame.Bounds.Height;

            return Math.Min(
                horizontalScale,
                verticalScale);
        }
    }

    /// <summary>
    /// Calcula el tamaño final de la pose nueva.
    /// </summary>
    public Size GetScaledSourceSize()
    {
        float effectiveScale =
            EffectiveScale;

        int width =
            Math.Max(
                1,
                (int)Math.Round(
                    SourceFrame.Bounds.Width *
                    effectiveScale));

        int height =
            Math.Max(
                1,
                (int)Math.Round(
                    SourceFrame.Bounds.Height *
                    effectiveScale));

        return new Size(
            width,
            height);
    }

    /// <summary>
    /// Calcula dónde debe colocarse la pose nueva
    /// dentro del rectángulo objetivo de Aladdin.
    /// Se centra horizontalmente y se alinea por los pies.
    /// </summary>
    public Rectangle GetDestinationBounds()
    {
        Size scaledSize =
            GetScaledSourceSize();

        int targetWidth =
            TargetFrame.Bounds.Width;

        int targetHeight =
            TargetFrame.Bounds.Height;

        int x =
            (targetWidth -
             scaledSize.Width) /
            2 +
            OffsetX;

        int y =
            targetHeight -
            scaledSize.Height +
            OffsetY;

        return new Rectangle(
            x,
            y,
            scaledSize.Width,
            scaledSize.Height);
    }

    public void ResetAdjustments()
    {
        AutoFit = true;
        Scale = 1f;

        OffsetX = 0;
        OffsetY = 0;

        FlipHorizontal = false;
        FlipVertical = false;
    }

    public override string ToString()
    {
        return
            $"Aladdin {TargetFrame.Index + 1:000} ← " +
            $"{SourceDocument.FileName} / " +
            $"Pose {SourceFrame.Index + 1:000}";
    }
}