using AladdinSpriteStudio.UI.Controls;
using AladdinSpriteStudio.UI.Core;
using AladdinSpriteStudio.UI.Graphics;

namespace AladdinSpriteStudio.UI;

public partial class MainForm : Form
{
    private Rom? _currentRom;
    private RomHeader? _currentHeader;
    private int? _currentPaletteOffset;

    private readonly MenuStrip _menu;

    // Visor individual.
    private readonly TileViewer _tileViewer;
    private readonly NumericUpDown _tileOffsetInput;
    private readonly Button _showTileButton;
    private readonly Button _previousTileButton;
    private readonly Button _nextTileButton;

    // Paleta.
    private readonly NumericUpDown _paletteOffsetInput;
    private readonly Button _loadPaletteButton;
    private readonly Button _resetPaletteButton;

    // Hoja de 256 tiles.
    private readonly TileSheetViewer _tileSheetViewer;
    private readonly NumericUpDown _sheetOffsetInput;
    private readonly Button _showSheetButton;
    private readonly Button _previousSheetButton;
    private readonly Button _nextSheetButton;

    public MainForm()
    {
        InitializeComponent();

#if DEBUG
        if (!Decoder4Bpp.RunSelfTest())
        {
            throw new InvalidOperationException(
                "La prueba del decodificador 4BPP falló.");
        }
#endif

        Text = "Aladdin Sprite Studio";
        Width = 1400;
        Height = 900;
        StartPosition = FormStartPosition.CenterScreen;
        DoubleBuffered = true;

        mainSplitContainer.SplitterDistance = 310;

        // =====================================================
        // VISOR INDIVIDUAL
        // =====================================================

        _tileViewer = new TileViewer
        {
            Zoom = 32,
            ShowGrid = true,
            Location = new Point(24, 40)
        };

        Label individualViewerLabel = new()
        {
            Text = "Tile individual",
            AutoSize = true,
            Location = new Point(24, 16),
            Font = new Font(
                Font,
                FontStyle.Bold)
        };

        // =====================================================
        // VISOR DE HOJA DE TILES
        // =====================================================

        _tileSheetViewer = new TileSheetViewer
        {
            Columns = 16,
            Rows = 16,
            Zoom = 4,
            ShowTileGrid = true,
            Location = new Point(24, 350)
        };
           _tileSheetViewer.TileSelected +=
            TileSheetViewer_TileSelected;

        Label sheetViewerLabel = new()
        {
            Text = "Hoja de tiles (16 × 16)",
            AutoSize = true,
            Location = new Point(24, 325),
            Font = new Font(
                Font,
                FontStyle.Bold)
        };

        mainSplitContainer.Panel2.AutoScroll = true;

        mainSplitContainer.Panel2.Controls.Add(
            individualViewerLabel);

        mainSplitContainer.Panel2.Controls.Add(
            _tileViewer);

        mainSplitContainer.Panel2.Controls.Add(
            sheetViewerLabel);

        mainSplitContainer.Panel2.Controls.Add(
            _tileSheetViewer);

        // =====================================================
        // CONTROLES DEL TILE INDIVIDUAL
        // =====================================================

        _tileOffsetInput = new NumericUpDown
        {
            Hexadecimal = true,
            Minimum = 0,
            Maximum = 0,
            Increment = Decoder4Bpp.BytesPerTile,
            Width = 160,
            Enabled = false
        };

        _showTileButton = new Button
        {
            Text = "Mostrar tile",
            AutoSize = true,
            Enabled = false
        };

        _previousTileButton = new Button
        {
            Text = "◀ Tile anterior",
            AutoSize = true,
            Enabled = false
        };

        _nextTileButton = new Button
        {
            Text = "Tile siguiente ▶",
            AutoSize = true,
            Enabled = false
        };

        _showTileButton.Click +=
            ShowTileButton_Click;

        _previousTileButton.Click +=
            PreviousTileButton_Click;

        _nextTileButton.Click +=
            NextTileButton_Click;

        FlowLayoutPanel tileNavigationPanel = new()
        {
            AutoSize = true,
            FlowDirection =
                FlowDirection.LeftToRight,
            WrapContents = false
        };

        tileNavigationPanel.Controls.Add(
            _previousTileButton);

        tileNavigationPanel.Controls.Add(
            _nextTileButton);

        // =====================================================
        // CONTROLES DE PALETA
        // =====================================================

        _paletteOffsetInput = new NumericUpDown
        {
            Hexadecimal = true,
            Minimum = 0,
            Maximum = 0,
            Increment = SnesPalette.BytesPerPalette,
            Width = 160,
            Enabled = false
        };

        _loadPaletteButton = new Button
        {
            Text = "Cargar paleta",
            AutoSize = true,
            Enabled = false
        };

        _resetPaletteButton = new Button
        {
            Text = "Usar escala de grises",
            AutoSize = true,
            Enabled = false
        };

        _loadPaletteButton.Click +=
            LoadPaletteButton_Click;

        _resetPaletteButton.Click +=
            ResetPaletteButton_Click;

        FlowLayoutPanel paletteButtonsPanel = new()
        {
            AutoSize = true,
            FlowDirection =
                FlowDirection.TopDown,
            WrapContents = false
        };

        paletteButtonsPanel.Controls.Add(
            _loadPaletteButton);

        paletteButtonsPanel.Controls.Add(
            _resetPaletteButton);

        // =====================================================
        // CONTROLES DE LA HOJA DE TILES
        // =====================================================

        _sheetOffsetInput = new NumericUpDown
        {
            Hexadecimal = true,
            Minimum = 0,
            Maximum = 0,
            Increment = 0x2000,
            Width = 160,
            Enabled = false
        };

        _showSheetButton = new Button
        {
            Text = "Mostrar hoja",
            AutoSize = true,
            Enabled = false
        };

        _previousSheetButton = new Button
        {
            Text = "◀ Página anterior",
            AutoSize = true,
            Enabled = false
        };

        _nextSheetButton = new Button
        {
            Text = "Página siguiente ▶",
            AutoSize = true,
            Enabled = false
        };

        _showSheetButton.Click +=
            ShowSheetButton_Click;

        _previousSheetButton.Click +=
            PreviousSheetButton_Click;

        _nextSheetButton.Click +=
            NextSheetButton_Click;

        FlowLayoutPanel sheetNavigationPanel = new()
        {
            AutoSize = true,
            FlowDirection =
                FlowDirection.TopDown,
            WrapContents = false
        };

        sheetNavigationPanel.Controls.Add(
            _previousSheetButton);

        sheetNavigationPanel.Controls.Add(
            _nextSheetButton);

        // =====================================================
        // PANEL IZQUIERDO
        // =====================================================

        FlowLayoutPanel controlsPanel = new()
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            FlowDirection =
                FlowDirection.TopDown,
            WrapContents = false,
            Padding = new Padding(12)
        };

        controlsPanel.Controls.Add(
            CreateSectionLabel(
                "Tile individual"));

        controlsPanel.Controls.Add(
            CreateNormalLabel(
                "Offset ROM hexadecimal:"));

        controlsPanel.Controls.Add(
            _tileOffsetInput);

        controlsPanel.Controls.Add(
            _showTileButton);

        controlsPanel.Controls.Add(
            tileNavigationPanel);

        controlsPanel.Controls.Add(
            CreateSectionLabel(
                "Paleta de colores"));

        controlsPanel.Controls.Add(
            CreateNormalLabel(
                "Offset de paleta hexadecimal:"));

        controlsPanel.Controls.Add(
            _paletteOffsetInput);

        controlsPanel.Controls.Add(
            paletteButtonsPanel);

        controlsPanel.Controls.Add(
            CreateSectionLabel(
                "Hoja de tiles"));

        controlsPanel.Controls.Add(
            CreateNormalLabel(
                "Offset inicial hexadecimal:"));

        controlsPanel.Controls.Add(
            _sheetOffsetInput);

        controlsPanel.Controls.Add(
            _showSheetButton);

        controlsPanel.Controls.Add(
            sheetNavigationPanel);

        mainSplitContainer.Panel1.Controls.Add(
            controlsPanel);

        // =====================================================
        // MENÚ
        // =====================================================

        _menu = new MenuStrip();

        BuildMenu();

        Controls.Add(_menu);
        MainMenuStrip = _menu;
    }

    private Label CreateSectionLabel(
        string text)
    {
        return new Label
        {
            Text = text,
            AutoSize = true,
            Font = new Font(
                Font,
                FontStyle.Bold),
            Margin = new Padding(
                3,
                18,
                3,
                5)
        };
    }

    private static Label CreateNormalLabel(
        string text)
    {
        return new Label
        {
            Text = text,
            AutoSize = true
        };
    }

    private void BuildMenu()
    {
        ToolStripMenuItem archivo =
            new("Archivo");

        ToolStripMenuItem abrirRom =
            new("Abrir ROM");

        abrirRom.Click += AbrirRom_Click;

        archivo.DropDownItems.Add(
            abrirRom);

        _menu.Items.Add(
            archivo);
    }

    // =========================================================
    // TILE INDIVIDUAL
    // =========================================================

    private void ShowTileButton_Click(
        object? sender,
        EventArgs e)
    {
        if (_currentRom is null)
        {
            MessageBox.Show(
                "Primero debe abrir una ROM.",
                "ROM no cargada",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);

            return;
        }

        int offset =
            decimal.ToInt32(
                _tileOffsetInput.Value);

        ShowTileAtOffset(offset);
    }

    private void PreviousTileButton_Click(
        object? sender,
        EventArgs e)
    {
        MoveTileOffset(
            -Decoder4Bpp.BytesPerTile);
    }

    private void NextTileButton_Click(
        object? sender,
        EventArgs e)
    {
        MoveTileOffset(
            Decoder4Bpp.BytesPerTile);
    }

    private void MoveTileOffset(
        int amount)
    {
        if (_currentRom is null)
        {
            return;
        }

        int currentOffset =
            decimal.ToInt32(
                _tileOffsetInput.Value);

        int maximumOffset =
            decimal.ToInt32(
                _tileOffsetInput.Maximum);

        int newOffset =
            Math.Clamp(
                currentOffset + amount,
                0,
                maximumOffset);

        _tileOffsetInput.Value =
            newOffset;

        ShowTileAtOffset(
            newOffset);
    }

    private void ShowTileAtOffset(
        int offset)
    {
        if (_currentRom is null)
        {
            return;
        }

        byte[,] pixels =
            Decoder4Bpp.DecodeTile(
                _currentRom.Data,
                offset);

        _tileViewer.Pixels =
            pixels;

        UpdateTileStatus(
            offset);

        UpdateTileNavigationButtons();
    }

    private void UpdateTileNavigationButtons()
    {
        if (_currentRom is null)
        {
            _previousTileButton.Enabled = false;
            _nextTileButton.Enabled = false;

            return;
        }

        int currentOffset =
            decimal.ToInt32(
                _tileOffsetInput.Value);

        int maximumOffset =
            decimal.ToInt32(
                _tileOffsetInput.Maximum);

        _previousTileButton.Enabled =
            currentOffset > 0;

        _nextTileButton.Enabled =
            currentOffset < maximumOffset;
    }

    private void TileSheetViewer_TileSelected(
    object? sender,
    TileSelectedEventArgs e)
    {
        if (_currentRom is null)
        {
            return;
        }

        int maximumOffset =
            decimal.ToInt32(
                _tileOffsetInput.Maximum);

        int selectedOffset =
            Math.Clamp(
                e.Offset,
                0,
                maximumOffset);

        _tileOffsetInput.Value =
            selectedOffset;

        ShowTileAtOffset(
            selectedOffset);
    }

    // =========================================================
    // PALETA
    // =========================================================

    private void LoadPaletteButton_Click(
        object? sender,
        EventArgs e)
    {
        if (_currentRom is null)
        {
            return;
        }

        try
        {
            int paletteOffset =
                decimal.ToInt32(
                    _paletteOffsetInput.Value);

            Color[] palette =
                SnesPalette.DecodePalette(
                    _currentRom.Data,
                    paletteOffset);

            _tileViewer.Palette =
                palette;

            _tileSheetViewer.Palette =
                palette;

            _currentPaletteOffset =
                paletteOffset;

            int tileOffset =
                decimal.ToInt32(
                    _tileOffsetInput.Value);

            UpdateTileStatus(
                tileOffset);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                ex.Message,
                "Error al cargar la paleta",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

    private void ResetPaletteButton_Click(
        object? sender,
        EventArgs e)
    {
        _tileViewer.ResetPalette();
        _tileSheetViewer.ResetPalette();

        _currentPaletteOffset = null;

        if (_currentRom is not null)
        {
            int tileOffset =
                decimal.ToInt32(
                    _tileOffsetInput.Value);

            UpdateTileStatus(
                tileOffset);
        }
    }

    // =========================================================
    // HOJA DE TILES
    // =========================================================

    private void ShowSheetButton_Click(
        object? sender,
        EventArgs e)
    {
        if (_currentRom is null)
        {
            return;
        }

        int offset =
            decimal.ToInt32(
                _sheetOffsetInput.Value);

        ShowSheetAtOffset(
            offset);
    }

    private void PreviousSheetButton_Click(
        object? sender,
        EventArgs e)
    {
        MoveSheetOffset(
            -_tileSheetViewer.BytesPerPage);
    }

    private void NextSheetButton_Click(
        object? sender,
        EventArgs e)
    {
        MoveSheetOffset(
            _tileSheetViewer.BytesPerPage);
    }

    private void MoveSheetOffset(
        int amount)
    {
        if (_currentRom is null)
        {
            return;
        }

        int currentOffset =
            decimal.ToInt32(
                _sheetOffsetInput.Value);

        int maximumOffset =
            decimal.ToInt32(
                _sheetOffsetInput.Maximum);

        int newOffset =
            Math.Clamp(
                currentOffset + amount,
                0,
                maximumOffset);

        newOffset -=
            newOffset %
            Decoder4Bpp.BytesPerTile;

        _sheetOffsetInput.Value =
            newOffset;

        ShowSheetAtOffset(
            newOffset);
    }

    private void ShowSheetAtOffset(
        int offset)
    {
        if (_currentRom is null)
        {
            return;
        }

        _tileSheetViewer.StartOffset =
            offset;

        _tileSheetViewer.Invalidate();

        UpdateSheetNavigationButtons();

        int firstTileNumber =
            offset /
            Decoder4Bpp.BytesPerTile;

        string paletteInformation =
            _currentPaletteOffset
                is int paletteOffset
                ? $"Paleta: 0x{paletteOffset:X}"
                : "Paleta: escala de grises";

        statusLabelMain.Text =
            $"Hoja de tiles | " +
            $"Primer tile: #{firstTileNumber:N0} | " +
            $"Offset inicial: 0x{offset:X} | " +
            paletteInformation;
    }

    private void UpdateSheetNavigationButtons()
    {
        if (_currentRom is null)
        {
            _previousSheetButton.Enabled = false;
            _nextSheetButton.Enabled = false;

            return;
        }

        int currentOffset =
            decimal.ToInt32(
                _sheetOffsetInput.Value);

        int maximumOffset =
            decimal.ToInt32(
                _sheetOffsetInput.Maximum);

        _previousSheetButton.Enabled =
            currentOffset > 0;

        _nextSheetButton.Enabled =
            currentOffset < maximumOffset;
    }

    // =========================================================
    // ESTADO
    // =========================================================

    private void UpdateTileStatus(
        int tileOffset)
    {
        if (_currentRom is null)
        {
            return;
        }

        string fileName =
            Path.GetFileName(
                _currentRom.FileName);

        string mapping =
            _currentHeader?.MappingName
            ?? "Desconocido";

        int tileNumber =
            tileOffset /
            Decoder4Bpp.BytesPerTile;

        string paletteInformation =
            _currentPaletteOffset
                is int paletteOffset
                ? $"Paleta: 0x{paletteOffset:X}"
                : "Paleta: escala de grises";

        statusLabelMain.Text =
            $"ROM: {fileName} | " +
            $"{mapping} | " +
            $"{_currentRom.Size:N0} bytes | " +
            $"Tile #{tileNumber:N0} | " +
            $"Offset: 0x{tileOffset:X} | " +
            paletteInformation;
    }

    // =========================================================
    // ABRIR ROM
    // =========================================================

    private void AbrirRom_Click(
        object? sender,
        EventArgs e)
    {
        using OpenFileDialog dialog =
            new();

        dialog.Filter =
            "SNES ROM (*.sfc;*.smc)|*.sfc;*.smc";

        if (dialog.ShowDialog() !=
            DialogResult.OK)
        {
            return;
        }

        try
        {
            _currentRom =
                RomLoader.Load(
                    dialog.FileName);

            RomHeader header =
                HeaderReader.Read(
                    _currentRom.Data);

            _currentHeader =
                header;

            string fileName =
                Path.GetFileName(
                    _currentRom.FileName);

            _currentPaletteOffset =
                null;

            _tileViewer.ResetPalette();
            _tileSheetViewer.ResetPalette();

            // Configurar paletas.
            int maximumPaletteOffset =
                _currentRom.Data.Length -
                SnesPalette.BytesPerPalette;

            if (maximumPaletteOffset >= 0)
            {
                int alignedPaletteOffset =
                    maximumPaletteOffset -
                    maximumPaletteOffset %
                    SnesPalette.BytesPerColor;

                _paletteOffsetInput.Maximum =
                    alignedPaletteOffset;

                _paletteOffsetInput.Value = 0;
                _paletteOffsetInput.Enabled = true;

                _loadPaletteButton.Enabled = true;
                _resetPaletteButton.Enabled = true;
            }

            // Configurar tiles.
            int maximumTileOffset =
                _currentRom.Data.Length -
                Decoder4Bpp.BytesPerTile;

            if (maximumTileOffset >= 0)
            {
                int alignedTileOffset =
                    maximumTileOffset -
                    maximumTileOffset %
                    Decoder4Bpp.BytesPerTile;

                _tileOffsetInput.Maximum =
                    alignedTileOffset;

                _tileOffsetInput.Value = 0;
                _tileOffsetInput.Enabled = true;

                _showTileButton.Enabled = true;

                // Configurar hoja.
                _sheetOffsetInput.Maximum =
                    alignedTileOffset;

                _sheetOffsetInput.Value = 0;
                _sheetOffsetInput.Enabled = true;

                _showSheetButton.Enabled = true;

                _tileSheetViewer.RomData =
                    _currentRom.Data;

                ShowTileAtOffset(0);
                ShowSheetAtOffset(0);
            }

            Text =
                $"Aladdin Sprite Studio - {fileName}";

            MessageBox.Show(
                $"ROM cargada correctamente.\n\n" +
                $"Archivo: {_currentRom.FileName}\n" +
                $"Tamaño: {_currentRom.Size:N0} bytes\n\n" +
                $"Título interno: {header.Title}\n" +
                $"Mapeo: {header.MappingName}\n" +
                $"Offset del encabezado: " +
                $"0x{header.HeaderOffset:X}\n" +
                $"Cabecera de copiador: " +
                $"{(header.HasCopierHeader ? "Sí" : "No")}\n" +
                $"Checksum: 0x{header.Checksum:X4}\n" +
                $"Complemento: " +
                $"0x{header.ChecksumComplement:X4}\n" +
                $"Checksum coherente: " +
                $"{(header.HasValidChecksum ? "Sí" : "No")}",
                "Información de la ROM",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            ResetInterface();

            MessageBox.Show(
                ex.Message,
                "Error al cargar la ROM",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

    private void ResetInterface()
    {
        _currentRom = null;
        _currentHeader = null;
        _currentPaletteOffset = null;

        _tileOffsetInput.Enabled = false;
        _showTileButton.Enabled = false;
        _previousTileButton.Enabled = false;
        _nextTileButton.Enabled = false;

        _paletteOffsetInput.Enabled = false;
        _loadPaletteButton.Enabled = false;
        _resetPaletteButton.Enabled = false;

        _sheetOffsetInput.Enabled = false;
        _showSheetButton.Enabled = false;
        _previousSheetButton.Enabled = false;
        _nextSheetButton.Enabled = false;

        _tileViewer.Pixels = null;
        _tileViewer.ResetPalette();

        _tileSheetViewer.RomData = null;
        _tileSheetViewer.ResetPalette();

        statusLabelMain.Text =
            "Sin ROM cargada";
    }
}