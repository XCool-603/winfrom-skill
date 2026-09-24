// ============================================================================
// 示例：Form 的 Designer.cs
// 主窗体退化成「布局壳」：工具栏 + 内容宿主 + 状态栏。
// 客户列表区块已拆到 Views/CustomerListView（R5）。
// 本文件只放界面。逻辑在 MainForm.cs。
// ============================================================================

namespace WinFormsSkillDemo.Forms;

partial class MainForm
{
    /// <summary>
    ///  Required designer variable.
    /// </summary>
    private System.ComponentModel.IContainer components = null;

    /// <summary>
    ///  Clean up any resources being used.
    /// </summary>
    /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
        {
            components.Dispose();
        }
        base.Dispose(disposing);
    }

    #region Windows Form Designer generated code

    /// <summary>
    ///  Required method for Designer support - do not modify
    ///  the contents of this method with the code editor.
    /// </summary>
    private void InitializeComponent()
    {
        this.rootLayout = new System.Windows.Forms.TableLayoutPanel();
        this.flowToolbar = new System.Windows.Forms.FlowLayoutPanel();
        this.btnRefresh = new System.Windows.Forms.Button();
        this.btnAdd = new System.Windows.Forms.Button();
        this.customerListView = new WinFormsSkillDemo.Views.CustomerListView();
        this.lblStatus = new System.Windows.Forms.Label();
        this.rootLayout.SuspendLayout();
        this.flowToolbar.SuspendLayout();
        this.SuspendLayout();
        //
        // rootLayout —— 三区：工具栏 / 内容 / 状态栏
        // 位置完全由行列决定，不依赖 Dock 顺序（见 references/layout-and-decomposition.md §3.2）
        //
        this.rootLayout.ColumnCount = 1;
        this.rootLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
        this.rootLayout.Dock = System.Windows.Forms.DockStyle.Fill;
        this.rootLayout.Location = new System.Drawing.Point(0, 0);
        this.rootLayout.Name = "rootLayout";
        this.rootLayout.RowCount = 3;
        this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
        this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
        this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
        this.rootLayout.Size = new System.Drawing.Size(900, 560);
        this.rootLayout.TabIndex = 0;
        //
        // flowToolbar —— 第 0 行
        //
        this.flowToolbar.AutoSize = true;
        this.flowToolbar.Controls.Add(this.btnRefresh);
        this.flowToolbar.Controls.Add(this.btnAdd);
        this.flowToolbar.Dock = System.Windows.Forms.DockStyle.Fill;
        this.flowToolbar.Location = new System.Drawing.Point(0, 0);
        this.flowToolbar.Margin = new System.Windows.Forms.Padding(0);
        this.flowToolbar.Name = "flowToolbar";
        this.flowToolbar.Padding = new System.Windows.Forms.Padding(8, 8, 8, 4);
        this.flowToolbar.Size = new System.Drawing.Size(900, 43);
        this.flowToolbar.TabIndex = 0;
        this.flowToolbar.WrapContents = false;
        //
        // btnRefresh
        //
        this.btnRefresh.AutoSize = true;
        this.btnRefresh.Location = new System.Drawing.Point(11, 11);
        this.btnRefresh.Name = "btnRefresh";
        this.btnRefresh.Size = new System.Drawing.Size(75, 25);
        this.btnRefresh.TabIndex = 0;
        this.btnRefresh.Text = "刷新";
        this.btnRefresh.UseVisualStyleBackColor = true;
        this.btnRefresh.Click += new System.EventHandler(this.btnRefresh_Click);
        //
        // btnAdd
        //
        this.btnAdd.AutoSize = true;
        this.btnAdd.Location = new System.Drawing.Point(92, 11);
        this.btnAdd.Name = "btnAdd";
        this.btnAdd.Size = new System.Drawing.Size(75, 25);
        this.btnAdd.TabIndex = 1;
        this.btnAdd.Text = "新增";
        this.btnAdd.UseVisualStyleBackColor = true;
        this.btnAdd.Click += new System.EventHandler(this.btnAdd_Click);
        //
        // customerListView —— 第 1 行。子 UserControl 用全限定类型名
        //
        this.customerListView.Dock = System.Windows.Forms.DockStyle.Fill;
        this.customerListView.Location = new System.Drawing.Point(0, 43);
        this.customerListView.Margin = new System.Windows.Forms.Padding(0);
        this.customerListView.Name = "customerListView";
        this.customerListView.Size = new System.Drawing.Size(900, 494);
        this.customerListView.TabIndex = 1;
        //
        // lblStatus —— 第 2 行。AutoSize + Anchor=Left：在单元格里垂直居中，不拉伸
        //
        this.lblStatus.Anchor = System.Windows.Forms.AnchorStyles.Left;
        this.lblStatus.AutoSize = true;
        this.lblStatus.Location = new System.Drawing.Point(8, 540);
        this.lblStatus.Margin = new System.Windows.Forms.Padding(8, 3, 3, 3);
        this.lblStatus.Name = "lblStatus";
        this.lblStatus.Size = new System.Drawing.Size(80, 17);
        this.lblStatus.TabIndex = 2;
        this.lblStatus.Text = "就绪";
        //
        // rootLayout 收子控件
        //
        this.rootLayout.Controls.Add(this.flowToolbar, 0, 0);
        this.rootLayout.Controls.Add(this.customerListView, 0, 1);
        this.rootLayout.Controls.Add(this.lblStatus, 0, 2);
        //
        // MainForm
        //
        this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
        this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        this.ClientSize = new System.Drawing.Size(900, 560);
        this.Controls.Add(this.rootLayout);
        this.MinimumSize = new System.Drawing.Size(640, 400);
        this.Name = "MainForm";
        this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
        this.Text = "客户管理 —— WinForms Skill 示例";
        this.flowToolbar.ResumeLayout(false);
        this.flowToolbar.PerformLayout();
        this.rootLayout.ResumeLayout(false);
        this.rootLayout.PerformLayout();
        this.ResumeLayout(false);
        this.PerformLayout();
    }

    #endregion

    // 控件字段声明放在 #endregion 之后，全部 private，类型全限定
    private System.Windows.Forms.TableLayoutPanel rootLayout;
    private System.Windows.Forms.FlowLayoutPanel flowToolbar;
    private System.Windows.Forms.Button btnRefresh;
    private System.Windows.Forms.Button btnAdd;
    private WinFormsSkillDemo.Views.CustomerListView customerListView;
    private System.Windows.Forms.Label lblStatus;
}
