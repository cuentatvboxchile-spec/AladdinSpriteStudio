using AladdinSpriteStudio.UI.Graphics;

namespace AladdinSpriteStudio.UI.Controls;

/// <summary>
/// Información del tile seleccionado en la hoja.
/// </summary>
public sealed class TileSelectedEventArgs : EventArgs
{
    public TileSelectedEventArgs(
        int tileIndex,
        int offset)
    {
        TileIndex = tileIndex;
        Offset = offset;
    }

    public int TileIndex { get; }

    public int Offset { get; }
}

/// <summary>
/// Muestra varios tiles SNES de 8 × 8 píxeles
/// organizados en una cuadrícula.
/// </summary>
public sealed class TileSheetViewer : Control
{
    private const int TileWidth = 8;
    private const int TileHeight = 8;

    private byte[]? _romData;
    private int _startOffset;
    private int _selectedOffset = -1;

    private int _columns = 16;
    private int _rows = 16;
    private int _zoom = 4;

    private bool _showTileGrid = true;

    private Color[] _palette =
        CreateDefaultPalette();

    public TileSheetViewer()
    {
        SetStyle(
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.UserPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.ResizeRedraw |
            ControlStyles.Selectable,
            true);

        BackColor = Color.FromArgb(
            35,
            35,
            38);

        ForeColor = Color.White;

        Cursor = Cursors.Hand;

        UpdateControlSize();
    }

    /// <summary>
    /// Se produce cuando el usuario selecciona un tile.
    /// </summary>
    public event EventHandler<TileSelectedEventArgs>?
        TileSelected;

    public byte[]? RomData
    {
        get => _romData;

        set
        {
            _romData = value;
            _selectedOffset = -1;

            if (_romData is null ||
                _romData.Length <
                Decoder4Bpp.BytesPerTile)
            {
                _startOffset = 0;
            }
            else
            {
                int maximumOffset =
                    _romData.Length -
                    Decoder4Bpp.BytesPerTile;

                if (_startOffset > maximumOffset)
                {
                    _startOffset =
                        AlignOffset(maximumOffset);
                }
            }

            Invalidate();
        }
    }

    public int StartOffset
    {
        get => _startOffset;

        set
        {
            if (value < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(value),
                    "El offset no puede ser negativo.");
            }

            _startOffset =
                AlignOffset(value);

            int pageEndOffset =
                _startOffset +
                BytesPerPage;

            if (_selectedOffset < _startOffset ||
                _selectedOffset >= pageEndOffset)
            {
                _selectedOffset = -1;
            }

            Invalidate();
        }
    }

    public int SelectedOffset =>
        _selectedOffset;

    public int Columns
    {
        get => _columns;

        set
        {
            if (value is < 1 or > 64)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(value),
                    "Las columnas deben estar entre 1 y 64.");
            }

            _columns = value;

            UpdateControlSize();
            Invalidate();
        }
    }

    public int Rows
    {
        get => _rows;

        set
        {
            if (value is < 1 or > 64)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(value),
                    "Las filas deben estar entre 1 y 64.");
            }

            _rows = value;

            UpdateControlSize();
            Invalidate();
        }
    }

    public int Zoom
    {
        get => _zoom;

        set
        {
            if (value is < 1 or > 16)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(value),
                    "El zoom debe estar entre 1 y 16.");
            }

            _zoom = value;

            UpdateControlSize();
            Invalidate();
        }
    }

    public bool ShowTileGrid
    {
        get => _showTileGrid;

        set
        {
            _showTileGrid = value;
            Invalidate();
        }
    }

    public Color[] Palette
    {
        get => (Color[])_palette.Clone();

        set
        {
            ArgumentNullException.ThrowIfNull(value);

            if (value.Length !=
                SnesPalette.ColorsPerPalette)
            {
                throw new ArgumentException(
                    "La paleta debe contener exactamente 16 colores.",
                    nameof(value));
            }

            _palette =
                (Color[])value.Clone();

            Invalidate();
        }
    }

    public int TilesPerPage =>
        _columns * _rows;

    public int BytesPerPage =>
        TilesPerPage *
        Decoder4Bpp.BytesPerTile;

    public void ResetPalette()
    {
        _palette =
            CreateDefaultPalette();

        Invalidate();
    }

    protected override void OnMouseDown(
        MouseEventArgs e)
    {
        base.OnMouseDown(e);

        if (e.Button != MouseButtons.Left ||
            _romData is null)
        {
            return;
        }

        int tileVisualWidth =
            TileWidth * _zoom;

        int tileVisualHeight =
            TileHeight * _zoom;

        int column =
            e.X / tileVisualWidth;

        int row =
            e.Y / tileVisualHeight;

        if (column < 0 ||
            column >= _columns ||
            row < 0 ||
            row >= _rows)
        {
            return;
        }

        int tileIndex =
            row * _columns +
            column;

        int tileOffset =
            _startOffset +
            tileIndex *
            Decoder4Bpp.BytesPerTile;

        if (tileOffset >
            _romData.Length -
            Decoder4Bpp.BytesPerTile)
        {
            return;
        }

        _selectedOffset =
            tileOffset;

        Invalidate();

        TileSelected?.Invoke(
            this,
            new TileSelectedEventArgs(
                tileIndex,
                tileOffset));
    }

    protected override void OnPaint(
        PaintEventArgs e)
    {
        base.OnPaint(e);

        e.Graphics.Clear(BackColor);

        if (_romData is null)
        {
            DrawCenteredMessage(
                e.Graphics,
                "Sin ROM cargada");

            return;
        }

        if (_romData.Length <
            Decoder4Bpp.BytesPerTile)
        {
            DrawCenteredMessage(
                e.Graphics,
                "La ROM no contiene datos suficientes");

            return;
        }

        SolidBrush[] brushes =
            CreatePaletteBrushes();

        using Pen tileGridPen =
            new(Color.FromArgb(
                100,
                100,
                100));

        using Pen selectedTilePen =
            new(Color.Gold, 3);

        try
        {
            for (int tileIndex = 0;
                 tileIndex < TilesPerPage;
                 tileIndex++)
            {
                int tileOffset =
                    _startOffset +
                    tileIndex *
                    Decoder4Bpp.BytesPerTile;

                if (tileOffset >
                    _romData.Length -
                    Decoder4Bpp.BytesPerTile)
                {
                    break;
                }

                byte[,] pixels =
                    Decoder4Bpp.DecodeTile(
                        _romData,
                        tileOffset);

                int tileColumn =
                    tileIndex % _columns;

                int tileRow =
                    tileIndex / _columns;

                int tileX =
                    tileColumn *
                    TileWidth *
                    _zoom;

                int tileY =
                    tileRow *
                    TileHeight *
                    _zoom;

                DrawTile(
                    e.Graphics,
                    pixels,
                    brushes,
                    tileX,
                    tileY);

                Rectangle tileRectangle =
                    new(
                        tileX,
                        tileY,
                        TileWidth * _zoom - 1,
                        TileHeight * _zoom - 1);

                if (_showTileGrid)
                {
                    e.Graphics.DrawRectangle(
                        tileGridPen,
                        tileRectangle);
                }

                if (tileOffset ==
                    _selectedOffset)
                {
                    e.Graphics.DrawRectangle(
                        selectedTilePen,
                        tileRectangle);
                }
            }
        }
        finally
        {
            foreach (SolidBrush brush
                     in brushes)
            {
                brush.Dispose();
            }
        }
    }

    private void DrawTile(
        System.Drawing.Graphics graphics,
        byte[,] pixels,
        SolidBrush[] brushes,
        int tileX,
        int tileY)
    {
        for (int row = 0;
             row < TileHeight;
             row++)
        {
            for (int column = 0;
                 column < TileWidth;
                 column++)
            {
                byte colorIndex =
                    pixels[row, column];

                int safeColorIndex =
                    Math.Min(
                        colorIndex,
                        (byte)15);

                Rectangle pixelRectangle =
                    new(
                        tileX +
                        column * _zoom,

                        tileY +
                        row * _zoom,

                        _zoom,
                        _zoom);

                graphics.FillRectangle(
                    brushes[safeColorIndex],
                    pixelRectangle);
            }
        }
    }

    private SolidBrush[] CreatePaletteBrushes()
    {
        SolidBrush[] brushes =
            new SolidBrush[
                SnesPalette.ColorsPerPalette];

        for (int index = 0;
             index < brushes.Length;
             index++)
        {
            brushes[index] =
                new SolidBrush(
                    _palette[index]);
        }

        return brushes;
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

    private void UpdateControlSize()
    {
        int width =
            _columns *
            TileWidth *
            _zoom;

        int height =
            _rows *
            TileHeight *
            _zoom;

        Size = new Size(
            width + 1,
            height + 1);
    }

    private static int AlignOffset(
        int offset)
    {
        return offset -
               offset %
               Decoder4Bpp.BytesPerTile;
    }

    private static Color[] CreateDefaultPalette()
    {
        Color[] colors =
            new Color[
                SnesPalette.ColorsPerPalette];

        for (int index = 0;
             index < colors.Length;
             index++)
        {
            int intensity =
                index * 255 /
                (colors.Length - 1);

            colors[index] =
                Color.FromArgb(
                    intensity,
                    intensity,
                    intensity);
        }

        return colors;
    }
}