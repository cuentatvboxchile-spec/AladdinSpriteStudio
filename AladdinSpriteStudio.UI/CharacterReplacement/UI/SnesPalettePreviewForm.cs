using AladdinSpriteStudio.UI.CharacterReplacement.Conversion;

namespace AladdinSpriteStudio.UI.CharacterReplacement.UI;

/// <summary>
/// Compara la hoja convertida con su versión reducida a una paleta
/// conservadora de SNES y permite exportar un paquete preliminar.
///
/// Esta ventana todavía no genera tiles 8 × 8 ni datos 4BPP.
/// </summary>
public sealed class SnesPalettePreviewForm
    : System.Windows.Forms.Form
{
    private sealed class NearestNeighborPictureBox
        : System.Windows.Forms.Control
    {
        private System.Drawing.Image? _image;
        private int _zoom = 1;

        public NearestNeighborPictureBox()
        {
            DoubleBuffered = true;
            BackColor = Color.FromArgb(32, 32, 35);
        }

        public System.Drawing.Image? Image
        {
            get => _image;

            set
            {
                _image = value;
                UpdateDisplaySize();
                Invalidate();
            }
        }

        public int Zoom
        {
            get => _zoom;

            set
            {
                int normalized =
                    Math.Clamp(value, 1, 8);

                if (_zoom == normalized)
                {
                    return;
                }

                _zoom = normalized;
                UpdateDisplaySize();
                Invalidate();
            }
        }

        private void UpdateDisplaySize()
        {
            if (_image is null)
            {
                Size = new Size(1, 1);
                return;
            }

            Size =
                new Size(
                    Math.Max(1, _image.Width * _zoom),
                    Math.Max(1, _image.Height * _zoom));
        }

        protected override void OnPaint(
            PaintEventArgs e)
        {
            base.OnPaint(e);

            if (_image is null)
            {
                return;
            }

            e.Graphics.InterpolationMode =
                System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;

            e.Graphics.PixelOffsetMode =
                System.Drawing.Drawing2D.PixelOffsetMode.Half;

            e.Graphics.CompositingQuality =
                System.Drawing.Drawing2D.CompositingQuality.HighSpeed;

            e.Graphics.DrawImage(
                _image,
                new Rectangle(
                    0,
                    0,
                    Width,
                    Height),
                new Rectangle(
                    0,
                    0,
                    _image.Width,
                    _image.Height),
                GraphicsUnit.Pixel);
        }
    }

    private readonly System.Drawing.Bitmap _originalBitmap;

    private readonly SnesSpritePaletteQuantizationResult
        _quantizationResult;

    private readonly string _suggestedBaseName;

    private readonly NearestNeighborPictureBox _originalPreview;
    private readonly NearestNeighborPictureBox _quantizedPreview;

    private readonly Label _summaryLabel;
    private readonly FlowLayoutPanel _palettePanel;
    private readonly NumericUpDown _zoomInput;

    private readonly Button _exportPackageButton;
    private readonly Button _closeButton;

    public SnesPalettePreviewForm(
        System.Drawing.Image originalImage,
        SnesSpritePaletteQuantizationResult quantizationResult,
        string suggestedBaseName = "personaje_snes")
    {
        ArgumentNullException.ThrowIfNull(
            originalImage);

        _quantizationResult =
            quantizationResult ??
            throw new ArgumentNullException(
                nameof(quantizationResult));

        _originalBitmap =
            new System.Drawing.Bitmap(
                originalImage);

        _suggestedBaseName =
            SanitizeFileName(
                suggestedBaseName);

        Text =
            "Vista previa de paleta SNES";

        Width =
            1420;

        Height =
            860;

        MinimumSize =
            new Size(
                1000,
                650);

        StartPosition =
            FormStartPosition.CenterParent;

        ShowInTaskbar =
            false;

        BackColor =
            SystemColors.Control;

        _originalPreview =
            new NearestNeighborPictureBox
            {
                Image =
                    _originalBitmap,

                Zoom =
                    1,

                Location =
                    Point.Empty
            };

        _quantizedPreview =
            new NearestNeighborPictureBox
            {
                Image =
                    _quantizationResult.QuantizedBitmap,

                Zoom =
                    1,

                Location =
                    Point.Empty
            };

        _summaryLabel =
            new Label
            {
                Text =
                    CreateSummaryText(),

                AutoSize =
                    true,

                MaximumSize =
                    new Size(
                        1300,
                        0),

                Margin =
                    new Padding(
                        3,
                        3,
                        3,
                        8)
            };

        _palettePanel =
            new FlowLayoutPanel
            {
                Dock =
                    DockStyle.Fill,

                AutoSize =
                    false,

                AutoScroll =
                    true,

                FlowDirection =
                    FlowDirection.LeftToRight,

                WrapContents =
                    false,

                Margin =
                    new Padding(
                        3,
                        3,
                        3,
                        3)
            };

        _zoomInput =
            new NumericUpDown
            {
                Minimum =
                    1,

                Maximum =
                    8,

                Value =
                    1,

                Width =
                    70
            };

        _zoomInput.ValueChanged +=
            ZoomInput_ValueChanged;

        _exportPackageButton =
            new Button
            {
                Text =
                    "Exportar paquete de paleta",

                AutoSize =
                    true
            };

        _closeButton =
            new Button
            {
                Text =
                    "Continuar",

                AutoSize =
                    true,

                DialogResult =
                    DialogResult.OK
            };

        _exportPackageButton.Click +=
            ExportPackageButton_Click;

        AcceptButton =
            _closeButton;

        CancelButton =
            _closeButton;

        Shown +=
            (_, _) =>
                _closeButton.Focus();

        BuildPaletteSwatches();

        Controls.Add(
            CreateMainLayout());
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
                    new Padding(10)
            };

        root.RowStyles.Add(
            new RowStyle(
                SizeType.AutoSize));

        root.RowStyles.Add(
            new RowStyle(
                SizeType.Absolute,
                92));

        root.RowStyles.Add(
            new RowStyle(
                SizeType.Percent,
                100));

        root.RowStyles.Add(
            new RowStyle(
                SizeType.Absolute,
                54));

        FlowLayoutPanel header =
            new()
            {
                Dock =
                    DockStyle.Fill,

                AutoSize =
                    true,

                FlowDirection =
                    FlowDirection.TopDown,

                WrapContents =
                    false
            };

        Label titleLabel =
            new()
            {
                Text =
                    "Reducción preliminar a una paleta SNES de 16 índices",

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

        Label warningLabel =
            new()
            {
                Text =
                    "Esta etapa prepara colores e índices. " +
                    "Todavía no genera tiles 8 × 8, datos 4BPP " +
                    "ni modifica la ROM.",

                AutoSize =
                    true,

                ForeColor =
                    Color.DarkGoldenrod,

                MaximumSize =
                    new Size(
                        1300,
                        0),

                Margin =
                    new Padding(
                        3,
                        3,
                        3,
                        8)
            };

        header.Controls.Add(
            titleLabel);

        header.Controls.Add(
            _summaryLabel);

        header.Controls.Add(
            warningLabel);

        root.Controls.Add(
            header,
            0,
            0);

        TableLayoutPanel paletteRow =
            new()
            {
                Dock =
                    DockStyle.Fill,

                AutoSize =
                    false,

                ColumnCount =
                    2,

                RowCount =
                    1,

                Margin =
                    new Padding(
                        0,
                        0,
                        0,
                        8)
            };

        paletteRow.ColumnStyles.Add(
            new ColumnStyle(
                SizeType.Percent,
                100));

        paletteRow.ColumnStyles.Add(
            new ColumnStyle(
                SizeType.AutoSize));

        GroupBox paletteGroup =
            new()
            {
                Text =
                    "Paleta resultante",

                Dock =
                    DockStyle.Fill,

                AutoSize =
                    false,

                Padding =
                    new Padding(8)
            };

        paletteGroup.Controls.Add(
            _palettePanel);

        FlowLayoutPanel zoomPanel =
            new()
            {
                AutoSize =
                    true,

                FlowDirection =
                    FlowDirection.LeftToRight,

                WrapContents =
                    false,

                Padding =
                    new Padding(
                        10,
                        8,
                        0,
                        0)
            };

        zoomPanel.Controls.Add(
            new Label
            {
                Text =
                    "Zoom:",

                AutoSize =
                    true,

                Margin =
                    new Padding(
                        3,
                        7,
                        6,
                        3)
            });

        zoomPanel.Controls.Add(
            _zoomInput);

        paletteRow.Controls.Add(
            paletteGroup,
            0,
            0);

        paletteRow.Controls.Add(
            zoomPanel,
            1,
            0);

        root.Controls.Add(
            paletteRow,
            0,
            1);

        TableLayoutPanel previews =
            new()
            {
                Dock =
                    DockStyle.Fill,

                ColumnCount =
                    2,

                RowCount =
                    1
            };

        previews.ColumnStyles.Add(
            new ColumnStyle(
                SizeType.Percent,
                50));

        previews.ColumnStyles.Add(
            new ColumnStyle(
                SizeType.Percent,
                50));

        previews.RowStyles.Add(
            new RowStyle(
                SizeType.Percent,
                100));

        previews.Controls.Add(
            CreatePreviewGroup(
                "Hoja convertida antes de reducir colores",
                _originalPreview),
            0,
            0);

        previews.Controls.Add(
            CreatePreviewGroup(
                "Resultado con paleta SNES preliminar",
                _quantizedPreview),
            1,
            0);

        root.Controls.Add(
            previews,
            0,
            2);

        FlowLayoutPanel buttons =
            new()
            {
                Dock =
                    DockStyle.Fill,

                AutoSize =
                    true,

                FlowDirection =
                    FlowDirection.RightToLeft,

                WrapContents =
                    false,

                Padding =
                    new Padding(
                        0,
                        10,
                        0,
                        0)
            };

        buttons.Controls.Add(
            _closeButton);

        buttons.Controls.Add(
            _exportPackageButton);

        root.Controls.Add(
            buttons,
            0,
            3);

        return root;
    }

    private static Control CreatePreviewGroup(
        string title,
        NearestNeighborPictureBox preview)
    {
        GroupBox group =
            new()
            {
                Text =
                    title,

                Dock =
                    DockStyle.Fill,

                Padding =
                    new Padding(8),

                Margin =
                    new Padding(4)
            };

        Panel scrollPanel =
            new()
            {
                Dock =
                    DockStyle.Fill,

                AutoScroll =
                    true,

                BackColor =
                    Color.FromArgb(
                        32,
                        32,
                        35)
            };

        scrollPanel.Controls.Add(
            preview);

        group.Controls.Add(
            scrollPanel);

        return group;
    }

    private string CreateSummaryText()
    {
        int visibleColorCount =
            Math.Max(
                0,
                _quantizationResult.Palette.Count - 1);

        return
            $"Colores visibles detectados antes de cuantizar: " +
            $"{_quantizationResult.OriginalUniqueColorCount:N0}\n" +
            $"Entradas de paleta generadas: " +
            $"{_quantizationResult.Palette.Count} " +
            $"(hasta {visibleColorCount} visibles y una reservada " +
            "para transparencia)\n" +
            $"Error cromático medio: " +
            $"{_quantizationResult.MeanColorError:N2}";
    }

    private void BuildPaletteSwatches()
    {
        _palettePanel.SuspendLayout();

        try
        {
            _palettePanel.Controls.Clear();

            for (int index = 0;
                 index <
                 _quantizationResult.Palette.Count;
                 index++)
            {
                System.Drawing.Color color =
                    _quantizationResult.Palette[index];

                Panel item =
                    new()
                    {
                        Width =
                            94,

                        Height =
                            54,

                        Margin =
                            new Padding(3),

                        BorderStyle =
                            BorderStyle.FixedSingle
                    };

                Panel swatch =
                    new()
                    {
                        Width =
                            38,

                        Height =
                            38,

                        Left =
                            5,

                        Top =
                            7,

                        BackColor =
                            color.A == 0
                                ? Color.Magenta
                                : color,

                        BorderStyle =
                            BorderStyle.FixedSingle
                    };

                Label label =
                    new()
                    {
                        AutoSize =
                            true,

                        Left =
                            49,

                        Top =
                            7,

                        Text =
                            $"{index:00}\n" +
                            (color.A == 0
                                ? "Transp."
                                : $"#{color.R:X2}" +
                                  $"{color.G:X2}" +
                                  $"{color.B:X2}")
                    };

                item.Controls.Add(
                    swatch);

                item.Controls.Add(
                    label);

                _palettePanel.Controls.Add(
                    item);
            }
        }
        finally
        {
            _palettePanel.ResumeLayout();
        }
    }

    private void ZoomInput_ValueChanged(
        object? sender,
        EventArgs e)
    {
        int zoom =
            decimal.ToInt32(
                _zoomInput.Value);

        _originalPreview.Zoom =
            zoom;

        _quantizedPreview.Zoom =
            zoom;
    }

    private void ExportPackageButton_Click(
        object? sender,
        EventArgs e)
    {
        using FolderBrowserDialog dialog =
            new()
            {
                Description =
                    "Seleccione la carpeta donde se guardará " +
                    "el paquete preliminar de paleta SNES.",

                UseDescriptionForTitle =
                    true,

                ShowNewFolderButton =
                    true
            };

        if (dialog.ShowDialog(this)
            != DialogResult.OK)
        {
            return;
        }

        try
        {
            string outputDirectory =
                Path.Combine(
                    dialog.SelectedPath,
                    _suggestedBaseName +
                    "_PaletaSNES");

            Directory.CreateDirectory(
                outputDirectory);

            string pngPath =
                Path.Combine(
                    outputDirectory,
                    _suggestedBaseName +
                    "_15colores.png");

            string jascPalettePath =
                Path.Combine(
                    outputDirectory,
                    _suggestedBaseName +
                    ".pal");

            string binaryPalettePath =
                Path.Combine(
                    outputDirectory,
                    _suggestedBaseName +
                    "_palette_bgr555.bin");

            string indicesPath =
                Path.Combine(
                    outputDirectory,
                    _suggestedBaseName +
                    "_indices.bin");

            string reportPath =
                Path.Combine(
                    outputDirectory,
                    _suggestedBaseName +
                    "_paleta_info.txt");

            _quantizationResult.QuantizedBitmap.Save(
                pngPath,
                System.Drawing.Imaging.ImageFormat.Png);

            _quantizationResult.SaveJascPalette(
                jascPalettePath);

            _quantizationResult.SaveSnesPaletteBinary(
                binaryPalettePath);

            SavePixelIndices(
                indicesPath);

            File.WriteAllText(
                reportPath,
                CreateExportReport());

            MessageBox.Show(
                this,
                "El paquete preliminar fue exportado correctamente.\n\n" +
                $"Carpeta:\n{outputDirectory}\n\n" +
                "Incluye la imagen cuantizada, paleta JASC, " +
                "paleta BGR555, índices lineales e informe.",
                "Paquete exportado",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                this,
                ex.Message,
                "No se pudo exportar el paquete",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

    private void SavePixelIndices(
        string filePath)
    {
        byte[,] indices =
            _quantizationResult.PixelIndices;

        int width =
            indices.GetLength(0);

        int height =
            indices.GetLength(1);

        byte[] linear =
            new byte[
                width *
                height];

        int position =
            0;

        for (int y = 0;
             y < height;
             y++)
        {
            for (int x = 0;
                 x < width;
                 x++)
            {
                linear[position++] =
                    indices[x, y];
            }
        }

        File.WriteAllBytes(
            filePath,
            linear);
    }

    private string CreateExportReport()
    {
        List<string> lines =
        [
            "ALADDIN SPRITE STUDIO",
            "PAQUETE PRELIMINAR DE PALETA SNES",
            "",
            $"Fecha: {DateTime.Now:yyyy-MM-dd HH:mm:ss}",
            $"Tamaño: {_quantizationResult.QuantizedBitmap.Width} × " +
            $"{_quantizationResult.QuantizedBitmap.Height}",
            $"Colores originales detectados: " +
            $"{_quantizationResult.OriginalUniqueColorCount}",
            $"Entradas de paleta: " +
            $"{_quantizationResult.Palette.Count}",
            $"Error cromático medio: " +
            $"{_quantizationResult.MeanColorError:N2}",
            "",
            "ARCHIVOS",
            "--------",
            $"{_suggestedBaseName}_15colores.png",
            $"{_suggestedBaseName}.pal",
            $"{_suggestedBaseName}_palette_bgr555.bin",
            $"{_suggestedBaseName}_indices.bin",
            "",
            "ADVERTENCIA",
            "-----------",
            "El archivo de índices todavía es lineal por píxel. " +
            "No representa tiles SNES 8 × 8 ni datos 4BPP. " +
            "La siguiente etapa debe recortar o mapear las regiones " +
            "reales, dividirlas en tiles, eliminar duplicados y " +
            "relacionarlas con las tablas de animación del juego.",
            "",
            "PALETA",
            "-------"
        ];

        for (int index = 0;
             index <
             _quantizationResult.Palette.Count;
             index++)
        {
            System.Drawing.Color color =
                _quantizationResult.Palette[index];

            lines.Add(
                $"{index:00}: " +
                $"A={color.A}, " +
                $"R={color.R}, " +
                $"G={color.G}, " +
                $"B={color.B}");
        }

        return string.Join(
            Environment.NewLine,
            lines);
    }

    private static string SanitizeFileName(
        string value)
    {
        string fallback =
            "personaje_snes";

        if (string.IsNullOrWhiteSpace(
                value))
        {
            return fallback;
        }

        char[] invalid =
            Path.GetInvalidFileNameChars();

        string sanitized =
            new(
                value
                    .Select(
                        character =>
                            invalid.Contains(character)
                                ? '_'
                                : character)
                    .ToArray());

        sanitized =
            sanitized.Trim();

        return string.IsNullOrWhiteSpace(
                sanitized)
            ? fallback
            : sanitized;
    }

    protected override void OnFormClosed(
        FormClosedEventArgs e)
    {
        _originalPreview.Image =
            null;

        _quantizedPreview.Image =
            null;

        _originalBitmap.Dispose();

        base.OnFormClosed(
            e);
    }
}
