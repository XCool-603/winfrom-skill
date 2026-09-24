// ============================================================================
// 示例：UserControl 的 Designer.cs
// 与 Form 的差异：① region 名是 "Component Designer" ② 用 Size 而非 ClientSize
// 本文件只放界面。逻辑在 CustomerListView.cs。
// ============================================================================

namespace WinFormsSkillDemo.Views;

partial class CustomerListView
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

    #region Component Designer generated code

    /// <summary>
    ///  Required method for Designer support - do not modify
    ///  the contents of this method with the code editor.
    /// </summary>
    private void InitializeComponent()
    {
        this.rootLayout = new System.Windows.Forms.TableLayoutPanel();
        this.flowSearch = new System.Windows.Forms.FlowLayoutPanel();
        this.lblSearch = new System.Windows.Forms.Label();
        this.txtSearch = new System.Windows.Forms.TextBox();
        this.dgvCustomers = new System.Windows.Forms.DataGridView();
        this.colName = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this.colEmail = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this.rootLayout.SuspendLayout();
        this.flowSearch.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)(this.dgvCustomers)).BeginInit();
        this.SuspendLayout();
        //
        // rootLayout
        //
        this.rootLayout.ColumnCount = 1;
        this.rootLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
        this.rootLayout.Dock = System.Windows.Forms.DockStyle.Fill;
        this.rootLayout.Location = new System.Drawing.Point(0, 0);
        this.rootLayout.Name = "rootLayout";
        this.rootLayout.Padding = new System.Windows.Forms.Padding(8);
        this.rootLayout.RowCount = 2;
        this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
        this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
        this.rootLayout.Size = new System.Drawing.Size(720, 420);
        this.rootLayout.TabIndex = 0;
        //
        // flowSearch
        //
        this.flowSearch.AutoSize = true;
        this.flowSearch.Controls.Add(this.lblSearch);
        this.flowSearch.Controls.Add(this.txtSearch);
        this.flowSearch.Dock = System.Windows.Forms.DockStyle.Fill;
        this.flowSearch.Location = new System.Drawing.Point(11, 11);
        this.flowSearch.Name = "flowSearch";
        this.flowSearch.Size = new System.Drawing.Size(698, 29);
        this.flowSearch.TabIndex = 0;
        this.flowSearch.WrapContents = false;
        //
        // lblSearch —— 在 FlowLayoutPanel 里 Anchor 不生效，靠 Margin 做垂直居中
        //
        this.lblSearch.AutoSize = true;
        this.lblSearch.Location = new System.Drawing.Point(3, 6);
        this.lblSearch.Margin = new System.Windows.Forms.Padding(3, 6, 3, 0);
        this.lblSearch.Name = "lblSearch";
        this.lblSearch.Size = new System.Drawing.Size(43, 17);
        this.lblSearch.TabIndex = 0;
        this.lblSearch.Text = "搜索";
        //
        // txtSearch
        //
        this.txtSearch.Location = new System.Drawing.Point(52, 3);
        this.txtSearch.Name = "txtSearch";
        this.txtSearch.Size = new System.Drawing.Size(240, 23);
        this.txtSearch.TabIndex = 1;
        this.txtSearch.TextChanged += new System.EventHandler(this.txtSearch_TextChanged);
        //
        // dgvCustomers
        //
        this.dgvCustomers.AllowUserToAddRows = false;
        this.dgvCustomers.AllowUserToDeleteRows = false;
        this.dgvCustomers.AllowUserToResizeRows = false;
        this.dgvCustomers.AutoGenerateColumns = false;
        this.dgvCustomers.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
        this.dgvCustomers.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
        this.dgvCustomers.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
        this.colName,
        this.colEmail});
        this.dgvCustomers.Dock = System.Windows.Forms.DockStyle.Fill;
        this.dgvCustomers.Location = new System.Drawing.Point(11, 46);
        this.dgvCustomers.MultiSelect = false;
        this.dgvCustomers.Name = "dgvCustomers";
        this.dgvCustomers.ReadOnly = true;
        this.dgvCustomers.RowHeadersVisible = false;
        this.dgvCustomers.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
        this.dgvCustomers.Size = new System.Drawing.Size(698, 363);
        this.dgvCustomers.TabIndex = 1;
        this.dgvCustomers.CellDoubleClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.dgvCustomers_CellDoubleClick);
        //
        // colName
        //
        this.colName.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
        this.colName.DataPropertyName = "Name";
        this.colName.FillWeight = 40F;
        this.colName.HeaderText = "姓名";
        this.colName.MinimumWidth = 6;
        this.colName.Name = "colName";
        this.colName.ReadOnly = true;
        //
        // colEmail
        //
        this.colEmail.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
        this.colEmail.DataPropertyName = "Email";
        this.colEmail.FillWeight = 60F;
        this.colEmail.HeaderText = "邮箱";
        this.colEmail.MinimumWidth = 6;
        this.colEmail.Name = "colEmail";
        this.colEmail.ReadOnly = true;
        //
        // rootLayout 收子控件
        //
        this.rootLayout.Controls.Add(this.flowSearch, 0, 0);
        this.rootLayout.Controls.Add(this.dgvCustomers, 0, 1);
        //
        // CustomerListView
        //
        this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
        this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        this.Controls.Add(this.rootLayout);
        this.Name = "CustomerListView";
        this.Size = new System.Drawing.Size(720, 420);
        ((System.ComponentModel.ISupportInitialize)(this.dgvCustomers)).EndInit();
        this.rootLayout.ResumeLayout(false);
        this.rootLayout.PerformLayout();
        this.flowSearch.ResumeLayout(false);
        this.flowSearch.PerformLayout();
        this.ResumeLayout(false);
        this.PerformLayout();
    }

    #endregion

    private System.Windows.Forms.TableLayoutPanel rootLayout;
    private System.Windows.Forms.FlowLayoutPanel flowSearch;
    private System.Windows.Forms.Label lblSearch;
    private System.Windows.Forms.TextBox txtSearch;
    private System.Windows.Forms.DataGridView dgvCustomers;
    private System.Windows.Forms.DataGridViewTextBoxColumn colName;
    private System.Windows.Forms.DataGridViewTextBoxColumn colEmail;
}
