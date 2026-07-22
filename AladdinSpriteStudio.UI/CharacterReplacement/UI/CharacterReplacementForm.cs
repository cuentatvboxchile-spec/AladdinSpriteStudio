using AladdinSpriteStudio.UI.CharacterReplacement.Analysis;
using AladdinSpriteStudio.UI.CharacterReplacement.Controls;
using AladdinSpriteStudio.UI.CharacterReplacement.Models;

namespace AladdinSpriteStudio.UI.CharacterReplacement.UI;

/// <summary>
/// Permite comparar la hoja original de Aladdin
/// con una o varias hojas del personaje sustituto.
/// </summary>
public sealed class CharacterReplacementForm :
    System.Windows.Forms.Form
{
    private enum SheetSide
    {
        Aladdin,
        Replacement
    }

    /// <summary>
    /// Elemento mostrado dentro del selector
    /// de hojas del personaje nuevo.
    /// </summary>
    private sealed class ReplacementSheetItem
    {
        public ReplacementSheetItem(
            SpriteSheetDocument document)
        {
            Document =
                document ??
                throw new ArgumentNullException(
                    nameof(document));
        }

        public SpriteSheetDocument Document { get; }

        public override string ToString()
        {
            return Document.FileName;
        }
    }

    private SpriteSheetDocument?
        _aladdinDocument;

    private readonly List<SpriteSheetDocument>
        _replacementDocuments =
            new();

    private readonly SpriteSheetPreview
        _aladdinPreview;

    private readonly SpriteSheetPreview
        _replacementPreview;

    private readonly Label
        _aladdinInfoLabel;

    private readonly Label
        _replacementInfoLabel;

    private readonly ComboBox
        _replacementSheetCombo;

    private readonly Button
        _removeReplacementSheetButton;

    private readonly ToolStripStatusLabel
        _statusLabel;

    public CharacterReplacementForm()
    {
        Text =
            "Asistente de reemplazo de personaje";

        Width = 1500;
        Height = 900;

        MinimumSize =
            new Size(
                1050,
                650);

        StartPosition =
            FormStartPosition.CenterParent;

        BackColor =
            SystemColors.Control;

        _aladdinPreview =
            new SpriteSheetPreview
            {
                Dock = DockStyle.Fill
            };

        _replacementPreview =
            new SpriteSheetPreview
            {
                Dock = DockStyle.Fill
            };

        _aladdinInfoLabel =
            CreateInformationLabel();

        _replacementInfoLabel =
            CreateInformationLabel();

        _replacementSheetCombo =
            new ComboBox
            {
                DropDownStyle =
                    ComboBoxStyle.DropDownList,

                Width = 390
            };

        _removeReplacementSheetButton =
            new Button
            {
                Text = "Quitar hoja",
                AutoSize = true,
                Enabled = false
            };

        _aladdinPreview.FrameSelected +=
            Preview_FrameSelected;

        _replacementPreview.FrameSelected +=
            Preview_FrameSelected;

        _replacementSheetCombo
            .SelectedIndexChanged +=
            ReplacementSheetCombo_SelectedIndexChanged;

        _removeReplacementSheetButton.Click +=
            RemoveReplacementSheet_Click;

        TableLayoutPanel comparisonPanel =
            new()
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                Padding = new Padding(8)
            };

        comparisonPanel.ColumnStyles.Add(
            new ColumnStyle(
                SizeType.Percent,
                50));

        comparisonPanel.ColumnStyles.Add(
            new ColumnStyle(
                SizeType.Percent,
                50));

        comparisonPanel.RowStyles.Add(
            new RowStyle(
                SizeType.Percent,
                100));

        comparisonPanel.Controls.Add(
            CreateAladdinPanel(),
            0,
            0);

        comparisonPanel.Controls.Add(
            CreateReplacementPanel(),
            1,
            0);

        StatusStrip statusStrip =
            new()
            {
                Dock = DockStyle.Bottom
            };

        _statusLabel =
            new ToolStripStatusLabel
            {
                Text =
                    "Cargue la hoja de Aladdin y " +
                    "una o varias hojas del personaje nuevo.",

                Spring = true,

                TextAlign =
                    ContentAlignment.MiddleLeft
            };

        statusStrip.Items.Add(
            _statusLabel);

        Controls.Add(
            comparisonPanel);

        Controls.Add(
            statusStrip);
    }

    // =========================================================
    // CREACIÓN DE LA INTERFAZ
    // =========================================================

    private Control CreateAladdinPanel()
    {
        GroupBox groupBox =
            new()
            {
                Text =
                    "Hoja original de Aladdin",

                Dock =
                    DockStyle.Fill,

                Padding =
                    new Padding(8)
            };

        TableLayoutPanel layout =
            new()
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3
            };

        layout.RowStyles.Add(
            new RowStyle(
                SizeType.AutoSize));

        layout.RowStyles.Add(
            new RowStyle(
                SizeType.AutoSize));

        layout.RowStyles.Add(
            new RowStyle(
                SizeType.Percent,
                100));

        FlowLayoutPanel toolbar =
            CreateToolbar();

        Button loadButton =
            CreateButton(
                "Cargar hoja");

        Button detectButton =
            CreateButton(
                "Detectar poses");

        Button backgroundButton =
            CreateButton(
                "Elegir fondo");

        Button removeFrameButton =
            CreateButton(
                "Eliminar selección");

        loadButton.Click +=
            (_, _) =>
                LoadSheets(
                    SheetSide.Aladdin);

        detectButton.Click +=
            (_, _) =>
                DetectFrames(
                    SheetSide.Aladdin);

        backgroundButton.Click +=
            (_, _) =>
                ChooseBackground(
                    SheetSide.Aladdin);

        removeFrameButton.Click +=
            (_, _) =>
                RemoveSelectedFrame(
                    SheetSide.Aladdin);

        toolbar.Controls.Add(
            loadButton);

        toolbar.Controls.Add(
            detectButton);

        toolbar.Controls.Add(
            backgroundButton);

        toolbar.Controls.Add(
            removeFrameButton);

        layout.Controls.Add(
            toolbar,
            0,
            0);

        layout.Controls.Add(
            _aladdinInfoLabel,
            0,
            1);

        layout.Controls.Add(
            _aladdinPreview,
            0,
            2);

        groupBox.Controls.Add(
            layout);

        return groupBox;
    }

    private Control CreateReplacementPanel()
    {
        GroupBox groupBox =
            new()
            {
                Text =
                    "Hojas del personaje nuevo",

                Dock =
                    DockStyle.Fill,

                Padding =
                    new Padding(8)
            };

        TableLayoutPanel layout =
            new()
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 4
            };

        layout.RowStyles.Add(
            new RowStyle(
                SizeType.AutoSize));

        layout.RowStyles.Add(
            new RowStyle(
                SizeType.AutoSize));

        layout.RowStyles.Add(
            new RowStyle(
                SizeType.AutoSize));

        layout.RowStyles.Add(
            new RowStyle(
                SizeType.Percent,
                100));

        FlowLayoutPanel toolbar =
            CreateToolbar();

        Button addSheetButton =
            CreateButton(
                "Agregar hojas");

        Button detectButton =
            CreateButton(
                "Detectar poses");

        Button backgroundButton =
            CreateButton(
                "Elegir fondo");

        Button removeFrameButton =
            CreateButton(
                "Eliminar selección");

        addSheetButton.Click +=
            (_, _) =>
                LoadSheets(
                    SheetSide.Replacement);

        detectButton.Click +=
            (_, _) =>
                DetectFrames(
                    SheetSide.Replacement);

        backgroundButton.Click +=
            (_, _) =>
                ChooseBackground(
                    SheetSide.Replacement);

        removeFrameButton.Click +=
            (_, _) =>
                RemoveSelectedFrame(
                    SheetSide.Replacement);

        toolbar.Controls.Add(
            addSheetButton);

        toolbar.Controls.Add(
            detectButton);

        toolbar.Controls.Add(
            backgroundButton);

        toolbar.Controls.Add(
            removeFrameButton);

        FlowLayoutPanel selectorPanel =
            new()
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                FlowDirection =
                    FlowDirection.LeftToRight,
                WrapContents = true,
                Padding =
                    new Padding(
                        0,
                        3,
                        0,
                        5)
            };

        selectorPanel.Controls.Add(
            new Label
            {
                Text = "Hoja activa:",
                AutoSize = true,
                Margin =
                    new Padding(
                        3,
                        7,
                        6,
                        3)
            });

        selectorPanel.Controls.Add(
            _replacementSheetCombo);

        selectorPanel.Controls.Add(
            _removeReplacementSheetButton);

        layout.Controls.Add(
            toolbar,
            0,
            0);

        layout.Controls.Add(
            selectorPanel,
            0,
            1);

        layout.Controls.Add(
            _replacementInfoLabel,
            0,
            2);

        layout.Controls.Add(
            _replacementPreview,
            0,
            3);

        groupBox.Controls.Add(
            layout);

        return groupBox;
    }

    private static FlowLayoutPanel CreateToolbar()
    {
        return new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            WrapContents = true,

            FlowDirection =
                FlowDirection.LeftToRight,

            Padding =
                new Padding(
                    0,
                    0,
                    0,
                    4)
        };
    }

    private static Button CreateButton(
        string text)
    {
        return new Button
        {
            Text = text,
            AutoSize = true
        };
    }

    private static Label CreateInformationLabel()
    {
        return new Label
        {
            Text =
                "Ninguna hoja cargada",

            AutoSize = true,

            Padding =
                new Padding(4),

            Margin =
                new Padding(
                    0,
                    3,
                    0,
                    8),

            MaximumSize =
                new Size(
                    650,
                    0)
        };
    }

    // =========================================================
    // CARGAR HOJAS
    // =========================================================

    private void LoadSheets(
        SheetSide side)
    {
        using OpenFileDialog dialog =
            new()
            {
                Title =
                    side ==
                    SheetSide.Aladdin
                        ? "Cargar hoja original de Aladdin"
                        : "Agregar hojas del personaje nuevo",

                Filter =
                    "Imágenes (*.png;*.bmp;*.gif)|" +
                    "*.png;*.bmp;*.gif|" +
                    "Todos los archivos (*.*)|*.*",

                CheckFileExists = true,

                Multiselect =
                    side ==
                    SheetSide.Replacement
            };

        if (dialog.ShowDialog(this) !=
            DialogResult.OK)
        {
            return;
        }

        try
        {
            UseWaitCursor = true;

            if (side ==
                SheetSide.Aladdin)
            {
                LoadAladdinSheet(
                    dialog.FileName);
            }
            else
            {
                LoadReplacementSheets(
                    dialog.FileNames);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                this,
                ex.Message,
                "Error al cargar las hojas",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
        finally
        {
            UseWaitCursor = false;
        }
    }

    private void LoadAladdinSheet(
        string filePath)
    {
        SpriteSheetDocument document =
            SpriteSheetDocument.Load(
                filePath);

        try
        {
            AnalyzeDocument(
                document);

            _aladdinPreview.Document =
                null;

            _aladdinDocument?.Dispose();

            _aladdinDocument =
                document;

            _aladdinPreview.Document =
                document;

            UpdateInformation(
                SheetSide.Aladdin);

            _statusLabel.Text =
                $"Hoja de Aladdin cargada: " +
                $"{document.FileName}";
        }
        catch
        {
            document.Dispose();
            throw;
        }
    }

    private void LoadReplacementSheets(
        IEnumerable<string> filePaths)
    {
        int addedCount = 0;

        foreach (string filePath
                 in filePaths)
        {
            int existingIndex =
                FindReplacementDocument(
                    filePath);

            if (existingIndex >= 0)
            {
                _replacementSheetCombo
                    .SelectedIndex =
                    existingIndex;

                continue;
            }

            SpriteSheetDocument document =
                SpriteSheetDocument.Load(
                    filePath);

            try
            {
                AnalyzeDocument(
                    document);

                _replacementDocuments.Add(
                    document);

                ReplacementSheetItem item =
                    new(document);

                _replacementSheetCombo
                    .Items.Add(
                        item);

                _replacementSheetCombo
                    .SelectedItem =
                    item;

                addedCount++;
            }
            catch
            {
                document.Dispose();
                throw;
            }
        }

        _removeReplacementSheetButton.Enabled =
            _replacementDocuments.Count > 0;

        UpdateInformation(
            SheetSide.Replacement);

        _statusLabel.Text =
            addedCount == 1
                ? "Se agregó una hoja del personaje nuevo."
                : $"Se agregaron {addedCount} hojas " +
                  "del personaje nuevo.";
    }

    private int FindReplacementDocument(
        string filePath)
    {
        string fullPath =
            Path.GetFullPath(
                filePath);

        for (int index = 0;
             index <
             _replacementSheetCombo.Items.Count;
             index++)
        {
            if (_replacementSheetCombo.Items[index]
                is not ReplacementSheetItem item)
            {
                continue;
            }

            string existingPath =
                Path.GetFullPath(
                    item.Document.FilePath);

            if (string.Equals(
                    fullPath,
                    existingPath,
                    StringComparison.OrdinalIgnoreCase))
            {
                return index;
            }
        }

        return -1;
    }

    // =========================================================
    // CAMBIAR HOJA ACTIVA
    // =========================================================

    private void ReplacementSheetCombo_SelectedIndexChanged(
        object? sender,
        EventArgs e)
    {
        SpriteSheetDocument? document =
            GetActiveReplacementDocument();

        _replacementPreview.Document =
            document;

        _removeReplacementSheetButton.Enabled =
            document is not null;

        UpdateInformation(
            SheetSide.Replacement);

        if (document is not null)
        {
            _statusLabel.Text =
                $"Hoja activa: " +
                $"{document.FileName}";
        }
    }

    private void RemoveReplacementSheet_Click(
        object? sender,
        EventArgs e)
    {
        if (_replacementSheetCombo.SelectedItem
            is not ReplacementSheetItem item)
        {
            MessageBox.Show(
                this,
                "No hay una hoja activa para quitar.",
                "Sin hoja seleccionada",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);

            return;
        }

        DialogResult confirmation =
            MessageBox.Show(
                this,
                $"¿Quitar la hoja " +
                $"\"{item.Document.FileName}\" " +
                $"del proyecto?\n\n" +
                "El archivo original no será eliminado.",
                "Quitar hoja",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

        if (confirmation !=
            DialogResult.Yes)
        {
            return;
        }

        int selectedIndex =
            _replacementSheetCombo
                .SelectedIndex;

        _replacementPreview.Document =
            null;

        _replacementSheetCombo.Items.Remove(
            item);

        _replacementDocuments.Remove(
            item.Document);

        item.Document.Dispose();

        if (_replacementSheetCombo.Items.Count > 0)
        {
            _replacementSheetCombo.SelectedIndex =
                Math.Min(
                    selectedIndex,
                    _replacementSheetCombo
                        .Items.Count - 1);
        }

        _removeReplacementSheetButton.Enabled =
            _replacementSheetCombo
                .Items.Count > 0;

        UpdateInformation(
            SheetSide.Replacement);

        _statusLabel.Text =
            "La hoja fue retirada del proyecto.";
    }

    // =========================================================
    // DETECTAR POSES
    // =========================================================

    private void DetectFrames(
        SheetSide side)
    {
        SpriteSheetDocument? document =
            GetDocument(
                side);

        SpriteSheetPreview preview =
            GetPreview(
                side);

        if (document is null)
        {
            MessageBox.Show(
                this,
                side == SheetSide.Aladdin
                    ? "Primero debe cargar la hoja de Aladdin."
                    : "Primero debe agregar y seleccionar " +
                      "una hoja del personaje nuevo.",
                "Hoja no cargada",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);

            return;
        }

        try
        {
            UseWaitCursor = true;

            AnalyzeDocument(
                document);

            preview.SelectedFrame =
                null;

            preview.Invalidate();

            UpdateInformation(
                side);

            _statusLabel.Text =
                $"Detección terminada en " +
                $"{document.FileName}: " +
                $"{document.Frames.Count} regiones.";
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                this,
                ex.Message,
                "Error al detectar poses",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
        finally
        {
            UseWaitCursor = false;
        }
    }

    private static void AnalyzeDocument(
        SpriteSheetDocument document)
    {
        SpriteSheetAnalysisOptions options =
            new()
            {
                BackgroundTolerance = 18,
                AlphaThreshold = 16,

                MinimumPixelCount = 12,
                MinimumWidth = 2,
                MinimumHeight = 2,

                // Evita unir automáticamente
                // personajes cercanos.
                MergeDistance = 0,

                Padding = 1
            };

        IReadOnlyList<SpriteFrame> frames =
            SpriteSheetAnalyzer.Analyze(
                document.Image,
                document.BackgroundColor,
                options);

        document.ReplaceFrames(
            frames);
    }

    // =========================================================
    // FONDO Y ELIMINACIÓN DE REGIONES
    // =========================================================

    private void ChooseBackground(
        SheetSide side)
    {
        SpriteSheetDocument? document =
            GetDocument(
                side);

        if (document is null)
        {
            MessageBox.Show(
                this,
                "Primero debe cargar una hoja.",
                "Hoja no cargada",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);

            return;
        }

        using ColorDialog dialog =
            new()
            {
                Color =
                    document.BackgroundColor,

                FullOpen =
                    true
            };

        if (dialog.ShowDialog(this) !=
            DialogResult.OK)
        {
            return;
        }

        document.BackgroundColor =
            dialog.Color;

        DetectFrames(
            side);
    }

    private void RemoveSelectedFrame(
        SheetSide side)
    {
        SpriteSheetDocument? document =
            GetDocument(
                side);

        SpriteSheetPreview preview =
            GetPreview(
                side);

        if (document is null ||
            preview.SelectedFrame is null)
        {
            MessageBox.Show(
                this,
                "Seleccione primero una región detectada.",
                "Sin selección",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);

            return;
        }

        SpriteFrame selectedFrame =
            preview.SelectedFrame;

        document.RemoveFrame(
            selectedFrame);

        preview.SelectedFrame =
            null;

        preview.Invalidate();

        UpdateInformation(
            side);

        _statusLabel.Text =
            "La región seleccionada fue eliminada.";
    }

    // =========================================================
    // SELECCIÓN
    // =========================================================

    private void Preview_FrameSelected(
        object? sender,
        SpriteFrameSelectedEventArgs e)
    {
        string sourceName =
            sender ==
            _aladdinPreview
                ? _aladdinDocument?.FileName
                    ?? "Aladdin"
                : GetActiveReplacementDocument()
                    ?.FileName
                    ?? "Personaje nuevo";

        _statusLabel.Text =
            $"{sourceName} | " +
            $"{e.Frame.Name} | " +
            $"X={e.Frame.Bounds.X}, " +
            $"Y={e.Frame.Bounds.Y} | " +
            $"{e.Frame.Bounds.Width} × " +
            $"{e.Frame.Bounds.Height} píxeles | " +
            $"{e.Frame.PixelCount:N0} píxeles visibles";
    }

    // =========================================================
    // DOCUMENTOS ACTIVOS
    // =========================================================

    private SpriteSheetDocument? GetDocument(
        SheetSide side)
    {
        return side ==
               SheetSide.Aladdin
            ? _aladdinDocument
            : GetActiveReplacementDocument();
    }

    private SpriteSheetDocument?
        GetActiveReplacementDocument()
    {
        return _replacementSheetCombo
                   .SelectedItem
               is ReplacementSheetItem item
            ? item.Document
            : null;
    }

    private SpriteSheetPreview GetPreview(
        SheetSide side)
    {
        return side ==
               SheetSide.Aladdin
            ? _aladdinPreview
            : _replacementPreview;
    }

    // =========================================================
    // INFORMACIÓN
    // =========================================================

    private void UpdateInformation(
        SheetSide side)
    {
        SpriteSheetDocument? document =
            GetDocument(
                side);

        Label informationLabel =
            side ==
            SheetSide.Aladdin
                ? _aladdinInfoLabel
                : _replacementInfoLabel;

        if (document is null)
        {
            informationLabel.Text =
                side == SheetSide.Aladdin
                    ? "Ninguna hoja de Aladdin cargada"
                    : "Ninguna hoja del personaje nuevo cargada";

            return;
        }

        string additionalInformation =
            side ==
            SheetSide.Replacement
                ? $"\nHojas disponibles: " +
                  $"{_replacementDocuments.Count}"
                : string.Empty;

        informationLabel.Text =
            $"Archivo: {document.FileName}\n" +
            $"Tamaño: {document.Width} × " +
            $"{document.Height} píxeles\n" +
            $"Color de fondo: " +
            $"#{document.BackgroundColor.R:X2}" +
            $"{document.BackgroundColor.G:X2}" +
            $"{document.BackgroundColor.B:X2}\n" +
            $"Regiones detectadas: " +
            $"{document.Frames.Count}" +
            additionalInformation;
    }

    // =========================================================
    // CERRAR
    // =========================================================

    protected override void OnFormClosed(
        FormClosedEventArgs e)
    {
        _aladdinPreview.Document =
            null;

        _replacementPreview.Document =
            null;

        _aladdinDocument?.Dispose();

        foreach (SpriteSheetDocument document
                 in _replacementDocuments)
        {
            document.Dispose();
        }

        _replacementDocuments.Clear();
        _replacementSheetCombo.Items.Clear();

        base.OnFormClosed(
            e);
    }
}