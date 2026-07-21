using AladdinSpriteStudio.UI.Controls;
using AladdinSpriteStudio.UI.Core;
using AladdinSpriteStudio.UI.Graphics;

namespace AladdinSpriteStudio.UI;

public partial class MainForm : Form
{
    private Rom? _currentRom;
    private RomHeader? _currentHeader;

    private readonly MenuStrip _menu;
    private readonly TileViewer _tileViewer;
    private readonly NumericUpDown _tileOffsetInput;
    private readonly Button _showTileButton;
    private readonly Button _previousTileButton;
    private readonly Button _nextTileButton;

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

        _tileViewer = new TileViewer
        {
            Zoom = 32,
            ShowGrid = true,
            Location = new Point(24, 24)
        };

        mainSplitContainer.Panel2.AutoScroll = true;
        mainSplitContainer.Panel2.Controls.Add(_tileViewer);

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

        mainSplitContainer.Panel1.Controls.Add(tileControlsPanel);

        Text = "Aladdin Sprite Studio";
        Width = 1400;
        Height = 900;
        StartPosition = FormStartPosition.CenterScreen;

        DoubleBuffered = true;

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

    private void MoveTileOffset(int amount)
    {
        if (_currentRom is null)
        {
            return;
        }

        int currentOffset =
            decimal.ToInt32(_tileOffsetInput.Value);

        int maximumOffset =
            decimal.ToInt32(_tileOffsetInput.Maximum);

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
            decimal.ToInt32(_tileOffsetInput.Value);

        int maximumOffset =
            decimal.ToInt32(_tileOffsetInput.Maximum);

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

        byte[,] pixels = Decoder4Bpp.DecodeTile(
            _currentRom.Data,
            offset);

        _tileViewer.Pixels = pixels;

        string fileName =
            Path.GetFileName(_currentRom.FileName);

        string mapping =
            _currentHeader?.MappingName ?? "Desconocido";

        int tileNumber =
            offset / Decoder4Bpp.BytesPerTile;

        statusLabelMain.Text =
            $"ROM: {fileName} | " +
            $"{mapping} | " +
            $"{_currentRom.Size:N0} bytes | " +
            $"Tile #{tileNumber:N0} | " +
            $"Offset: 0x{offset:X}";

        UpdateTileNavigationButtons();
    }

    private void AbrirRom_Click(
        object? sender,
        EventArgs e)
    {
        using OpenFileDialog dialog = new();

        dialog.Filter =
            "SNES ROM (*.sfc;*.smc)|*.sfc;*.smc";

        if (dialog.ShowDialog() != DialogResult.OK)
        {
            return;
        }

        try
        {
            _currentRom = RomLoader.Load(
                dialog.FileName);

            RomHeader header =
                HeaderReader.Read(_currentRom.Data);

            _currentHeader = header;

            string fileName =
                Path.GetFileName(_currentRom.FileName);

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
                $"Aladdin Sprite Studio - {fileName}";

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
                $"Checksum: 0x{header.Checksum:X4}\n" +
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

            _tileOffsetInput.Enabled = false;
            _showTileButton.Enabled = false;
            _previousTileButton.Enabled = false;
            _nextTileButton.Enabled = false;

            _tileViewer.Pixels = null;

            MessageBox.Show(
                ex.Message,
                "Error al cargar la ROM",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }
}