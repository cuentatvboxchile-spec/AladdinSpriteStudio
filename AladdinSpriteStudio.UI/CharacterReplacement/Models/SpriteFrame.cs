namespace AladdinSpriteStudio.UI.CharacterReplacement.Models;

/// <summary>
/// Representa una pose detectada dentro de una hoja de sprites.
/// </summary>
public sealed class SpriteFrame
{
    public SpriteFrame(
        int index,
        Rectangle bounds,
        int pixelCount)
    {
        if (index < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(index));
        }

        if (bounds.Width <= 0 ||
            bounds.Height <= 0)
        {
            throw new ArgumentException(
                "El rectángulo de la pose no es válido.",
                nameof(bounds));
        }

        if (pixelCount < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(pixelCount));
        }

        Index = index;
        Bounds = bounds;
        PixelCount = pixelCount;
        Name = $"Pose {index + 1:000}";
        IsEnabled = true;
    }

    public int Index { get; internal set; }

    public string Name { get; set; }

    public Rectangle Bounds { get; set; }

    public int PixelCount { get; set; }

    public bool IsEnabled { get; set; }

    public Point DefaultAnchor =>
        new(
            Bounds.Left + Bounds.Width / 2,
            Bounds.Bottom - 1);

    public override string ToString()
    {
        return
            $"{Index + 1:000} - {Name} " +
            $"({Bounds.Width} × {Bounds.Height})";
    }
}