using System.Globalization;
using AladdinSpriteStudio.UI.CharacterReplacement.Conversion;
using AladdinSpriteStudio.UI.CharacterReplacement.Models;
using AladdinSpriteStudio.UI.CharacterReplacement.Rendering;
using AladdinSpriteStudio.UI.CharacterReplacement.RomTesting;

namespace AladdinSpriteStudio.UI.CharacterReplacement.UI;

/// <summary>
/// Editor experimental para comparar un segmento de la ROM con los tiles
/// de una pose nueva y reemplazar únicamente posiciones seleccionadas.
/// </summary>
public sealed class RomTileSegmentEditorForm
    : System.Windows.Forms.Form
{
    private enum TileOrientation
    {
        Normal,
        FlipHorizontal,
        FlipVertical,
        FlipBoth
    }

    private sealed class TileAssignment
    {
        public required int SourceTileIndex { get; init; }

        public required TileOrientation Orientation { get; init; }
    }

    private sealed class FrameItem
    {
        public required SpriteFrame Frame { get; init; }

        public override string ToString()
        {
            return
                $"Pose {Frame.Index + 1:000} | " +
                $"{Frame.Bounds.Width} × " +
                $"{Frame.Bounds.Height} px";
        }
    }

    private sealed class TileGridControl
        : System.Windows.Forms.Control
    {
        private IReadOnlyList<byte[]> _tiles =
            Array.Empty<byte[]>();

        private IReadOnlyList<System.Drawing.Color> _defaultPalette =
            CreateGrayPalette();

        private IReadOnlyList<System.Drawing.Color>? _replacementPalette;

        private HashSet<int> _replacementIndexes =
            new();

        private int _selectedIndex =
            -1;

        private int _columns =
            8;

        private int _zoom =
            4;

        public TileGridControl()
        {
            DoubleBuffered =
                true;

            BackColor =
                Color.FromArgb(
                    28,
                    28,
                    32);
        }

        public event EventHandler? SelectedIndexChanged;

        public IReadOnlyList<byte[]> Tiles
        {
            get => _tiles;

            set
            {
                _tiles =
                    value ??
                    Array.Empty<byte[]>();

                if (_selectedIndex >=
                    _tiles.Count)
                {
                    _selectedIndex =
                        -1;
                }

                UpdateDisplaySize();
                Invalidate();
            }
        }

        public IReadOnlyList<System.Drawing.Color> DefaultPalette
        {
            get => _defaultPalette;

            set
            {
                _defaultPalette =
                    value ??
                    CreateGrayPalette();

                Invalidate();
            }
        }

        public IReadOnlyList<System.Drawing.Color>? ReplacementPalette
        {
            get => _replacementPalette;

            set
            {
                _replacementPalette =
                    value;

                Invalidate();
            }
        }

        public IEnumerable<int> ReplacementIndexes
        {
            set
            {
                _replacementIndexes =
                    value is null
                        ? new HashSet<int>()
                        : value.ToHashSet();

                Invalidate();
            }
        }

        public int Columns
        {
            get => _columns;

            set
            {
                _columns =
                    Math.Max(
                        1,
                        value);

                UpdateDisplaySize();
                Invalidate();
            }
        }

        public int Zoom
        {
            get => _zoom;

            set
            {
                _zoom =
                    Math.Clamp(
                        value,
                        2,
                        10);

                UpdateDisplaySize();
                Invalidate();
            }
        }

        public int SelectedIndex
        {
            get => _selectedIndex;

            set
            {
                int normalized =
                    value >= 0 &&
                    value < _tiles.Count
                        ? value
                        : -1;

                if (_selectedIndex ==
                    normalized)
                {
                    return;
                }

                _selectedIndex =
                    normalized;

                Invalidate();

                SelectedIndexChanged?.Invoke(
                    this,
                    EventArgs.Empty);
            }
        }

        private int CellSize =>
            8 * _zoom + 10;

        private void UpdateDisplaySize()
        {
            int rows =
                _tiles.Count == 0
                    ? 1
                    : (int)Math.Ceiling(
                        _tiles.Count /
                        (double)_columns);

            Size =
                new Size(
                    Math.Max(
                        1,
                        _columns *
                        CellSize),
                    Math.Max(
                        1,
                        rows *
                        CellSize));
        }

        protected override void OnMouseDown(
            MouseEventArgs e)
        {
            base.OnMouseDown(e);

            int column =
                e.X /
                CellSize;

            int row =
                e.Y /
                CellSize;

            int index =
                row *
                _columns +
                column;

            SelectedIndex =
                index;
        }

        protected override void OnPaint(
            PaintEventArgs e)
        {
            base.OnPaint(e);

            e.Graphics.InterpolationMode =
                System.Drawing.Drawing2D.InterpolationMode
                    .NearestNeighbor;

            e.Graphics.PixelOffsetMode =
                System.Drawing.Drawing2D.PixelOffsetMode.Half;

            for (int index = 0;
                 index < _tiles.Count;
                 index++)
            {
                int column =
                    index %
                    _columns;

                int row =
                    index /
                    _columns;

                Rectangle cell =
                    new(
                        column *
                        CellSize,
                        row *
                        CellSize,
                        CellSize,
                        CellSize);

                DrawTile(
                    e.Graphics,
                    index,
                    cell);
            }
        }

        private void DrawTile(
            System.Drawing.Graphics graphics,
            int index,
            Rectangle cell)
        {
            bool isReplacement =
                _replacementIndexes.Contains(
                    index);

            IReadOnlyList<System.Drawing.Color> palette =
                isReplacement &&
                _replacementPalette is not null
                    ? _replacementPalette
                    : _defaultPalette;

            byte[] indices =
                DecodeTile4Bpp(
                    _tiles[index]);

            int imageSize =
                8 *
                _zoom;

            using System.Drawing.Bitmap tileBitmap =
                new(
                    8,
                    8,
                    System.Drawing.Imaging.PixelFormat
                        .Format32bppArgb);

            for (int y = 0;
                 y < 8;
                 y++)
            {
                for (int x = 0;
                     x < 8;
                     x++)
                {
                    int paletteIndex =
                        indices[
                            y *
                            8 +
                            x];

                    System.Drawing.Color color =
                        palette[
                            Math.Clamp(
                                paletteIndex,
                                0,
                                palette.Count - 1)];

                    tileBitmap.SetPixel(
                        x,
                        y,
                        paletteIndex == 0
                            ? Color.Transparent
                            : Color.FromArgb(
                                255,
                                color.R,
                                color.G,
                                color.B));
                }
            }

            Rectangle imageRectangle =
                new(
                    cell.X + 4,
                    cell.Y + 4,
                    imageSize,
                    imageSize);

            DrawCheckerboard(
                graphics,
                imageRectangle,
                Math.Max(
                    2,
                    _zoom *
                    2));

            graphics.DrawImage(
                tileBitmap,
                imageRectangle,
                new Rectangle(
                    0,
                    0,
                    8,
                    8),
                GraphicsUnit.Pixel);

            using Pen borderPen =
                new(
                    isReplacement
                        ? Color.LimeGreen
                        : Color.DimGray,
                    isReplacement
                        ? 2f
                        : 1f);

            graphics.DrawRectangle(
                borderPen,
                imageRectangle);

            if (index ==
                _selectedIndex)
            {
                using Pen selectionPen =
                    new(
                        Color.Yellow,
                        3f);

                graphics.DrawRectangle(
                    selectionPen,
                    Rectangle.Inflate(
                        imageRectangle,
                        2,
                        2));
            }

            string number =
                index.ToString(
                    "000",
                    CultureInfo.InvariantCulture);

            using Font font =
                new(
                    FontFamily.GenericMonospace,
                    Math.Max(
                        7f,
                        _zoom + 4f),
                    FontStyle.Bold);

            SizeF textSize =
                graphics.MeasureString(
                    number,
                    font);

            RectangleF labelRectangle =
                new(
                    cell.X +
                    (CellSize -
                     textSize.Width) /
                    2f,
                    cell.Bottom -
                    textSize.Height -
                    1,
                    textSize.Width,
                    textSize.Height);

            using Brush labelBack =
                new SolidBrush(
                    Color.FromArgb(
                        210,
                        0,
                        0,
                        0));

            using Brush labelBrush =
                new SolidBrush(
                    isReplacement
                        ? Color.LimeGreen
                        : Color.White);

            graphics.FillRectangle(
                labelBack,
                labelRectangle);

            graphics.DrawString(
                number,
                font,
                labelBrush,
                labelRectangle.Location);
        }

        private static void DrawCheckerboard(
            System.Drawing.Graphics graphics,
            Rectangle rectangle,
            int squareSize)
        {
            using Brush light =
                new SolidBrush(
                    Color.FromArgb(
                        72,
                        72,
                        76));

            using Brush dark =
                new SolidBrush(
                    Color.FromArgb(
                        50,
                        50,
                        54));

            for (int y = rectangle.Top;
                 y < rectangle.Bottom;
                 y += squareSize)
            {
                for (int x = rectangle.Left;
                     x < rectangle.Right;
                     x += squareSize)
                {
                    bool useLight =
                        ((x - rectangle.Left) /
                         squareSize +
                         (y - rectangle.Top) /
                         squareSize) %
                        2 ==
                        0;

                    graphics.FillRectangle(
                        useLight
                            ? light
                            : dark,
                        new Rectangle(
                            x,
                            y,
                            Math.Min(
                                squareSize,
                                rectangle.Right -
                                x),
                            Math.Min(
                                squareSize,
                                rectangle.Bottom -
                                y)));
                }
            }
        }

        private static byte[] DecodeTile4Bpp(
            IReadOnlyList<byte> tile)
        {
            if (tile.Count != 32)
            {
                throw new ArgumentException(
                    "Un tile SNES 4BPP debe contener 32 bytes.",
                    nameof(tile));
            }

            byte[] result =
                new byte[64];

            for (int row = 0;
                 row < 8;
                 row++)
            {
                byte plane0 =
                    tile[row * 2];

                byte plane1 =
                    tile[row * 2 + 1];

                byte plane2 =
                    tile[16 + row * 2];

                byte plane3 =
                    tile[16 + row * 2 + 1];

                for (int column = 0;
                     column < 8;
                     column++)
                {
                    int bit =
                        7 -
                        column;

                    result[
                        row *
                        8 +
                        column] =
                            (byte)(
                                ((plane0 >> bit) & 1) |
                                (((plane1 >> bit) & 1) << 1) |
                                (((plane2 >> bit) & 1) << 2) |
                                (((plane3 >> bit) & 1) << 3));
                }
            }

            return result;
        }

        private static IReadOnlyList<System.Drawing.Color>
            CreateGrayPalette()
        {
            List<System.Drawing.Color> palette =
                new()
                {
                    Color.Transparent
                };

            for (int index = 1;
                 index < 16;
                 index++)
            {
                int value =
                    20 +
                    index *
                    15;

                palette.Add(
                    Color.FromArgb(
                        255,
                        value,
                        value,
                        value));
            }

            return palette;
        }
    }

    /// <summary>
    /// Mini mapa de una página gráfica de 0x2000 bytes:
    /// 256 tiles SNES de 8 × 8, distribuidos en 16 × 16.
    /// </summary>
    private sealed class RomPageNavigatorControl
        : System.Windows.Forms.Control
    {
        private const int PageColumns = 16;
        private const int PageRows = 16;
        private const int PageTileCount = 256;

        private HashSet<int> _loadedTileIndexes =
            new();

        private HashSet<int> _modifiedTileIndexes =
            new();

        private int _selectedTileIndex =
            -1;

        public RomPageNavigatorControl()
        {
            DoubleBuffered =
                true;

            BackColor =
                Color.FromArgb(
                    28,
                    28,
                    32);

            MinimumSize =
                new Size(
                    260,
                    260);

            Size =
                new Size(
                    280,
                    280);
        }

        public event Action<int>? PageTileClicked;

        public IEnumerable<int> LoadedTileIndexes
        {
            set
            {
                _loadedTileIndexes =
                    value is null
                        ? new HashSet<int>()
                        : value
                            .Where(index =>
                                index >= 0 &&
                                index < PageTileCount)
                            .ToHashSet();

                Invalidate();
            }
        }

        public IEnumerable<int> ModifiedTileIndexes
        {
            set
            {
                _modifiedTileIndexes =
                    value is null
                        ? new HashSet<int>()
                        : value
                            .Where(index =>
                                index >= 0 &&
                                index < PageTileCount)
                            .ToHashSet();

                Invalidate();
            }
        }

        public int SelectedTileIndex
        {
            get =>
                _selectedTileIndex;

            set
            {
                _selectedTileIndex =
                    value >= 0 &&
                    value < PageTileCount
                        ? value
                        : -1;

                Invalidate();
            }
        }

        protected override void OnMouseDown(
            MouseEventArgs e)
        {
            base.OnMouseDown(e);

            Rectangle gridRectangle =
                GetGridRectangle();

            if (!gridRectangle.Contains(
                    e.Location))
            {
                return;
            }

            float cellWidth =
                gridRectangle.Width /
                (float)PageColumns;

            float cellHeight =
                gridRectangle.Height /
                (float)PageRows;

            int column =
                Math.Clamp(
                    (int)(
                        (e.X -
                         gridRectangle.Left) /
                        cellWidth),
                    0,
                    PageColumns - 1);

            int row =
                Math.Clamp(
                    (int)(
                        (e.Y -
                         gridRectangle.Top) /
                        cellHeight),
                    0,
                    PageRows - 1);

            int pageTileIndex =
                row *
                PageColumns +
                column;

            PageTileClicked?.Invoke(
                pageTileIndex);
        }

        protected override void OnPaint(
            PaintEventArgs e)
        {
            base.OnPaint(e);

            e.Graphics.SmoothingMode =
                System.Drawing.Drawing2D.SmoothingMode.None;

            Rectangle gridRectangle =
                GetGridRectangle();

            float cellWidth =
                gridRectangle.Width /
                (float)PageColumns;

            float cellHeight =
                gridRectangle.Height /
                (float)PageRows;

            using Brush emptyBrush =
                new SolidBrush(
                    Color.FromArgb(
                        42,
                        42,
                        47));

            using Brush loadedBrush =
                new SolidBrush(
                    Color.FromArgb(
                        75,
                        95,
                        125));

            using Brush modifiedBrush =
                new SolidBrush(
                    Color.FromArgb(
                        45,
                        145,
                        70));

            using Brush selectedBrush =
                new SolidBrush(
                    Color.FromArgb(
                        215,
                        175,
                        20));

            using Pen gridPen =
                new(
                    Color.FromArgb(
                        78,
                        78,
                        84));

            using Pen outerPen =
                new(
                    Color.White,
                    1f);

            for (int index = 0;
                 index < PageTileCount;
                 index++)
            {
                int column =
                    index %
                    PageColumns;

                int row =
                    index /
                    PageColumns;

                RectangleF cell =
                    new(
                        gridRectangle.Left +
                        column *
                        cellWidth,
                        gridRectangle.Top +
                        row *
                        cellHeight,
                        cellWidth,
                        cellHeight);

                Brush fillBrush =
                    index ==
                    _selectedTileIndex
                        ? selectedBrush
                        : _modifiedTileIndexes.Contains(
                            index)
                            ? modifiedBrush
                            : _loadedTileIndexes.Contains(
                                index)
                                ? loadedBrush
                                : emptyBrush;

                e.Graphics.FillRectangle(
                    fillBrush,
                    cell);

                e.Graphics.DrawRectangle(
                    gridPen,
                    cell.X,
                    cell.Y,
                    cell.Width,
                    cell.Height);
            }

            e.Graphics.DrawRectangle(
                outerPen,
                gridRectangle);

            DrawAxisLabels(
                e.Graphics,
                gridRectangle,
                cellWidth,
                cellHeight);
        }

        private Rectangle GetGridRectangle()
        {
            int marginLeft =
                24;

            int marginTop =
                24;

            int marginRight =
                6;

            int marginBottom =
                6;

            return new Rectangle(
                marginLeft,
                marginTop,
                Math.Max(
                    16,
                    ClientSize.Width -
                    marginLeft -
                    marginRight),
                Math.Max(
                    16,
                    ClientSize.Height -
                    marginTop -
                    marginBottom));
        }

        private static void DrawAxisLabels(
            System.Drawing.Graphics graphics,
            Rectangle gridRectangle,
            float cellWidth,
            float cellHeight)
        {
            using Font font =
                new(
                    FontFamily.GenericMonospace,
                    7f,
                    FontStyle.Regular);

            using Brush brush =
                new SolidBrush(
                    Color.Gainsboro);

            for (int column = 0;
                 column < PageColumns;
                 column++)
            {
                string text =
                    column.ToString(
                        "X1",
                        CultureInfo.InvariantCulture);

                float x =
                    gridRectangle.Left +
                    column *
                    cellWidth +
                    1;

                graphics.DrawString(
                    text,
                    font,
                    brush,
                    x,
                    4);
            }

            for (int row = 0;
                 row < PageRows;
                 row++)
            {
                string text =
                    row.ToString(
                        "X1",
                        CultureInfo.InvariantCulture);

                float y =
                    gridRectangle.Top +
                    row *
                    cellHeight;

                graphics.DrawString(
                    text,
                    font,
                    brush,
                    5,
                    y);
            }
        }
    }

    private readonly SnesSpritePaletteQuantizationResult
        _quantization;

    private readonly SpriteSheetDocument
        _targetDocument;

    private readonly string
        _suggestedBaseName;

    private readonly IReadOnlyList<System.Drawing.Color>
        _grayscaleRomPalette;

    private readonly IReadOnlyList<System.Drawing.Color>
        _aladdinRomPalette;

    private readonly ComboBox
        _romPaletteModeCombo;

    private readonly ComboBox
        _romLayoutModeCombo;

    private readonly CheckBox
        _synchronizeSelectionCheckBox;

    private bool
        _synchronizingSelection;

    private readonly ComboBox
        _frameCombo;

    private readonly TextBox
        _romPathTextBox;

    private readonly Button
        _browseRomButton;

    private readonly TextBox
        _offsetTextBox;

    private readonly CheckBox
        _headerlessOffsetCheckBox;

    private readonly NumericUpDown
        _segmentTileCountInput;

    private readonly Button
        _loadSegmentButton;

    private readonly TextBox
        _outputPathTextBox;

    private readonly Button
        _browseOutputButton;

    private readonly TileGridControl
        _originalGrid;

    private readonly TileGridControl
        _sourceGrid;

    private readonly RomPageNavigatorControl
        _romPageNavigator;

    private readonly Label
        _romLocationLabel;

    private readonly Label
        _selectionLabel;

    private readonly Label
        _summaryLabel;

    private readonly ComboBox
        _orientationCombo;

    private readonly TileGridControl
        _orientationPreviewGrid;

    private readonly Button
        _assignTileButton;

    private readonly Button
        _restoreTileButton;

    private readonly Button
        _assignLinearButton;

    private readonly Button
        _clearChangesButton;

    private readonly Button
        _createRomButton;

    private readonly Button
        _closeButton;

    private SnesPoseTileEncodingResult?
        _poseEncoding;

    private List<byte[]>
        _originalSegmentTiles =
            new();

    private List<byte[]>
        _workingSegmentTiles =
            new();

    private readonly Dictionary<int, TileAssignment>
        _sourceTileByDestination =
            new();

    private int
        _logicalSegmentOffset;

    private int
        _actualSegmentOffset;

    private bool
        _copierHeaderDetected;

    public RomTileSegmentEditorForm(
        SnesSpritePaletteQuantizationResult quantization,
        SpriteSheetDocument targetDocument,
        SpriteFrame? initialFrame,
        string suggestedBaseName)
    {
        _quantization =
            quantization ??
            throw new ArgumentNullException(
                nameof(quantization));

        _targetDocument =
            targetDocument ??
            throw new ArgumentNullException(
                nameof(targetDocument));

        _suggestedBaseName =
            SanitizeFileName(
                suggestedBaseName);

        _grayscaleRomPalette =
            TilePreviewPaletteProvider
                .CreateGrayscalePalette();

        _aladdinRomPalette =
            TilePreviewPaletteProvider
                .CreateAladdinPreviewPalette(
                    _targetDocument);

        Text =
            "Editor tile por tile para copia de ROM";

        Width =
            1500;

        Height =
            900;

        MinimumSize =
            new Size(
                1100,
                700);

        StartPosition =
            FormStartPosition.CenterParent;

        ShowInTaskbar =
            false;

        BackColor =
            SystemColors.Control;

        _romPaletteModeCombo =
            new ComboBox
            {
                DropDownStyle =
                    ComboBoxStyle.DropDownList,

                Width =
                    230
            };

        _romPaletteModeCombo.Items.AddRange(
            new object[]
            {
                "Paleta visual de Aladdin",
                "Escala de grises"
            });

        _romPaletteModeCombo.SelectedIndex =
            0;

        _romPaletteModeCombo.SelectedIndexChanged +=
            (_, _) =>
            {
                ApplyRomPreviewPalette();
                UpdateInformation();
            };

        _romLayoutModeCombo =
            new ComboBox
            {
                DropDownStyle =
                    ComboBoxStyle.DropDownList,

                Width =
                    250
            };

        _romLayoutModeCombo.Items.AddRange(
            new object[]
            {
                "Misma cuadrícula que la pose",
                "Orden físico ROM (8 columnas)"
            });

        _romLayoutModeCombo.SelectedIndex =
            0;

        _romLayoutModeCombo.SelectedIndexChanged +=
            (_, _) =>
            {
                ApplyGridLayout();
                UpdateInformation();
            };

        _synchronizeSelectionCheckBox =
            new CheckBox
            {
                Text =
                    "Sincronizar selección por número",

                Checked =
                    true,

                AutoSize =
                    true,

                Margin =
                    new Padding(
                        10,
                        6,
                        3,
                        3)
            };

        _frameCombo =
            new ComboBox
            {
                DropDownStyle =
                    ComboBoxStyle.DropDownList,

                Width =
                    430
            };

        foreach (SpriteFrame frame
                 in _targetDocument.Frames
                     .OrderBy(item => item.Index))
        {
            _frameCombo.Items.Add(
                new FrameItem
                {
                    Frame =
                        frame
                });
        }

        _frameCombo.SelectedIndexChanged +=
            FrameCombo_SelectedIndexChanged;

        _romPathTextBox =
            new TextBox
            {
                Width =
                    500,

                ReadOnly =
                    true
            };

        _browseRomButton =
            CreateButton(
                "Seleccionar ROM original");

        _offsetTextBox =
            new TextBox
            {
                Text =
                    "40000",

                Width =
                    130,

                CharacterCasing =
                    CharacterCasing.Upper
            };

        _headerlessOffsetCheckBox =
            new CheckBox
            {
                Text =
                    "Offset sin encabezado de 0x200",

                Checked =
                    true,

                AutoSize =
                    true
            };

        _segmentTileCountInput =
            new NumericUpDown
            {
                Minimum =
                    1,

                Maximum =
                    512,

                Value =
                    32,

                Width =
                    90
            };

        _loadSegmentButton =
            CreateButton(
                "Cargar segmento");

        _outputPathTextBox =
            new TextBox
            {
                Width =
                    500,

                ReadOnly =
                    true
            };

        _browseOutputButton =
            CreateButton(
                "Elegir copia de salida");

        _originalGrid =
            new TileGridControl
            {
                Columns =
                    8,

                Zoom =
                    4,

                DefaultPalette =
                    _aladdinRomPalette
            };

        _sourceGrid =
            new TileGridControl
            {
                Columns =
                    8,

                Zoom =
                    4,

                DefaultPalette =
                    _quantization.Palette
            };

        _romPageNavigator =
            new RomPageNavigatorControl
            {
                Dock =
                    DockStyle.Fill
            };

        _romPageNavigator.PageTileClicked +=
            RomPageNavigator_PageTileClicked;

        _romLocationLabel =
            new Label
            {
                AutoSize =
                    true,

                MaximumSize =
                    new Size(
                        300,
                        0),

                Font =
                    new Font(
                        FontFamily.GenericMonospace,
                        9f,
                        FontStyle.Regular),

                Margin =
                    new Padding(
                        3,
                        8,
                        3,
                        3)
            };

        _originalGrid.SelectedIndexChanged +=
            OriginalGrid_SelectedIndexChanged;

        _sourceGrid.SelectedIndexChanged +=
            SourceGrid_SelectedIndexChanged;

        _selectionLabel =
            new Label
            {
                AutoSize =
                    true,

                MaximumSize =
                    new Size(
                        1400,
                        0),

                Margin =
                    new Padding(
                        3,
                        6,
                        3,
                        6)
            };

        _summaryLabel =
            new Label
            {
                AutoSize =
                    true,

                MaximumSize =
                    new Size(
                        1400,
                        0),

                Font =
                    new Font(
                        SystemFonts.MessageBoxFont
                        ?? SystemFonts.DefaultFont,
                        FontStyle.Bold),

                Margin =
                    new Padding(
                        3,
                        3,
                        3,
                        8)
            };

        _orientationCombo =
            new ComboBox
            {
                DropDownStyle =
                    ComboBoxStyle.DropDownList,

                Width =
                    210
            };

        _orientationCombo.Items.AddRange(
            new object[]
            {
                "Normal",
                "Voltear horizontalmente",
                "Voltear verticalmente",
                "Voltear horizontal y vertical"
            });

        _orientationCombo.SelectedIndex =
            0;

        _orientationCombo.SelectedIndexChanged +=
            (_, _) =>
            {
                UpdateOrientationPreview();
                UpdateSelectionInformation();
            };

        _orientationPreviewGrid =
            new TileGridControl
            {
                Columns =
                    1,

                Zoom =
                    7,

                DefaultPalette =
                    _quantization.Palette
            };

        _assignTileButton =
            CreateButton(
                "Asignar tile seleccionado");

        _restoreTileButton =
            CreateButton(
                "Restaurar destino");

        _assignLinearButton =
            CreateButton(
                "Asignar linealmente");

        _clearChangesButton =
            CreateButton(
                "Restaurar todos");

        _createRomButton =
            CreateButton(
                "Crear copia ROM con cambios");

        _closeButton =
            CreateButton(
                "Cerrar");

        _browseRomButton.Click +=
            BrowseRomButton_Click;

        _loadSegmentButton.Click +=
            LoadSegmentButton_Click;

        _browseOutputButton.Click +=
            BrowseOutputButton_Click;

        _assignTileButton.Click +=
            AssignTileButton_Click;

        _restoreTileButton.Click +=
            RestoreTileButton_Click;

        _assignLinearButton.Click +=
            AssignLinearButton_Click;

        _clearChangesButton.Click +=
            ClearChangesButton_Click;

        _createRomButton.Click +=
            CreateRomButton_Click;

        _closeButton.Click +=
            (_, _) =>
                Close();

        Controls.Add(
            CreateMainLayout());

        SelectInitialFrame(
            initialFrame);

        ApplyRomPreviewPalette();
        ApplyGridLayout();
        UpdateRomLocationInformation();
        UpdateButtons();
    }

    private Control CreateMainLayout()
    {
        TableLayoutPanel root =
            new()
            {
                Dock =
                    DockStyle.Fill,

                ColumnCount =
                    1,

                RowCount =
                    4,

                Padding =
                    new Padding(
                        10)
            };

        root.RowStyles.Add(
            new RowStyle(
                SizeType.AutoSize));

        root.RowStyles.Add(
            new RowStyle(
                SizeType.Percent,
                100));

        root.RowStyles.Add(
            new RowStyle(
                SizeType.AutoSize));

        root.RowStyles.Add(
            new RowStyle(
                SizeType.AutoSize));

        root.Controls.Add(
            CreateHeaderPanel(),
            0,
            0);

        TableLayoutPanel grids =
            new()
            {
                Dock =
                    DockStyle.Fill,

                ColumnCount =
                    3,

                RowCount =
                    1
            };

        grids.ColumnStyles.Add(
            new ColumnStyle(
                SizeType.Percent,
                50));

        grids.ColumnStyles.Add(
            new ColumnStyle(
                SizeType.Absolute,
                330));

        grids.ColumnStyles.Add(
            new ColumnStyle(
                SizeType.Percent,
                50));

        grids.RowStyles.Add(
            new RowStyle(
                SizeType.Percent,
                100));

        grids.Controls.Add(
            CreateGridGroup(
                "Segmento original de la ROM",
                _originalGrid),
            0,
            0);

        grids.Controls.Add(
            CreateRomLocationGroup(),
            1,
            0);

        grids.Controls.Add(
            CreateGridGroup(
                "Tiles de la pose nueva",
                _sourceGrid),
            2,
            0);

        root.Controls.Add(
            grids,
            0,
            1);

        FlowLayoutPanel actions =
            new()
            {
                Dock =
                    DockStyle.Fill,

                AutoSize =
                    true,

                FlowDirection =
                    FlowDirection.LeftToRight,

                WrapContents =
                    true,

                Padding =
                    new Padding(
                        0,
                        8,
                        0,
                        0)
            };

        actions.Controls.Add(
            CreateFieldLabel(
                "Orientación:"));

        actions.Controls.Add(
            _orientationCombo);

        GroupBox orientationPreviewGroup =
            new()
            {
                Text =
                    "Vista del tile",

                Width =
                    86,

                Height =
                    86,

                Padding =
                    new Padding(3),

                Margin =
                    new Padding(
                        12,
                        0,
                        12,
                        0)
            };

        Panel orientationPreviewPanel =
            new()
            {
                Dock =
                    DockStyle.Fill,

                AutoScroll =
                    true,

                BackColor =
                    Color.FromArgb(
                        28,
                        28,
                        32)
            };

        orientationPreviewPanel.Controls.Add(
            _orientationPreviewGrid);

        orientationPreviewGroup.Controls.Add(
            orientationPreviewPanel);

        actions.Controls.Add(
            orientationPreviewGroup);

        actions.Controls.Add(
            _assignTileButton);

        actions.Controls.Add(
            _restoreTileButton);

        actions.Controls.Add(
            _assignLinearButton);

        actions.Controls.Add(
            _clearChangesButton);

        root.Controls.Add(
            actions,
            0,
            2);

        FlowLayoutPanel footer =
            new()
            {
                Dock =
                    DockStyle.Fill,

                AutoSize =
                    true,

                FlowDirection =
                    FlowDirection.RightToLeft,

                WrapContents =
                    true,

                Padding =
                    new Padding(
                        0,
                        8,
                        0,
                        0)
            };

        footer.Controls.Add(
            _closeButton);

        footer.Controls.Add(
            _createRomButton);

        root.Controls.Add(
            footer,
            0,
            3);

        return root;
    }

    private Control CreateHeaderPanel()
    {
        TableLayoutPanel header =
            new()
            {
                Dock =
                    DockStyle.Fill,

                AutoSize =
                    true,

                ColumnCount =
                    1,

                RowCount =
                    6
            };

        Label title =
            new()
            {
                Text =
                    "Comparación y reemplazo tile por tile",

                AutoSize =
                    true,

                Font =
                    new Font(
                        (SystemFonts.MessageBoxFont
                         ?? SystemFonts.DefaultFont).FontFamily,
                        14f,
                        FontStyle.Bold),

                Margin =
                    new Padding(
                        3,
                        3,
                        3,
                        6)
            };

        Label warning =
            new()
            {
                Text =
                    "Los dos paneles pueden mostrarse con la misma cuadrícula " +
                    "visual de la pose para facilitar la comparación. " +
                    "Esto no demuestra que el tile 000 de la ROM sea la " +
                    "misma parte que el tile 000 de la pose: solamente " +
                    "alinea las casillas en pantalla. La paleta y la " +
                    "distribución visual no modifican la ROM.",

                AutoSize =
                    true,

                ForeColor =
                    Color.DarkGoldenrod,

                MaximumSize =
                    new Size(
                        1400,
                        0),

                Margin =
                    new Padding(
                        3,
                        3,
                        3,
                        8)
            };

        header.Controls.Add(
            title,
            0,
            0);

        header.Controls.Add(
            warning,
            0,
            1);

        FlowLayoutPanel firstRow =
            new()
            {
                AutoSize =
                    true,

                FlowDirection =
                    FlowDirection.LeftToRight,

                WrapContents =
                    true
            };

        firstRow.Controls.Add(
            CreateFieldLabel(
                "Pose nueva:"));

        firstRow.Controls.Add(
            _frameCombo);

        firstRow.Controls.Add(
            CreateFieldLabel(
                "Tiles del segmento:"));

        firstRow.Controls.Add(
            _segmentTileCountInput);

        firstRow.Controls.Add(
            CreateFieldLabel(
                "Vista de tiles ROM:"));

        firstRow.Controls.Add(
            _romPaletteModeCombo);

        firstRow.Controls.Add(
            CreateFieldLabel(
                "Distribución ROM:"));

        firstRow.Controls.Add(
            _romLayoutModeCombo);

        firstRow.Controls.Add(
            _synchronizeSelectionCheckBox);

        header.Controls.Add(
            firstRow,
            0,
            2);

        FlowLayoutPanel secondRow =
            new()
            {
                AutoSize =
                    true,

                FlowDirection =
                    FlowDirection.LeftToRight,

                WrapContents =
                    true
            };

        secondRow.Controls.Add(
            CreateFieldLabel(
                "ROM original:"));

        secondRow.Controls.Add(
            _romPathTextBox);

        secondRow.Controls.Add(
            _browseRomButton);

        secondRow.Controls.Add(
            CreateFieldLabel(
                "Offset: 0x"));

        secondRow.Controls.Add(
            _offsetTextBox);

        secondRow.Controls.Add(
            _headerlessOffsetCheckBox);

        secondRow.Controls.Add(
            _loadSegmentButton);

        header.Controls.Add(
            secondRow,
            0,
            3);

        FlowLayoutPanel thirdRow =
            new()
            {
                AutoSize =
                    true,

                FlowDirection =
                    FlowDirection.LeftToRight,

                WrapContents =
                    true
            };

        thirdRow.Controls.Add(
            CreateFieldLabel(
                "Copia de salida:"));

        thirdRow.Controls.Add(
            _outputPathTextBox);

        thirdRow.Controls.Add(
            _browseOutputButton);

        header.Controls.Add(
            thirdRow,
            0,
            4);

        FlowLayoutPanel information =
            new()
            {
                AutoSize =
                    true,

                FlowDirection =
                    FlowDirection.TopDown,

                WrapContents =
                    false,

                Margin =
                    new Padding(
                        0,
                        8,
                        0,
                        0)
            };

        information.Controls.Add(
            _summaryLabel);

        information.Controls.Add(
            _selectionLabel);

        header.Controls.Add(
            information,
            0,
            5);

        return header;
    }

    private Control CreateRomLocationGroup()
    {
        GroupBox group =
            new()
            {
                Text =
                    "Ubicación del tile en la ROM",

                Dock =
                    DockStyle.Fill,

                Padding =
                    new Padding(
                        8),

                Margin =
                    new Padding(
                        4)
            };

        TableLayoutPanel layout =
            new()
            {
                Dock =
                    DockStyle.Fill,

                ColumnCount =
                    1,

                RowCount =
                    3
            };

        layout.RowStyles.Add(
            new RowStyle(
                SizeType.AutoSize));

        layout.RowStyles.Add(
            new RowStyle(
                SizeType.Percent,
                100));

        layout.RowStyles.Add(
            new RowStyle(
                SizeType.AutoSize));

        Label explanation =
            new()
            {
                Text =
                    "Mapa de una página de 256 tiles " +
                    "(16 × 16 = 0x2000 bytes).",

                AutoSize =
                    true,

                MaximumSize =
                    new Size(
                        300,
                        0),

                Margin =
                    new Padding(
                        3,
                        3,
                        3,
                        6)
            };

        layout.Controls.Add(
            explanation,
            0,
            0);

        layout.Controls.Add(
            _romPageNavigator,
            0,
            1);

        layout.Controls.Add(
            _romLocationLabel,
            0,
            2);

        group.Controls.Add(
            layout);

        return group;
    }

    private static Control CreateGridGroup(
        string title,
        TileGridControl grid)
    {
        GroupBox group =
            new()
            {
                Text =
                    title,

                Dock =
                    DockStyle.Fill,

                Padding =
                    new Padding(
                        8),

                Margin =
                    new Padding(
                        4)
            };

        Panel scroll =
            new()
            {
                Dock =
                    DockStyle.Fill,

                AutoScroll =
                    true,

                BackColor =
                    Color.FromArgb(
                        28,
                        28,
                        32)
            };

        scroll.Controls.Add(
            grid);

        group.Controls.Add(
            scroll);

        return group;
    }

    private static Label CreateFieldLabel(
        string text)
    {
        return new Label
        {
            Text =
                text,

            AutoSize =
                true,

            Font =
                new Font(
                    SystemFonts.MessageBoxFont
                    ?? SystemFonts.DefaultFont,
                    FontStyle.Bold),

            Margin =
                new Padding(
                    8,
                    7,
                    4,
                    3)
        };
    }

    private static Button CreateButton(
        string text)
    {
        return new Button
        {
            Text =
                text,

            AutoSize =
                true
        };
    }

    private SpriteFrame? SelectedFrame =>
        _frameCombo.SelectedItem
        is FrameItem item
            ? item.Frame
            : null;

    private void SelectInitialFrame(
        SpriteFrame? initialFrame)
    {
        if (_frameCombo.Items.Count == 0)
        {
            return;
        }

        int selectedIndex =
            0;

        if (initialFrame is not null)
        {
            for (int index = 0;
                 index < _frameCombo.Items.Count;
                 index++)
            {
                if (_frameCombo.Items[index]
                    is FrameItem item &&
                    ReferenceEquals(
                        item.Frame,
                        initialFrame))
                {
                    selectedIndex =
                        index;

                    break;
                }
            }
        }

        _frameCombo.SelectedIndex =
            selectedIndex;
    }

    private void FrameCombo_SelectedIndexChanged(
        object? sender,
        EventArgs e)
    {
        RebuildSourceTiles();
    }

    private void RebuildSourceTiles()
    {
        SpriteFrame? frame =
            SelectedFrame;

        if (frame is null)
        {
            _poseEncoding =
                null;

            _sourceGrid.Tiles =
                Array.Empty<byte[]>();

            ApplyGridLayout();
            UpdateInformation();
            UpdateButtons();

            return;
        }

        try
        {
            _poseEncoding =
                SnesPoseTileEncoder.Encode(
                    _quantization,
                    frame);

            _sourceGrid.Tiles =
                _poseEncoding.Tiles;

            ApplyGridLayout();

            _sourceGrid.SelectedIndex =
                _poseEncoding.TileCount > 0
                    ? 0
                    : -1;

            _segmentTileCountInput.Value =
                Math.Clamp(
                    _poseEncoding.TileCount,
                    (int)_segmentTileCountInput.Minimum,
                    (int)_segmentTileCountInput.Maximum);
        }
        catch (Exception ex)
        {
            _poseEncoding =
                null;

            _sourceGrid.Tiles =
                Array.Empty<byte[]>();

            MessageBox.Show(
                this,
                ex.Message,
                "No se pudo preparar la pose",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }

        UpdateOrientationPreview();
        UpdateInformation();
        UpdateButtons();
    }

    private void BrowseRomButton_Click(
        object? sender,
        EventArgs e)
    {
        using OpenFileDialog dialog =
            new()
            {
                Title =
                    "Seleccionar ROM original",

                Filter =
                    "ROM de SNES (*.sfc;*.smc)|*.sfc;*.smc|" +
                    "Todos los archivos (*.*)|*.*",

                CheckFileExists =
                    true,

                Multiselect =
                    false
            };

        if (dialog.ShowDialog(this)
            != DialogResult.OK)
        {
            return;
        }

        _romPathTextBox.Text =
            dialog.FileName;

        SuggestOutputPath();
        UpdateButtons();
    }

    private void BrowseOutputButton_Click(
        object? sender,
        EventArgs e)
    {
        using SaveFileDialog dialog =
            new()
            {
                Title =
                    "Guardar copia de ROM",

                Filter =
                    "ROM de SNES (*.sfc)|*.sfc|" +
                    "ROM con encabezado (*.smc)|*.smc|" +
                    "Todos los archivos (*.*)|*.*",

                AddExtension =
                    true,

                OverwritePrompt =
                    true,

                FileName =
                    Path.GetFileName(
                        _outputPathTextBox.Text)
            };

        if (!string.IsNullOrWhiteSpace(
                _outputPathTextBox.Text))
        {
            dialog.InitialDirectory =
                Path.GetDirectoryName(
                    _outputPathTextBox.Text);
        }

        if (dialog.ShowDialog(this)
            != DialogResult.OK)
        {
            return;
        }

        _outputPathTextBox.Text =
            dialog.FileName;

        UpdateButtons();
    }

    private void SuggestOutputPath()
    {
        if (!File.Exists(
                _romPathTextBox.Text))
        {
            return;
        }

        string directory =
            Path.GetDirectoryName(
                _romPathTextBox.Text)
            ?? Environment.CurrentDirectory;

        string extension =
            Path.GetExtension(
                _romPathTextBox.Text);

        if (string.IsNullOrWhiteSpace(extension))
        {
            extension =
                ".sfc";
        }

        string outputName =
            $"{Path.GetFileNameWithoutExtension(_romPathTextBox.Text)}_" +
            $"{_suggestedBaseName}_" +
            "TilePorTile_Prueba" +
            extension;

        _outputPathTextBox.Text =
            Path.Combine(
                directory,
                outputName);
    }

    private void LoadSegmentButton_Click(
        object? sender,
        EventArgs e)
    {
        if (!File.Exists(
                _romPathTextBox.Text))
        {
            MessageBox.Show(
                this,
                "Seleccione una ROM original válida.",
                "ROM no encontrada",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);

            return;
        }

        if (!TryParseHexOffset(
                _offsetTextBox.Text,
                out int logicalOffset))
        {
            MessageBox.Show(
                this,
                "Escriba un offset hexadecimal válido.",
                "Offset inválido",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);

            return;
        }

        int tileCount =
            decimal.ToInt32(
                _segmentTileCountInput.Value);

        try
        {
            byte[] rom =
                File.ReadAllBytes(
                    _romPathTextBox.Text);

            _copierHeaderDetected =
                RomTilePatchService.HasCopierHeader(
                    rom.Length);

            _logicalSegmentOffset =
                logicalOffset;

            _actualSegmentOffset =
                logicalOffset;

            if (_headerlessOffsetCheckBox.Checked &&
                _copierHeaderDetected)
            {
                _actualSegmentOffset +=
                    0x200;
            }

            int byteCount =
                checked(
                    tileCount *
                    32);

            if (_actualSegmentOffset < 0 ||
                (long)_actualSegmentOffset +
                    byteCount >
                rom.Length)
            {
                throw new InvalidOperationException(
                    "El segmento solicitado queda fuera de la ROM.");
            }

            _originalSegmentTiles =
                new List<byte[]>(
                    tileCount);

            for (int index = 0;
                 index < tileCount;
                 index++)
            {
                byte[] tile =
                    new byte[32];

                Buffer.BlockCopy(
                    rom,
                    _actualSegmentOffset +
                    index *
                    32,
                    tile,
                    0,
                    32);

                _originalSegmentTiles.Add(
                    tile);
            }

            _workingSegmentTiles =
                _originalSegmentTiles
                    .Select(tile => tile.ToArray())
                    .ToList();

            _sourceTileByDestination.Clear();

            _originalGrid.Tiles =
                _workingSegmentTiles;

            ApplyGridLayout();

            _originalGrid.ReplacementIndexes =
                Array.Empty<int>();

            _originalGrid.SelectedIndex =
                _workingSegmentTiles.Count > 0
                    ? 0
                    : -1;

            UpdateRomLocationInformation();
            UpdateInformation();
            UpdateButtons();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                this,
                ex.Message,
                "No se pudo cargar el segmento",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

    private void AssignTileButton_Click(
        object? sender,
        EventArgs e)
    {
        int destinationIndex =
            _originalGrid.SelectedIndex;

        int sourceIndex =
            _sourceGrid.SelectedIndex;

        if (!IsValidDestinationIndex(
                destinationIndex) ||
            !IsValidSourceIndex(
                sourceIndex))
        {
            return;
        }

        TileOrientation orientation =
            SelectedOrientation;

        _workingSegmentTiles[destinationIndex] =
            TransformTile4Bpp(
                _poseEncoding!.Tiles[sourceIndex],
                orientation);

        _sourceTileByDestination[
            destinationIndex] =
                new TileAssignment
                {
                    SourceTileIndex =
                        sourceIndex,

                    Orientation =
                        orientation
                };

        RefreshWorkingGrid();
    }

    private void RestoreTileButton_Click(
        object? sender,
        EventArgs e)
    {
        int destinationIndex =
            _originalGrid.SelectedIndex;

        if (!IsValidDestinationIndex(
                destinationIndex))
        {
            return;
        }

        _workingSegmentTiles[
            destinationIndex] =
                _originalSegmentTiles[
                    destinationIndex].ToArray();

        _sourceTileByDestination.Remove(
            destinationIndex);

        RefreshWorkingGrid();
    }

    private void AssignLinearButton_Click(
        object? sender,
        EventArgs e)
    {
        if (_poseEncoding is null ||
            _workingSegmentTiles.Count == 0)
        {
            return;
        }

        int destinationStart =
            _originalGrid.SelectedIndex >= 0
                ? _originalGrid.SelectedIndex
                : 0;

        int sourceStart =
            _sourceGrid.SelectedIndex >= 0
                ? _sourceGrid.SelectedIndex
                : 0;

        int count =
            Math.Min(
                _workingSegmentTiles.Count -
                destinationStart,
                _poseEncoding.TileCount -
                sourceStart);

        if (count <= 0)
        {
            return;
        }

        DialogResult confirmation =
            MessageBox.Show(
                this,
                $"Se asignarán {count} tiles consecutivos desde " +
                $"el destino {destinationStart:000} usando los " +
                $"tiles nuevos desde {sourceStart:000}.\n\n" +
                "Esta opción reproduce una prueba lineal, pero mantiene " +
                "intactos los demás tiles del segmento.\n\n" +
                "¿Continuar?",
                "Asignación lineal",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

        if (confirmation !=
            DialogResult.Yes)
        {
            return;
        }

        TileOrientation orientation =
            SelectedOrientation;

        for (int index = 0;
             index < count;
             index++)
        {
            int destinationIndex =
                destinationStart +
                index;

            int sourceIndex =
                sourceStart +
                index;

            _workingSegmentTiles[
                destinationIndex] =
                    TransformTile4Bpp(
                        _poseEncoding.Tiles[sourceIndex],
                        orientation);

            _sourceTileByDestination[
                destinationIndex] =
                    new TileAssignment
                    {
                        SourceTileIndex =
                            sourceIndex,

                        Orientation =
                            orientation
                    };
        }

        RefreshWorkingGrid();
    }

    private void ClearChangesButton_Click(
        object? sender,
        EventArgs e)
    {
        if (_originalSegmentTiles.Count == 0)
        {
            return;
        }

        _workingSegmentTiles =
            _originalSegmentTiles
                .Select(tile => tile.ToArray())
                .ToList();

        _sourceTileByDestination.Clear();

        RefreshWorkingGrid();
    }

    private void RefreshWorkingGrid()
    {
        int selectedIndex =
            _originalGrid.SelectedIndex;

        _originalGrid.Tiles =
            _workingSegmentTiles;

        _originalGrid.ReplacementPalette =
            _quantization.Palette;

        _originalGrid.ReplacementIndexes =
            _sourceTileByDestination.Keys;

        _originalGrid.SelectedIndex =
            selectedIndex;

        UpdateRomLocationInformation();
        UpdateInformation();
        UpdateButtons();
    }

    private void CreateRomButton_Click(
        object? sender,
        EventArgs e)
    {
        if (_workingSegmentTiles.Count == 0 ||
            _sourceTileByDestination.Count == 0)
        {
            MessageBox.Show(
                this,
                "Primero asigne al menos un tile nuevo.",
                "Sin cambios",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);

            return;
        }

        if (!TryParseHexOffset(
                _offsetTextBox.Text,
                out int logicalOffset))
        {
            MessageBox.Show(
                this,
                "El offset hexadecimal no es válido.",
                "Offset inválido",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);

            return;
        }

        string originalPath =
            _romPathTextBox.Text.Trim();

        string outputPath =
            _outputPathTextBox.Text.Trim();

        if (!File.Exists(originalPath) ||
            string.IsNullOrWhiteSpace(outputPath))
        {
            MessageBox.Show(
                this,
                "Seleccione la ROM original y una copia de salida.",
                "Rutas incompletas",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);

            return;
        }

        byte[] patchBytes =
            CombineTiles(
                _workingSegmentTiles);

        DialogResult confirmation =
            MessageBox.Show(
                this,
                $"Se creará una copia y se escribirán " +
                $"{patchBytes.Length:N0} bytes desde 0x" +
                $"{logicalOffset:X6}.\n\n" +
                $"Tiles modificados: " +
                $"{_sourceTileByDestination.Count} de " +
                $"{_workingSegmentTiles.Count}.\n\n" +
                "Los demás tiles del segmento conservarán exactamente " +
                "sus bytes originales. También se creará un respaldo.\n\n" +
                "¿Crear la copia de prueba?",
                "Confirmar prueba tile por tile",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

        if (confirmation !=
            DialogResult.Yes)
        {
            return;
        }

        try
        {
            UseWaitCursor =
                true;

            _createRomButton.Enabled =
                false;

            RomTilePatchResult result =
                RomTilePatchService.CreatePatchedCopy(
                    new RomTilePatchOptions
                    {
                        OriginalRomPath =
                            originalPath,

                        OutputRomPath =
                            outputPath,

                        LogicalOffset =
                            logicalOffset,

                        PatchBytes =
                            patchBytes,

                        TreatOffsetAsHeaderless =
                            _headerlessOffsetCheckBox.Checked,

                        DetectCopierHeader =
                            true,

                        RequirePatchLengthMultipleOf32 =
                            true,

                        CreateSegmentBackup =
                            true,

                        CreateManifest =
                            true
                    });

            string mappingPath =
                outputPath +
                ".tile_mapping.csv";

            File.WriteAllLines(
                mappingPath,
                CreateMappingCsv());

            MessageBox.Show(
                this,
                "La copia tile por tile fue creada.\n\n" +
                $"Salida:\n{result.OutputRomPath}\n\n" +
                $"Offset real: 0x{result.ActualFileOffset:X6}\n" +
                $"Tiles modificados: " +
                $"{_sourceTileByDestination.Count}\n" +
                $"Mapa:\n{mappingPath}\n\n" +
                "Pruebe únicamente la copia en el emulador.",
                "Copia creada",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                this,
                ex.Message,
                "No se pudo crear la copia",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
        finally
        {
            UseWaitCursor =
                false;

            UpdateButtons();
        }
    }

    private IEnumerable<string> CreateMappingCsv()
    {
        yield return
            "tile_destino,tile_fuente_pose,orientacion," +
            "offset_relativo,offset_logico_absoluto";

        if (!TryParseHexOffset(
                _offsetTextBox.Text,
                out int logicalOffset))
        {
            logicalOffset =
                0;
        }

        foreach (KeyValuePair<int, TileAssignment> pair
                 in _sourceTileByDestination
                     .OrderBy(item => item.Key))
        {
            int relativeOffset =
                pair.Key *
                32;

            yield return string.Join(
                ",",
                pair.Key,
                pair.Value.SourceTileIndex,
                GetOrientationCode(
                    pair.Value.Orientation),
                $"0x{relativeOffset:X4}",
                $"0x{logicalOffset + relativeOffset:X6}");
        }
    }

    private void RomPageNavigator_PageTileClicked(
        int pageTileIndex)
    {
        if (_originalSegmentTiles.Count == 0)
        {
            return;
        }

        int pageBaseLogicalOffset =
            GetCurrentPageBaseLogicalOffset();

        int clickedLogicalOffset =
            pageBaseLogicalOffset +
            pageTileIndex *
            32;

        int relativeBytes =
            clickedLogicalOffset -
            _logicalSegmentOffset;

        if (relativeBytes < 0 ||
            relativeBytes % 32 != 0)
        {
            return;
        }

        int segmentTileIndex =
            relativeBytes /
            32;

        if (segmentTileIndex < 0 ||
            segmentTileIndex >=
                _originalSegmentTiles.Count)
        {
            MessageBox.Show(
                this,
                "Ese tile pertenece a la página mostrada, pero no " +
                "está incluido en el segmento cargado. Aumente la " +
                "cantidad de tiles o cambie el offset para editarlo.",
                "Tile fuera del segmento",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);

            return;
        }

        _originalGrid.SelectedIndex =
            segmentTileIndex;
    }

    private int GetCurrentPageBaseLogicalOffset()
    {
        int selectedSegmentIndex =
            _originalGrid.SelectedIndex >= 0
                ? _originalGrid.SelectedIndex
                : 0;

        int selectedLogicalOffset =
            _logicalSegmentOffset +
            selectedSegmentIndex *
            32;

        return
            selectedLogicalOffset &
            ~0x1FFF;
    }

    private void UpdateRomLocationInformation()
    {
        if (_originalSegmentTiles.Count == 0 ||
            _originalGrid.SelectedIndex < 0)
        {
            _romLocationLabel.Text =
                "Cargue un segmento y seleccione un tile.";

            _romPageNavigator.LoadedTileIndexes =
                Array.Empty<int>();

            _romPageNavigator.ModifiedTileIndexes =
                Array.Empty<int>();

            _romPageNavigator.SelectedTileIndex =
                -1;

            return;
        }

        int segmentTileIndex =
            _originalGrid.SelectedIndex;

        int logicalTileOffset =
            _logicalSegmentOffset +
            segmentTileIndex *
            32;

        int actualTileOffset =
            _actualSegmentOffset +
            segmentTileIndex *
            32;

        int pageBaseLogicalOffset =
            logicalTileOffset &
            ~0x1FFF;

        int pageEndLogicalOffset =
            pageBaseLogicalOffset +
            0x1FFF;

        int offsetInsidePage =
            logicalTileOffset -
            pageBaseLogicalOffset;

        int pageTileIndex =
            offsetInsidePage /
            32;

        int pageRow =
            pageTileIndex /
            16;

        int pageColumn =
            pageTileIndex %
            16;

        List<int> loadedPageIndexes =
            new();

        for (int index = 0;
             index < _originalSegmentTiles.Count;
             index++)
        {
            int tileLogicalOffset =
                _logicalSegmentOffset +
                index *
                32;

            if ((tileLogicalOffset &
                 ~0x1FFF) !=
                pageBaseLogicalOffset)
            {
                continue;
            }

            loadedPageIndexes.Add(
                (tileLogicalOffset -
                 pageBaseLogicalOffset) /
                32);
        }

        List<int> modifiedPageIndexes =
            new();

        foreach (int modifiedSegmentIndex
                 in _sourceTileByDestination.Keys)
        {
            int tileLogicalOffset =
                _logicalSegmentOffset +
                modifiedSegmentIndex *
                32;

            if ((tileLogicalOffset &
                 ~0x1FFF) !=
                pageBaseLogicalOffset)
            {
                continue;
            }

            modifiedPageIndexes.Add(
                (tileLogicalOffset -
                 pageBaseLogicalOffset) /
                32);
        }

        _romPageNavigator.LoadedTileIndexes =
            loadedPageIndexes;

        _romPageNavigator.ModifiedTileIndexes =
            modifiedPageIndexes;

        _romPageNavigator.SelectedTileIndex =
            pageTileIndex;

        _romLocationLabel.Text =
            $"Tile del segmento: {segmentTileIndex:000}\n" +
            $"Offset lógico: 0x{logicalTileOffset:X6}\n" +
            $"Offset real archivo: 0x{actualTileOffset:X6}\n" +
            $"Página: 0x{pageBaseLogicalOffset:X6}–" +
            $"0x{pageEndLogicalOffset:X6}\n" +
            $"Tile dentro de página: {pageTileIndex:000}\n" +
            $"Fila: {pageRow + 1:00} | " +
            $"Columna: {pageColumn + 1:00}\n" +
            $"Posición hexadecimal: " +
            $"fila {pageRow:X1}, columna {pageColumn:X1}\n" +
            $"Offset dentro de página: " +
            $"0x{offsetInsidePage:X4}\n" +
            $"Bytes del tile: 0x{logicalTileOffset:X6}–" +
            $"0x{logicalTileOffset + 31:X6}";
    }

    private void ApplyGridLayout()
    {
        int poseColumns =
            _poseEncoding is null
                ? 8
                : Math.Max(
                    1,
                    _poseEncoding.TileColumns);

        _sourceGrid.Columns =
            poseColumns;

        _originalGrid.Columns =
            _romLayoutModeCombo.SelectedIndex == 1
                ? 8
                : poseColumns;

        _originalGrid.Invalidate();
        _sourceGrid.Invalidate();
    }

    private void OriginalGrid_SelectedIndexChanged(
        object? sender,
        EventArgs e)
    {
        if (!_synchronizingSelection &&
            _synchronizeSelectionCheckBox.Checked &&
            _originalGrid.SelectedIndex >= 0 &&
            _originalGrid.SelectedIndex <
                _sourceGrid.Tiles.Count)
        {
            try
            {
                _synchronizingSelection =
                    true;

                _sourceGrid.SelectedIndex =
                    _originalGrid.SelectedIndex;
            }
            finally
            {
                _synchronizingSelection =
                    false;
            }
        }

        UpdateOrientationPreview();
        UpdateRomLocationInformation();
        UpdateSelectionInformation();
        UpdateButtons();
    }

    private void SourceGrid_SelectedIndexChanged(
        object? sender,
        EventArgs e)
    {
        if (!_synchronizingSelection &&
            _synchronizeSelectionCheckBox.Checked &&
            _sourceGrid.SelectedIndex >= 0 &&
            _sourceGrid.SelectedIndex <
                _originalGrid.Tiles.Count)
        {
            try
            {
                _synchronizingSelection =
                    true;

                _originalGrid.SelectedIndex =
                    _sourceGrid.SelectedIndex;
            }
            finally
            {
                _synchronizingSelection =
                    false;
            }
        }

        UpdateOrientationPreview();
        UpdateSelectionInformation();
        UpdateButtons();
    }

    private static string GetGridPositionText(
        int index,
        int columns)
    {
        if (index < 0 ||
            columns <= 0)
        {
            return string.Empty;
        }

        int row =
            index /
            columns;

        int column =
            index %
            columns;

        return
            $"(fila {row + 1}, columna {column + 1})";
    }

    private void ApplyRomPreviewPalette()
    {
        IReadOnlyList<System.Drawing.Color> selectedPalette =
            _romPaletteModeCombo.SelectedIndex == 1
                ? _grayscaleRomPalette
                : _aladdinRomPalette;

        _originalGrid.DefaultPalette =
            selectedPalette;

        _originalGrid.ReplacementPalette =
            _quantization.Palette;

        _originalGrid.Invalidate();
    }

    private TileOrientation SelectedOrientation =>
        _orientationCombo.SelectedIndex switch
        {
            1 =>
                TileOrientation.FlipHorizontal,

            2 =>
                TileOrientation.FlipVertical,

            3 =>
                TileOrientation.FlipBoth,

            _ =>
                TileOrientation.Normal
        };

    private void UpdateOrientationPreview()
    {
        if (_poseEncoding is null ||
            !IsValidSourceIndex(
                _sourceGrid.SelectedIndex))
        {
            _orientationPreviewGrid.Tiles =
                Array.Empty<byte[]>();

            _orientationPreviewGrid.SelectedIndex =
                -1;

            return;
        }

        byte[] transformed =
            TransformTile4Bpp(
                _poseEncoding.Tiles[
                    _sourceGrid.SelectedIndex],
                SelectedOrientation);

        _orientationPreviewGrid.Tiles =
            new[]
            {
                transformed
            };

        _orientationPreviewGrid.DefaultPalette =
            _quantization.Palette;

        _orientationPreviewGrid.SelectedIndex =
            -1;
    }

    private static byte[] TransformTile4Bpp(
        IReadOnlyList<byte> encodedTile,
        TileOrientation orientation)
    {
        byte[] sourceIndices =
            DecodeTile4Bpp(
                encodedTile);

        byte[] transformedIndices =
            new byte[64];

        bool flipHorizontal =
            orientation is
                TileOrientation.FlipHorizontal or
                TileOrientation.FlipBoth;

        bool flipVertical =
            orientation is
                TileOrientation.FlipVertical or
                TileOrientation.FlipBoth;

        for (int destinationY = 0;
             destinationY < 8;
             destinationY++)
        {
            for (int destinationX = 0;
                 destinationX < 8;
                 destinationX++)
            {
                int sourceX =
                    flipHorizontal
                        ? 7 - destinationX
                        : destinationX;

                int sourceY =
                    flipVertical
                        ? 7 - destinationY
                        : destinationY;

                transformedIndices[
                    destinationY *
                    8 +
                    destinationX] =
                        sourceIndices[
                            sourceY *
                            8 +
                            sourceX];
            }
        }

        return
            Snes4BppTilePackageBuilder.EncodeTile4Bpp(
                transformedIndices);
    }

    private static byte[] DecodeTile4Bpp(
        IReadOnlyList<byte> tile)
    {
        ArgumentNullException.ThrowIfNull(
            tile);

        if (tile.Count != 32)
        {
            throw new ArgumentException(
                "Un tile SNES 4BPP debe contener 32 bytes.",
                nameof(tile));
        }

        byte[] result =
            new byte[64];

        for (int row = 0;
             row < 8;
             row++)
        {
            byte plane0 =
                tile[row * 2];

            byte plane1 =
                tile[row * 2 + 1];

            byte plane2 =
                tile[16 + row * 2];

            byte plane3 =
                tile[16 + row * 2 + 1];

            for (int column = 0;
                 column < 8;
                 column++)
            {
                int bit =
                    7 - column;

                result[
                    row *
                    8 +
                    column] =
                        (byte)(
                            ((plane0 >> bit) & 1) |
                            (((plane1 >> bit) & 1) << 1) |
                            (((plane2 >> bit) & 1) << 2) |
                            (((plane3 >> bit) & 1) << 3));
            }
        }

        return result;
    }

    private static string GetOrientationCode(
        TileOrientation orientation)
    {
        return orientation switch
        {
            TileOrientation.FlipHorizontal =>
                "H",

            TileOrientation.FlipVertical =>
                "V",

            TileOrientation.FlipBoth =>
                "HV",

            _ =>
                "NORMAL"
        };
    }

    private static string GetOrientationDisplayName(
        TileOrientation orientation)
    {
        return orientation switch
        {
            TileOrientation.FlipHorizontal =>
                "horizontal",

            TileOrientation.FlipVertical =>
                "vertical",

            TileOrientation.FlipBoth =>
                "horizontal + vertical",

            _ =>
                "normal"
        };
    }

    private void UpdateSelectionInformation()
    {
        string destinationText =
            _originalGrid.SelectedIndex >= 0
                ? _originalGrid.SelectedIndex.ToString(
                    "000",
                    CultureInfo.InvariantCulture)
                : "ninguno";

        string sourceText =
            _sourceGrid.SelectedIndex >= 0
                ? _sourceGrid.SelectedIndex.ToString(
                    "000",
                    CultureInfo.InvariantCulture)
                : "ninguno";

        string destinationPosition =
            GetGridPositionText(
                _originalGrid.SelectedIndex,
                _originalGrid.Columns);

        string sourcePosition =
            GetGridPositionText(
                _sourceGrid.SelectedIndex,
                _sourceGrid.Columns);

        _selectionLabel.Text =
            $"Destino: {destinationText} {destinationPosition} | " +
            $"Fuente: {sourceText} {sourcePosition} | " +
            $"Orientación: {GetOrientationDisplayName(SelectedOrientation)}. " +
            "La selección sincronizada relaciona números iguales solo " +
            "como ayuda visual; la correspondencia real debe probarse.";
    }

    private void UpdateInformation()
    {
        string poseText =
            _poseEncoding is null
                ? "Pose nueva no preparada."
                : $"Pose nueva: " +
                  $"{_poseEncoding.TileCount} tiles, " +
                  $"{_poseEncoding.ByteCount:N0} bytes, " +
                  $"cuadrícula " +
                  $"{_poseEncoding.TileColumns} × " +
                  $"{_poseEncoding.TileRows}.";

        string segmentText =
            _originalSegmentTiles.Count == 0
                ? " Segmento ROM no cargado."
                : $" Segmento ROM: " +
                  $"{_originalSegmentTiles.Count} tiles, " +
                  $"{_originalSegmentTiles.Count * 32:N0} bytes, " +
                  $"offset real 0x{_actualSegmentOffset:X6}, " +
                  $"encabezado detectado: " +
                  $"{(_copierHeaderDetected ? "sí" : "no")}.";

        string paletteModeText =
            _romPaletteModeCombo.SelectedIndex == 1
                ? "escala de grises"
                : "paleta visual de Aladdin";

        string layoutModeText =
            _romLayoutModeCombo.SelectedIndex == 1
                ? "orden físico de 8 columnas"
                : _poseEncoding is null
                    ? "cuadrícula de pose no disponible"
                    : $"cuadrícula comparable " +
                      $"{_poseEncoding.TileColumns} × " +
                      $"{_poseEncoding.TileRows}";

        _summaryLabel.Text =
            poseText +
            segmentText +
            $" Cambios actuales: " +
            $"{_sourceTileByDestination.Count}. " +
            $"Vista ROM: {paletteModeText}; " +
            $"distribución: {layoutModeText}.";

        UpdateSelectionInformation();
    }

    private void UpdateButtons()
    {
        bool hasPose =
            _poseEncoding is not null &&
            _poseEncoding.TileCount > 0;

        bool hasSegment =
            _workingSegmentTiles.Count > 0;

        bool validDestination =
            IsValidDestinationIndex(
                _originalGrid.SelectedIndex);

        bool validSource =
            IsValidSourceIndex(
                _sourceGrid.SelectedIndex);

        _loadSegmentButton.Enabled =
            File.Exists(
                _romPathTextBox.Text);

        _assignTileButton.Enabled =
            hasPose &&
            hasSegment &&
            validDestination &&
            validSource;

        _restoreTileButton.Enabled =
            hasSegment &&
            validDestination &&
            _sourceTileByDestination.ContainsKey(
                _originalGrid.SelectedIndex);

        _assignLinearButton.Enabled =
            hasPose &&
            hasSegment;

        _clearChangesButton.Enabled =
            _sourceTileByDestination.Count > 0;

        _createRomButton.Enabled =
            _sourceTileByDestination.Count > 0 &&
            File.Exists(
                _romPathTextBox.Text) &&
            !string.IsNullOrWhiteSpace(
                _outputPathTextBox.Text);
    }

    private bool IsValidDestinationIndex(
        int index)
    {
        return
            index >= 0 &&
            index < _workingSegmentTiles.Count;
    }

    private bool IsValidSourceIndex(
        int index)
    {
        return
            _poseEncoding is not null &&
            index >= 0 &&
            index < _poseEncoding.TileCount;
    }

    private static byte[] CombineTiles(
        IReadOnlyList<byte[]> tiles)
    {
        byte[] result =
            new byte[
                tiles.Count *
                32];

        for (int index = 0;
             index < tiles.Count;
             index++)
        {
            if (tiles[index].Length != 32)
            {
                throw new InvalidOperationException(
                    "Todos los tiles deben medir 32 bytes.");
            }

            Buffer.BlockCopy(
                tiles[index],
                0,
                result,
                index *
                32,
                32);
        }

        return result;
    }

    private static bool TryParseHexOffset(
        string text,
        out int offset)
    {
        text =
            (text ?? string.Empty)
                .Trim();

        if (text.StartsWith(
                "0x",
                StringComparison.OrdinalIgnoreCase))
        {
            text =
                text[2..];
        }

        bool success =
            long.TryParse(
                text,
                NumberStyles.AllowHexSpecifier,
                CultureInfo.InvariantCulture,
                out long parsed) &&
            parsed >= 0 &&
            parsed <= int.MaxValue;

        offset =
            success
                ? (int)parsed
                : 0;

        return success;
    }

    private static string SanitizeFileName(
        string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "Personaje";
        }

        char[] invalid =
            Path.GetInvalidFileNameChars();

        string sanitized =
            new(
                value
                    .Select(character =>
                        invalid.Contains(character)
                            ? '_'
                            : character)
                    .ToArray());

        sanitized =
            sanitized.Trim();

        return string.IsNullOrWhiteSpace(sanitized)
            ? "Personaje"
            : sanitized;
    }
}
