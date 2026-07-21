using AladdinSpriteStudio.UI.Controls;
using AladdinSpriteStudio.UI.Core;
using AladdinSpriteStudio.UI.Graphics;

namespace AladdinSpriteStudio.UI;

public partial class MainForm : Form
{
    private Rom? _currentRom;
    private RomHeader? _currentHeader;

    private int? _currentPaletteOffset;
    private LoadedPaletteFile? _loadedPaletteFile;
    private string? _externalPaletteDescription;

    private readonly MenuStrip _menu;

    // Tile individual.
    private readonly TileViewer _tileViewer;
    private readonly NumericUpDown _tileOffsetInput;
    private readonly Button _showTileButton;
    private readonly Button _previousTileButton;
    private readonly Button _nextTileButton;

    // Paleta desde la ROM.
    private readonly NumericUpDown _paletteOffsetInput;
    private readonly Button _loadPaletteButton;
    private readonly Button _resetPaletteButton;

    // Paleta externa.
    private readonly NumericUpDown _paletteBankInput;
    private readonly Button _applyPaletteBankButton;
    private readonly Label _paletteFileInfoLabel;

    // Hoja de tiles.
    private readonly TileSheetViewer _tileSheetViewer;
    private readonly NumericUpDown _sheetOffsetInput;
    private readonly Button _showSheetButton;
    private readonly Button _exportPageButton;
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
        mainSplitContainer.Panel2.AutoScroll = true;

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
            Font = new Font(Font, FontStyle.Bold)
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
            Font = new Font(Font, FontStyle.Bold)
        };

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
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false
        };

        tileNavigationPanel.Controls.Add(
            _previousTileButton);

        tileNavigationPanel.Controls.Add(
            _nextTileButton);

        // =====================================================
        // PALETA DESDE LA ROM
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
            Text = "Cargar paleta desde ROM",
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
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false
        };

        paletteButtonsPanel.Controls.Add(
            _loadPaletteButton);

        paletteButtonsPanel.Controls.Add(
            _resetPaletteButton);

        // =====================================================
        // PALETA EXTERNA
        // =====================================================

        _paletteFileInfoLabel = new Label
        {
            Text = "Ninguna paleta externa cargada",
            AutoSize = true,
            MaximumSize = new Size(260, 0)
        };

        _paletteBankInput = new NumericUpDown
        {
            Minimum = 0,
            Maximum = 0,
            Width = 160,
            Enabled = false
        };

        _applyPaletteBankButton = new Button
        {
            Text = "Aplicar banco importado",
            AutoSize = true,
            Enabled = false
        };

        _applyPaletteBankButton.Click +=
            ApplyPaletteBankButton_Click;

        // =====================================================
        // HOJA DE TILES
        // =====================================================

        _sheetOffsetInput = new NumericUpDown
        {
            Hexadecimal = true,
            Minimum = 0,
            Maximum = 0,
            Increment = TilePageExporter.BytesPerPage,
            Width = 160,
            Enabled = false
        };

        _showSheetButton = new Button
        {
            Text = "Mostrar hoja",
            AutoSize = true,
            Enabled = false
        };

        _exportPageButton = new Button
        {
            Text = "Exportar página actual",
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

        _exportPageButton.Click +=
            ExportPageButton_Click;

        _previousSheetButton.Click +=
            PreviousSheetButton_Click;

        _nextSheetButton.Click +=
            NextSheetButton_Click;

        FlowLayoutPanel sheetNavigationPanel = new()
        {
            AutoSize = true,
            FlowDirection = FlowDirection.TopDown,
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
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            Padding = new Padding(12)
        };

        controlsPanel.Controls.Add(
            CreateSectionLabel("Tile individual"));

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
                "Paleta desde la ROM"));

        controlsPanel.Controls.Add(
            CreateNormalLabel(
                "Offset de paleta hexadecimal:"));

        controlsPanel.Controls.Add(
            _paletteOffsetInput);

        controlsPanel.Controls.Add(
            paletteButtonsPanel);

        controlsPanel.Controls.Add(
            CreateSectionLabel(
                "Paleta externa"));

        controlsPanel.Controls.Add(
            _paletteFileInfoLabel);

        controlsPanel.Controls.Add(
            CreateNormalLabel(
                "Banco de 16 colores:"));

        controlsPanel.Controls.Add(
            _paletteBankInput);

        controlsPanel.Controls.Add(
            _applyPaletteBankButton);

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
            _exportPageButton);

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

        ToolStripMenuItem importarPaleta =
            new("Importar paleta .pal");

        ToolStripMenuItem importarPagina =
            new("Importar página PNG en copia de la ROM...");

        ToolStripMenuItem exportarPagina =
            new("Exportar página actual...");

        ToolStripMenuItem exportarPaginas =
            new("Exportar todas las páginas de Aladdin...");

        ToolStripMenuItem exportarBloque =
            new("Exportar bloque completo de Aladdin...");

        abrirRom.Click +=
            AbrirRom_Click;

        importarPaleta.Click +=
            ImportPalette_Click;

        importarPagina.Click +=
            ImportTilePageToRomCopy_Click;

        exportarPagina.Click +=
            ExportPageButton_Click;

        exportarPaginas.Click +=
            ExportAllAladdinPages_Click;

        exportarBloque.Click +=
            ExportAladdinTiles_Click;

        archivo.DropDownItems.Add(
            abrirRom);

        archivo.DropDownItems.Add(
            new ToolStripSeparator());

        archivo.DropDownItems.Add(
            importarPaleta);

        archivo.DropDownItems.Add(
            importarPagina);

        archivo.DropDownItems.Add(
            new ToolStripSeparator());

        archivo.DropDownItems.Add(
            exportarPagina);

        archivo.DropDownItems.Add(
            exportarPaginas);

        archivo.DropDownItems.Add(
            exportarBloque);

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

        ShowTileAtOffset(
            offset);
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
            _previousTileButton.Enabled =
                false;

            _nextTileButton.Enabled =
                false;

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
    // IMPORTAR PALETA EXTERNA
    // =========================================================

    private void ImportPalette_Click(
        object? sender,
        EventArgs e)
    {
        using OpenFileDialog dialog = new()
        {
            Title = "Importar archivo de paleta",
            Filter =
                "Archivos de paleta (*.pal)|*.pal|" +
                "Todos los archivos (*.*)|*.*"
        };

        if (dialog.ShowDialog() !=
            DialogResult.OK)
        {
            return;
        }

        try
        {
            _loadedPaletteFile =
                PaletteFileLoader.Load(
                    dialog.FileName);

            _paletteBankInput.Minimum =
                0;

            _paletteBankInput.Maximum =
                Math.Max(
                    0,
                    _loadedPaletteFile.BankCount - 1);

            _paletteBankInput.Value =
                0;

            _paletteBankInput.Enabled =
                true;

            _applyPaletteBankButton.Enabled =
                true;

            _paletteFileInfoLabel.Text =
                $"{Path.GetFileName(dialog.FileName)}\n" +
                $"{_loadedPaletteFile.FormatName}\n" +
                $"{_loadedPaletteFile.ColorCount} colores\n" +
                $"{_loadedPaletteFile.BankCount} bancos";

            ApplyLoadedPaletteBank(
                0);

            MessageBox.Show(
                $"Paleta cargada correctamente.\n\n" +
                $"Archivo: " +
                $"{Path.GetFileName(dialog.FileName)}\n" +
                $"Formato: " +
                $"{_loadedPaletteFile.FormatName}\n" +
                $"Colores: " +
                $"{_loadedPaletteFile.ColorCount}\n" +
                $"Bancos de 16 colores: " +
                $"{_loadedPaletteFile.BankCount}",
                "Paleta importada",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                ex.Message,
                "Error al importar la paleta",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

    private void ApplyPaletteBankButton_Click(
        object? sender,
        EventArgs e)
    {
        if (_loadedPaletteFile is null)
        {
            return;
        }

        int bankIndex =
            decimal.ToInt32(
                _paletteBankInput.Value);

        ApplyLoadedPaletteBank(
            bankIndex);
    }

    private void ApplyLoadedPaletteBank(
        int bankIndex)
    {
        if (_loadedPaletteFile is null)
        {
            return;
        }

        Color[] palette =
            _loadedPaletteFile.GetBank(
                bankIndex);

        _tileViewer.Palette =
            palette;

        _tileSheetViewer.Palette =
            palette;

        _currentPaletteOffset =
            null;

        _externalPaletteDescription =
            $"Paleta externa: " +
            $"{Path.GetFileName(_loadedPaletteFile.FilePath)} " +
            $"| Banco {bankIndex}";

        if (_currentRom is not null)
        {
            int tileOffset =
                decimal.ToInt32(
                    _tileOffsetInput.Value);

            UpdateTileStatus(
                tileOffset);
        }

        _tileSheetViewer.Invalidate();
    }

    // =========================================================
    // PALETA DESDE LA ROM
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

            _externalPaletteDescription =
                null;

            int tileOffset =
                decimal.ToInt32(
                    _tileOffsetInput.Value);

            UpdateTileStatus(
                tileOffset);

            _tileSheetViewer.Invalidate();
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

        _currentPaletteOffset =
            null;

        _externalPaletteDescription =
            null;

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

        offset -=
            offset %
            Decoder4Bpp.BytesPerTile;

        int maximumOffset =
            decimal.ToInt32(
                _sheetOffsetInput.Maximum);

        offset =
            Math.Clamp(
                offset,
                0,
                maximumOffset);

        _sheetOffsetInput.Value =
            offset;

        _tileSheetViewer.StartOffset =
            offset;

        _tileSheetViewer.Invalidate();

        UpdateSheetNavigationButtons();

        int firstTileNumber =
            offset /
            Decoder4Bpp.BytesPerTile;

        statusLabelMain.Text =
            $"Hoja de tiles | " +
            $"Primer tile: #{firstTileNumber:N0} | " +
            $"Offset inicial: 0x{offset:X} | " +
            GetPaletteStatusText();
    }

    private void UpdateSheetNavigationButtons()
    {
        if (_currentRom is null)
        {
            _previousSheetButton.Enabled =
                false;

            _nextSheetButton.Enabled =
                false;

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
    // IMPORTAR PÁGINA PNG EN COPIA DE LA ROM
    // =========================================================

    private void ImportTilePageToRomCopy_Click(
        object? sender,
        EventArgs e)
    {
        if (_currentRom is null)
        {
            MessageBox.Show(
                "Primero debe abrir la ROM de Aladdin.",
                "ROM no cargada",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);

            return;
        }

        int startOffset =
            _tileSheetViewer.StartOffset;

        using OpenFileDialog pngDialog = new()
        {
            Title =
                $"Seleccionar página PNG para 0x{startOffset:X}",

            Filter =
                "Imagen PNG (*.png)|*.png",

            CheckFileExists = true,
            Multiselect = false
        };

        if (pngDialog.ShowDialog() !=
            DialogResult.OK)
        {
            return;
        }

        string romName =
            Path.GetFileNameWithoutExtension(
                _currentRom.FileName);

        string originalExtension =
            Path.GetExtension(
                _currentRom.FileName);

        if (string.IsNullOrWhiteSpace(
                originalExtension))
        {
            originalExtension =
                ".sfc";
        }

        string suggestedName =
            $"{romName}_Modificado_" +
            $"{startOffset:X5}" +
            originalExtension;

        using SaveFileDialog romDialog = new()
        {
            Title =
                "Guardar copia modificada de la ROM",

            Filter =
                "ROM de SNES (*.sfc)|*.sfc|" +
                "ROM de SNES con cabecera (*.smc)|*.smc",

            FileName =
                suggestedName,

            DefaultExt =
                originalExtension.TrimStart('.'),

            AddExtension = true,
            OverwritePrompt = true
        };

        if (romDialog.ShowDialog() !=
            DialogResult.OK)
        {
            return;
        }

        try
        {
            string originalPath =
                Path.GetFullPath(
                    _currentRom.FileName);

            string outputPath =
                Path.GetFullPath(
                    romDialog.FileName);

            if (string.Equals(
                    originalPath,
                    outputPath,
                    StringComparison.OrdinalIgnoreCase))
            {
                MessageBox.Show(
                    "No puede sobrescribir la ROM original.\n\n" +
                    "Seleccione otro nombre para la copia modificada.",
                    "Protección de la ROM original",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                return;
            }

            Color[] palette =
                _tileSheetViewer.Palette;

            TilePageImportResult result =
                TilePageImporter.ImportPageToRomCopy(
                    _currentRom.Data,
                    startOffset,
                    pngDialog.FileName,
                    palette,
                    outputPath);

            MessageBox.Show(
                $"Página insertada correctamente.\n\n" +
                $"PNG utilizado:\n" +
                $"{pngDialog.FileName}\n\n" +
                $"Tiles insertados: " +
                $"{result.TileCount:N0}\n" +
                $"Inicio: 0x{result.StartOffset:X}\n" +
                $"Final: 0x{result.FinalOffset:X}\n\n" +
                $"ROM modificada:\n" +
                $"{result.OutputRomPath}\n\n" +
                "La ROM original no fue modificada.",
                "ROM modificada creada",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);

            statusLabelMain.Text =
                $"Página importada | " +
                $"0x{result.StartOffset:X}–" +
                $"0x{result.FinalOffset:X} | " +
                $"{result.TileCount:N0} tiles";
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                ex.Message,
                "Error al importar la página",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

    // =========================================================
    // EXPORTAR PÁGINA ACTUAL
    // =========================================================

    private void ExportPageButton_Click(
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

        int startOffset =
            _tileSheetViewer.StartOffset;

        int tileCount =
            TilePageExporter.GetTileCountForPage(
                _currentRom.Data,
                startOffset);

        if (tileCount <= 0)
        {
            MessageBox.Show(
                "No hay tiles válidos para exportar " +
                "desde la página mostrada.",
                "Exportación no válida",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);

            return;
        }

        int finalOffset =
            startOffset +
            tileCount *
            Decoder4Bpp.BytesPerTile -
            1;

        string romName =
            Path.GetFileNameWithoutExtension(
                _currentRom.FileName);

        string defaultFileName =
            $"{romName}_Page_" +
            $"{startOffset:X5}_" +
            $"{finalOffset:X5}.png";

        using SaveFileDialog dialog = new()
        {
            Title = "Exportar página de tiles",
            Filter = "Imagen PNG (*.png)|*.png",
            DefaultExt = "png",
            AddExtension = true,
            FileName = defaultFileName,
            OverwritePrompt = true
        };

        if (dialog.ShowDialog() !=
            DialogResult.OK)
        {
            return;
        }

        try
        {
            string pngPath =
                Path.ChangeExtension(
                    dialog.FileName,
                    ".png");

            string binPath =
                Path.ChangeExtension(
                    pngPath,
                    ".bin");

            Color[] palette =
                _tileSheetViewer.Palette;

            int exportedTiles =
                TilePageExporter.ExportPage(
                    _currentRom.Data,
                    startOffset,
                    palette,
                    pngPath,
                    binPath);

            MessageBox.Show(
                $"Página exportada correctamente.\n\n" +
                $"Tiles exportados: {exportedTiles:N0}\n" +
                $"Inicio: 0x{startOffset:X}\n" +
                $"Final: 0x{finalOffset:X}\n\n" +
                $"Imagen PNG:\n{pngPath}\n\n" +
                $"Datos 4BPP:\n{binPath}",
                "Página exportada",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);

            statusLabelMain.Text =
                $"Página exportada | " +
                $"0x{startOffset:X}–0x{finalOffset:X} | " +
                $"{exportedTiles:N0} tiles";
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                ex.Message,
                "Error al exportar la página",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

    // =========================================================
    // EXPORTAR TODAS LAS PÁGINAS
    // =========================================================

    private void ExportAllAladdinPages_Click(
        object? sender,
        EventArgs e)
    {
        if (_currentRom is null)
        {
            MessageBox.Show(
                "Primero debe abrir la ROM de Aladdin.",
                "ROM no cargada",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);

            return;
        }

        const int blockStartOffset =
            0x40000;

        const int blockEndOffsetExclusive =
            0x4F000;

        if (_currentRom.Data.Length <
            blockEndOffsetExclusive)
        {
            MessageBox.Show(
                "La ROM no contiene el bloque esperado " +
                "entre 0x40000 y 0x4F000.",
                "ROM incompatible",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);

            return;
        }

        using FolderBrowserDialog dialog = new()
        {
            Description =
                "Seleccione la carpeta donde se guardarán " +
                "todas las páginas de Aladdin."
        };

        if (dialog.ShowDialog() !=
            DialogResult.OK)
        {
            return;
        }

        try
        {
            string romName =
                Path.GetFileNameWithoutExtension(
                    _currentRom.FileName);

            string outputDirectory =
                Path.Combine(
                    dialog.SelectedPath,
                    "Aladdin_Pages_40000_4EFFF");

            Directory.CreateDirectory(
                outputDirectory);

            Color[] palette =
                _tileSheetViewer.Palette;

            List<string> manifestLines =
            [
                "ALADDIN SPRITE STUDIO",
                "Exportación de páginas de tiles",
                "",
                $"ROM: {_currentRom.FileName}",
                $"Rango: 0x{blockStartOffset:X}–" +
                $"0x{blockEndOffsetExclusive - 1:X}",
                $"Paleta: {GetPaletteStatusText()}",
                ""
            ];

            int exportedPageCount = 0;
            int exportedTileCount = 0;

            for (int pageStartOffset =
                     blockStartOffset;

                 pageStartOffset <
                     blockEndOffsetExclusive;

                 pageStartOffset +=
                     TilePageExporter.BytesPerPage)
            {
                int pageEndOffsetExclusive =
                    Math.Min(
                        pageStartOffset +
                        TilePageExporter.BytesPerPage,
                        blockEndOffsetExclusive);

                int tileCount =
                    TilePageExporter.GetTileCountForPage(
                        _currentRom.Data,
                        pageStartOffset,
                        pageEndOffsetExclusive);

                if (tileCount <= 0)
                {
                    continue;
                }

                int pageFinalOffset =
                    pageStartOffset +
                    tileCount *
                    Decoder4Bpp.BytesPerTile -
                    1;

                string baseFileName =
                    $"{romName}_Page_" +
                    $"{pageStartOffset:X5}_" +
                    $"{pageFinalOffset:X5}";

                string pngPath =
                    Path.Combine(
                        outputDirectory,
                        baseFileName + ".png");

                string binPath =
                    Path.Combine(
                        outputDirectory,
                        baseFileName + ".bin");

                int exportedTiles =
                    TilePageExporter.ExportPage(
                        _currentRom.Data,
                        pageStartOffset,
                        palette,
                        pngPath,
                        binPath,
                        pageEndOffsetExclusive);

                exportedPageCount++;
                exportedTileCount +=
                    exportedTiles;

                manifestLines.Add(
                    $"Página {exportedPageCount}: " +
                    $"0x{pageStartOffset:X5}–" +
                    $"0x{pageFinalOffset:X5} | " +
                    $"{exportedTiles} tiles | " +
                    $"{baseFileName}");
            }

            string manifestPath =
                Path.Combine(
                    outputDirectory,
                    "Aladdin_Pages_Manifest.txt");

            manifestLines.Add("");
            manifestLines.Add(
                $"Páginas exportadas: " +
                $"{exportedPageCount}");

            manifestLines.Add(
                $"Tiles exportados: " +
                $"{exportedTileCount}");

            File.WriteAllLines(
                manifestPath,
                manifestLines);

            int generatedFileCount =
                exportedPageCount * 2 + 1;

            MessageBox.Show(
                $"Exportación completada correctamente.\n\n" +
                $"Páginas exportadas: {exportedPageCount}\n" +
                $"Tiles exportados: {exportedTileCount:N0}\n" +
                $"Archivos generados: {generatedFileCount}\n\n" +
                $"Carpeta:\n{outputDirectory}\n\n" +
                $"Manifiesto:\n{manifestPath}",
                "Páginas exportadas",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);

            statusLabelMain.Text =
                $"Exportadas {exportedPageCount} páginas | " +
                $"{exportedTileCount:N0} tiles | " +
                $"0x{blockStartOffset:X}–" +
                $"0x{blockEndOffsetExclusive - 1:X}";
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                ex.Message,
                "Error al exportar las páginas",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

    // =========================================================
    // EXPORTAR BLOQUE COMPLETO
    // =========================================================

    private void ExportAladdinTiles_Click(
        object? sender,
        EventArgs e)
    {
        if (_currentRom is null)
        {
            MessageBox.Show(
                "Primero debe abrir la ROM de Aladdin.",
                "ROM no cargada",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);

            return;
        }

        const int startOffset =
            0x40000;

        const int endOffsetExclusive =
            0x4F000;

        if (_currentRom.Data.Length <
            endOffsetExclusive)
        {
            MessageBox.Show(
                "La ROM cargada no contiene el bloque " +
                "esperado entre 0x40000 y 0x4F000.",
                "ROM incompatible",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);

            return;
        }

        using SaveFileDialog dialog = new()
        {
            Title =
                "Exportar bloque completo de Aladdin",

            Filter =
                "Imagen PNG (*.png)|*.png",

            DefaultExt =
                "png",

            AddExtension =
                true,

            FileName =
                "Aladdin_Tiles_40000_4EFFF.png"
        };

        if (dialog.ShowDialog() !=
            DialogResult.OK)
        {
            return;
        }

        try
        {
            string pngPath =
                Path.ChangeExtension(
                    dialog.FileName,
                    ".png");

            Color[] currentPalette =
                _tileSheetViewer.Palette;

            int tileCount =
                TileBlockExporter.ExportPngAndRaw(
                    _currentRom.Data,
                    startOffset,
                    endOffsetExclusive,
                    currentPalette,
                    pngPath,
                    columns: 16,
                    scale: 4,
                    transparentColorZero: true);

            string binaryFilePath =
                Path.ChangeExtension(
                    pngPath,
                    ".bin");

            MessageBox.Show(
                $"Exportación completada correctamente.\n\n" +
                $"Tiles exportados: {tileCount:N0}\n" +
                $"Inicio: 0x{startOffset:X}\n" +
                $"Final: 0x{endOffsetExclusive - 1:X}\n\n" +
                $"Imagen:\n{pngPath}\n\n" +
                $"Datos 4BPP:\n{binaryFilePath}",
                "Tiles exportados",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);

            statusLabelMain.Text =
                $"Exportados {tileCount:N0} tiles " +
                $"desde 0x{startOffset:X} " +
                $"hasta 0x{endOffsetExclusive - 1:X}";
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                ex.Message,
                "Error al exportar los tiles",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

    // =========================================================
    // ESTADO
    // =========================================================

    private string GetPaletteStatusText()
    {
        if (!string.IsNullOrWhiteSpace(
                _externalPaletteDescription))
        {
            return _externalPaletteDescription;
        }

        if (_currentPaletteOffset
            is int paletteOffset)
        {
            return
                $"Paleta ROM: 0x{paletteOffset:X}";
        }

        return
            "Paleta: escala de grises";
    }

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

        statusLabelMain.Text =
            $"ROM: {fileName} | " +
            $"{mapping} | " +
            $"{_currentRom.Size:N0} bytes | " +
            $"Tile #{tileNumber:N0} | " +
            $"Offset: 0x{tileOffset:X} | " +
            GetPaletteStatusText();
    }

    // =========================================================
    // ABRIR ROM
    // =========================================================

    private void AbrirRom_Click(
        object? sender,
        EventArgs e)
    {
        using OpenFileDialog dialog = new()
        {
            Filter =
                "SNES ROM (*.sfc;*.smc)|*.sfc;*.smc"
        };

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

            ResetPaletteSelection();

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

                _paletteOffsetInput.Value =
                    0;

                _paletteOffsetInput.Enabled =
                    true;

                _loadPaletteButton.Enabled =
                    true;

                _resetPaletteButton.Enabled =
                    true;
            }

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

                _tileOffsetInput.Value =
                    0;

                _tileOffsetInput.Enabled =
                    true;

                _showTileButton.Enabled =
                    true;

                _sheetOffsetInput.Maximum =
                    alignedTileOffset;

                _sheetOffsetInput.Value =
                    0;

                _sheetOffsetInput.Enabled =
                    true;

                _showSheetButton.Enabled =
                    true;

                _exportPageButton.Enabled =
                    true;

                _tileSheetViewer.RomData =
                    _currentRom.Data;

                ShowTileAtOffset(
                    0);

                ShowSheetAtOffset(
                    0);
            }

            Text =
                $"Aladdin Sprite Studio - " +
                $"{fileName}";

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
                $"Checksum: " +
                $"0x{header.Checksum:X4}\n" +
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

    private void ResetPaletteSelection()
    {
        _currentPaletteOffset =
            null;

        _externalPaletteDescription =
            null;

        _loadedPaletteFile =
            null;

        _paletteFileInfoLabel.Text =
            "Ninguna paleta externa cargada";

        _paletteBankInput.Minimum =
            0;

        _paletteBankInput.Maximum =
            0;

        _paletteBankInput.Value =
            0;

        _paletteBankInput.Enabled =
            false;

        _applyPaletteBankButton.Enabled =
            false;

        _tileViewer.ResetPalette();
        _tileSheetViewer.ResetPalette();
    }

    private void ResetInterface()
    {
        _currentRom =
            null;

        _currentHeader =
            null;

        ResetPaletteSelection();

        _tileOffsetInput.Enabled =
            false;

        _showTileButton.Enabled =
            false;

        _previousTileButton.Enabled =
            false;

        _nextTileButton.Enabled =
            false;

        _paletteOffsetInput.Enabled =
            false;

        _loadPaletteButton.Enabled =
            false;

        _resetPaletteButton.Enabled =
            false;

        _sheetOffsetInput.Enabled =
            false;

        _showSheetButton.Enabled =
            false;

        _exportPageButton.Enabled =
            false;

        _previousSheetButton.Enabled =
            false;

        _nextSheetButton.Enabled =
            false;

        _tileViewer.Pixels =
            null;

        _tileSheetViewer.RomData =
            null;

        statusLabelMain.Text =
            "Sin ROM cargada";

        Text =
            "Aladdin Sprite Studio";
    }
}