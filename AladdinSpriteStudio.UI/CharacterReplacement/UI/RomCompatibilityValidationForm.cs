using AladdinSpriteStudio.UI.CharacterReplacement.Validation;

namespace AladdinSpriteStudio.UI.CharacterReplacement.UI;

/// <summary>
/// Muestra el resultado del validador gráfico para ROM y permite
/// guardar un informe de texto.
/// </summary>
public sealed class RomCompatibilityValidationForm
    : System.Windows.Forms.Form
{
    private readonly RomCompatibilityValidationResult _result;

    private readonly Label _statusLabel;
    private readonly Label _summaryLabel;
    private readonly ListView _issuesList;

    private readonly Button _saveReportButton;
    private readonly Button _closeButton;

    public RomCompatibilityValidationForm(
        RomCompatibilityValidationResult result)
    {
        _result =
            result ??
            throw new ArgumentNullException(
                nameof(result));

        Text =
            "Validación de compatibilidad gráfica para ROM";

        Width =
            1100;

        Height =
            720;

        MinimumSize =
            new Size(
                850,
                560);

        StartPosition =
            FormStartPosition.CenterParent;

        ShowInTaskbar =
            false;

        BackColor =
            SystemColors.Control;

        _statusLabel =
            new Label
            {
                Text =
                    _result.StatusText,

                AutoSize =
                    true,

                Font =
                    new Font(
                        (SystemFonts.MessageBoxFont
                         ?? SystemFonts.DefaultFont).FontFamily,
                        16f,
                        FontStyle.Bold),

                Margin =
                    new Padding(
                        3,
                        3,
                        3,
                        8)
            };

        ApplyStatusAppearance();

        _summaryLabel =
            new Label
            {
                Text =
                    CreateSummaryText(),

                AutoSize =
                    true,

                MaximumSize =
                    new Size(
                        1000,
                        0),

                Margin =
                    new Padding(
                        3,
                        3,
                        3,
                        10)
            };

        _issuesList =
            new ListView
            {
                Dock =
                    DockStyle.Fill,

                View =
                    View.Details,

                FullRowSelect =
                    true,

                GridLines =
                    true,

                HideSelection =
                    false,

                MultiSelect =
                    false
            };

        _issuesList.Columns.Add(
            "Nivel",
            110);

        _issuesList.Columns.Add(
            "Código",
            190);

        _issuesList.Columns.Add(
            "Pose",
            100);

        _issuesList.Columns.Add(
            "Observación",
            280);

        _issuesList.Columns.Add(
            "Descripción",
            560);

        _saveReportButton =
            new Button
            {
                Text =
                    "Guardar informe",

                AutoSize =
                    true
            };

        _closeButton =
            new Button
            {
                Text =
                    "Cerrar",

                AutoSize =
                    true,

                DialogResult =
                    DialogResult.OK
            };

        _saveReportButton.Click +=
            SaveReportButton_Click;

        AcceptButton =
            _closeButton;

        CancelButton =
            _closeButton;

        Controls.Add(
            CreateMainLayout());

        LoadIssues();
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
                    3,

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
            _statusLabel);

        header.Controls.Add(
            _summaryLabel);

        root.Controls.Add(
            header,
            0,
            0);

        GroupBox issuesGroup =
            new()
            {
                Text =
                    "Resultados de la validación",

                Dock =
                    DockStyle.Fill,

                Padding =
                    new Padding(8)
            };

        issuesGroup.Controls.Add(
            _issuesList);

        root.Controls.Add(
            issuesGroup,
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
            _saveReportButton);

        root.Controls.Add(
            buttons,
            0,
            2);

        return root;
    }

    private string CreateSummaryText()
    {
        string nextStep =
            _result.CanProceedToTilePreparation
                ? "La hoja puede pasar a la preparación técnica " +
                  "de paleta y tiles. Las advertencias deben " +
                  "revisarse antes de generar 4BPP."
                : "Corrija los errores antes de preparar paleta, " +
                  "tiles o una copia modificada de la ROM.";

        return
            $"Poses objetivo: {_result.TargetFrameCount} | " +
            $"Asignadas: {_result.MappedFrameCount} | " +
            $"Pendientes: {_result.PendingFrameCount}\n" +
            $"Errores: {_result.ErrorCount} | " +
            $"Advertencias: {_result.WarningCount} | " +
            $"Información: {_result.InformationCount}\n\n" +
            nextStep;
    }

    private void ApplyStatusAppearance()
    {
        if (!_result.CanProceedToTilePreparation)
        {
            _statusLabel.ForeColor =
                Color.Firebrick;

            return;
        }

        if (_result.WarningCount > 0)
        {
            _statusLabel.ForeColor =
                Color.DarkGoldenrod;

            return;
        }

        _statusLabel.ForeColor =
            Color.DarkGreen;
    }

    private void LoadIssues()
    {
        _issuesList.BeginUpdate();

        try
        {
            _issuesList.Items.Clear();

            foreach (RomValidationIssue issue
                     in _result.Issues
                         .OrderByDescending(
                             item => item.Severity)
                         .ThenBy(
                             item =>
                                 item.TargetFrameIndex
                                 ?? int.MaxValue)
                         .ThenBy(
                             item => item.Code))
            {
                string poseText =
                    issue.TargetFrameIndex
                    is int frameIndex
                        ? $"{frameIndex + 1:000}"
                        : "—";

                ListViewItem item =
                    new(
                        issue.SeverityText);

                item.SubItems.Add(
                    issue.Code);

                item.SubItems.Add(
                    poseText);

                item.SubItems.Add(
                    issue.Title);

                item.SubItems.Add(
                    issue.Description);

                item.Tag =
                    issue;

                ApplyIssueAppearance(
                    item,
                    issue.Severity);

                _issuesList.Items.Add(
                    item);
            }

            foreach (ColumnHeader column
                     in _issuesList.Columns)
            {
                if (column.Index == 4)
                {
                    continue;
                }

                column.Width =
                    -2;
            }
        }
        finally
        {
            _issuesList.EndUpdate();
        }
    }

    private static void ApplyIssueAppearance(
        ListViewItem item,
        RomValidationSeverity severity)
    {
        item.UseItemStyleForSubItems =
            true;

        item.ForeColor =
            severity switch
            {
                RomValidationSeverity.Error =>
                    Color.Firebrick,

                RomValidationSeverity.Warning =>
                    Color.DarkGoldenrod,

                _ =>
                    SystemColors.WindowText
            };
    }

    private void SaveReportButton_Click(
        object? sender,
        EventArgs e)
    {
        using SaveFileDialog dialog =
            new()
            {
                Title =
                    "Guardar informe de validación",

                Filter =
                    "Documento de texto (*.txt)|*.txt",

                DefaultExt =
                    "txt",

                AddExtension =
                    true,

                FileName =
                    "Informe_Validacion_ROM.txt",

                OverwritePrompt =
                    true
            };

        if (dialog.ShowDialog(this)
            != DialogResult.OK)
        {
            return;
        }

        try
        {
            File.WriteAllText(
                dialog.FileName,
                _result.CreateTextReport());

            MessageBox.Show(
                this,
                "El informe fue guardado correctamente.",
                "Informe guardado",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                this,
                ex.Message,
                "No se pudo guardar el informe",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }
}
