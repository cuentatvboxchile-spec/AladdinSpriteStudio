using AladdinSpriteStudio.UI.Core;
using AladdinSpriteStudio.UI.Graphics;

namespace AladdinSpriteStudio.UI;

public partial class MainForm : Form
{
    private Rom? _currentRom;
    private RomHeader? _currentHeader;

    private readonly MenuStrip _menu;

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

            RomHeader header = HeaderReader.Read(_currentRom.Data);
            _currentHeader = header;

            string fileName = Path.GetFileName(_currentRom.FileName);

            Text = $"Aladdin Sprite Studio - {fileName}";

            statusLabelMain.Text =
                $"ROM: {fileName} | " +
                $"{header.MappingName} | " +
                $"{_currentRom.Size:N0} bytes";

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
            MessageBox.Show(
                ex.Message,
                "Error al cargar la ROM",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }
}