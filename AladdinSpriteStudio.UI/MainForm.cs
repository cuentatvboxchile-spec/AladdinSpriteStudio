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

        _showTileButton.Click += ShowTileButton_Click;

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

        statusLabelMain.Text =
            $"ROM: {fileName} | " +
            $"{mapping} | " +
            $"Tile: 0x{offset:X}";
    }

    private void AbrirRom_Click(object? sender, EventArgs e)
    {
        using OpenFileDialog dialog = new();

        dialog.Filter = "SNES ROM (*.sfc;*.smc)|*.sfc;*.smc";

        if (dialog.ShowDialog() != DialogResult.OK)
        {
            return;
        }

        try
        {
            _currentRom = RomLoader.Load(dialog.FileName);

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
                _tileOffsetInput.Maximum = maximumOffset;
                _tileOffsetInput.Value = 0;
                _tileOffsetInput.Enabled = true;

                _showTileButton.Enabled = true;

                ShowTileAtOffset(0);
            }

            Text = $"Aladdin Sprite Studio - {fileName}";

            statusLabelMain.Text =
                $"ROM: {fileName} | " +
                $"{header.MappingName} | " +
                $"{_currentRom.Size:N0} bytes | " +
                "Tile: 0x0";

            MessageBox.Show(
                $"ROM cargada correctamente.\n\n" +
                $"Archivo: {_currentRom.FileName}\n" +
                $"Tamaño del archivo: {_currentRom.Size:N0} bytes\n\n" +
                $"Título interno: {header.Title}\n" +
                $"Mapeo: {header.MappingName}\n" +
                $"Offset del encabezado: 0x{header.HeaderOffset:X}\n" +
                $"Cabecera de copiador: " +
                $"{(header.HasCopierHeader ? "Sí" : "No")}\n" +
                $"Tamaño ROM declarado: " +
                $"{header.DeclaredRomSizeBytes:N0} bytes\n" +
                $"Tamaño RAM declarado: " +
                $"{header.DeclaredRamSizeBytes:N0} bytes\n" +
                $"Código de región: 0x{header.CountryCode:X2}\n" +
                $"Versión: {header.Version}\n" +
                $"Checksum: 0x{header.Checksum:X4}\n" +
                $"Complemento: 0x{header.ChecksumComplement:X4}\n" +
                $"Checksum y complemento coherentes: " +
                $"{(header.HasValidChecksum ? "Sí" : "No")}",
                "Información de la ROM",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            _tileOffsetInput.Enabled = false;
            _showTileButton.Enabled = false;
            _tileViewer.Pixels = null;

            MessageBox.Show(
                ex.Message,
                "Error al cargar la ROM",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }
}