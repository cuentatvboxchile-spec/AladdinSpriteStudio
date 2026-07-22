using System.Drawing.Imaging;

namespace AladdinSpriteStudio.UI.CharacterReplacement.Models;

/// <summary>
/// Representa una hoja de sprites cargada desde una imagen.
/// También administra las regiones detectadas y el historial
/// de modificaciones.
/// </summary>
public sealed class SpriteSheetDocument :
    IDisposable
{
    private sealed class FrameSnapshot
    {
        public required int Index { get; init; }

        public required string Name { get; init; }

        public required Rectangle Bounds { get; init; }

        public required int PixelCount { get; init; }

        public required bool IsEnabled { get; init; }
    }

    private readonly Stack<List<FrameSnapshot>>
        _undoHistory = new();

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

    public bool CanUndo =>
        _undoHistory.Count > 0;

    /// <summary>
    /// Carga la imagen sin mantener bloqueado
    /// el archivo original.
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

    /// <summary>
    /// Reemplaza todas las poses detectadas.
    /// Se usa normalmente después de analizar nuevamente la hoja.
    /// </summary>
    public void ReplaceFrames(
        IEnumerable<SpriteFrame> frames)
    {
        ArgumentNullException.ThrowIfNull(
            frames);

        Frames.Clear();
        Frames.AddRange(frames);

        _undoHistory.Clear();

        ReindexFrames();
    }

    /// <summary>
    /// Agrega manualmente una región nueva.
    /// </summary>
    public SpriteFrame AddFrame(
        Rectangle bounds)
    {
        Rectangle validBounds =
            Rectangle.Intersect(
                bounds,
                new Rectangle(
                    0,
                    0,
                    Image.Width,
                    Image.Height));

        if (validBounds.Width < 2 ||
            validBounds.Height < 2)
        {
            throw new ArgumentException(
                "La región debe medir al menos 2 × 2 píxeles.",
                nameof(bounds));
        }

        SaveUndoState();

        int pixelCount =
            CountVisiblePixels(
                validBounds);

        SpriteFrame frame =
            new(
                Frames.Count,
                validBounds,
                pixelCount);

        Frames.Add(
            frame);

        ReindexFrames();

        return frame;
    }

    /// <summary>
    /// Elimina una sola región.
    /// </summary>
    public bool RemoveFrame(
        SpriteFrame frame)
    {
        ArgumentNullException.ThrowIfNull(
            frame);

        return RemoveFrames(
            new[] { frame });
    }

    /// <summary>
    /// Elimina varias regiones al mismo tiempo.
    /// </summary>
    public bool RemoveFrames(
    IEnumerable<SpriteFrame> frames)
    {
        ArgumentNullException.ThrowIfNull(
            frames);

        // Guardamos los índices porque las referencias
        // pueden cambiar después de ordenar o combinar regiones.
        int[] indexesToRemove =
            frames
                .Where(frame =>
                    frame is not null)
                .Select(frame =>
                    frame.Index)
                .Where(index =>
                    index >= 0 &&
                    index < Frames.Count)
                .Distinct()
                .OrderByDescending(index =>
                    index)
                .ToArray();

        if (indexesToRemove.Length == 0)
        {
            return false;
        }

        SaveUndoState();

        // Se eliminan desde el índice más alto para evitar
        // que los demás índices cambien durante el proceso.
        foreach (int index in indexesToRemove)
        {
            Frames.RemoveAt(
                index);
        }

        ReindexFrames();

        return true;
    }

    /// <summary>
    /// Combina varias regiones en un único rectángulo.
    /// </summary>
    public SpriteFrame? MergeFrames(
        IEnumerable<SpriteFrame> frames)
    {
        ArgumentNullException.ThrowIfNull(
            frames);

        List<SpriteFrame> framesToMerge =
            frames
                .Where(frame =>
                    Frames.Contains(frame))
                .Distinct()
                .OrderBy(frame =>
                    frame.Index)
                .ToList();

        if (framesToMerge.Count < 2)
        {
            return null;
        }

        SaveUndoState();

        Rectangle mergedBounds =
            framesToMerge[0].Bounds;

        int insertIndex =
            framesToMerge[0].Index;

        foreach (SpriteFrame frame
                 in framesToMerge.Skip(1))
        {
            mergedBounds =
                Rectangle.Union(
                    mergedBounds,
                    frame.Bounds);
        }

        foreach (SpriteFrame frame
                 in framesToMerge)
        {
            Frames.Remove(
                frame);
        }

        int pixelCount =
            CountVisiblePixels(
                mergedBounds);

        SpriteFrame mergedFrame =
            new(
                insertIndex,
                mergedBounds,
                pixelCount)
            {
                Name =
                    $"Pose combinada"
            };

        insertIndex =
            Math.Clamp(
                insertIndex,
                0,
                Frames.Count);

        Frames.Insert(
            insertIndex,
            mergedFrame);

        ReindexFrames();

        return mergedFrame;
    }

    /// <summary>
    /// Ordena las poses por filas, de izquierda a derecha.
    /// </summary>
    public void SortFramesByRows(
        int rowTolerance = 10)
    {
        if (Frames.Count < 2)
        {
            return;
        }

        if (rowTolerance < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(rowTolerance));
        }

        SaveUndoState();

        List<SpriteFrame> pending =
            Frames
                .OrderBy(frame =>
                    frame.Bounds.Top)
                .ThenBy(frame =>
                    frame.Bounds.Left)
                .ToList();

        List<SpriteFrame> ordered =
            new();

        while (pending.Count > 0)
        {
            SpriteFrame first =
                pending[0];

            int rowReference =
                first.Bounds.Top;

            List<SpriteFrame> currentRow =
                pending
                    .Where(frame =>
                        Math.Abs(
                            frame.Bounds.Top -
                            rowReference) <=
                        rowTolerance)
                    .OrderBy(frame =>
                        frame.Bounds.Left)
                    .ToList();

            ordered.AddRange(
                currentRow);

            foreach (SpriteFrame frame
                     in currentRow)
            {
                pending.Remove(
                    frame);
            }
        }

        Frames.Clear();
        Frames.AddRange(
            ordered);

        ReindexFrames();
    }

    /// <summary>
    /// Restaura el estado anterior de las regiones.
    /// </summary>
    public bool UndoLastChange()
    {
        if (_undoHistory.Count == 0)
        {
            return false;
        }

        List<FrameSnapshot> previousState =
            _undoHistory.Pop();

        Frames.Clear();

        foreach (FrameSnapshot snapshot
                 in previousState)
        {
            SpriteFrame frame =
                new(
                    snapshot.Index,
                    snapshot.Bounds,
                    snapshot.PixelCount)
                {
                    Name =
                        snapshot.Name,

                    IsEnabled =
                        snapshot.IsEnabled
                };

            Frames.Add(
                frame);
        }

        ReindexFrames();

        return true;
    }

    public void ReindexFrames()
    {
        for (int index = 0;
             index < Frames.Count;
             index++)
        {
            SpriteFrame frame =
                Frames[index];

            string previousAutomaticName =
                $"Pose {frame.Index + 1:000}";

            frame.Index =
                index;

            if (string.IsNullOrWhiteSpace(
                    frame.Name) ||
                frame.Name.StartsWith(
                    "Pose ",
                    StringComparison.OrdinalIgnoreCase) ||
                string.Equals(
                    frame.Name,
                    previousAutomaticName,
                    StringComparison.OrdinalIgnoreCase))
            {
                frame.Name =
                    $"Pose {index + 1:000}";
            }
        }
    }

    private void SaveUndoState()
    {
        List<FrameSnapshot> snapshot =
            Frames
                .Select(frame =>
                    new FrameSnapshot
                    {
                        Index =
                            frame.Index,

                        Name =
                            frame.Name,

                        Bounds =
                            frame.Bounds,

                        PixelCount =
                            frame.PixelCount,

                        IsEnabled =
                            frame.IsEnabled
                    })
                .ToList();

        _undoHistory.Push(
            snapshot);
    }

    private int CountVisiblePixels(
        Rectangle bounds)
    {
        int pixelCount =
            0;

        for (int y = bounds.Top;
             y < bounds.Bottom;
             y++)
        {
            for (int x = bounds.Left;
                 x < bounds.Right;
                 x++)
            {
                Color color =
                    Image.GetPixel(
                        x,
                        y);

                if (color.A < 16)
                {
                    continue;
                }

                bool isBackground =
                    Math.Abs(
                        color.R -
                        BackgroundColor.R) <= 18
                    &&
                    Math.Abs(
                        color.G -
                        BackgroundColor.G) <= 18
                    &&
                    Math.Abs(
                        color.B -
                        BackgroundColor.B) <= 18;

                if (!isBackground)
                {
                    pixelCount++;
                }
            }
        }

        return pixelCount;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        Image.Dispose();

        _undoHistory.Clear();

        _disposed = true;

        GC.SuppressFinalize(
            this);
    }
}