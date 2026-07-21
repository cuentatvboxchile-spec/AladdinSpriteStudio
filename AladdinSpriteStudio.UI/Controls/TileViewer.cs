namespace AladdinSpriteStudio.UI.Controls;

/// <summary>
/// Muestra un tile SNES de 8 × 8 píxeles utilizando
/// índices de color comprendidos entre 0 y 15.
/// </summary>
public sealed class TileViewer : Control
{
    private static readonly Color[] DefaultPalette =
    [
        Color.Black,
        Color.FromArgb(32, 32, 32),
        Color.FromArgb(64, 64, 64),
        Color.FromArgb(80, 80, 80),
        Color.FromArgb(96, 96, 96),
        Color.FromArgb(112, 112, 112),
        Color.FromArgb(128, 128, 128),
        Color.FromArgb(144, 144, 144),
        Color.FromArgb(160, 160, 160),
        Color.FromArgb(176, 176, 176),
        Color.FromArgb(192, 192, 192),
        Color.FromArgb(208, 208, 208),
        Color.FromArgb(224, 224, 224),
        Color.FromArgb(232, 232, 232),
        Color.FromArgb(244, 244, 244),
        Color.White
    ];

    private byte[,]? _pixels;

    private Color[] _palette =
        (Color[])DefaultPalette.Clone();

    private int _zoom = 24;
    private bool _showGrid = true;

    public TileViewer()
    {
        SetStyle(
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.UserPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.ResizeRedraw,
            true);

        BackColor = Color.FromArgb(45, 45, 48);
        ForeColor = Color.White;

        UpdateControlSize();
    }

    /// <summary>
    /// Matriz de 8 × 8 que contiene índices de color de 0 a 15.
    /// </summary>
    public byte[,]? Pixels
    {
        get => _pixels;

        set
        {
            if (value is not null &&
                (value.GetLength(0) != 8 ||
                 value.GetLength(1) != 8))
            {
                throw new ArgumentException(
                    "El TileViewer necesita una matriz de 8 × 8.",
                    nameof(value));
            }

            _pixels = value is null
                ? null
                : (byte[,])value.Clone();

            Invalidate();
        }
    }

    /// <summary>
    /// Paleta de 16 colores utilizada para dibujar el tile.
    /// </summary>
    public Color[] Palette
    {
        get => (Color[])_palette.Clone();

        set
        {
            ArgumentNullException.ThrowIfNull(value);

            if (value.Length != 16)
            {
                throw new ArgumentException(
                    "La paleta debe contener exactamente 16 colores.",
                    nameof(value));
            }

            _palette = (Color[])value.Clone();

            Invalidate();
        }
    }

    /// <summary>
    /// Tamaño visual de cada píxel del tile.
    /// </summary>
    public int Zoom
    {
        get => _zoom;

        set
        {
            if (value is < 2 or > 64)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(value),
                    "El zoom debe estar entre 2 y 64.");
            }

            _zoom = value;

            UpdateControlSize();
            Invalidate();
        }
    }

    /// <summary>
    /// Indica si se dibujan líneas entre los píxeles.
    /// </summary>
    public bool ShowGrid
    {
        get => _showGrid;

        set
        {
            _showGrid = value;
            Invalidate();
        }
    }

    /// <summary>
    /// Restablece la paleta provisional en escala de grises.
    /// </summary>
    public void ResetPalette()
    {
        _palette =
            (Color[])DefaultPalette.Clone();

        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        if (_pixels is null)
        {
            TextRenderer.DrawText(
                e.Graphics,
                "Sin tile",
                Font,
                ClientRectangle,
                ForeColor,
                TextFormatFlags.HorizontalCenter |
                TextFormatFlags.VerticalCenter);

            return;
        }

        for (int row = 0; row < 8; row++)
        {
            for (int column = 0; column < 8; column++)
            {
                byte colorIndex =
                    _pixels[row, column];

                Color color =
                    _palette[Math.Min(
                        colorIndex,
                        (byte)15)];

                using SolidBrush brush =
                    new(color);

                Rectangle pixelRectangle = new(
                    column * _zoom,
                    row * _zoom,
                    _zoom,
                    _zoom);

                e.Graphics.FillRectangle(
                    brush,
                    pixelRectangle);

                if (_showGrid)
                {
                    e.Graphics.DrawRectangle(
                        Pens.DimGray,
                        pixelRectangle);
                }
            }
        }
    }

    private void UpdateControlSize()
    {
        int tileSize = 8 * _zoom;

        Size = new Size(
            tileSize + 1,
            tileSize + 1);
    }
}