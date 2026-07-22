using System.Drawing.Drawing2D;
using AladdinSpriteStudio.UI.CharacterReplacement.Models;

namespace AladdinSpriteStudio.UI.CharacterReplacement.Controls;

/// <summary>
/// Información de la pose seleccionada.
/// </summary>
public sealed class SpriteFrameSelectedEventArgs :
    EventArgs
{
    public SpriteFrameSelectedEventArgs(
        SpriteFrame frame)
    {
        Frame =
            frame ??
            throw new ArgumentNullException(
                nameof(frame));
    }

    public SpriteFrame Frame { get; }
}

/// <summary>
/// Control que muestra una hoja de sprites y las regiones detectadas.
/// </summary>
public sealed class SpriteSheetPreview :
    System.Windows.Forms.Control
{
    private SpriteSheetDocument? _document;
    private SpriteFrame? _selectedFrame;

    private RectangleF _imageRectangle =
        RectangleF.Empty;

    public SpriteSheetPreview()
    {
        SetStyle(
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.UserPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.ResizeRedraw |
            ControlStyles.Selectable,
            true);

        BackColor =
            Color.FromArgb(
                32,
                32,
                35);

        ForeColor =
            Color.White;

        Cursor =
            Cursors.Hand;

        TabStop =
            true;
    }

    public event EventHandler<
        SpriteFrameSelectedEventArgs>?
        FrameSelected;

    public SpriteSheetDocument? Document
    {
        get => _document;

        set
        {
            _document = value;
            _selectedFrame = null;

            Invalidate();
        }
    }

    public SpriteFrame? SelectedFrame
    {
        get => _selectedFrame;

        set
        {
            _selectedFrame = value;

            Invalidate();
        }
    }

    public bool ShowFrameNumbers { get; set; } =
        true;

    protected override void OnPaint(
        PaintEventArgs e)
    {
        base.OnPaint(e);

        e.Graphics.Clear(
            BackColor);

        if (_document is null)
        {
            _imageRectangle =
                RectangleF.Empty;

            DrawCenteredMessage(
                e.Graphics,
                "Cargue una hoja de sprites");

            return;
        }

        _imageRectangle =
            CalculateImageRectangle(
                _document.Image.Size,
                ClientSize);

        if (_imageRectangle.IsEmpty)
        {
            return;
        }

        e.Graphics.InterpolationMode =
            InterpolationMode.NearestNeighbor;

        e.Graphics.PixelOffsetMode =
            PixelOffsetMode.Half;

        e.Graphics.SmoothingMode =
            SmoothingMode.None;

        e.Graphics.DrawImage(
            _document.Image,
            _imageRectangle);

        float scaleX =
            _imageRectangle.Width /
            _document.Image.Width;

        float scaleY =
            _imageRectangle.Height /
            _document.Image.Height;

        using Pen normalPen =
            new(
                Color.LimeGreen,
                1.5f);

        using Pen selectedPen =
            new(
                Color.Gold,
                3f);

        using SolidBrush labelBackground =
            new(
                Color.FromArgb(
                    210,
                    0,
                    0,
                    0));

        foreach (SpriteFrame frame
                 in _document.Frames)
        {
            if (!frame.IsEnabled)
            {
                continue;
            }

            RectangleF frameRectangle =
                ConvertImageRectangleToControl(
                    frame.Bounds,
                    scaleX,
                    scaleY);

            Pen currentPen =
                ReferenceEquals(
                    frame,
                    _selectedFrame)
                    ? selectedPen
                    : normalPen;

            e.Graphics.DrawRectangle(
                currentPen,
                frameRectangle.X,
                frameRectangle.Y,
                frameRectangle.Width,
                frameRectangle.Height);

            if (!ShowFrameNumbers)
            {
                continue;
            }

            string label =
                (frame.Index + 1)
                .ToString("000");

            Size labelSize =
                TextRenderer.MeasureText(
                    label,
                    Font);

            Rectangle labelRectangle =
                new(
                    (int)frameRectangle.X,
                    Math.Max(
                        0,
                        (int)frameRectangle.Y -
                        labelSize.Height),
                    labelSize.Width + 6,
                    labelSize.Height);

            e.Graphics.FillRectangle(
                labelBackground,
                labelRectangle);

            TextRenderer.DrawText(
                e.Graphics,
                label,
                Font,
                labelRectangle,
                Color.White,
                TextFormatFlags.HorizontalCenter |
                TextFormatFlags.VerticalCenter);
        }
    }

    protected override void OnMouseDown(
        MouseEventArgs e)
    {
        base.OnMouseDown(e);

        Focus();

        if (e.Button != MouseButtons.Left ||
            _document is null ||
            _imageRectangle.IsEmpty ||
            !_imageRectangle.Contains(e.Location))
        {
            return;
        }

        float scaleX =
            _imageRectangle.Width /
            _document.Image.Width;

        float scaleY =
            _imageRectangle.Height /
            _document.Image.Height;

        int imageX =
            (int)(
                (e.X - _imageRectangle.Left) /
                scaleX);

        int imageY =
            (int)(
                (e.Y - _imageRectangle.Top) /
                scaleY);

        SpriteFrame? selected =
            _document.Frames
                .Where(frame =>
                    frame.IsEnabled &&
                    frame.Bounds.Contains(
                        imageX,
                        imageY))
                .OrderBy(frame =>
                    frame.Bounds.Width *
                    frame.Bounds.Height)
                .FirstOrDefault();

        if (selected is null)
        {
            _selectedFrame = null;

            Invalidate();

            return;
        }

        _selectedFrame =
            selected;

        Invalidate();

        FrameSelected?.Invoke(
            this,
            new SpriteFrameSelectedEventArgs(
                selected));
    }

    private RectangleF ConvertImageRectangleToControl(
        Rectangle rectangle,
        float scaleX,
        float scaleY)
    {
        return new RectangleF(
            _imageRectangle.Left +
            rectangle.Left * scaleX,

            _imageRectangle.Top +
            rectangle.Top * scaleY,

            rectangle.Width * scaleX,

            rectangle.Height * scaleY);
    }

    private static RectangleF CalculateImageRectangle(
        Size imageSize,
        Size availableSize)
    {
        if (imageSize.Width <= 0 ||
            imageSize.Height <= 0 ||
            availableSize.Width <= 0 ||
            availableSize.Height <= 0)
        {
            return RectangleF.Empty;
        }

        float scale =
            Math.Min(
                availableSize.Width /
                (float)imageSize.Width,

                availableSize.Height /
                (float)imageSize.Height);

        float width =
            imageSize.Width *
            scale;

        float height =
            imageSize.Height *
            scale;

        float x =
            (availableSize.Width - width) /
            2f;

        float y =
            (availableSize.Height - height) /
            2f;

        return new RectangleF(
            x,
            y,
            width,
            height);
    }

    private void DrawCenteredMessage(
        System.Drawing.Graphics graphics,
        string message)
    {
        TextRenderer.DrawText(
            graphics,
            message,
            Font,
            ClientRectangle,
            ForeColor,
            TextFormatFlags.HorizontalCenter |
            TextFormatFlags.VerticalCenter);
    }
}