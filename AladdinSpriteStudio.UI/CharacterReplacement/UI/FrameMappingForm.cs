using AladdinSpriteStudio.UI.CharacterReplacement.Controls;
using AladdinSpriteStudio.UI.CharacterReplacement.Models;

namespace AladdinSpriteStudio.UI.CharacterReplacement.UI;

/// <summary>
/// Ventana independiente para adaptar una pose del personaje nuevo
/// al espacio ocupado por una pose original de Aladdin.
/// </summary>
public sealed class FrameMappingForm : System.Windows.Forms.Form
{
    private readonly SpriteSheetDocument _targetDocument;
    private readonly SpriteFrame _targetFrame;
    private readonly SpriteSheetDocument _sourceDocument;
    private readonly SpriteFrame _sourceFrame;

    private readonly FrameMapping _workingMapping;
    private readonly FrameMappingPreview _mappingPreview;

    private readonly CheckBox _autoFitCheckBox;
    private readonly NumericUpDown _scaleInput;
    private readonly NumericUpDown _offsetXInput;
    private readonly NumericUpDown _offsetYInput;
    private readonly CheckBox _flipHorizontalCheckBox;
    private readonly CheckBox _flipVerticalCheckBox;

    private readonly Label _effectiveScaleLabel;
    private readonly Label _destinationBoundsLabel;

    private readonly Button _resetButton;
    private readonly Button _saveButton;
    private readonly Button _cancelButton;

    private bool _updatingControls;

    /// <summary>
    /// Asignación resultante. Solo se establece cuando el usuario
    /// guarda y la ventana se cierra con DialogResult.OK.
    /// </summary>
    public FrameMapping? ResultMapping { get; private set; }

    public FrameMappingForm(
        SpriteSheetDocument targetDocument,
        SpriteFrame targetFrame,
        SpriteSheetDocument sourceDocument,
        SpriteFrame sourceFrame,
        FrameMapping? existingMapping = null)
    {
        _targetDocument =
            targetDocument ??
            throw new ArgumentNullException(nameof(targetDocument));

        _targetFrame =
            targetFrame ??
            throw new ArgumentNullException(nameof(targetFrame));

        _sourceDocument =
            sourceDocument ??
            throw new ArgumentNullException(nameof(sourceDocument));

        _sourceFrame =
            sourceFrame ??
            throw new ArgumentNullException(nameof(sourceFrame));

        _workingMapping = new FrameMapping(
            _targetDocument,
            _targetFrame,
            _sourceDocument,
            _sourceFrame);

        if (existingMapping is not null)
        {
            CopySettings(existingMapping, _workingMapping);
        }

        Text = "Editor de asignación de pose";
        Width = 1180;
        Height = 760;
        MinimumSize = new Size(900, 620);
        StartPosition = FormStartPosition.CenterParent;
        ShowInTaskbar = false;
        BackColor = SystemColors.Control;
        KeyPreview = true;

        _mappingPreview = new FrameMappingPreview
        {
            Dock = DockStyle.Fill,
            Mapping = _workingMapping,
            Margin = new Padding(8)
        };

        _autoFitCheckBox = new CheckBox
        {
            Text = "Ajustar automáticamente al espacio de Aladdin",
            AutoSize = true,
            Checked = _workingMapping.AutoFit,
            Margin = new Padding(3, 6, 3, 10)
        };

        _scaleInput = new NumericUpDown
        {
            DecimalPlaces = 2,
            Increment = 0.05m,
            Minimum = 0.05m,
            Maximum = 16m,
            Width = 130
        };

        _offsetXInput = new NumericUpDown
        {
            Minimum = -512,
            Maximum = 512,
            Width = 130
        };

        _offsetYInput = new NumericUpDown
        {
            Minimum = -512,
            Maximum = 512,
            Width = 130
        };

        _flipHorizontalCheckBox = new CheckBox
        {
            Text = "Voltear horizontalmente",
            AutoSize = true
        };

        _flipVerticalCheckBox = new CheckBox
        {
            Text = "Voltear verticalmente",
            AutoSize = true
        };

        _effectiveScaleLabel = new Label
        {
            AutoSize = true,
            Margin = new Padding(3, 8, 3, 3)
        };

        _destinationBoundsLabel = new Label
        {
            AutoSize = true,
            MaximumSize = new Size(340, 0),
            Margin = new Padding(3, 3, 3, 10)
        };

        _resetButton = new Button
        {
            Text = "Restablecer",
            AutoSize = true
        };

        _saveButton = new Button
        {
            Text = "Guardar asignación",
            AutoSize = true
        };

        _cancelButton = new Button
        {
            Text = "Cancelar",
            AutoSize = true,
            DialogResult = DialogResult.Cancel
        };

        _autoFitCheckBox.CheckedChanged += AdjustmentControlChanged;
        _scaleInput.ValueChanged += AdjustmentControlChanged;
        _offsetXInput.ValueChanged += AdjustmentControlChanged;
        _offsetYInput.ValueChanged += AdjustmentControlChanged;
        _flipHorizontalCheckBox.CheckedChanged += AdjustmentControlChanged;
        _flipVerticalCheckBox.CheckedChanged += AdjustmentControlChanged;

        _resetButton.Click += ResetButton_Click;
        _saveButton.Click += SaveButton_Click;

        AcceptButton = _saveButton;
        CancelButton = _cancelButton;

        Controls.Add(CreateMainLayout());

        LoadWorkingMappingIntoControls();
        UpdatePreviewInformation();
    }

    // =========================================================
    // INTERFAZ
    // =========================================================

    private Control CreateMainLayout()
    {
        TableLayoutPanel root = new()
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Padding = new Padding(10)
        };

        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        root.Controls.Add(CreateHeaderPanel(), 0, 0);
        root.Controls.Add(CreateEditorPanel(), 0, 1);
        root.Controls.Add(CreateBottomButtonPanel(), 0, 2);

        return root;
    }

    private Control CreateHeaderPanel()
    {
        TableLayoutPanel header = new()
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            ColumnCount = 2,
            RowCount = 1,
            Margin = new Padding(0, 0, 0, 8)
        };

        header.ColumnStyles.Add(
            new ColumnStyle(SizeType.Percent, 50));

        header.ColumnStyles.Add(
            new ColumnStyle(SizeType.Percent, 50));

        header.Controls.Add(
            CreateFrameInformationGroup(
                "Pose objetivo de Aladdin",
                _targetDocument,
                _targetFrame),
            0,
            0);

        header.Controls.Add(
            CreateFrameInformationGroup(
                "Pose del personaje nuevo",
                _sourceDocument,
                _sourceFrame),
            1,
            0);

        return header;
    }

    private static Control CreateFrameInformationGroup(
        string title,
        SpriteSheetDocument document,
        SpriteFrame frame)
    {
        GroupBox group = new()
        {
            Text = title,
            Dock = DockStyle.Fill,
            AutoSize = true,
            Padding = new Padding(8),
            Margin = new Padding(4)
        };

        Label information = new()
        {
            AutoSize = true,
            MaximumSize = new Size(500, 0),
            Text =
                $"Archivo: {document.FileName}\n" +
                $"Pose: {frame.Index + 1:000}\n" +
                $"Posición: X={frame.Bounds.X}, Y={frame.Bounds.Y}\n" +
                $"Tamaño: {frame.Bounds.Width} × " +
                $"{frame.Bounds.Height} píxeles"
        };

        group.Controls.Add(information);

        return group;
    }

    private Control CreateEditorPanel()
    {
        TableLayoutPanel editor = new()
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1
        };

        editor.ColumnStyles.Add(
            new ColumnStyle(SizeType.Percent, 68));

        editor.ColumnStyles.Add(
            new ColumnStyle(SizeType.Percent, 32));

        editor.RowStyles.Add(
            new RowStyle(SizeType.Percent, 100));

        editor.Controls.Add(_mappingPreview, 0, 0);
        editor.Controls.Add(CreateSettingsPanel(), 1, 0);

        return editor;
    }

    private Control CreateSettingsPanel()
    {
        GroupBox settingsGroup = new()
        {
            Text = "Ajustes de adaptación",
            Dock = DockStyle.Fill,
            Padding = new Padding(10),
            Margin = new Padding(8)
        };

        Panel scrollPanel = new()
        {
            Dock = DockStyle.Fill,
            AutoScroll = true
        };

        FlowLayoutPanel settingsFlow = new()
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            Padding = new Padding(4)
        };

        Label explanation = new()
        {
            Text =
                "La pose nueva se adapta al rectángulo ocupado " +
                "por la pose original de Aladdin. El ajuste " +
                "automático conserva la proporción y alinea los pies.",
            AutoSize = true,
            MaximumSize = new Size(330, 0),
            Margin = new Padding(3, 3, 3, 12)
        };

        settingsFlow.Controls.Add(explanation);
        settingsFlow.Controls.Add(_autoFitCheckBox);

        TableLayoutPanel adjustmentTable = new()
        {
            AutoSize = true,
            ColumnCount = 2,
            RowCount = 5,
            Margin = new Padding(3, 4, 3, 8)
        };

        adjustmentTable.ColumnStyles.Add(
            new ColumnStyle(SizeType.AutoSize));

        adjustmentTable.ColumnStyles.Add(
            new ColumnStyle(SizeType.AutoSize));

        AddSettingRow(
            adjustmentTable,
            0,
            "Escala:",
            _scaleInput);

        AddSettingRow(
            adjustmentTable,
            1,
            "Posición X:",
            _offsetXInput);

        AddSettingRow(
            adjustmentTable,
            2,
            "Posición Y:",
            _offsetYInput);

        AddSettingRow(
            adjustmentTable,
            3,
            "Orientación:",
            _flipHorizontalCheckBox);

        AddSettingRow(
            adjustmentTable,
            4,
            string.Empty,
            _flipVerticalCheckBox);

        settingsFlow.Controls.Add(adjustmentTable);
        settingsFlow.Controls.Add(_effectiveScaleLabel);
        settingsFlow.Controls.Add(_destinationBoundsLabel);
        settingsFlow.Controls.Add(_resetButton);

        scrollPanel.Controls.Add(settingsFlow);
        settingsGroup.Controls.Add(scrollPanel);

        return settingsGroup;
    }

    private static void AddSettingRow(
        TableLayoutPanel table,
        int row,
        string labelText,
        Control control)
    {
        Label label = new()
        {
            Text = labelText,
            AutoSize = true,
            Anchor = AnchorStyles.Left,
            Margin = new Padding(3, 7, 10, 5)
        };

        control.Anchor = AnchorStyles.Left;
        control.Margin = new Padding(3, 4, 3, 5);

        table.Controls.Add(label, 0, row);
        table.Controls.Add(control, 1, row);
    }

    private Control CreateBottomButtonPanel()
    {
        FlowLayoutPanel buttonPanel = new()
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            Padding = new Padding(0, 10, 0, 0)
        };

        buttonPanel.Controls.Add(_cancelButton);
        buttonPanel.Controls.Add(_saveButton);

        return buttonPanel;
    }

    // =========================================================
    // AJUSTES Y VISTA PREVIA
    // =========================================================

    private void LoadWorkingMappingIntoControls()
    {
        _updatingControls = true;

        try
        {
            _autoFitCheckBox.Checked =
                _workingMapping.AutoFit;

            _scaleInput.Value = ClampDecimal(
                (decimal)_workingMapping.Scale,
                _scaleInput.Minimum,
                _scaleInput.Maximum);

            _offsetXInput.Value = ClampDecimal(
                _workingMapping.OffsetX,
                _offsetXInput.Minimum,
                _offsetXInput.Maximum);

            _offsetYInput.Value = ClampDecimal(
                _workingMapping.OffsetY,
                _offsetYInput.Minimum,
                _offsetYInput.Maximum);

            _flipHorizontalCheckBox.Checked =
                _workingMapping.FlipHorizontal;

            _flipVerticalCheckBox.Checked =
                _workingMapping.FlipVertical;
        }
        finally
        {
            _updatingControls = false;
        }

        UpdateScaleInputState();
    }

    private void AdjustmentControlChanged(
        object? sender,
        EventArgs e)
    {
        if (_updatingControls)
        {
            return;
        }

        ApplyControlsToWorkingMapping();
    }

    private void ApplyControlsToWorkingMapping()
    {
        _workingMapping.AutoFit =
            _autoFitCheckBox.Checked;

        _workingMapping.Scale =
            (float)_scaleInput.Value;

        _workingMapping.OffsetX =
            decimal.ToInt32(_offsetXInput.Value);

        _workingMapping.OffsetY =
            decimal.ToInt32(_offsetYInput.Value);

        _workingMapping.FlipHorizontal =
            _flipHorizontalCheckBox.Checked;

        _workingMapping.FlipVertical =
            _flipVerticalCheckBox.Checked;

        UpdateScaleInputState();

        _mappingPreview.Mapping =
            _workingMapping;

        _mappingPreview.Invalidate();

        UpdatePreviewInformation();
    }

    private void UpdateScaleInputState()
    {
        _scaleInput.Enabled =
            !_autoFitCheckBox.Checked;
    }

    private void UpdatePreviewInformation()
    {
        Rectangle destination =
            _workingMapping.GetDestinationBounds();

        _effectiveScaleLabel.Text =
            $"Escala efectiva: " +
            $"{_workingMapping.EffectiveScale:0.00}";

        _destinationBoundsLabel.Text =
            $"Resultado dentro del espacio objetivo:\n" +
            $"X={destination.X}, Y={destination.Y}\n" +
            $"Tamaño: {destination.Width} × " +
            $"{destination.Height} píxeles";
    }

    private void ResetButton_Click(
        object? sender,
        EventArgs e)
    {
        _workingMapping.ResetAdjustments();

        LoadWorkingMappingIntoControls();

        _mappingPreview.Mapping =
            _workingMapping;

        _mappingPreview.Invalidate();

        UpdatePreviewInformation();
    }

    // =========================================================
    // GUARDAR
    // =========================================================

    private void SaveButton_Click(
        object? sender,
        EventArgs e)
    {
        ApplyControlsToWorkingMapping();

        FrameMapping result = new(
            _targetDocument,
            _targetFrame,
            _sourceDocument,
            _sourceFrame);

        CopySettings(
            _workingMapping,
            result);

        ResultMapping =
            result;

        DialogResult =
            DialogResult.OK;

        Close();
    }

    private static void CopySettings(
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

    private static decimal ClampDecimal(
        decimal value,
        decimal minimum,
        decimal maximum)
    {
        return Math.Min(
            maximum,
            Math.Max(
                minimum,
                value));
    }

    protected override void OnFormClosed(
        FormClosedEventArgs e)
    {
        _mappingPreview.Mapping = null;

        base.OnFormClosed(e);
    }
}
