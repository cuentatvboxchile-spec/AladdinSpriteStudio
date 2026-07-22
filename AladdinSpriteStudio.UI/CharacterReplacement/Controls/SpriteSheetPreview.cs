using System.Drawing.Drawing2D;
using AladdinSpriteStudio.UI.CharacterReplacement.Models;

namespace AladdinSpriteStudio.UI.CharacterReplacement.Controls;

public sealed class SpriteFrameSelectedEventArgs : EventArgs
{
    public SpriteFrameSelectedEventArgs(SpriteFrame frame)
    {
        Frame = frame ??
            throw new ArgumentNullException(nameof(frame));
    }

    public SpriteFrame Frame { get; }
}

public sealed class SpriteFrameSelectionChangedEventArgs : EventArgs
{
    public SpriteFrameSelectionChangedEventArgs(
        IReadOnlyList<SpriteFrame> selectedFrames)
    {
        SelectedFrames = selectedFrames;
    }

    public IReadOnlyList<SpriteFrame> SelectedFrames { get; }
}

public sealed class SpriteRegionCreatedEventArgs : EventArgs
{
    public SpriteRegionCreatedEventArgs(Rectangle bounds)
    {
        Bounds = bounds;
    }

    public Rectangle Bounds { get; }
}

/// <summary>
/// Muestra una hoja de sprites, permite seleccionar
/// varias poses, crear regiones manualmente y desplazarse
/// verticalmente cuando la hoja es más alta que el panel.
/// </summary>
public sealed class SpriteSheetPreview :
    System.Windows.Forms.Control
{
    private SpriteSheetDocument? _document;

    private readonly HashSet<SpriteFrame> _selectedFrames =
        new();

    private RectangleF _imageRectangle =
        RectangleF.Empty;

    private bool _createRegionMode;
    private bool _isDraggingRegion;

    private Point _dragStartImagePoint;
    private Point _dragCurrentImagePoint;

    private ScrollableControl? _scrollParent;

    public SpriteSheetPreview()
    {
        SetStyle(
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.UserPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.ResizeRedraw |
            ControlStyles.Selectable,
            true);

        BackColor = Color.FromArgb(32, 32, 35);
        ForeColor = Color.White;
        Cursor = Cursors.Hand;
        TabStop = true;
    }

    public event EventHandler<SpriteFrameSelectedEventArgs>?
        FrameSelected;

    public event EventHandler<SpriteFrameSelectionChangedEventArgs>?
        SelectionChanged;

    public event EventHandler<SpriteRegionCreatedEventArgs>?
        RegionCreated;

    public SpriteSheetDocument? Document
    {
        get => _document;

        set
        {
            _document = value;

            ClearSelection();

            _isDraggingRegion = false;

            UpdateDisplaySize();
            Invalidate();
        }
    }

    /// <summary>
    /// Primera región seleccionada.
    /// Se mantiene por compatibilidad con el resto del programa.
    /// </summary>
    public SpriteFrame? SelectedFrame
    {
        get =>
            _selectedFrames
                .OrderBy(frame => frame.Index)
                .FirstOrDefault();

        set
        {
            _selectedFrames.Clear();

            if (value is not null)
            {
                _selectedFrames.Add(value);
            }

            RaiseSelectionChanged();
            Invalidate();
        }
    }

    public IReadOnlyList<SpriteFrame> SelectedFrames =>
        _selectedFrames
            .OrderBy(frame => frame.Index)
            .ToArray();

    public bool ShowFrameNumbers { get; set; } = true;

    /// <summary>
    /// Activa la creación manual de una región
    /// mediante el arrastre del mouse.
    /// </summary>
    public bool CreateRegionMode
    {
        get => _createRegionMode;

        set
        {
            _createRegionMode = value;
            _isDraggingRegion = false;

            Cursor = value
                ? Cursors.Cross
                : Cursors.Hand;

            Invalidate();
        }
    }

    public void ClearSelection()
    {
        if (_selectedFrames.Count == 0)
        {
            return;
        }

        _selectedFrames.Clear();

        RaiseSelectionChanged();
        Invalidate();
    }

    protected override void OnParentChanged(EventArgs e)
    {
        if (_scrollParent is not null)
        {
            _scrollParent.Resize -=
                ScrollParent_Resize;
        }

        base.OnParentChanged(e);

        _scrollParent =
            Parent as ScrollableControl;

        if (_scrollParent is not null)
        {
            _scrollParent.Resize +=
                ScrollParent_Resize;
        }

        UpdateDisplaySize();
    }

    private void ScrollParent_Resize(
        object? sender,
        EventArgs e)
    {
        UpdateDisplaySize();
    }

    /// <summary>
    /// Ajusta la hoja al ancho disponible.
    /// La altura conserva la proporción original,
    /// provocando que aparezca la barra vertical.
    /// </summary>
    private void UpdateDisplaySize()
    {
        if (_scrollParent is null)
        {
            return;
        }

        int availableWidth = Math.Max(
            150,
            _scrollParent.ClientSize.Width -
            SystemInformation.VerticalScrollBarWidth -
            8);

        int desiredHeight;

        if (_document is null)
        {
            desiredHeight = Math.Max(
                300,
                _scrollParent.ClientSize.Height);
        }
        else
        {
            double scale =
                availableWidth /
                (double)_document.Image.Width;

            desiredHeight = Math.Max(
                1,
                (int)Math.Ceiling(
                    _document.Image.Height * scale));
        }

        Dock = DockStyle.None;
        Location = Point.Empty;

        Size = new Size(
            availableWidth,
            desiredHeight);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        e.Graphics.Clear(BackColor);

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
            new(Color.LimeGreen, 1.5f);

        using Pen selectedPen =
            new(Color.Gold, 3f);

        using SolidBrush labelBackground =
            new(Color.FromArgb(210, 0, 0, 0));

        foreach (SpriteFrame frame in _document.Frames)
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
                _selectedFrames.Contains(frame)
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

        DrawManualRegionSelection(
            e.Graphics,
            scaleX,
            scaleY);
    }

    protected override void OnMouseDown(MouseEventArgs e)
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

        Point imagePoint =
            ConvertControlPointToImage(
                e.Location);

        if (_createRegionMode)
        {
            _isDraggingRegion = true;

            _dragStartImagePoint =
                imagePoint;

            _dragCurrentImagePoint =
                imagePoint;

            Capture = true;

            Invalidate();
            return;
        }

        SelectFrameAtPoint(
            imagePoint);
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);

        if (!_isDraggingRegion ||
            _document is null)
        {
            return;
        }

        _dragCurrentImagePoint =
            ConvertControlPointToImage(
                e.Location,
                clampToImage: true);

        Invalidate();
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);

        if (e.Button != MouseButtons.Left ||
            !_isDraggingRegion ||
            _document is null)
        {
            return;
        }

        _dragCurrentImagePoint =
            ConvertControlPointToImage(
                e.Location,
                clampToImage: true);

        _isDraggingRegion = false;
        Capture = false;

        Rectangle bounds =
            CreateRectangleFromPoints(
                _dragStartImagePoint,
                _dragCurrentImagePoint);

        CreateRegionMode = false;

        if (bounds.Width >= 2 &&
            bounds.Height >= 2)
        {
            RegionCreated?.Invoke(
                this,
                new SpriteRegionCreatedEventArgs(
                    bounds));
        }

        Invalidate();
    }

    protected override void OnMouseWheel(MouseEventArgs e)
    {
        if (_scrollParent is null ||
            !_scrollParent.VerticalScroll.Visible)
        {
            base.OnMouseWheel(e);
            return;
        }

        int currentPosition =
            -_scrollParent.AutoScrollPosition.Y;

        int maximumPosition = Math.Max(
            0,
            _scrollParent.VerticalScroll.Maximum -
            _scrollParent.VerticalScroll.LargeChange +
            1);

        int movement =
            e.Delta > 0
                ? -120
                : 120;

        int newPosition =
            Math.Clamp(
                currentPosition + movement,
                0,
                maximumPosition);

        _scrollParent.AutoScrollPosition =
            new Point(
                0,
                newPosition);
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        if (e.KeyCode == Keys.Escape)
        {
            CreateRegionMode = false;
            _isDraggingRegion = false;
            Capture = false;

            Invalidate();

            e.Handled = true;
        }
    }

    private void SelectFrameAtPoint(Point imagePoint)
    {
        if (_document is null)
        {
            return;
        }

        SpriteFrame? selected =
            _document.Frames
                .Where(frame =>
                    frame.IsEnabled &&
                    frame.Bounds.Contains(
                        imagePoint))
                .OrderBy(frame =>
                    frame.Bounds.Width *
                    frame.Bounds.Height)
                .FirstOrDefault();

        bool controlPressed =
            (ModifierKeys & Keys.Control) ==
            Keys.Control;

        if (selected is null)
        {
            if (!controlPressed)
            {
                ClearSelection();
            }

            return;
        }

        if (!controlPressed)
        {
            _selectedFrames.Clear();
            _selectedFrames.Add(selected);
        }
        else if (_selectedFrames.Contains(selected))
        {
            _selectedFrames.Remove(selected);
        }
        else
        {
            _selectedFrames.Add(selected);
        }

        RaiseSelectionChanged();
        Invalidate();

        if (_selectedFrames.Contains(selected))
        {
            FrameSelected?.Invoke(
                this,
                new SpriteFrameSelectedEventArgs(
                    selected));
        }
    }

    private void RaiseSelectionChanged()
    {
        SelectionChanged?.Invoke(
            this,
            new SpriteFrameSelectionChangedEventArgs(
                SelectedFrames));
    }

    private void DrawManualRegionSelection(
        System.Drawing.Graphics graphics,
        float scaleX,
        float scaleY)
    {
        if (!_isDraggingRegion)
        {
            return;
        }

        Rectangle imageRectangle =
            CreateRectangleFromPoints(
                _dragStartImagePoint,
                _dragCurrentImagePoint);

        RectangleF controlRectangle =
            ConvertImageRectangleToControl(
                imageRectangle,
                scaleX,
                scaleY);

        using Pen selectionPen =
            new(Color.DeepSkyBlue, 2f)
            {
                DashStyle =
                    DashStyle.Dash
            };

        using SolidBrush selectionBrush =
            new(Color.FromArgb(
                45,
                Color.DeepSkyBlue));

        graphics.FillRectangle(
            selectionBrush,
            controlRectangle);

        graphics.DrawRectangle(
            selectionPen,
            controlRectangle.X,
            controlRectangle.Y,
            controlRectangle.Width,
            controlRectangle.Height);
    }

    private Point ConvertControlPointToImage(
        Point controlPoint,
        bool clampToImage = false)
    {
        if (_document is null ||
            _imageRectangle.IsEmpty)
        {
            return Point.Empty;
        }

        float scaleX =
            _imageRectangle.Width /
            _document.Image.Width;

        float scaleY =
            _imageRectangle.Height /
            _document.Image.Height;

        int imageX =
            (int)(
                (controlPoint.X -
                 _imageRectangle.Left) /
                scaleX);

        int imageY =
            (int)(
                (controlPoint.Y -
                 _imageRectangle.Top) /
                scaleY);

        if (clampToImage)
        {
            imageX = Math.Clamp(
                imageX,
                0,
                _document.Image.Width - 1);

            imageY = Math.Clamp(
                imageY,
                0,
                _document.Image.Height - 1);
        }

        return new Point(
            imageX,
            imageY);
    }

    private static Rectangle CreateRectangleFromPoints(
        Point first,
        Point second)
    {
        int left =
            Math.Min(
                first.X,
                second.X);

        int top =
            Math.Min(
                first.Y,
                second.Y);

        int right =
            Math.Max(
                first.X,
                second.X) + 1;

        int bottom =
            Math.Max(
                first.Y,
                second.Y) + 1;

        return Rectangle.FromLTRB(
            left,
            top,
            right,
            bottom);
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
            imageSize.Width * scale;

        float height =
            imageSize.Height * scale;

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

    protected override void Dispose(bool disposing)
    {
        if (disposing &&
            _scrollParent is not null)
        {
            _scrollParent.Resize -=
                ScrollParent_Resize;

            _scrollParent = null;
        }

        base.Dispose(disposing);
    }
}