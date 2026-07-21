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
    private readonly TileViewer _tileViewer;

    private readonly NumericUpDown _tileOffsetInput;
    private readonly Button _showTileButton;
    private readonly Button _previousTileButton;
    private readonly Button _nextTileButton;

    private readonly NumericUpDown _paletteOffsetInput;
    private readonly Button _loadPaletteButton;
    private readonly Button _resetPaletteButton;

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

        // Visor del tile.
        _tileViewer = new TileViewer
        {
            Zoom = 32,
            ShowGrid = true,
            Location = new Point(24, 24)
        };

        mainSplitContainer.Panel2.AutoScroll = true;
        mainSplitContainer.Panel2.Controls.Add(_tileViewer);

        // Selector del offset del tile.
        _tileOffsetInput = new NumericUpDown
        {
            Hexadecimal = true,
            Minimum = 0,
            Maximum = 0,
            Increment = Decoder4Bpp.BytesPerTile,
            Width = 150,
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

        _showTileButton.Click += ShowTileButton_Click;
        _previousTileButton.Click += PreviousTileButton_Click;
        _nextTileButton.Click += NextTileButton_Click;

        FlowLayoutPanel navigationButtonsPanel = new()
        {
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false
        };

        navigationButtonsPanel.Controls.Add(_previousTileButton);
        navigationButtonsPanel.Controls.Add(_nextTileButton);

        // Selector del offset de la paleta.
        _paletteOffsetInput = new NumericUpDown
        {
            Hexadecimal = true,
            Minimum = 0,
            Maximum = 0,
            Increment = SnesPalette.BytesPerPalette,
            Width = 150,
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

        _loadPaletteButton.Click += LoadPaletteButton_Click;
        _resetPaletteButton.Click += ResetPaletteButton_Click;

        FlowLayoutPanel paletteButtonsPanel = new()
        {
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false
        };

        paletteButtonsPanel.Controls.Add(_loadPaletteButton);
        paletteButtonsPanel.Controls.Add(_resetPaletteButton);

        // Panel izquierdo con los controles.
        FlowLayoutPanel tileControlsPanel = new()
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            Padding = new Padding(12)
        };

        tileControlsPanel.Controls.Add(
            new Label
            {
                Text = "Offset ROM hexadecimal:",
                AutoSize = true
            });

        tileControlsPanel.Controls.Add(_tileOffsetInput);
        tileControlsPanel.Controls.Add(_showTileButton);
        tileControlsPanel.Controls.Add(navigationButtonsPanel);

        tileControlsPanel.Controls.Add(
            new Label
            {
                Text = "Offset de paleta hexadecimal:",
                AutoSize = true,
                Margin = new Padding(3, 18, 3, 3)
            });

        tileControlsPanel.Controls.Add(_paletteOffsetInput);
        tileControlsPanel.Controls.Add(paletteButtonsPanel);

        mainSplitContainer.Panel1.Controls.Add(tileControlsPanel);

        // Configuración de la ventana.
        Text = "Aladdin Sprite Studio";
        Width = 1400;
        Height = 900;
        StartPosition = FormStartPosition.CenterScreen;
        DoubleBuffered = true;

        // Menú principal.
        _menu = new MenuStrip();

        BuildMenu();

        Controls.Add(_menu);
        MainMenuStrip = _menu;
    }

    private void BuildMenu()
    {
        var archivo = new ToolStripMenuItem("Archivo");
        var abrirRom = new ToolStripMenuItem("Abrir ROM");

        abrirRom.Click += AbrirRom_Click;

        archivo.DropDownItems.Add(abrirRom);
        _menu.Items.Add(archivo);
    }

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

        int offset = decimal.ToInt32(
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

            _tileViewer.Palette = palette;
            _currentPaletteOffset = paletteOffset;

            int tileOffset =
                decimal.ToInt32(
                    _tileOffsetInput.Value);

            UpdateStatus(tileOffset);
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
        _currentPaletteOffset = null;

        if (_currentRom is not null)
        {
            int tileOffset =
                decimal.ToInt32(
                    _tileOffsetInput.Value);

            UpdateStatus(tileOffset);
        }
    }

    private void MoveTileOffset(int amount)
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

        int newOffset = Math.Clamp(
            currentOffset + amount,
            0,
            maximumOffset);

        _tileOffsetInput.Value = newOffset;

        ShowTileAtOffset(newOffset);
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

    private void ShowTileAtOffset(int offset)
    {
        if (_currentRom is null)
        {
            return;
        }

        byte[,] pixels =
            Decoder4Bpp.DecodeTile(
                _currentRom.Data,
                offset);

        _tileViewer.Pixels = pixels;

        UpdateStatus(offset);
        UpdateTileNavigationButtons();
    }

    private void UpdateStatus(int tileOffset)
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

    private void AbrirRom_Click(
        object? sender,
        EventArgs e)
    {
        using OpenFileDialog dialog = new();

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

            _currentHeader = header;

            string fileName =
                Path.GetFileName(
                    _currentRom.FileName);

            // Restablecer la paleta inicial.
            _currentPaletteOffset = null;
            _tileViewer.ResetPalette();

            // Configurar selector de paleta.
            int maximumPaletteOffset =
                _currentRom.Data.Length -
                SnesPalette.BytesPerPalette;

            if (maximumPaletteOffset >= 0)
            {
                int maximumAlignedPaletteOffset =
                    maximumPaletteOffset -
                    (maximumPaletteOffset %
                     SnesPalette.BytesPerColor);

                _paletteOffsetInput.Maximum =
                    maximumAlignedPaletteOffset;

                _paletteOffsetInput.Value = 0;
                _paletteOffsetInput.Enabled = true;
                _loadPaletteButton.Enabled = true;
                _resetPaletteButton.Enabled = true;
            }

            // Configurar selector de tiles.
            int maximumOffset =
                _currentRom.Data.Length -
                Decoder4Bpp.BytesPerTile;

            if (maximumOffset >= 0)
            {
                int maximumAlignedOffset =
                    maximumOffset -
                    (maximumOffset %
                     Decoder4Bpp.BytesPerTile);

                _tileOffsetInput.Maximum =
                    maximumAlignedOffset;

                _tileOffsetInput.Value = 0;
                _tileOffsetInput.Enabled = true;
                _showTileButton.Enabled = true;

                ShowTileAtOffset(0);
            }

            Text =
                $"Aladdin Sprite Studio - " +
                $"{fileName}";

            MessageBox.Show(
                $"ROM cargada correctamente.\n\n" +
                $"Archivo: {_currentRom.FileName}\n" +
                $"Tamaño del archivo: " +
                $"{_currentRom.Size:N0} bytes\n\n" +
                $"Título interno: {header.Title}\n" +
                $"Mapeo: {header.MappingName}\n" +
                $"Offset del encabezado: " +
                $"0x{header.HeaderOffset:X}\n" +
                $"Cabecera de copiador: " +
                $"{(header.HasCopierHeader ? "Sí" : "No")}\n" +
                $"Tamaño ROM declarado: " +
                $"{header.DeclaredRomSizeBytes:N0} bytes\n" +
                $"Tamaño RAM declarado: " +
                $"{header.DeclaredRamSizeBytes:N0} bytes\n" +
                $"Código de región: " +
                $"0x{header.CountryCode:X2}\n" +
                $"Versión: {header.Version}\n" +
                $"Checksum: " +
                $"0x{header.Checksum:X4}\n" +
                $"Complemento: " +
                $"0x{header.ChecksumComplement:X4}\n" +
                $"Checksum y complemento coherentes: " +
                $"{(header.HasValidChecksum ? "Sí" : "No")}",
                "Información de la ROM",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }
        catch (Exception ex)
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

            _tileViewer.Pixels = null;
            _tileViewer.ResetPalette();

            statusLabelMain.Text =
                "Sin ROM cargada";

            MessageBox.Show(
                ex.Message,
                "Error al cargar la ROM",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }
}