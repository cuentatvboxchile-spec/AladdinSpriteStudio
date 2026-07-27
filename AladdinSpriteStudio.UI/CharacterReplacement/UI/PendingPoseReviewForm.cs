using AladdinSpriteStudio.UI.CharacterReplacement.Analysis;
using AladdinSpriteStudio.UI.CharacterReplacement.Controls;
using AladdinSpriteStudio.UI.CharacterReplacement.Models;

namespace AladdinSpriteStudio.UI.CharacterReplacement.UI;

/// <summary>
/// Asistente para revisar manualmente las poses que quedaron sin asignar.
/// Muestra las mejores candidatas y permite asignarlas con ajuste seguro
/// o abrir el editor detallado antes de guardarlas.
/// </summary>
public sealed class PendingPoseReviewForm : System.Windows.Forms.Form
{
    private readonly CharacterReplacementProject _project;
    private readonly List<RomPendingPoseReviewItem> _items;

    private int _currentIndex;

    private readonly Label _progressLabel;
    private readonly Label _targetLabel;
    private readonly Label _candidateLabel;

    private readonly ListBox _candidateList;
    private readonly FrameMappingPreview _mappingPreview;

    private readonly Button _previousButton;
    private readonly Button _nextButton;
    private readonly Button _assignButton;
    private readonly Button _editAndAssignButton;
    private readonly Button _skipButton;
    private readonly Button _closeButton;

    public PendingPoseReviewForm(
        CharacterReplacementProject project,
        IReadOnlyList<RomPendingPoseReviewItem> items)
    {
        _project =
            project ??
            throw new ArgumentNullException(nameof(project));

        ArgumentNullException.ThrowIfNull(items);

        _items =
            items.ToList();

        Text =
            "Revisar poses pendientes";

        Width =
            1180;

        Height =
            760;

        MinimumSize =
            new Size(
                920,
                620);

        StartPosition =
            FormStartPosition.CenterParent;

        ShowInTaskbar =
            false;

        BackColor =
            SystemColors.Control;

        _progressLabel =
            CreateBoldLabel(
                "Sin poses pendientes");

        _targetLabel =
            new Label
            {
                AutoSize =
                    true,

                MaximumSize =
                    new Size(
                        500,
                        0),

                Margin =
                    new Padding(
                        3,
                        4,
                        3,
                        8)
            };

        _candidateLabel =
            new Label
            {
                AutoSize =
                    true,

                MaximumSize =
                    new Size(
                        500,
                        0),

                Margin =
                    new Padding(
                        3,
                        4,
                        3,
                        8)
            };

        _candidateList =
            new ListBox
            {
                Dock =
                    DockStyle.Fill,

                IntegralHeight =
                    false
            };

        _candidateList.SelectedIndexChanged +=
            CandidateList_SelectedIndexChanged;

        _candidateList.DoubleClick +=
            (_, _) =>
                EditAndAssignCurrent();

        _mappingPreview =
            new FrameMappingPreview
            {
                Dock =
                    DockStyle.Fill
            };

        _previousButton =
            CreateButton(
                "Pose anterior");

        _nextButton =
            CreateButton(
                "Pose siguiente");

        _assignButton =
            CreateButton(
                "Asignar y continuar");

        _editAndAssignButton =
            CreateButton(
                "Ajustar y asignar");

        _skipButton =
            CreateButton(
                "Saltar esta pose");

        _closeButton =
            CreateButton(
                "Cerrar revisión");

        _previousButton.Click +=
            (_, _) =>
                MoveCurrent(-1);

        _nextButton.Click +=
            (_, _) =>
                MoveCurrent(1);

        _assignButton.Click +=
            (_, _) =>
                AssignCurrentWithSafeDefaults();

        _editAndAssignButton.Click +=
            (_, _) =>
                EditAndAssignCurrent();

        _skipButton.Click +=
            (_, _) =>
                MoveCurrent(1);

        _closeButton.Click +=
            (_, _) =>
                Close();

        Controls.Add(
            CreateMainLayout());

        LoadCurrentItem();
    }

    public int AssignedCount { get; private set; }

    public int RemainingCount =>
        _items.Count;

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
                    3,

                Padding =
                    new Padding(10)
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
                    false,

                Margin =
                    new Padding(
                        0,
                        0,
                        0,
                        8)
            };

        header.Controls.Add(
            _progressLabel);

        header.Controls.Add(
            _targetLabel);

        header.Controls.Add(
            _candidateLabel);

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
                68));

        content.ColumnStyles.Add(
            new ColumnStyle(
                SizeType.Percent,
                32));

        content.RowStyles.Add(
            new RowStyle(
                SizeType.Percent,
                100));

        GroupBox previewGroup =
            new()
            {
                Text =
                    "Vista previa de la candidata seleccionada",

                Dock =
                    DockStyle.Fill,

                Padding =
                    new Padding(8)
            };

        previewGroup.Controls.Add(
            _mappingPreview);

        GroupBox candidatesGroup =
            new()
            {
                Text =
                    "Mejores candidatas",

                Dock =
                    DockStyle.Fill,

                Padding =
                    new Padding(8),

                Margin =
                    new Padding(
                        8,
                        0,
                        0,
                        0)
            };

        candidatesGroup.Controls.Add(
            _candidateList);

        content.Controls.Add(
            previewGroup,
            0,
            0);

        content.Controls.Add(
            candidatesGroup,
            1,
            0);

        root.Controls.Add(
            content,
            0,
            1);

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
                        10,
                        0,
                        0)
            };

        buttons.Controls.Add(
            _closeButton);

        buttons.Controls.Add(
            _skipButton);

        buttons.Controls.Add(
            _nextButton);

        buttons.Controls.Add(
            _previousButton);

        buttons.Controls.Add(
            _editAndAssignButton);

        buttons.Controls.Add(
            _assignButton);

        root.Controls.Add(
            buttons,
            0,
            2);

        return root;
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
                    1000,
                    0),

            Margin =
                new Padding(
                    3,
                    3,
                    3,
                    6)
        };
    }

    private RomPendingPoseReviewItem? CurrentItem
    {
        get
        {
            if (_items.Count == 0 ||
                _currentIndex < 0 ||
                _currentIndex >= _items.Count)
            {
                return null;
            }

            return _items[_currentIndex];
        }
    }

    private RomPendingPoseCandidate? SelectedCandidate =>
        _candidateList.SelectedItem
        as RomPendingPoseCandidate;

    private void LoadCurrentItem()
    {
        if (_items.Count == 0)
        {
            _currentIndex =
                0;

            _progressLabel.Text =
                "No quedan poses pendientes con candidatas disponibles.";

            _targetLabel.Text =
                $"Asignaciones realizadas en esta revisión: " +
                $"{AssignedCount}";

            _candidateLabel.Text =
                string.Empty;

            _candidateList.Items.Clear();
            _mappingPreview.Mapping =
                null;

            SetAssignmentButtonsEnabled(
                false);

            _previousButton.Enabled =
                false;

            _nextButton.Enabled =
                false;

            _skipButton.Enabled =
                false;

            return;
        }

        _currentIndex =
            Math.Clamp(
                _currentIndex,
                0,
                _items.Count - 1);

        RomPendingPoseReviewItem item =
            _items[_currentIndex];

        _progressLabel.Text =
            $"Pose pendiente {_currentIndex + 1} de " +
            $"{_items.Count} | " +
            $"Asignadas durante esta revisión: {AssignedCount}";

        _targetLabel.Text =
            $"Aladdin {item.TargetFrame.Index + 1:000} | " +
            $"X={item.TargetFrame.Bounds.X}, " +
            $"Y={item.TargetFrame.Bounds.Y} | " +
            $"{item.TargetFrame.Bounds.Width} × " +
            $"{item.TargetFrame.Bounds.Height} píxeles";

        _candidateList.BeginUpdate();

        try
        {
            _candidateList.Items.Clear();

            foreach (RomPendingPoseCandidate candidate
                     in item.Candidates)
            {
                _candidateList.Items.Add(
                    candidate);
            }
        }
        finally
        {
            _candidateList.EndUpdate();
        }

        if (_candidateList.Items.Count > 0)
        {
            _candidateList.SelectedIndex =
                0;
        }
        else
        {
            _mappingPreview.Mapping =
                null;

            _candidateLabel.Text =
                "No se encontraron candidatas utilizables.";

            SetAssignmentButtonsEnabled(
                false);
        }

        _previousButton.Enabled =
            _currentIndex > 0;

        _nextButton.Enabled =
            _currentIndex <
            _items.Count - 1;

        _skipButton.Enabled =
            true;
    }

    private void CandidateList_SelectedIndexChanged(
        object? sender,
        EventArgs e)
    {
        UpdateCandidatePreview();
    }

    private void UpdateCandidatePreview()
    {
        RomPendingPoseReviewItem? item =
            CurrentItem;

        RomPendingPoseCandidate? candidate =
            SelectedCandidate;

        if (item is null ||
            candidate is null)
        {
            _mappingPreview.Mapping =
                null;

            _candidateLabel.Text =
                "Seleccione una candidata.";

            SetAssignmentButtonsEnabled(
                false);

            return;
        }

        FrameMapping temporaryMapping =
            CreateSafeMapping(
                item,
                candidate);

        _mappingPreview.Mapping =
            temporaryMapping;

        _candidateLabel.Text =
            $"Candidata: " +
            $"{candidate.SourceDocument.FileName} / " +
            $"{candidate.SourceFrame.Index + 1:000} | " +
            $"Similitud: {candidate.Score:P1} " +
            $"({candidate.ConfidenceText}) | " +
            $"Volteo horizontal: " +
            $"{(candidate.FlipHorizontal ? "Sí" : "No")}";

        SetAssignmentButtonsEnabled(
            true);
    }

    private void SetAssignmentButtonsEnabled(
        bool enabled)
    {
        _assignButton.Enabled =
            enabled;

        _editAndAssignButton.Enabled =
            enabled;
    }

    private void MoveCurrent(
        int amount)
    {
        if (_items.Count == 0)
        {
            return;
        }

        _currentIndex =
            Math.Clamp(
                _currentIndex + amount,
                0,
                _items.Count - 1);

        LoadCurrentItem();
    }

    private void AssignCurrentWithSafeDefaults()
    {
        RomPendingPoseReviewItem? item =
            CurrentItem;

        RomPendingPoseCandidate? candidate =
            SelectedCandidate;

        if (item is null ||
            candidate is null)
        {
            return;
        }

        FrameMapping mapping =
            _project.AssignFrame(
                item.TargetFrame,
                candidate.SourceDocument,
                candidate.SourceFrame);

        ApplySafeDefaults(
            mapping,
            candidate.FlipHorizontal);

        AssignedCount++;

        RemoveCurrentItem();
    }

    private void EditAndAssignCurrent()
    {
        RomPendingPoseReviewItem? item =
            CurrentItem;

        RomPendingPoseCandidate? candidate =
            SelectedCandidate;

        if (item is null ||
            candidate is null)
        {
            return;
        }

        FrameMapping temporaryMapping =
            CreateSafeMapping(
                item,
                candidate);

        using FrameMappingForm editor =
            new(
                item.TargetDocument,
                item.TargetFrame,
                candidate.SourceDocument,
                candidate.SourceFrame,
                temporaryMapping);

        DialogResult result =
            editor.ShowDialog(
                this);

        if (result != DialogResult.OK ||
            editor.ResultMapping is null)
        {
            return;
        }

        FrameMapping savedMapping =
            _project.AssignFrame(
                item.TargetFrame,
                editor.ResultMapping.SourceDocument,
                editor.ResultMapping.SourceFrame);

        CopySettings(
            editor.ResultMapping,
            savedMapping);

        AssignedCount++;

        RemoveCurrentItem();
    }

    private FrameMapping CreateSafeMapping(
        RomPendingPoseReviewItem item,
        RomPendingPoseCandidate candidate)
    {
        FrameMapping mapping =
            new(
                item.TargetDocument,
                item.TargetFrame,
                candidate.SourceDocument,
                candidate.SourceFrame);

        ApplySafeDefaults(
            mapping,
            candidate.FlipHorizontal);

        return mapping;
    }

    private static void ApplySafeDefaults(
        FrameMapping mapping,
        bool flipHorizontal)
    {
        mapping.AutoFit =
            true;

        mapping.Scale =
            1.0f;

        mapping.OffsetX =
            0;

        mapping.OffsetY =
            0;

        mapping.FlipHorizontal =
            flipHorizontal;

        mapping.FlipVertical =
            false;
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

    private void RemoveCurrentItem()
    {
        if (_items.Count == 0)
        {
            return;
        }

        _items.RemoveAt(
            _currentIndex);

        if (_currentIndex >=
            _items.Count)
        {
            _currentIndex =
                Math.Max(
                    0,
                    _items.Count - 1);
        }

        LoadCurrentItem();
    }

    protected override void OnFormClosed(
        FormClosedEventArgs e)
    {
        _mappingPreview.Mapping =
            null;

        base.OnFormClosed(
            e);
    }
}
