using AladdinSpriteStudio.UI.CharacterReplacement.Analysis;
using AladdinSpriteStudio.UI.CharacterReplacement.Controls;
using AladdinSpriteStudio.UI.CharacterReplacement.Conversion;
using AladdinSpriteStudio.UI.CharacterReplacement.Models;
using AladdinSpriteStudio.UI.CharacterReplacement.Rendering;
using AladdinSpriteStudio.UI.CharacterReplacement.Validation;

namespace AladdinSpriteStudio.UI.CharacterReplacement.UI;

/// <summary>
/// Asistente estable para preparar hojas de sprites, corregir regiones
/// y abrir el editor independiente de asignación de poses.
/// </summary>
public sealed class CharacterReplacementForm : System.Windows.Forms.Form
{
    private enum SheetSide
    {
        Aladdin,
        Replacement
    }

    private sealed class ReplacementSheetItem
    {
        public ReplacementSheetItem(SpriteSheetDocument document)
        {
            Document = document ??
                throw new ArgumentNullException(nameof(document));
        }

        public SpriteSheetDocument Document { get; }

        public override string ToString()
        {
            return Document.FileName;
        }
    }

    // =========================================================
    // PROYECTO Y DOCUMENTOS
    // =========================================================

    private readonly CharacterReplacementProject _project = new();

    private readonly List<SpriteSheetDocument>
        _replacementDocuments = new();

    private SpriteSheetDocument? _aladdinDocument;

    private Bitmap? _readySheetBitmap;
    private string? _readySheetPath;
    private bool _readySheetWasNormalized;
    private Size _readySheetOriginalSize;
    private RomCompatibilityValidationResult?
        _readySheetValidationResult;

    private SpriteFrame? _selectedTargetFrame;
    private SpriteFrame? _selectedSourceFrame;

    // =========================================================
    // CONTROLES DE LAS HOJAS
    // =========================================================

    private readonly SpriteSheetPreview _aladdinPreview = new();
    private readonly SpriteSheetPreview _replacementPreview = new();

    private readonly Label _aladdinInfoLabel;
    private readonly Label _replacementInfoLabel;

    private readonly ComboBox _replacementSheetCombo;
    private readonly Button _removeReplacementSheetButton;

    // =========================================================
    // CONTROLES DE ASIGNACIÓN
    // =========================================================

    private readonly Label _targetSelectionLabel;
    private readonly Label _sourceSelectionLabel;
    private readonly Label _mappingStatisticsLabel;

    private readonly Button _openMappingEditorButton;
    private readonly Button _removeMappingButton;
    private readonly Button _autoMapButton;
    private readonly Button _reviewPendingButton;
    private readonly Button _loadReadySheetButton;
    private readonly Button _clearReadySheetButton;
    private readonly Button _validateRomButton;
    private readonly Button _prepareSnesPaletteButton;
    private readonly Button _testPoseRomButton;
    private readonly Button _tileSegmentEditorButton;
    private readonly Button _previewConvertedSheetButton;

    private readonly ToolStripStatusLabel _statusLabel;

    public CharacterReplacementForm()
    {
        Text = "Asistente de reemplazo de personaje";
        Width = 1500;
        Height = 900;
        MinimumSize = new Size(1080, 680);
        StartPosition = FormStartPosition.CenterParent;
        WindowState = FormWindowState.Maximized;
        BackColor = SystemColors.Control;
        KeyPreview = true;

        _aladdinInfoLabel =
            CreateInformationLabel();

        _replacementInfoLabel =
            CreateInformationLabel();

        _replacementSheetCombo =
            new ComboBox
            {
                DropDownStyle =
                    ComboBoxStyle.DropDownList,

                Width =
                    420
            };

        _removeReplacementSheetButton =
            new Button
            {
                Text =
                    "Quitar hoja",

                AutoSize =
                    true,

                Enabled =
                    false
            };

        _targetSelectionLabel =
            CreateBoldLabel(
                "Pose objetivo de Aladdin: ninguna");

        _sourceSelectionLabel =
            new Label
            {
                Text =
                    "Pose del personaje nuevo: ninguna",

                AutoSize =
                    true,

                MaximumSize =
                    new Size(
                        700,
                        0),

                Margin =
                    new Padding(
                        3,
                        3,
                        3,
                        6)
            };

        _mappingStatisticsLabel =
            CreateBoldLabel(
                "Total de poses objetivo: 0 | " +
                "Asignadas: 0 | Pendientes: 0");

        _openMappingEditorButton =
            CreateButton(
                "Abrir editor de pose",
                enabled: false);

        _removeMappingButton =
            CreateButton(
                "Quitar asignación",
                enabled: false);

        _autoMapButton =
            CreateButton(
                "Autoasignar poses (modo ROM)",
                enabled: false);

        _reviewPendingButton =
            CreateButton(
                "Revisar poses pendientes",
                enabled: false);

        _loadReadySheetButton =
            CreateButton(
                "Cargar hoja final",
                enabled: false);

        _clearReadySheetButton =
            CreateButton(
                "Quitar hoja final",
                enabled: false);

        _validateRomButton =
            CreateButton(
                "Validar para ROM",
                enabled: false);

        _prepareSnesPaletteButton =
            CreateButton(
                "Preparar paleta SNES",
                enabled: false);

        _testPoseRomButton =
            CreateButton(
                "Probar una pose en copia ROM",
                enabled: false);

        _tileSegmentEditorButton =
            CreateButton(
                "Editor tile por tile",
                enabled: false);

        _previewConvertedSheetButton =
            CreateButton(
                "Ver hoja convertida",
                enabled: false);

        WireEvents();

        TableLayoutPanel comparisonPanel =
            new()
            {
                Dock =
                    DockStyle.Fill,

                ColumnCount =
                    2,

                RowCount =
                    1,

                Padding =
                    new Padding(8)
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
            CreateSheetPanel(
                SheetSide.Aladdin),
            0,
            0);

        comparisonPanel.Controls.Add(
            CreateSheetPanel(
                SheetSide.Replacement),
            1,
            0);

        TableLayoutPanel mainLayout =
            new()
            {
                Dock =
                    DockStyle.Fill,

                ColumnCount =
                    1,

                RowCount =
                    2
            };

        mainLayout.ColumnStyles.Add(
            new ColumnStyle(
                SizeType.Percent,
                100));

        mainLayout.RowStyles.Add(
            new RowStyle(
                SizeType.Percent,
                100));

        mainLayout.RowStyles.Add(
            new RowStyle(
                SizeType.AutoSize));

        mainLayout.Controls.Add(
            comparisonPanel,
            0,
            0);

        mainLayout.Controls.Add(
            CreateAssignmentPanel(),
            0,
            1);

        StatusStrip statusStrip =
            new()
            {
                Dock =
                    DockStyle.Bottom
            };

        _statusLabel =
            new ToolStripStatusLabel
            {
                Text =
                    "Cargue la hoja de Aladdin y una o varias " +
                    "hojas del personaje nuevo.",

                Spring =
                    true,

                TextAlign =
                    ContentAlignment.MiddleLeft
            };

        statusStrip.Items.Add(
            _statusLabel);

        Controls.Add(
            mainLayout);

        Controls.Add(
            statusStrip);

        UpdateAssignmentEditor();
    }

    // =========================================================
    // EVENTOS
    // =========================================================

    private void WireEvents()
    {
        _aladdinPreview.FrameSelected +=
            Preview_FrameSelected;

        _replacementPreview.FrameSelected +=
            Preview_FrameSelected;

        _aladdinPreview.SelectionChanged +=
            Preview_SelectionChanged;

        _replacementPreview.SelectionChanged +=
            Preview_SelectionChanged;

        _aladdinPreview.RegionCreated +=
            Preview_RegionCreated;

        _replacementPreview.RegionCreated +=
            Preview_RegionCreated;

        _replacementSheetCombo.SelectedIndexChanged +=
            ReplacementSheetCombo_SelectedIndexChanged;

        _removeReplacementSheetButton.Click +=
            RemoveReplacementSheet_Click;

        _openMappingEditorButton.Click +=
            OpenMappingEditorButton_Click;

        _removeMappingButton.Click +=
            RemoveMappingButton_Click;

        _autoMapButton.Click +=
            AutoMapButton_Click;

        _reviewPendingButton.Click +=
            ReviewPendingButton_Click;

        _loadReadySheetButton.Click +=
            LoadReadySheetButton_Click;

        _clearReadySheetButton.Click +=
            ClearReadySheetButton_Click;

        _validateRomButton.Click +=
            ValidateRomButton_Click;

        _prepareSnesPaletteButton.Click +=
            PrepareSnesPaletteButton_Click;

        _testPoseRomButton.Click +=
            TestPoseRomButton_Click;

        _tileSegmentEditorButton.Click +=
            TileSegmentEditorButton_Click;

        _previewConvertedSheetButton.Click +=
            PreviewConvertedSheetButton_Click;
    }

    // =========================================================
    // INTERFAZ PRINCIPAL
    // =========================================================

    private Control CreateSheetPanel(
        SheetSide side)
    {
        bool isAladdin =
            side == SheetSide.Aladdin;

        GroupBox groupBox =
            new()
            {
                Text =
                    isAladdin
                        ? "Hoja original de Aladdin"
                        : "Hojas del personaje nuevo",

                Dock =
                    DockStyle.Fill,

                Padding =
                    new Padding(8)
            };

        TableLayoutPanel layout =
            new()
            {
                Dock =
                    DockStyle.Fill,

                ColumnCount =
                    1,

                RowCount =
                    isAladdin
                        ? 3
                        : 4
            };

        layout.RowStyles.Add(
            new RowStyle(
                SizeType.AutoSize));

        if (!isAladdin)
        {
            layout.RowStyles.Add(
                new RowStyle(
                    SizeType.AutoSize));
        }

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
                isAladdin
                    ? "Cargar hoja"
                    : "Agregar hojas");

        Button detectButton =
            CreateButton(
                "Detectar poses");

        Button backgroundButton =
            CreateButton(
                "Elegir fondo");

        Button createRegionButton =
            CreateButton(
                "Crear región");

        Button mergeButton =
            CreateButton(
                "Unir seleccionadas");

        Button removeButton =
            CreateButton(
                "Eliminar selección");

        Button undoButton =
            CreateButton(
                "Deshacer");

        Button sortButton =
            CreateButton(
                "Ordenar por filas");

        loadButton.Click +=
            (_, _) =>
                LoadSheets(side);

        detectButton.Click +=
            (_, _) =>
                DetectFrames(side);

        backgroundButton.Click +=
            (_, _) =>
                ChooseBackground(side);

        createRegionButton.Click +=
            (_, _) =>
                BeginCreateRegion(side);

        mergeButton.Click +=
            (_, _) =>
                MergeSelectedFrames(side);

        removeButton.Click +=
            (_, _) =>
                RemoveSelectedFrames(side);

        undoButton.Click +=
            (_, _) =>
                UndoLastChange(side);

        sortButton.Click +=
            (_, _) =>
                SortFramesByRows(side);

        toolbar.Controls.Add(
            loadButton);

        toolbar.Controls.Add(
            detectButton);

        toolbar.Controls.Add(
            backgroundButton);

        toolbar.Controls.Add(
            createRegionButton);

        toolbar.Controls.Add(
            mergeButton);

        toolbar.Controls.Add(
            removeButton);

        toolbar.Controls.Add(
            undoButton);

        toolbar.Controls.Add(
            sortButton);

        int row =
            0;

        layout.Controls.Add(
            toolbar,
            0,
            row++);

        if (!isAladdin)
        {
            layout.Controls.Add(
                CreateReplacementSelectorPanel(),
                0,
                row++);
        }

        Label informationLabel =
            isAladdin
                ? _aladdinInfoLabel
                : _replacementInfoLabel;

        SpriteSheetPreview preview =
            isAladdin
                ? _aladdinPreview
                : _replacementPreview;

        layout.Controls.Add(
            informationLabel,
            0,
            row++);

        layout.Controls.Add(
            CreateScrollablePreviewPanel(
                preview),
            0,
            row);

        groupBox.Controls.Add(
            layout);

        return groupBox;
    }

    private Control CreateReplacementSelectorPanel()
    {
        FlowLayoutPanel selectorPanel =
            new()
            {
                Dock =
                    DockStyle.Fill,

                AutoSize =
                    true,

                FlowDirection =
                    FlowDirection.LeftToRight,

                WrapContents =
                    true,

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
                Text =
                    "Hoja activa:",

                AutoSize =
                    true,

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

        return selectorPanel;
    }

    private Control CreateAssignmentPanel()
    {
        GroupBox groupBox =
            new()
            {
                Text =
                    "Asignación de poses",

                Dock =
                    DockStyle.Fill,

                AutoSize =
                    true,

                Padding =
                    new Padding(8),

                Margin =
                    new Padding(
                        8,
                        0,
                        8,
                        8)
            };

        TableLayoutPanel layout =
            new()
            {
                Dock =
                    DockStyle.Fill,

                AutoSize =
                    true,

                ColumnCount =
                    2,

                RowCount =
                    2
            };

        layout.ColumnStyles.Add(
            new ColumnStyle(
                SizeType.Percent,
                100));

        layout.ColumnStyles.Add(
            new ColumnStyle(
                SizeType.AutoSize));

        layout.RowStyles.Add(
            new RowStyle(
                SizeType.AutoSize));

        layout.RowStyles.Add(
            new RowStyle(
                SizeType.AutoSize));

        FlowLayoutPanel informationPanel =
            new()
            {
                Dock =
                    DockStyle.Fill,

                AutoSize =
                    true,

                FlowDirection =
                    FlowDirection.TopDown,

                WrapContents =
                    false,

                Margin =
                    new Padding(
                        3,
                        2,
                        12,
                        3)
            };

        informationPanel.Controls.Add(
            _targetSelectionLabel);

        informationPanel.Controls.Add(
            _sourceSelectionLabel);

        informationPanel.Controls.Add(
            _mappingStatisticsLabel);

        FlowLayoutPanel buttonPanel =
            new()
            {
                Dock =
                    DockStyle.Fill,

                AutoSize =
                    true,

                FlowDirection =
                    FlowDirection.LeftToRight,

                WrapContents =
                    true,

                Padding =
                    new Padding(
                        4,
                        7,
                        4,
                        4)
            };

        buttonPanel.Controls.Add(
            _openMappingEditorButton);

        buttonPanel.Controls.Add(
            _removeMappingButton);

        buttonPanel.Controls.Add(
            _autoMapButton);

        buttonPanel.Controls.Add(
            _reviewPendingButton);

        buttonPanel.Controls.Add(
            _loadReadySheetButton);

        buttonPanel.Controls.Add(
            _clearReadySheetButton);

        buttonPanel.Controls.Add(
            _validateRomButton);

        buttonPanel.Controls.Add(
            _prepareSnesPaletteButton);

        buttonPanel.Controls.Add(
            _testPoseRomButton);

        buttonPanel.Controls.Add(
            _tileSegmentEditorButton);

        buttonPanel.Controls.Add(
            _previewConvertedSheetButton);

        Label instructionsLabel =
            new()
            {
                Text =
                    "Seleccione primero una pose de Aladdin y " +
                    "después una pose del personaje nuevo. " +
                    "También puede autoasignar, revisar pendientes o " +
                    "cargar directamente una hoja final preparada por IA.",

                AutoSize =
                    true,

                MaximumSize =
                    new Size(
                        1100,
                        0),

                Margin =
                    new Padding(
                        3,
                        5,
                        3,
                        3)
            };

        layout.Controls.Add(
            informationPanel,
            0,
            0);

        layout.SetRowSpan(
            informationPanel,
            2);

        layout.Controls.Add(
            buttonPanel,
            1,
            0);

        layout.Controls.Add(
            instructionsLabel,
            1,
            1);

        groupBox.Controls.Add(
            layout);

        return groupBox;
    }

    private static FlowLayoutPanel CreateToolbar()
    {
        return new FlowLayoutPanel
        {
            Dock =
                DockStyle.Fill,

            AutoSize =
                true,

            FlowDirection =
                FlowDirection.LeftToRight,

            WrapContents =
                true,

            Padding =
                new Padding(
                    0,
                    0,
                    0,
                    4)
        };
    }

    private static Panel CreateScrollablePreviewPanel(
        SpriteSheetPreview preview)
    {
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
                        35),

                Margin =
                    Padding.Empty,

                Padding =
                    Padding.Empty
            };

        preview.Dock =
            DockStyle.None;

        preview.Location =
            Point.Empty;

        scrollPanel.Controls.Add(
            preview);

        return scrollPanel;
    }

    private static Button CreateButton(
        string text,
        bool enabled = true)
    {
        return new Button
        {
            Text =
                text,

            AutoSize =
                true,

            Enabled =
                enabled
        };
    }

    private static Label CreateInformationLabel()
    {
        return new Label
        {
            Text =
                "Ninguna hoja cargada",

            AutoSize =
                true,

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

    private static Label CreateBoldLabel(
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

            MaximumSize =
                new Size(
                    700,
                    0),

            Margin =
                new Padding(
                    3,
                    3,
                    3,
                    6)
        };
    }

    // =========================================================
    // CARGA DE HOJAS
    // =========================================================

    private void LoadSheets(
        SheetSide side)
    {
        using OpenFileDialog dialog =
            new()
            {
                Title =
                    side == SheetSide.Aladdin
                        ? "Cargar hoja original de Aladdin"
                        : "Agregar hojas del personaje nuevo",

                Filter =
                    "Imágenes (*.png;*.bmp;*.gif)|" +
                    "*.png;*.bmp;*.gif|" +
                    "Todos los archivos (*.*)|*.*",

                CheckFileExists =
                    true,

                Multiselect =
                    side == SheetSide.Replacement
            };

        if (dialog.ShowDialog(this)
            != DialogResult.OK)
        {
            return;
        }

        try
        {
            UseWaitCursor =
                true;

            if (side == SheetSide.Aladdin)
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
            UseWaitCursor =
                false;
        }
    }

    private void LoadAladdinSheet(
        string filePath)
    {
        ClearReadySheet(
            updateInterface: false);

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

            _project.SetTargetDocument(
                document);

            _selectedTargetFrame =
                null;

            _selectedSourceFrame =
                null;

            _aladdinPreview.Document =
                document;

            _replacementPreview.ClearSelection();

            UpdateInformation(
                SheetSide.Aladdin);

            UpdateAssignmentEditor();

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
        int addedCount =
            0;

        foreach (string filePath
                 in filePaths)
        {
            int existingIndex =
                FindReplacementDocument(
                    filePath);

            if (existingIndex >= 0)
            {
                _replacementSheetCombo.SelectedIndex =
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

                _project.AddSourceDocument(
                    document);

                ReplacementSheetItem item =
                    new(document);

                _replacementSheetCombo.Items.Add(
                    item);

                _replacementSheetCombo.SelectedItem =
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

        UpdateAssignmentEditor();

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

    private void ReplacementSheetCombo_SelectedIndexChanged(
        object? sender,
        EventArgs e)
    {
        SpriteSheetDocument? document =
            GetActiveReplacementDocument();

        _selectedSourceFrame =
            null;

        _replacementPreview.CreateRegionMode =
            false;

        _replacementPreview.Document =
            document;

        _removeReplacementSheetButton.Enabled =
            document is not null;

        UpdateInformation(
            SheetSide.Replacement);

        UpdateAssignmentEditor();

        if (document is not null)
        {
            _statusLabel.Text =
                $"Hoja activa: {document.FileName}";
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
                "del proyecto?\n\n" +
                "El archivo original no será eliminado.",
                "Quitar hoja",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

        if (confirmation
            != DialogResult.Yes)
        {
            return;
        }

        int selectedIndex =
            _replacementSheetCombo.SelectedIndex;

        _selectedSourceFrame =
            null;

        _replacementPreview.Document =
            null;

        _project.RemoveSourceDocument(
            item.Document);

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
                    _replacementSheetCombo.Items.Count - 1);
        }

        _removeReplacementSheetButton.Enabled =
            _replacementSheetCombo.Items.Count > 0;

        UpdateInformation(
            SheetSide.Replacement);

        UpdateAssignmentEditor();

        _statusLabel.Text =
            "La hoja fue retirada del proyecto.";
    }

    // =========================================================
    // DETECCIÓN Y EDICIÓN DE REGIONES
    // =========================================================

    private void DetectFrames(
        SheetSide side)
    {
        SpriteSheetDocument? document =
            GetDocument(side);

        SpriteSheetPreview preview =
            GetPreview(side);

        if (document is null)
        {
            ShowMissingSheetMessage(
                side);

            return;
        }

        try
        {
            UseWaitCursor =
                true;

            AnalyzeDocument(
                document);

            ResetSelectionForSide(
                side);

            preview.SelectedFrame =
                null;

            preview.CreateRegionMode =
                false;

            preview.Invalidate();

            int removedMappings =
                _project.RemoveInvalidMappings();

            UpdateInformation(
                side);

            UpdateAssignmentEditor();

            _statusLabel.Text =
                $"Detección terminada en " +
                $"{document.FileName}: " +
                $"{document.Frames.Count} regiones. " +
                $"Asignaciones eliminadas: " +
                $"{removedMappings}.";
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
            UseWaitCursor =
                false;
        }
    }

    private static void AnalyzeDocument(
        SpriteSheetDocument document)
    {
        SpriteSheetAnalysisOptions options =
            new()
            {
                BackgroundTolerance =
                    18,

                AlphaThreshold =
                    16,

                MinimumPixelCount =
                    12,

                MinimumWidth =
                    2,

                MinimumHeight =
                    2,

                MergeDistance =
                    0,

                Padding =
                    1
            };

        IReadOnlyList<SpriteFrame> frames =
            SpriteSheetAnalyzer.Analyze(
                document.Image,
                document.BackgroundColor,
                options);

        document.ReplaceFrames(
            frames);
    }

    private void BeginCreateRegion(
        SheetSide side)
    {
        if (GetDocument(side)
            is null)
        {
            ShowMissingSheetMessage(
                side);

            return;
        }

        SpriteSheetPreview preview =
            GetPreview(side);

        SpriteSheetPreview otherPreview =
            side == SheetSide.Aladdin
                ? _replacementPreview
                : _aladdinPreview;

        otherPreview.CreateRegionMode =
            false;

        preview.ClearSelection();
        preview.CreateRegionMode =
            true;

        preview.Focus();

        _statusLabel.Text =
            "Arrastre el mouse alrededor de la pose. " +
            "Presione Escape para cancelar.";
    }

    private void Preview_RegionCreated(
        object? sender,
        SpriteRegionCreatedEventArgs e)
    {
        SheetSide side =
            ReferenceEquals(
                sender,
                _aladdinPreview)
                ? SheetSide.Aladdin
                : SheetSide.Replacement;

        SpriteSheetDocument? document =
            GetDocument(side);

        SpriteSheetPreview preview =
            GetPreview(side);

        if (document is null)
        {
            return;
        }

        try
        {
            SpriteFrame newFrame =
                document.AddFrame(
                    e.Bounds);

            if (side == SheetSide.Aladdin)
            {
                _selectedTargetFrame =
                    newFrame;
            }
            else
            {
                _selectedSourceFrame =
                    newFrame;
            }

            preview.SelectedFrame =
                newFrame;

            preview.Invalidate();

            UpdateInformation(
                side);

            UpdateAssignmentEditor();

            _statusLabel.Text =
                $"Región creada: " +
                $"X={newFrame.Bounds.X}, " +
                $"Y={newFrame.Bounds.Y}, " +
                $"{newFrame.Bounds.Width} × " +
                $"{newFrame.Bounds.Height} píxeles.";
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                this,
                ex.Message,
                "No se pudo crear la región",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

    private void MergeSelectedFrames(
        SheetSide side)
    {
        SpriteSheetDocument? document =
            GetDocument(side);

        SpriteSheetPreview preview =
            GetPreview(side);

        if (document is null)
        {
            ShowMissingSheetMessage(
                side);

            return;
        }

        SpriteFrame[] selectedFrames =
            preview.SelectedFrames.ToArray();

        if (selectedFrames.Length < 2)
        {
            MessageBox.Show(
                this,
                "Seleccione al menos dos regiones.\n\n" +
                "Mantenga presionada Ctrl mientras hace clic " +
                "sobre cada región.",
                "Selección insuficiente",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);

            return;
        }

        SpriteFrame? mergedFrame =
            document.MergeFrames(
                selectedFrames);

        if (mergedFrame is null)
        {
            return;
        }

        int removedMappings =
            _project.RemoveInvalidMappings();

        if (side == SheetSide.Aladdin)
        {
            _selectedTargetFrame =
                mergedFrame;
        }
        else
        {
            _selectedSourceFrame =
                mergedFrame;
        }

        preview.SelectedFrame =
            mergedFrame;

        preview.Invalidate();

        UpdateInformation(
            side);

        UpdateAssignmentEditor();

        _statusLabel.Text =
            $"Se combinaron " +
            $"{selectedFrames.Length} regiones. " +
            $"Asignaciones eliminadas: " +
            $"{removedMappings}.";
    }

    private void RemoveSelectedFrames(
        SheetSide side)
    {
        SpriteSheetDocument? document =
            GetDocument(side);

        SpriteSheetPreview preview =
            GetPreview(side);

        if (document is null)
        {
            ShowMissingSheetMessage(
                side);

            return;
        }

        SpriteFrame[] selectedFrames =
            preview.SelectedFrames.ToArray();

        if (selectedFrames.Length == 0)
        {
            MessageBox.Show(
                this,
                "Seleccione una o varias regiones.\n\n" +
                "Use Ctrl + clic para seleccionar varias.",
                "Sin selección",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);

            return;
        }

        DialogResult confirmation =
            MessageBox.Show(
                this,
                selectedFrames.Length == 1
                    ? "¿Eliminar la región seleccionada?"
                    : $"¿Eliminar las " +
                      $"{selectedFrames.Length} " +
                      "regiones seleccionadas?",
                "Eliminar regiones",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

        if (confirmation
            != DialogResult.Yes)
        {
            return;
        }

        int previousCount =
            document.Frames.Count;

        bool removed =
            document.RemoveFrames(
                selectedFrames);

        int currentCount =
            document.Frames.Count;

        if (!removed ||
            currentCount >= previousCount)
        {
            MessageBox.Show(
                this,
                "No fue posible eliminar las regiones " +
                "seleccionadas.",
                "Error al eliminar",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);

            return;
        }

        ResetSelectionForSide(
            side);

        preview.ClearSelection();
        preview.Invalidate();

        int removedMappings =
            _project.RemoveInvalidMappings();

        UpdateInformation(
            side);

        UpdateAssignmentEditor();

        _statusLabel.Text =
            $"Regiones eliminadas: " +
            $"{previousCount - currentCount}. " +
            $"Antes: {previousCount}. " +
            $"Ahora: {currentCount}. " +
            $"Asignaciones eliminadas: " +
            $"{removedMappings}.";
    }

    private void UndoLastChange(
        SheetSide side)
    {
        SpriteSheetDocument? document =
            GetDocument(side);

        SpriteSheetPreview preview =
            GetPreview(side);

        if (document is null)
        {
            ShowMissingSheetMessage(
                side);

            return;
        }

        preview.ClearSelection();

        if (!document.UndoLastChange())
        {
            MessageBox.Show(
                this,
                "No hay modificaciones para deshacer.",
                "Historial vacío",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);

            return;
        }

        ResetSelectionForSide(
            side);

        int removedMappings =
            _project.RemoveInvalidMappings();

        preview.Invalidate();

        UpdateInformation(
            side);

        UpdateAssignmentEditor();

        _statusLabel.Text =
            "Se restauró el estado anterior. " +
            $"Asignaciones eliminadas: " +
            $"{removedMappings}.";
    }

    private void SortFramesByRows(
        SheetSide side)
    {
        SpriteSheetDocument? document =
            GetDocument(side);

        SpriteSheetPreview preview =
            GetPreview(side);

        if (document is null)
        {
            ShowMissingSheetMessage(
                side);

            return;
        }

        if (document.Frames.Count < 2)
        {
            return;
        }

        document.SortFramesByRows(
            rowTolerance: 10);

        preview.ClearSelection();
        preview.Invalidate();

        UpdateInformation(
            side);

        UpdateAssignmentEditor();

        _statusLabel.Text =
            "Las regiones fueron ordenadas por filas.";
    }

    private void ChooseBackground(
        SheetSide side)
    {
        SpriteSheetDocument? document =
            GetDocument(side);

        if (document is null)
        {
            ShowMissingSheetMessage(
                side);

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

        if (dialog.ShowDialog(this)
            != DialogResult.OK)
        {
            return;
        }

        document.BackgroundColor =
            dialog.Color;

        DetectFrames(
            side);
    }

    // =========================================================
    // SELECCIÓN DE POSES
    // =========================================================

    private void Preview_FrameSelected(
        object? sender,
        SpriteFrameSelectedEventArgs e)
    {
        if (ReferenceEquals(
                sender,
                _aladdinPreview))
        {
            _selectedTargetFrame =
                e.Frame;

            _selectedSourceFrame =
                null;

            _replacementPreview.ClearSelection();
        }
        else
        {
            _selectedSourceFrame =
                e.Frame;
        }

        UpdateAssignmentEditor();

        string sourceName =
            ReferenceEquals(
                sender,
                _aladdinPreview)
                ? _aladdinDocument?.FileName
                  ?? "Aladdin"
                : GetActiveReplacementDocument()?.FileName
                  ?? "Personaje nuevo";

        _statusLabel.Text =
            $"{sourceName} | " +
            $"{e.Frame.Name} | " +
            $"X={e.Frame.Bounds.X}, " +
            $"Y={e.Frame.Bounds.Y} | " +
            $"{e.Frame.Bounds.Width} × " +
            $"{e.Frame.Bounds.Height} píxeles | " +
            $"{e.Frame.PixelCount:N0} " +
            "píxeles visibles";
    }

    private void Preview_SelectionChanged(
        object? sender,
        SpriteFrameSelectionChangedEventArgs e)
    {
        if (e.SelectedFrames.Count > 1)
        {
            _statusLabel.Text =
                $"{e.SelectedFrames.Count} " +
                "regiones seleccionadas. " +
                "Puede unirlas o eliminarlas.";
        }
    }

    // =========================================================
    // AUTOASIGNACIÓN COMPATIBLE CON LA ESTRUCTURA DE LA ROM
    // =========================================================

    private void AutoMapButton_Click(
        object? sender,
        EventArgs e)
    {
        if (_aladdinDocument is null)
        {
            MessageBox.Show(
                this,
                "Primero debe cargar la hoja de Aladdin.",
                "Hoja de Aladdin no cargada",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);

            return;
        }

        SpriteSheetDocument? sourceDocument =
            GetActiveReplacementDocument();

        if (sourceDocument is null)
        {
            MessageBox.Show(
                this,
                "Primero debe agregar y seleccionar una hoja " +
                "del personaje nuevo.",
                "Hoja fuente no cargada",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);

            return;
        }

        if (_aladdinDocument.Frames.Count == 0 ||
            sourceDocument.Frames.Count == 0)
        {
            MessageBox.Show(
                this,
                "Ambas hojas deben tener poses detectadas antes " +
                "de ejecutar la autoasignación.",
                "No hay poses detectadas",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);

            return;
        }

        DialogResult confirmation =
            MessageBox.Show(
                this,
                "El modo ROM filtrará letras, títulos, fondos y " +
                "fragmentos pequeños antes de comparar las siluetas.\n\n" +
                "Solo se aceptarán coincidencias claras. Las poses " +
                "dudosas quedarán sin asignar para evitar símbolos " +
                "extraños en la hoja convertida. Se conservarán las " +
                "asignaciones manuales existentes y no se usará " +
                "volteo vertical.\n\n" +
                "¿Desea continuar?",
                "Autoasignar poses en modo ROM",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

        if (confirmation != DialogResult.Yes)
        {
            return;
        }

        try
        {
            UseWaitCursor = true;
            _autoMapButton.Enabled = false;

            _statusLabel.Text =
                "Analizando siluetas y buscando poses similares...";

            Application.DoEvents();

            RomSafeAutoMappingOptions options =
                new()
                {
                    NormalizedGridSize = 16,
                    MinimumAcceptedScore = 0.66,
                    PreferUniqueSourceFrames = true,
                    AllowHorizontalFlip = true,
                    AllowSourceReuseWhenNecessary = false,
                    BackgroundTolerance = 34,
                    AlphaThreshold = 16,
                    MinimumSourceContentWidth = 10,
                    MinimumSourceContentHeight = 14,
                    MinimumSourceForegroundPixels = 80,
                    MinimumBestSecondGap = 0.015
                };

            IReadOnlyList<RomSafeAutoMappingSuggestion> suggestions =
                RomSafeAutoMappingEngine.Analyze(
                    _aladdinDocument,
                    sourceDocument,
                    options);

            int highConfidenceCount =
                suggestions.Count(
                    item => item.Score >= 0.82);

            int mediumConfidenceCount =
                suggestions.Count(
                    item =>
                        item.Score >= 0.70 &&
                        item.Score < 0.82);

            int lowConfidenceCount =
                suggestions.Count(
                    item =>
                        item.Score >= options.MinimumAcceptedScore &&
                        item.Score < 0.70);

            int appliedCount =
                RomSafeAutoMappingEngine.ApplySuggestions(
                    _project,
                    suggestions,
                    options.MinimumAcceptedScore,
                    replaceExistingMappings: false);

            UpdateAssignmentEditor();

            UseWaitCursor = false;

            if (appliedCount == 0)
            {
                MessageBox.Show(
                    this,
                    "No se encontraron nuevas coincidencias aceptables.\n\n" +
                    "Las asignaciones manuales existentes no fueron " +
                    "modificadas.",
                    "Autoasignación terminada",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);

                _statusLabel.Text =
                    "La autoasignación terminó sin crear " +
                    "nuevas asignaciones.";

                return;
            }

            MessageBox.Show(
                this,
                $"Autoasignación terminada.\n\n" +
                $"Nuevas asignaciones: {appliedCount}\n" +
                $"Confianza alta: {highConfidenceCount}\n" +
                $"Confianza media: {mediumConfidenceCount}\n" +
                $"Confianza baja aceptada: {lowConfidenceCount}\n\n" +
                "Las asignaciones manuales anteriores fueron conservadas. " +
                "Las poses dudosas permanecen como Aladdin para que " +
                "puedan revisarse manualmente antes de convertir a tiles.",
                "Autoasignación compatible con ROM",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);

            _statusLabel.Text =
                $"Autoasignación terminada: {appliedCount} " +
                "nuevas poses asignadas.";

            ShowConvertedSheetPreview();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                this,
                ex.Message,
                "No se pudo completar la autoasignación",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);

            _statusLabel.Text =
                "La autoasignación no pudo completarse.";
        }
        finally
        {
            UseWaitCursor = false;
            UpdateAssignmentEditor();
        }
    }

    // =========================================================
    // REVISIÓN DE POSES PENDIENTES
    // =========================================================

    private void ReviewPendingButton_Click(
        object? sender,
        EventArgs e)
    {
        if (_aladdinDocument is null)
        {
            MessageBox.Show(
                this,
                "Primero debe cargar la hoja de Aladdin.",
                "Hoja de Aladdin no cargada",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);

            return;
        }

        if (_replacementDocuments.Count == 0)
        {
            MessageBox.Show(
                this,
                "Primero debe agregar al menos una hoja " +
                "del personaje nuevo.",
                "Hoja fuente no cargada",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);

            return;
        }

        int pendingCount =
            _project.GetUnmappedTargetCount();

        if (pendingCount == 0)
        {
            MessageBox.Show(
                this,
                "No quedan poses de Aladdin pendientes de asignación.",
                "Revisión completada",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);

            return;
        }

        try
        {
            UseWaitCursor =
                true;

            _reviewPendingButton.Enabled =
                false;

            _statusLabel.Text =
                "Buscando las mejores candidatas para las " +
                "poses pendientes...";

            Application.DoEvents();

            RomPendingPoseReviewOptions options =
                new()
                {
                    NormalizedGridSize =
                        16,

                    TopCandidateCount =
                        5,

                    AllowHorizontalFlip =
                        true,

                    BackgroundTolerance =
                        34,

                    AlphaThreshold =
                        16,

                    MinimumSourceContentWidth =
                        10,

                    MinimumSourceContentHeight =
                        14,

                    MinimumSourceForegroundPixels =
                        80
                };

            IReadOnlyList<RomPendingPoseReviewItem> items =
                RomPendingPoseReviewEngine.Build(
                    _project,
                    _aladdinDocument,
                    _replacementDocuments,
                    options);

            UseWaitCursor =
                false;

            if (items.Count == 0)
            {
                MessageBox.Show(
                    this,
                    "No se encontraron candidatas utilizables para " +
                    "las poses pendientes.\n\n" +
                    "Revise la detección de regiones de las hojas " +
                    "del personaje nuevo y elimine texto, iconos o " +
                    "fragmentos que no sean poses.",
                    "Sin candidatas utilizables",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);

                _statusLabel.Text =
                    "No se encontraron candidatas utilizables " +
                    "para las poses pendientes.";

                return;
            }

            using PendingPoseReviewForm reviewForm =
                new(
                    _project,
                    items);

            reviewForm.ShowDialog(
                this);

            UpdateAssignmentEditor();

            if (reviewForm.AssignedCount > 0)
            {
                _statusLabel.Text =
                    $"Revisión terminada: " +
                    $"{reviewForm.AssignedCount} poses asignadas. " +
                    $"Pendientes: " +
                    $"{_project.GetUnmappedTargetCount()}.";

                ShowConvertedSheetPreview();
            }
            else
            {
                _statusLabel.Text =
                    "La revisión se cerró sin crear " +
                    "nuevas asignaciones.";
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                this,
                ex.Message,
                "No se pudo revisar las poses pendientes",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);

            _statusLabel.Text =
                "La revisión de poses pendientes no pudo completarse.";
        }
        finally
        {
            UseWaitCursor =
                false;

            UpdateAssignmentEditor();
        }
    }

    // =========================================================
    // CARGA DIRECTA DE UNA HOJA FINAL PREPARADA
    // =========================================================

    private void LoadReadySheetButton_Click(
        object? sender,
        EventArgs e)
    {
        if (_aladdinDocument is null)
        {
            MessageBox.Show(
                this,
                "Primero debe cargar la hoja original de Aladdin. " +
                "Esa hoja proporciona el tamaño y las 102 regiones " +
                "que se utilizarán como referencia.",
                "Referencia de Aladdin no cargada",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);

            return;
        }

        using OpenFileDialog dialog =
            new()
            {
                Title =
                    "Cargar hoja final preparada",

                Filter =
                    "Imágenes (*.png;*.bmp;*.gif)|" +
                    "*.png;*.bmp;*.gif|" +
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

        try
        {
            UseWaitCursor =
                true;

            _statusLabel.Text =
                "Cargando y comprobando la hoja final...";

            Application.DoEvents();

            using Image loadedImage =
                Image.FromFile(
                    dialog.FileName);

            Bitmap readyBitmap =
                new(
                    loadedImage);

            Size originalReadySize =
                readyBitmap.Size;

            Size requiredSize =
                new(
                    _aladdinDocument.Width,
                    _aladdinDocument.Height);

            bool wasNormalized =
                false;

            if (readyBitmap.Size !=
                requiredSize)
            {
                bool canNormalize =
                    ReadySheetNormalizer
                        .CanNormalizeProportionally(
                            readyBitmap,
                            requiredSize,
                            maximumRelativeAspectDifference: 0.01);

                if (canNormalize)
                {
                    DialogResult normalizeResult =
                        MessageBox.Show(
                            this,
                            $"La hoja cargada mide " +
                            $"{readyBitmap.Width} × " +
                            $"{readyBitmap.Height} píxeles.\n\n" +
                            $"La plantilla de Aladdin mide " +
                            $"{requiredSize.Width} × " +
                            $"{requiredSize.Height} píxeles.\n\n" +
                            "Las proporciones son prácticamente iguales. " +
                            "El programa puede ajustar automáticamente " +
                            "la hoja al tamaño exacto usando vecino más " +
                            "cercano, sin suavizado.\n\n" +
                            "¿Desea normalizarla ahora?",
                            "Normalizar hoja final",
                            MessageBoxButtons.YesNoCancel,
                            MessageBoxIcon.Question);

                    if (normalizeResult ==
                        DialogResult.Cancel)
                    {
                        readyBitmap.Dispose();
                        return;
                    }

                    if (normalizeResult ==
                        DialogResult.Yes)
                    {
                        Bitmap normalizedBitmap =
                            ReadySheetNormalizer.Normalize(
                                readyBitmap,
                                requiredSize);

                        readyBitmap.Dispose();

                        readyBitmap =
                            normalizedBitmap;

                        wasNormalized =
                            true;
                    }
                }
                else
                {
                    DialogResult continueResult =
                        MessageBox.Show(
                            this,
                            $"La hoja cargada mide " +
                            $"{readyBitmap.Width} × " +
                            $"{readyBitmap.Height}, mientras que la " +
                            $"plantilla mide {requiredSize.Width} × " +
                            $"{requiredSize.Height}.\n\n" +
                            "Las proporciones son diferentes, por lo que " +
                            "un ajuste automático podría deformar o mover " +
                            "las poses.\n\n" +
                            "¿Desea cargarla de todas formas para ver " +
                            "el informe de errores?",
                            "Tamaño no compatible",
                            MessageBoxButtons.YesNo,
                            MessageBoxIcon.Warning);

                    if (continueResult !=
                        DialogResult.Yes)
                    {
                        readyBitmap.Dispose();
                        return;
                    }
                }
            }

            ReadyReplacementSheetValidationOptions options =
                new()
                {
                    ExpectedTargetFrameCount =
                        _aladdinDocument.Frames.Count,

                    BackgroundTolerance =
                        24,

                    AlphaThreshold =
                        16,

                    MinimumVisiblePixelsPerFrame =
                        20,

                    UnchangedPixelRatioThreshold =
                        0.88,

                    UnchangedColorTolerance =
                        12,

                    MaximumVisibleColorsPerFrame =
                        15,

                    OutsideRegionWarningPixelCount =
                        50
                };

            RomCompatibilityValidationResult validationResult =
                ReadyReplacementSheetValidator.Validate(
                    _aladdinDocument,
                    readyBitmap,
                    options);

            _readySheetBitmap?.Dispose();

            _readySheetBitmap =
                readyBitmap;

            _readySheetPath =
                dialog.FileName;

            _readySheetWasNormalized =
                wasNormalized;

            _readySheetOriginalSize =
                originalReadySize;

            _readySheetValidationResult =
                validationResult;

            _loadReadySheetButton.Text =
                "Cambiar hoja final";

            UseWaitCursor =
                false;

            UpdateAssignmentEditor();

            string normalizationText =
                wasNormalized
                    ? $" | Normalizada desde " +
                      $"{originalReadySize.Width} × " +
                      $"{originalReadySize.Height} a " +
                      $"{readyBitmap.Width} × " +
                      $"{readyBitmap.Height}"
                    : string.Empty;

            _statusLabel.Text =
                $"Hoja final cargada: " +
                $"{Path.GetFileName(dialog.FileName)} | " +
                $"{validationResult.StatusText}" +
                normalizationText +
                ".";

            using RomCompatibilityValidationForm validationForm =
                new(
                    validationResult);

            validationForm.ShowDialog(
                this);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                this,
                ex.Message,
                "No se pudo cargar la hoja final",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);

            _statusLabel.Text =
                "La hoja final no pudo cargarse.";
        }
        finally
        {
            UseWaitCursor =
                false;

            UpdateAssignmentEditor();
        }
    }

    private void ClearReadySheetButton_Click(
        object? sender,
        EventArgs e)
    {
        if (_readySheetBitmap is null)
        {
            return;
        }

        DialogResult confirmation =
            MessageBox.Show(
                this,
                "¿Quitar la hoja final cargada?\n\n" +
                "El archivo original no será eliminado.",
                "Quitar hoja final",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

        if (confirmation !=
            DialogResult.Yes)
        {
            return;
        }

        ClearReadySheet(
            updateInterface: true);

        _statusLabel.Text =
            "La hoja final fue retirada. Se mantiene el " +
            "proyecto de asignaciones.";
    }

    private void ClearReadySheet(
        bool updateInterface)
    {
        _readySheetBitmap?.Dispose();

        _readySheetBitmap =
            null;

        _readySheetPath =
            null;

        _readySheetWasNormalized =
            false;

        _readySheetOriginalSize =
            Size.Empty;

        _readySheetValidationResult =
            null;

        if (!updateInterface)
        {
            return;
        }

        _loadReadySheetButton.Text =
            "Cargar hoja final";

        UpdateAssignmentEditor();
    }

    // =========================================================
    // VALIDACIÓN GRÁFICA PARA ROM
    // =========================================================

    private void ValidateRomButton_Click(
        object? sender,
        EventArgs e)
    {
        if (_aladdinDocument is null)
        {
            MessageBox.Show(
                this,
                "Primero debe cargar la hoja original de Aladdin.",
                "Hoja de Aladdin no cargada",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);

            return;
        }

        if (_readySheetBitmap is null &&
            _project.MappingCount == 0)
        {
            MessageBox.Show(
                this,
                "Cargue una hoja final o cree al menos una " +
                "asignación antes de validar.",
                "Sin contenido para validar",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);

            return;
        }

        try
        {
            UseWaitCursor =
                true;

            _validateRomButton.Enabled =
                false;

            _statusLabel.Text =
                "Validando tamaño, poses, límites y colores...";

            Application.DoEvents();

            RomCompatibilityValidationResult result;

            if (_readySheetBitmap is not null)
            {
                ReadyReplacementSheetValidationOptions options =
                    new()
                    {
                        ExpectedTargetFrameCount =
                            _aladdinDocument.Frames.Count,

                        BackgroundTolerance =
                            24,

                        AlphaThreshold =
                            16,

                        MinimumVisiblePixelsPerFrame =
                            20,

                        UnchangedPixelRatioThreshold =
                            0.88,

                        UnchangedColorTolerance =
                            12,

                        MaximumVisibleColorsPerFrame =
                            15,

                        OutsideRegionWarningPixelCount =
                            50
                    };

                result =
                    ReadyReplacementSheetValidator.Validate(
                        _aladdinDocument,
                        _readySheetBitmap,
                        options);

                _readySheetValidationResult =
                    result;
            }
            else
            {
                RomCompatibilityValidationOptions options =
                    new()
                    {
                        ExpectedSheetWidth =
                            _aladdinDocument.Width,

                        ExpectedSheetHeight =
                            _aladdinDocument.Height,

                        ExpectedTargetFrameCount =
                            _aladdinDocument.Frames.Count,

                        RequireAllFramesMapped =
                            true,

                        MaximumVisibleColorsPerFrame =
                            15,

                        MinimumSourceFrameWidth =
                            10,

                        MinimumSourceFrameHeight =
                            14,

                        MinimumSourceVisiblePixels =
                            80,

                        BackgroundTolerance =
                            20,

                        AlphaThreshold =
                            16,

                        RenderConvertedSheet =
                            true
                    };

                result =
                    RomCompatibilityValidator.Validate(
                        _project,
                        _aladdinDocument,
                        options);
            }

            UseWaitCursor =
                false;

            using RomCompatibilityValidationForm validationForm =
                new(
                    result);

            validationForm.ShowDialog(
                this);

            _statusLabel.Text =
                $"Validación ROM: {result.StatusText} | " +
                $"Errores: {result.ErrorCount} | " +
                $"Advertencias: {result.WarningCount}.";
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                this,
                ex.Message,
                "No se pudo completar la validación",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);

            _statusLabel.Text =
                "La validación para ROM no pudo completarse.";
        }
        finally
        {
            UseWaitCursor =
                false;

            UpdateAssignmentEditor();
        }
    }

    // =========================================================
    // PREPARACIÓN PRELIMINAR DE PALETA SNES
    // =========================================================

    private void PrepareSnesPaletteButton_Click(
        object? sender,
        EventArgs e)
    {
        if (_aladdinDocument is null)
        {
            MessageBox.Show(
                this,
                "Primero debe cargar la hoja original de Aladdin.",
                "Hoja de Aladdin no cargada",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);

            return;
        }

        if (_readySheetBitmap is null &&
            _project.MappingCount == 0)
        {
            MessageBox.Show(
                this,
                "Cargue una hoja final o cree al menos una " +
                "asignación antes de preparar la paleta.",
                "Sin contenido",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);

            return;
        }

        int pendingCount =
            _readySheetBitmap is not null
                ? _readySheetValidationResult?.PendingFrameCount
                  ?? 0
                : _project.GetUnmappedTargetCount();

        if (pendingCount > 0)
        {
            DialogResult continueResult =
                MessageBox.Show(
                    this,
                    $"Todavía quedan {pendingCount} poses sin asignar.\n\n" +
                    "La imagen contiene regiones vacías o aparentemente " +
                    "no reemplazadas. El resultado será únicamente " +
                    "una prueba de reducción " +
                    "de colores y no deberá insertarse en la ROM.\n\n" +
                    "¿Desea continuar con la vista previa?",
                    "Preparación incompleta",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);

            if (continueResult !=
                DialogResult.Yes)
            {
                return;
            }
        }

        try
        {
            UseWaitCursor =
                true;

            _prepareSnesPaletteButton.Enabled =
                false;

            _statusLabel.Text =
                "Generando hoja convertida y reduciendo colores...";

            Application.DoEvents();

            using Bitmap convertedSheet =
                _readySheetBitmap is not null
                    ? new Bitmap(
                        _readySheetBitmap)
                    : MappedSpriteSheetRenderer.Render(
                        _aladdinDocument,
                        _project.Mappings,
                        preserveUnmappedTargetFrames: true);

            SnesSpritePaletteQuantizationOptions options =
                new()
                {
                    MaximumPaletteColors =
                        16,

                    ReserveIndexZeroForTransparency =
                        true,

                    BackgroundColor =
                        _aladdinDocument.BackgroundColor,

                    BackgroundTolerance =
                        20,

                    AlphaThreshold =
                        16,

                    SamplingStride =
                        1,

                    UseDithering =
                        false
                };

            using SnesSpritePaletteQuantizationResult quantized =
                SnesSpritePaletteQuantizer.Quantize(
                    convertedSheet,
                    options);

            string sourceName =
                _readySheetPath is not null
                    ? Path.GetFileNameWithoutExtension(
                        _readySheetPath)
                    : GetActiveReplacementDocument()
                      is SpriteSheetDocument sourceDocument
                        ? Path.GetFileNameWithoutExtension(
                            sourceDocument.FileName)
                        : "personaje";

            string suggestedBaseName =
                $"{sourceName}_para_Aladdin";

            UseWaitCursor =
                false;

            using SnesPalettePreviewForm preview =
                new(
                    convertedSheet,
                    quantized,
                    suggestedBaseName);

            preview.ShowDialog(
                this);

            _statusLabel.Text =
                "Vista previa de paleta SNES generada: " +
                $"{quantized.OriginalUniqueColorCount:N0} colores " +
                "originales reducidos a 16 índices.";
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                this,
                ex.Message,
                "No se pudo preparar la paleta SNES",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);

            _statusLabel.Text =
                "La preparación de paleta SNES no pudo completarse.";
        }
        finally
        {
            UseWaitCursor =
                false;

            UpdateAssignmentEditor();
        }
    }

    // =========================================================
    // PRUEBA CONTROLADA DE UNA POSE EN COPIA DE ROM
    // =========================================================

    private void TestPoseRomButton_Click(
        object? sender,
        EventArgs e)
    {
        if (_aladdinDocument is null)
        {
            MessageBox.Show(
                this,
                "Primero debe cargar la hoja original de Aladdin.",
                "Hoja de referencia no cargada",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);

            return;
        }

        if (_readySheetBitmap is null &&
            _project.MappingCount == 0)
        {
            MessageBox.Show(
                this,
                "Cargue una hoja final o cree asignaciones antes " +
                "de preparar una prueba de pose.",
                "Sin contenido",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);

            return;
        }

        if (_readySheetBitmap is not null &&
            _readySheetValidationResult is not null &&
            _readySheetValidationResult.ErrorCount > 0)
        {
            MessageBox.Show(
                this,
                "La hoja final todavía contiene errores de validación. " +
                "Corríjalos antes de crear una copia de ROM.",
                "Hoja final no apta",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);

            return;
        }

        int warningCount =
            _readySheetBitmap is not null
                ? _readySheetValidationResult?.WarningCount
                  ?? 0
                : 0;

        int pendingCount =
            _readySheetBitmap is not null
                ? _readySheetValidationResult?.PendingFrameCount
                  ?? 0
                : _project.GetUnmappedTargetCount();

        if (warningCount > 0 ||
            pendingCount > 0)
        {
            DialogResult continueResult =
                MessageBox.Show(
                    this,
                    $"La preparación todavía tiene " +
                    $"{warningCount} advertencias y " +
                    $"{pendingCount} poses pendientes.\n\n" +
                    "La prueba se realizará únicamente sobre una copia " +
                    "de la ROM y puede mostrar colores o gráficos " +
                    "incorrectos.\n\n" +
                    "¿Desea continuar con la prueba técnica?",
                    "Prueba experimental",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);

            if (continueResult !=
                DialogResult.Yes)
            {
                return;
            }
        }

        try
        {
            UseWaitCursor =
                true;

            _testPoseRomButton.Enabled =
                false;

            _statusLabel.Text =
                "Preparando una pose cuantizada para la prueba de ROM...";

            Application.DoEvents();

            using Bitmap convertedSheet =
                _readySheetBitmap is not null
                    ? new Bitmap(
                        _readySheetBitmap)
                    : MappedSpriteSheetRenderer.Render(
                        _aladdinDocument,
                        _project.Mappings,
                        preserveUnmappedTargetFrames: true);

            SnesSpritePaletteQuantizationOptions options =
                new()
                {
                    MaximumPaletteColors =
                        16,

                    ReserveIndexZeroForTransparency =
                        true,

                    BackgroundColor =
                        _aladdinDocument.BackgroundColor,

                    BackgroundTolerance =
                        20,

                    AlphaThreshold =
                        16,

                    SamplingStride =
                        1,

                    UseDithering =
                        false
                };

            using SnesSpritePaletteQuantizationResult quantized =
                SnesSpritePaletteQuantizer.Quantize(
                    convertedSheet,
                    options);

            string sourceName =
                _readySheetPath is not null
                    ? Path.GetFileNameWithoutExtension(
                        _readySheetPath)
                    : GetActiveReplacementDocument()
                      is SpriteSheetDocument sourceDocument
                        ? Path.GetFileNameWithoutExtension(
                            sourceDocument.FileName)
                        : "Personaje";

            SpriteFrame? initialFrame =
                _selectedTargetFrame ??
                _aladdinDocument.Frames
                    .OrderBy(frame => frame.Index)
                    .FirstOrDefault();

            UseWaitCursor =
                false;

            using RomPoseTestForm testForm =
                new(
                    quantized,
                    _aladdinDocument,
                    initialFrame,
                    sourceName);

            testForm.ShowDialog(
                this);

            _statusLabel.Text =
                "La ventana de prueba controlada de ROM fue cerrada.";
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                this,
                ex.Message,
                "No se pudo preparar la prueba de ROM",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);

            _statusLabel.Text =
                "La prueba controlada de ROM no pudo prepararse.";
        }
        finally
        {
            UseWaitCursor =
                false;

            UpdateAssignmentEditor();
        }
    }

    // =========================================================
    // EDITOR TILE POR TILE PARA SEGMENTO DE ROM
    // =========================================================

    private void TileSegmentEditorButton_Click(
        object? sender,
        EventArgs e)
    {
        if (_aladdinDocument is null)
        {
            MessageBox.Show(
                this,
                "Primero debe cargar la hoja original de Aladdin.",
                "Hoja de referencia no cargada",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);

            return;
        }

        if (_readySheetBitmap is null &&
            _project.MappingCount == 0)
        {
            MessageBox.Show(
                this,
                "Cargue una hoja final o cree asignaciones antes " +
                "de abrir el editor tile por tile.",
                "Sin contenido",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);

            return;
        }

        if (_readySheetBitmap is not null &&
            _readySheetValidationResult is not null &&
            _readySheetValidationResult.ErrorCount > 0)
        {
            MessageBox.Show(
                this,
                "La hoja final contiene errores de validación. " +
                "Corríjalos antes de preparar una nueva prueba.",
                "Hoja final no apta",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);

            return;
        }

        try
        {
            UseWaitCursor =
                true;

            _tileSegmentEditorButton.Enabled =
                false;

            _statusLabel.Text =
                "Preparando tiles para el editor de segmento ROM...";

            Application.DoEvents();

            using Bitmap convertedSheet =
                _readySheetBitmap is not null
                    ? new Bitmap(
                        _readySheetBitmap)
                    : MappedSpriteSheetRenderer.Render(
                        _aladdinDocument,
                        _project.Mappings,
                        preserveUnmappedTargetFrames: true);

            SnesSpritePaletteQuantizationOptions options =
                new()
                {
                    MaximumPaletteColors =
                        16,

                    ReserveIndexZeroForTransparency =
                        true,

                    BackgroundColor =
                        _aladdinDocument.BackgroundColor,

                    BackgroundTolerance =
                        20,

                    AlphaThreshold =
                        16,

                    SamplingStride =
                        1,

                    UseDithering =
                        false
                };

            using SnesSpritePaletteQuantizationResult quantized =
                SnesSpritePaletteQuantizer.Quantize(
                    convertedSheet,
                    options);

            string sourceName =
                _readySheetPath is not null
                    ? Path.GetFileNameWithoutExtension(
                        _readySheetPath)
                    : GetActiveReplacementDocument()
                      is SpriteSheetDocument sourceDocument
                        ? Path.GetFileNameWithoutExtension(
                            sourceDocument.FileName)
                        : "Personaje";

            SpriteFrame? initialFrame =
                _selectedTargetFrame ??
                _aladdinDocument.Frames
                    .OrderBy(frame => frame.Index)
                    .FirstOrDefault();

            UseWaitCursor =
                false;

            using RomTileSegmentEditorForm editor =
                new(
                    quantized,
                    _aladdinDocument,
                    initialFrame,
                    sourceName);

            editor.ShowDialog(
                this);

            _statusLabel.Text =
                "El editor tile por tile fue cerrado.";
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                this,
                ex.Message,
                "No se pudo abrir el editor tile por tile",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);

            _statusLabel.Text =
                "El editor tile por tile no pudo abrirse.";
        }
        finally
        {
            UseWaitCursor =
                false;

            UpdateAssignmentEditor();
        }
    }

    // =========================================================
    // VENTANA INDEPENDIENTE DE ASIGNACIÓN
    // =========================================================

    private void OpenMappingEditorButton_Click(
        object? sender,
        EventArgs e)
    {
        if (_aladdinDocument is null ||
            _selectedTargetFrame is null)
        {
            MessageBox.Show(
                this,
                "Seleccione una pose objetivo de Aladdin.",
                "Pose de Aladdin no seleccionada",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);

            return;
        }

        FrameMapping? existingMapping =
            GetSelectedTargetMapping();

        SpriteSheetDocument? sourceDocument =
            null;

        SpriteFrame? sourceFrame =
            null;

        FrameMapping? mappingToEdit =
            null;

        SpriteSheetDocument? activeSourceDocument =
            GetActiveReplacementDocument();

        bool hasSelectedSource =
            activeSourceDocument is not null &&
            _selectedSourceFrame is not null &&
            activeSourceDocument.Frames.Contains(
                _selectedSourceFrame);

        if (hasSelectedSource)
        {
            sourceDocument =
                activeSourceDocument;

            sourceFrame =
                _selectedSourceFrame;

            bool sameAsExisting =
                existingMapping is not null &&
                ReferenceEquals(
                    existingMapping.SourceDocument,
                    sourceDocument) &&
                ReferenceEquals(
                    existingMapping.SourceFrame,
                    sourceFrame);

            if (sameAsExisting)
            {
                mappingToEdit =
                    existingMapping;
            }
        }
        else if (existingMapping is not null)
        {
            sourceDocument =
                existingMapping.SourceDocument;

            sourceFrame =
                existingMapping.SourceFrame;

            mappingToEdit =
                existingMapping;
        }

        if (sourceDocument is null ||
            sourceFrame is null)
        {
            MessageBox.Show(
                this,
                "Seleccione una pose del personaje nuevo.",
                "Pose fuente no seleccionada",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);

            return;
        }

        using FrameMappingForm editor =
            new(
                _aladdinDocument,
                _selectedTargetFrame,
                sourceDocument,
                sourceFrame,
                mappingToEdit);

        DialogResult result =
            editor.ShowDialog(
                this);

        if (result != DialogResult.OK ||
            editor.ResultMapping is null)
        {
            _statusLabel.Text =
                "Edición de la asignación cancelada.";

            return;
        }

        FrameMapping savedMapping =
            _project.AssignFrame(
                _selectedTargetFrame,
                editor.ResultMapping.SourceDocument,
                editor.ResultMapping.SourceFrame);

        CopyMappingSettings(
            editor.ResultMapping,
            savedMapping);

        UpdateAssignmentEditor();

        _statusLabel.Text =
            $"Asignación guardada: Aladdin " +
            $"{savedMapping.TargetFrame.Index + 1:000} ← " +
            $"{savedMapping.SourceDocument.FileName} / " +
            $"Pose " +
            $"{savedMapping.SourceFrame.Index + 1:000}.";

        ShowConvertedSheetPreview();
    }

    private void PreviewConvertedSheetButton_Click(
        object? sender,
        EventArgs e)
    {
        ShowConvertedSheetPreview();
    }

    private void ShowConvertedSheetPreview()
    {
        if (_aladdinDocument is null)
        {
            MessageBox.Show(
                this,
                "Primero debe cargar la hoja de Aladdin.",
                "Hoja no cargada",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);

            return;
        }

        if (_readySheetBitmap is null &&
            _project.MappingCount == 0)
        {
            MessageBox.Show(
                this,
                "Todavía no hay una hoja final ni poses " +
                "asignadas para mostrar.",
                "Sin contenido",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);

            return;
        }

        try
        {
            UseWaitCursor = true;

            using Bitmap convertedSheet =
                _readySheetBitmap is not null
                    ? new Bitmap(
                        _readySheetBitmap)
                    : MappedSpriteSheetRenderer.Render(
                        _aladdinDocument,
                        _project.Mappings,
                        preserveUnmappedTargetFrames: true);

            int completedCount =
                _readySheetBitmap is not null
                    ? _readySheetValidationResult?.MappedFrameCount
                      ?? _aladdinDocument.Frames.Count
                    : _project.MappingCount;

            using ConvertedSheetPreviewForm preview =
                new(
                    convertedSheet,
                    completedCount,
                    _aladdinDocument.Frames.Count);

            UseWaitCursor = false;

            preview.ShowDialog(this);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                this,
                ex.Message,
                "No se pudo generar la conversión",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
        finally
        {
            UseWaitCursor = false;
        }
    }

    private void RemoveMappingButton_Click(
        object? sender,
        EventArgs e)
    {
        if (_selectedTargetFrame is null)
        {
            return;
        }

        DialogResult confirmation =
            MessageBox.Show(
                this,
                "¿Quitar la asignación de la pose " +
                "seleccionada de Aladdin?",
                "Quitar asignación",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

        if (confirmation
            != DialogResult.Yes)
        {
            return;
        }

        bool removed =
            _project.RemoveMapping(
                _selectedTargetFrame);

        if (!removed)
        {
            return;
        }

        UpdateAssignmentEditor();

        _statusLabel.Text =
            "La asignación fue eliminada.";
    }

    private static void CopyMappingSettings(
        FrameMapping source,
        FrameMapping destination)
    {
        destination.AutoFit =
            source.AutoFit;

        destination.Scale =
            source.Scale;

        destination.OffsetX =
            source.OffsetX;

        destination.OffsetY =
            source.OffsetY;

        destination.FlipHorizontal =
            source.FlipHorizontal;

        destination.FlipVertical =
            source.FlipVertical;
    }

    // =========================================================
    // ESTADO DEL EDITOR
    // =========================================================

    private void UpdateAssignmentEditor()
    {
        FrameMapping? existingMapping =
            GetSelectedTargetMapping();

        SpriteSheetDocument? activeSourceDocument =
            GetActiveReplacementDocument();

        _autoMapButton.Enabled =
            _aladdinDocument is not null &&
            _aladdinDocument.Frames.Count > 0 &&
            activeSourceDocument is not null &&
            activeSourceDocument.Frames.Count > 0;

        _reviewPendingButton.Enabled =
            _aladdinDocument is not null &&
            _aladdinDocument.Frames.Count > 0 &&
            _replacementDocuments.Any(
                document => document.Frames.Count > 0) &&
            _project.GetUnmappedTargetCount() > 0;

        _loadReadySheetButton.Enabled =
            _aladdinDocument is not null &&
            _aladdinDocument.Frames.Count > 0;

        _clearReadySheetButton.Enabled =
            _readySheetBitmap is not null;

        _validateRomButton.Enabled =
            _aladdinDocument is not null &&
            _aladdinDocument.Frames.Count > 0 &&
            (_readySheetBitmap is not null ||
             _project.MappingCount > 0);

        _prepareSnesPaletteButton.Enabled =
            _aladdinDocument is not null &&
            _aladdinDocument.Frames.Count > 0 &&
            (_readySheetBitmap is not null ||
             _project.MappingCount > 0);

        _testPoseRomButton.Enabled =
            _aladdinDocument is not null &&
            _aladdinDocument.Frames.Count > 0 &&
            (_readySheetBitmap is not null ||
             _project.MappingCount > 0);

        _tileSegmentEditorButton.Enabled =
            _aladdinDocument is not null &&
            _aladdinDocument.Frames.Count > 0 &&
            (_readySheetBitmap is not null ||
             _project.MappingCount > 0);

        bool selectedSourceIsValid =
            activeSourceDocument is not null &&
            _selectedSourceFrame is not null &&
            activeSourceDocument.Frames.Contains(
                _selectedSourceFrame);

        bool canOpenEditor =
            _aladdinDocument is not null &&
            _selectedTargetFrame is not null &&
            (selectedSourceIsValid ||
             existingMapping is not null);

        _openMappingEditorButton.Enabled =
            canOpenEditor;

        _removeMappingButton.Enabled =
            existingMapping is not null;

        _previewConvertedSheetButton.Enabled =
            _readySheetBitmap is not null ||
            _project.MappingCount > 0;

        if (existingMapping is null)
        {
            _openMappingEditorButton.Text =
                "Crear asignación";
        }
        else if (selectedSourceIsValid &&
                 (!ReferenceEquals(
                      existingMapping.SourceDocument,
                      activeSourceDocument) ||
                  !ReferenceEquals(
                      existingMapping.SourceFrame,
                      _selectedSourceFrame)))
        {
            _openMappingEditorButton.Text =
                "Reemplazar asignación";
        }
        else
        {
            _openMappingEditorButton.Text =
                "Editar asignación";
        }

        UpdateSelectionLabels(
            existingMapping);

        UpdateMappingStatistics();
    }

    private void UpdateSelectionLabels(
        FrameMapping? existingMapping)
    {
        if (_selectedTargetFrame is null)
        {
            _targetSelectionLabel.Text =
                "Pose objetivo de Aladdin: ninguna";
        }
        else
        {
            _targetSelectionLabel.Text =
                $"Pose objetivo de Aladdin: " +
                $"{_selectedTargetFrame.Index + 1:000} | " +
                $"{_selectedTargetFrame.Bounds.Width} × " +
                $"{_selectedTargetFrame.Bounds.Height} píxeles";
        }

        SpriteSheetDocument? sourceDocument =
            GetActiveReplacementDocument();

        if (_selectedSourceFrame is not null &&
            sourceDocument is not null &&
            sourceDocument.Frames.Contains(
                _selectedSourceFrame))
        {
            _sourceSelectionLabel.Text =
                $"Pose del personaje nuevo: " +
                $"{sourceDocument.FileName} / " +
                $"{_selectedSourceFrame.Index + 1:000} | " +
                $"{_selectedSourceFrame.Bounds.Width} × " +
                $"{_selectedSourceFrame.Bounds.Height} píxeles";

            return;
        }

        if (existingMapping is not null)
        {
            _sourceSelectionLabel.Text =
                $"Pose asignada actualmente: " +
                $"{existingMapping.SourceDocument.FileName} / " +
                $"{existingMapping.SourceFrame.Index + 1:000}";

            return;
        }

        _sourceSelectionLabel.Text =
            "Pose del personaje nuevo: ninguna";
    }

    private void UpdateMappingStatistics()
    {
        int targetCount =
            _aladdinDocument?.Frames.Count
            ?? 0;

        string readySheetText =
            _readySheetBitmap is null
                ? string.Empty
                : $" | Hoja final: " +
                  $"{Path.GetFileName(_readySheetPath)}" +
                  (_readySheetWasNormalized
                      ? $" (normalizada desde " +
                        $"{_readySheetOriginalSize.Width} × " +
                        $"{_readySheetOriginalSize.Height})"
                      : string.Empty);

        int assignedCount =
            _readySheetBitmap is not null
                ? _readySheetValidationResult?.MappedFrameCount
                  ?? 0
                : _project.MappingCount;

        int pendingCount =
            _readySheetBitmap is not null
                ? _readySheetValidationResult?.PendingFrameCount
                  ?? targetCount
                : _project.GetUnmappedTargetCount();

        _mappingStatisticsLabel.Text =
            $"Total de poses objetivo: " +
            $"{targetCount} | " +
            $"Completadas: {assignedCount} | " +
            $"Pendientes: {pendingCount}" +
            readySheetText;
    }

    private FrameMapping?
        GetSelectedTargetMapping()
    {
        return _selectedTargetFrame is null
            ? null
            : _project.GetMapping(
                _selectedTargetFrame);
    }

    // =========================================================
    // UTILIDADES
    // =========================================================

    private SpriteSheetDocument? GetDocument(
        SheetSide side)
    {
        return side == SheetSide.Aladdin
            ? _aladdinDocument
            : GetActiveReplacementDocument();
    }

    private SpriteSheetDocument?
        GetActiveReplacementDocument()
    {
        return _replacementSheetCombo.SelectedItem
               is ReplacementSheetItem item
            ? item.Document
            : null;
    }

    private SpriteSheetPreview GetPreview(
        SheetSide side)
    {
        return side == SheetSide.Aladdin
            ? _aladdinPreview
            : _replacementPreview;
    }

    private void ResetSelectionForSide(
        SheetSide side)
    {
        if (side == SheetSide.Aladdin)
        {
            _selectedTargetFrame =
                null;

            _selectedSourceFrame =
                null;

            _replacementPreview.ClearSelection();
        }
        else
        {
            _selectedSourceFrame =
                null;
        }
    }

    private void ShowMissingSheetMessage(
        SheetSide side)
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
    }

    private void UpdateInformation(
        SheetSide side)
    {
        SpriteSheetDocument? document =
            GetDocument(side);

        Label informationLabel =
            side == SheetSide.Aladdin
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
            side == SheetSide.Replacement
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
    // CIERRE
    // =========================================================

    protected override void OnFormClosed(
        FormClosedEventArgs e)
    {
        _aladdinPreview.Document =
            null;

        _replacementPreview.Document =
            null;

        _project.ClearMappings();

        ClearReadySheet(
            updateInterface: false);

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
