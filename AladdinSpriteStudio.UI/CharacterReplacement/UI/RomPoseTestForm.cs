using System.Globalization;
using AladdinSpriteStudio.UI.CharacterReplacement.Conversion;
using AladdinSpriteStudio.UI.CharacterReplacement.Models;
using AladdinSpriteStudio.UI.CharacterReplacement.RomTesting;

namespace AladdinSpriteStudio.UI.CharacterReplacement.UI;

/// <summary>
/// Ventana de prueba controlada para convertir una sola pose a tiles SNES
/// 4BPP y escribirlos en una copia de la ROM.
/// </summary>
public sealed class RomPoseTestForm
    : System.Windows.Forms.Form
{
    private sealed class FrameItem
    {
        public required SpriteFrame Frame { get; init; }

        public override string ToString()
        {
            return
                $"Pose {Frame.Index + 1:000} | " +
                $"X={Frame.Bounds.X}, Y={Frame.Bounds.Y} | " +
                $"{Frame.Bounds.Width} × {Frame.Bounds.Height} px";
        }
    }

    private sealed class PixelArtPreview
        : System.Windows.Forms.Control
    {
        private System.Drawing.Image? _image;
        private int _zoom = 5;

        public PixelArtPreview()
        {
            DoubleBuffered = true;

            BackColor =
                Color.FromArgb(
                    30,
                    30,
                    34);
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
                _zoom =
                    Math.Clamp(
                        value,
                        1,
                        12);

                UpdateDisplaySize();
                Invalidate();
            }
        }

        public bool ShowTileGrid { get; set; } = true;

        private void UpdateDisplaySize()
        {
            if (_image is null)
            {
                Size =
                    new Size(
                        1,
                        1);

                return;
            }

            Size =
                new Size(
                    Math.Max(
                        1,
                        _image.Width *
                        _zoom),
                    Math.Max(
                        1,
                        _image.Height *
                        _zoom));
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
                System.Drawing.Drawing2D.InterpolationMode
                    .NearestNeighbor;

            e.Graphics.PixelOffsetMode =
                System.Drawing.Drawing2D.PixelOffsetMode.Half;

            e.Graphics.CompositingQuality =
                System.Drawing.Drawing2D.CompositingQuality
                    .HighSpeed;

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

            if (!ShowTileGrid)
            {
                return;
            }

            int tileStep =
                8 *
                _zoom;

            using Pen gridPen =
                new(
                    Color.FromArgb(
                        150,
                        255,
                        255,
                        0),
                    1f);

            for (int x = 0;
                 x <= Width;
                 x += tileStep)
            {
                e.Graphics.DrawLine(
                    gridPen,
                    x,
                    0,
                    x,
                    Height);
            }

            for (int y = 0;
                 y <= Height;
                 y += tileStep)
            {
                e.Graphics.DrawLine(
                    gridPen,
                    0,
                    y,
                    Width,
                    y);
            }
        }
    }

    private readonly SnesSpritePaletteQuantizationResult
        _quantization;

    private readonly SpriteSheetDocument _targetDocument;
    private readonly string _suggestedBaseName;

    private readonly ComboBox _frameCombo;
    private readonly PixelArtPreview _posePreview;
    private readonly Label _poseInfoLabel;

    private readonly TextBox _romPathTextBox;
    private readonly Button _browseRomButton;

    private readonly TextBox _offsetTextBox;
    private readonly CheckBox _headerlessOffsetCheckBox;

    private readonly TextBox _outputPathTextBox;
    private readonly Button _browseOutputButton;

    private readonly Button _exportPoseButton;
    private readonly Button _createTestRomButton;
    private readonly Button _closeButton;

    private System.Drawing.Bitmap? _currentPreview;
    private SnesPoseTileEncodingResult? _currentEncoding;

    public RomPoseTestForm(
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

        Text =
            "Prueba controlada de una pose en copia de ROM";

        Width =
            1180;

        Height =
            760;

        MinimumSize =
            new Size(
                940,
                640);

        StartPosition =
            FormStartPosition.CenterParent;

        ShowInTaskbar =
            false;

        BackColor =
            SystemColors.Control;

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

        _posePreview =
            new PixelArtPreview
            {
                Location =
                    Point.Empty,

                Zoom =
                    5,

                ShowTileGrid =
                    true
            };

        _poseInfoLabel =
            new Label
            {
                AutoSize =
                    true,

                MaximumSize =
                    new Size(
                        700,
                        0),

                Margin =
                    new Padding(
                        3,
                        5,
                        3,
                        8)
            };

        _romPathTextBox =
            new TextBox
            {
                Dock =
                    DockStyle.Fill,

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
                    150,

                CharacterCasing =
                    CharacterCasing.Upper
            };

        _headerlessOffsetCheckBox =
            new CheckBox
            {
                Text =
                    "El offset no incluye encabezado de 0x200 bytes",

                Checked =
                    true,

                AutoSize =
                    true
            };

        _outputPathTextBox =
            new TextBox
            {
                Dock =
                    DockStyle.Fill,

                ReadOnly =
                    true
            };

        _browseOutputButton =
            CreateButton(
                "Elegir copia de salida");

        _exportPoseButton =
            CreateButton(
                "Exportar bloque de la pose");

        _createTestRomButton =
            CreateButton(
                "Crear ROM de prueba");

        _closeButton =
            CreateButton(
                "Cerrar");

        _browseRomButton.Click +=
            BrowseRomButton_Click;

        _browseOutputButton.Click +=
            BrowseOutputButton_Click;

        _exportPoseButton.Click +=
            ExportPoseButton_Click;

        _createTestRomButton.Click +=
            CreateTestRomButton_Click;

        _closeButton.Click +=
            (_, _) =>
                Close();

        Controls.Add(
            CreateMainLayout());

        SelectInitialFrame(
            initialFrame);

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
                    new Padding(12)
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

        Label title =
            new()
            {
                Text =
                    "Prueba técnica de una sola pose",

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
                    "Esta herramienta crea una copia modificada y " +
                    "respalda los bytes reemplazados. El offset 0x40000 " +
                    "es solo un punto inicial de investigación: todavía " +
                    "no está confirmado que corresponda a la pose elegida.",

                AutoSize =
                    true,

                ForeColor =
                    Color.DarkGoldenrod,

                MaximumSize =
                    new Size(
                        1080,
                        0),

                Margin =
                    new Padding(
                        3,
                        3,
                        3,
                        10)
            };

        header.Controls.Add(
            title);

        header.Controls.Add(
            warning);

        root.Controls.Add(
            header,
            0,
            0);

        TableLayoutPanel content =
            new()
            {
                Dock =
                    DockStyle.Fill,

                ColumnCount =
                    2,

                RowCount =
                    1
            };

        content.ColumnStyles.Add(
            new ColumnStyle(
                SizeType.Percent,
                58));

        content.ColumnStyles.Add(
            new ColumnStyle(
                SizeType.Percent,
                42));

        content.RowStyles.Add(
            new RowStyle(
                SizeType.Percent,
                100));

        GroupBox previewGroup =
            new()
            {
                Text =
                    "Pose cuantizada y cuadrícula de tiles 8 × 8",

                Dock =
                    DockStyle.Fill,

                Padding =
                    new Padding(8)
            };

        TableLayoutPanel previewLayout =
            new()
            {
                Dock =
                    DockStyle.Fill,

                ColumnCount =
                    1,

                RowCount =
                    3
            };

        previewLayout.RowStyles.Add(
            new RowStyle(
                SizeType.AutoSize));

        previewLayout.RowStyles.Add(
            new RowStyle(
                SizeType.Percent,
                100));

        previewLayout.RowStyles.Add(
            new RowStyle(
                SizeType.AutoSize));

        previewLayout.Controls.Add(
            _frameCombo,
            0,
            0);

        Panel previewScroll =
            new()
            {
                Dock =
                    DockStyle.Fill,

                AutoScroll =
                    true,

                BackColor =
                    Color.FromArgb(
                        30,
                        30,
                        34),

                Margin =
                    new Padding(
                        0,
                        8,
                        0,
                        8)
            };

        previewScroll.Controls.Add(
            _posePreview);

        previewLayout.Controls.Add(
            previewScroll,
            0,
            1);

        previewLayout.Controls.Add(
            _poseInfoLabel,
            0,
            2);

        previewGroup.Controls.Add(
            previewLayout);

        content.Controls.Add(
            previewGroup,
            0,
            0);

        content.Controls.Add(
            CreateRomSettingsGroup(),
            1,
            0);

        root.Controls.Add(
            content,
            0,
            1);

        Label safetyLabel =
            new()
            {
                Text =
                    "Antes de ejecutar la copia en el emulador, " +
                    "conserve la ROM original fuera de la carpeta de " +
                    "pruebas. La copia generada puede mostrar gráficos " +
                    "incorrectos o no arrancar si el offset no corresponde.",

                AutoSize =
                    true,

                ForeColor =
                    Color.Firebrick,

                MaximumSize =
                    new Size(
                        1080,
                        0),

                Margin =
                    new Padding(
                        3,
                        10,
                        3,
                        5)
            };

        root.Controls.Add(
            safetyLabel,
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
                    true,

                Padding =
                    new Padding(
                        0,
                        8,
                        0,
                        0)
            };

        buttons.Controls.Add(
            _closeButton);

        buttons.Controls.Add(
            _createTestRomButton);

        buttons.Controls.Add(
            _exportPoseButton);

        root.Controls.Add(
            buttons,
            0,
            3);

        return root;
    }

    private Control CreateRomSettingsGroup()
    {
        GroupBox group =
            new()
            {
                Text =
                    "ROM y segmento de prueba",

                Dock =
                    DockStyle.Fill,

                Padding =
                    new Padding(10),

                Margin =
                    new Padding(
                        8,
                        0,
                        0,
                        0)
            };

        TableLayoutPanel layout =
            new()
            {
                Dock =
                    DockStyle.Fill,

                ColumnCount =
                    2,

                RowCount =
                    8
            };

        layout.ColumnStyles.Add(
            new ColumnStyle(
                SizeType.Percent,
                100));

        layout.ColumnStyles.Add(
            new ColumnStyle(
                SizeType.AutoSize));

        for (int row = 0; row < 8; row++)
        {
            layout.RowStyles.Add(
                new RowStyle(
                    row == 7
                        ? SizeType.Percent
                        : SizeType.AutoSize,
                    row == 7
                        ? 100
                        : 0));
        }

        layout.Controls.Add(
            CreateFieldLabel(
                "ROM original:"),
            0,
            0);

        layout.SetColumnSpan(
            layout.GetControlFromPosition(
                0,
                0)!,
            2);

        layout.Controls.Add(
            _romPathTextBox,
            0,
            1);

        layout.Controls.Add(
            _browseRomButton,
            1,
            1);

        layout.Controls.Add(
            CreateFieldLabel(
                "Offset lógico hexadecimal:"),
            0,
            2);

        layout.SetColumnSpan(
            layout.GetControlFromPosition(
                0,
                2)!,
            2);

        FlowLayoutPanel offsetPanel =
            new()
            {
                AutoSize =
                    true,

                FlowDirection =
                    FlowDirection.LeftToRight,

                WrapContents =
                    true,

                Margin =
                    new Padding(
                        0,
                        3,
                        0,
                        8)
            };

        offsetPanel.Controls.Add(
            new Label
            {
                Text =
                    "0x",

                AutoSize =
                    true,

                Margin =
                    new Padding(
                        3,
                        6,
                        2,
                        3)
            });

        offsetPanel.Controls.Add(
            _offsetTextBox);

        offsetPanel.Controls.Add(
            _headerlessOffsetCheckBox);

        layout.Controls.Add(
            offsetPanel,
            0,
            3);

        layout.SetColumnSpan(
            offsetPanel,
            2);

        layout.Controls.Add(
            CreateFieldLabel(
                "Copia de salida:"),
            0,
            4);

        layout.SetColumnSpan(
            layout.GetControlFromPosition(
                0,
                4)!,
            2);

        layout.Controls.Add(
            _outputPathTextBox,
            0,
            5);

        layout.Controls.Add(
            _browseOutputButton,
            1,
            5);

        Label details =
            new()
            {
                Text =
                    "Al crear la prueba también se guardarán:\n" +
                    "• los bytes originales reemplazados;\n" +
                    "• offset lógico y offset real;\n" +
                    "• hashes SHA-256 de ambas ROM;\n" +
                    "• un informe .patch_info.txt.",

                AutoSize =
                    true,

                MaximumSize =
                    new Size(
                        400,
                        0),

                Margin =
                    new Padding(
                        3,
                        12,
                        3,
                        3)
            };

        layout.Controls.Add(
            details,
            0,
            6);

        layout.SetColumnSpan(
            details,
            2);

        group.Controls.Add(
            layout);

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
                    3,
                    8,
                    3,
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

    private SpriteFrame? SelectedFrame =>
        _frameCombo.SelectedItem
        is FrameItem item
            ? item.Frame
            : null;

    private void FrameCombo_SelectedIndexChanged(
        object? sender,
        EventArgs e)
    {
        RebuildPosePreview();
    }

    private void RebuildPosePreview()
    {
        _currentPreview?.Dispose();

        _currentPreview =
            null;

        _currentEncoding =
            null;

        SpriteFrame? frame =
            SelectedFrame;

        if (frame is null)
        {
            _posePreview.Image =
                null;

            _poseInfoLabel.Text =
                "No hay una pose seleccionada.";

            UpdateButtons();

            return;
        }

        try
        {
            SnesPoseTileEncodingResult encoding =
                SnesPoseTileEncoder.Encode(
                    _quantization,
                    frame);

            System.Drawing.Bitmap preview =
                encoding.CreateIndexedPreview(
                    _quantization.Palette);

            _currentEncoding =
                encoding;

            _currentPreview =
                preview;

            _posePreview.Image =
                preview;

            _poseInfoLabel.Text =
                $"Pose {frame.Index + 1:000} | " +
                $"Región original: " +
                $"{frame.Bounds.Width} × " +
                $"{frame.Bounds.Height} px | " +
                $"Cuadrícula: {encoding.TileColumns} × " +
                $"{encoding.TileRows} tiles | " +
                $"Tiles: {encoding.TileCount} | " +
                $"Bloque: {encoding.ByteCount:N0} bytes.";
        }
        catch (Exception ex)
        {
            _posePreview.Image =
                null;

            _poseInfoLabel.Text =
                ex.Message;
        }

        UpdateSuggestedOutputPath();
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
                    "Seleccionar ROM original de Aladdin",

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

        UpdateSuggestedOutputPath();
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
                    "Guardar copia de ROM para la prueba",

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

    private void UpdateSuggestedOutputPath()
    {
        if (string.IsNullOrWhiteSpace(
                _romPathTextBox.Text) ||
            !File.Exists(
                _romPathTextBox.Text))
        {
            return;
        }

        SpriteFrame? frame =
            SelectedFrame;

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
            $"Pose_{(frame?.Index ?? 0) + 1:000}_Prueba" +
            extension;

        _outputPathTextBox.Text =
            Path.Combine(
                directory,
                outputName);
    }

    private void ExportPoseButton_Click(
        object? sender,
        EventArgs e)
    {
        if (_currentEncoding is null ||
            SelectedFrame is null)
        {
            return;
        }

        using SaveFileDialog dialog =
            new()
            {
                Title =
                    "Exportar bloque SNES 4BPP de la pose",

                Filter =
                    "Datos binarios (*.bin)|*.bin",

                DefaultExt =
                    "bin",

                AddExtension =
                    true,

                OverwritePrompt =
                    true,

                FileName =
                    $"{_suggestedBaseName}_" +
                    $"Pose_{SelectedFrame.Index + 1:000}_" +
                    "4bpp.bin"
            };

        if (dialog.ShowDialog(this)
            != DialogResult.OK)
        {
            return;
        }

        try
        {
            File.WriteAllBytes(
                dialog.FileName,
                _currentEncoding.GetCombinedBytes());

            MessageBox.Show(
                this,
                "El bloque 4BPP de la pose fue guardado.",
                "Bloque exportado",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                this,
                ex.Message,
                "No se pudo exportar la pose",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

    private void CreateTestRomButton_Click(
        object? sender,
        EventArgs e)
    {
        if (_currentEncoding is null)
        {
            return;
        }

        if (!TryParseHexOffset(
                _offsetTextBox.Text,
                out int logicalOffset))
        {
            MessageBox.Show(
                this,
                "Escriba un offset hexadecimal válido. " +
                "Ejemplo: 40000.",
                "Offset inválido",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);

            _offsetTextBox.Focus();

            return;
        }

        string originalPath =
            _romPathTextBox.Text.Trim();

        string outputPath =
            _outputPathTextBox.Text.Trim();

        if (!File.Exists(originalPath))
        {
            MessageBox.Show(
                this,
                "Seleccione una ROM original válida.",
                "ROM no encontrada",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);

            return;
        }

        if (string.IsNullOrWhiteSpace(outputPath))
        {
            MessageBox.Show(
                this,
                "Seleccione una ruta para la copia de prueba.",
                "Salida no definida",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);

            return;
        }

        byte[] patchBytes =
            _currentEncoding.GetCombinedBytes();

        DialogResult confirmation =
            MessageBox.Show(
                this,
                $"Se creará una copia de prueba y se reemplazarán " +
                $"{patchBytes.Length:N0} bytes " +
                $"({_currentEncoding.TileCount} tiles) desde el " +
                $"offset lógico 0x{logicalOffset:X6}.\n\n" +
                "El offset todavía no está confirmado. La copia puede " +
                "mostrar gráficos corruptos o no arrancar.\n\n" +
                "La ROM original no será modificada y los bytes " +
                "reemplazados serán respaldados.\n\n" +
                "¿Crear la copia de prueba?",
                "Confirmar prueba de ROM",
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

            _createTestRomButton.Enabled =
                false;

            RomTilePatchOptions options =
                new()
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
                };

            RomTilePatchResult result =
                RomTilePatchService.CreatePatchedCopy(
                    options);

            MessageBox.Show(
                this,
                "La copia de prueba fue creada correctamente.\n\n" +
                $"ROM de prueba:\n{result.OutputRomPath}\n\n" +
                $"Offset lógico: 0x{result.LogicalOffset:X6}\n" +
                $"Offset real: 0x{result.ActualFileOffset:X6}\n" +
                $"Bytes reemplazados: {result.PatchLength:N0}\n" +
                $"Encabezado detectado: " +
                $"{(result.CopierHeaderDetected ? "Sí" : "No")}\n\n" +
                "Pruebe únicamente la copia generada en el emulador.",
                "ROM de prueba creada",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                this,
                ex.Message,
                "No se pudo crear la ROM de prueba",
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

    private void UpdateButtons()
    {
        bool hasEncoding =
            _currentEncoding is not null;

        _exportPoseButton.Enabled =
            hasEncoding;

        _createTestRomButton.Enabled =
            hasEncoding &&
            File.Exists(
                _romPathTextBox.Text) &&
            !string.IsNullOrWhiteSpace(
                _outputPathTextBox.Text);
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

    protected override void OnFormClosed(
        FormClosedEventArgs e)
    {
        _posePreview.Image =
            null;

        _currentPreview?.Dispose();

        _currentPreview =
            null;

        base.OnFormClosed(e);
    }
}
