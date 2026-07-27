using System.Drawing.Imaging;

namespace AladdinSpriteStudio.UI.CharacterReplacement.UI;

/// <summary>
/// Muestra la hoja gráfica generada a partir de las asignaciones
/// y permite guardarla como PNG sin modificar la ROM original.
/// </summary>
public sealed class ConvertedSheetPreviewForm : System.Windows.Forms.Form
{
    private readonly Bitmap _convertedSheet;
    private readonly PictureBox _pictureBox;

    public ConvertedSheetPreviewForm(
        Image convertedSheet,
        int mappedFrameCount,
        int totalFrameCount)
    {
        ArgumentNullException.ThrowIfNull(convertedSheet);

        _convertedSheet =
            new Bitmap(convertedSheet);

        Text = "Vista previa de la hoja convertida";
        Width = 1100;
        Height = 820;
        MinimumSize = new Size(760, 560);
        StartPosition = FormStartPosition.CenterParent;
        ShowInTaskbar = false;
        BackColor = SystemColors.Control;

        Label informationLabel = new()
        {
            Text =
                $"Poses convertidas: {mappedFrameCount} de " +
                $"{totalFrameCount}. Las poses todavía no " +
                "asignadas conservan la imagen original de Aladdin.",
            AutoSize = true,
            MaximumSize = new Size(1000, 0),
            Margin = new Padding(8)
        };

        _pictureBox = new PictureBox
        {
            Image = _convertedSheet,
            SizeMode = PictureBoxSizeMode.AutoSize,
            Location = Point.Empty
        };

        Panel imagePanel = new()
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            BackColor = Color.FromArgb(32, 32, 35),
            Padding = new Padding(10)
        };

        imagePanel.Controls.Add(_pictureBox);

        Button saveButton = new()
        {
            Text = "Guardar PNG",
            AutoSize = true
        };

        Button closeButton = new()
        {
            Text = "Cerrar",
            AutoSize = true,
            DialogResult = DialogResult.OK
        };

        saveButton.Click += SaveButton_Click;

        FlowLayoutPanel buttonPanel = new()
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            Padding = new Padding(0, 8, 0, 0)
        };

        buttonPanel.Controls.Add(closeButton);
        buttonPanel.Controls.Add(saveButton);

        TableLayoutPanel layout = new()
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Padding = new Padding(8)
        };

        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        layout.Controls.Add(informationLabel, 0, 0);
        layout.Controls.Add(imagePanel, 0, 1);
        layout.Controls.Add(buttonPanel, 0, 2);

        Controls.Add(layout);

        AcceptButton = closeButton;
    }

    private void SaveButton_Click(
        object? sender,
        EventArgs e)
    {
        using SaveFileDialog dialog = new()
        {
            Title = "Guardar hoja convertida",
            Filter = "Imagen PNG (*.png)|*.png",
            DefaultExt = "png",
            AddExtension = true,
            FileName = "personaje_convertido.png",
            OverwritePrompt = true
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        try
        {
            _convertedSheet.Save(
                dialog.FileName,
                ImageFormat.Png);

            MessageBox.Show(
                this,
                "La hoja convertida fue guardada correctamente.",
                "PNG guardado",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                this,
                ex.Message,
                "No se pudo guardar la imagen",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

    protected override void OnFormClosed(
        FormClosedEventArgs e)
    {
        _pictureBox.Image = null;
        _convertedSheet.Dispose();

        base.OnFormClosed(e);
    }
}