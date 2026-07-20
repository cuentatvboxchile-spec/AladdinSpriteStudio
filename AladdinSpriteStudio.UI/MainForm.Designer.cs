#nullable enable

namespace AladdinSpriteStudio.UI;

partial class MainForm
{
    private System.ComponentModel.IContainer? components = null;

    protected override void Dispose(bool disposing)
    {
        if (disposing && components != null)
            components.Dispose();

        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        statusStripMain = new StatusStrip();
        statusLabelMain = new ToolStripStatusLabel();
        mainSplitContainer = new SplitContainer();
        statusStripMain.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)mainSplitContainer).BeginInit();
        mainSplitContainer.SuspendLayout();
        SuspendLayout();
        // 
        // statusStripMain
        // 
        statusStripMain.ImageScalingSize = new Size(20, 20);
        statusStripMain.Items.AddRange(new ToolStripItem[] { statusLabelMain });
        statusStripMain.Location = new Point(0, 874);
        statusStripMain.Name = "statusStripMain";
        statusStripMain.Size = new Size(1400, 26);
        statusStripMain.TabIndex = 0;
        statusStripMain.Text = "statusStrip1";
        // 
        // statusLabelMain
        // 
        statusLabelMain.Name = "statusLabelMain";
        statusLabelMain.Size = new Size(1346, 20);
        statusLabelMain.Spring = true;
        statusLabelMain.Text = "Sin ROM cargada";
        statusLabelMain.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // mainSplitContainer
        // 
        mainSplitContainer.Cursor = Cursors.IBeam;
        mainSplitContainer.Dock = DockStyle.Fill;
        mainSplitContainer.Location = new Point(0, 0);
        mainSplitContainer.Name = "mainSplitContainer";
        mainSplitContainer.Size = new Size(1400, 874);
        mainSplitContainer.SplitterDistance = 280;
        mainSplitContainer.TabIndex = 1;
        // 
        // MainForm
        // 
        ClientSize = new Size(1400, 900);
        Controls.Add(mainSplitContainer);
        Controls.Add(statusStripMain);
        Name = "MainForm";
        Text = "Aladdin Sprite Studio";
        statusStripMain.ResumeLayout(false);
        statusStripMain.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)mainSplitContainer).EndInit();
        mainSplitContainer.ResumeLayout(false);
        ResumeLayout(false);
        PerformLayout();
    }

    private StatusStrip statusStripMain = null!;
    private ToolStripStatusLabel statusLabelMain = null!;
    private SplitContainer mainSplitContainer = null!;
}