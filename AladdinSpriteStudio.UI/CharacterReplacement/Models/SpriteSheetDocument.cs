using System.Drawing.Imaging;

namespace AladdinSpriteStudio.UI.CharacterReplacement.Models;

/// <summary>
/// Representa una hoja de sprites cargada desde una imagen.
/// </summary>
public sealed class SpriteSheetDocument :
    IDisposable
{
    private bool _disposed;

    private SpriteSheetDocument(
        string filePath,
        Bitmap image,
        Color backgroundColor)
    {
        FilePath = filePath;
        Image = image;
        BackgroundColor = backgroundColor;
    }

    public string FilePath { get; }

    public string FileName =>
        Path.GetFileName(FilePath);

    public Bitmap Image { get; }

    public Color BackgroundColor { get; set; }

    public List<SpriteFrame> Frames { get; } =
        new();

    public int Width =>
        Image.Width;

    public int Height =>
        Image.Height;

    /// <summary>
    /// Carga la imagen sin mantener bloqueado el archivo original.
    /// </summary>
    public static SpriteSheetDocument Load(
        string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException(
                "Debe indicar la ruta de la hoja de sprites.",
                nameof(filePath));
        }

        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException(
                "No se encontró la hoja de sprites.",
                filePath);
        }

        using Bitmap source =
            new(filePath);

        Bitmap image =
            new(
                source.Width,
                source.Height,
                PixelFormat.Format32bppArgb);

        using (System.Drawing.Graphics graphics =
               System.Drawing.Graphics.FromImage(image))
        {
            graphics.Clear(
                Color.Transparent);

            graphics.DrawImageUnscaled(
                source,
                0,
                0);
        }

        Color backgroundColor =
            image.GetPixel(
                0,
                0);

        return new SpriteSheetDocument(
            filePath,
            image,
            backgroundColor);
    }

    public void ReplaceFrames(
        IEnumerable<SpriteFrame> frames)
    {
        ArgumentNullException.ThrowIfNull(
            frames);

        Frames.Clear();
        Frames.AddRange(frames);

        ReindexFrames();
    }

    public bool RemoveFrame(
        SpriteFrame frame)
    {
        ArgumentNullException.ThrowIfNull(
            frame);

        bool removed =
            Frames.Remove(frame);

        if (removed)
        {
            ReindexFrames();
        }

        return removed;
    }

    public void ReindexFrames()
    {
        for (int index = 0;
             index < Frames.Count;
             index++)
        {
            SpriteFrame frame =
                Frames[index];

            frame.Index = index;

            if (string.IsNullOrWhiteSpace(frame.Name) ||
                frame.Name.StartsWith(
                    "Pose ",
                    StringComparison.OrdinalIgnoreCase))
            {
                frame.Name =
                    $"Pose {index + 1:000}";
            }
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        Image.Dispose();

        _disposed = true;

        GC.SuppressFinalize(this);
    }
}