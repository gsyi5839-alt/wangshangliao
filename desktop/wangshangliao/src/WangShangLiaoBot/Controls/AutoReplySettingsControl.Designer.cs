namespace WangShangLiaoBot.Controls
{
    partial class AutoReplySettingsControl
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Component Designer generated code

        private void InitializeComponent()
        {
            // NOTE:
            // This UI is intentionally built with layout containers (SplitContainer/TableLayoutPanel)
            // to keep alignment consistent and make the control resize-friendly.

            // Root containers
            this.splitMain = new System.Windows.Forms.SplitContainer();
            this.panelLeft = new System.Windows.Forms.Panel();
            this.panelRight = new System.Windows.Forms.Panel();
            
            // Left side groups (templates)
            this.tblLeft = new System.Windows.Forms.TableLayoutPanel();
            this.grpCft = new System.Windows.Forms.GroupBox();
            this.grpZfb = new System.Windows.Forms.GroupBox();
            this.grpWx = new System.Windows.Forms.GroupBox();

            this.tblCft = new System.Windows.Forms.TableLayoutPanel();
            this.tblZfb = new System.Windows.Forms.TableLayoutPanel();
            this.tblWx = new System.Windows.Forms.TableLayoutPanel();

            // CaiFuTong controls
            this.lblCftSend = new System.Windows.Forms.Label();
            this.txtCftSend = new System.Windows.Forms.TextBox();
            this.lblCftText = new System.Windows.Forms.Label();
            this.txtCftText = new System.Windows.Forms.TextBox();
            this.btnCftQrCode = new System.Windows.Forms.Button();
            this.lblCftReply = new System.Windows.Forms.Label();
            this.txtCftReply = new System.Windows.Forms.TextBox();
            
            // ZhiFuBao controls
            this.lblZfbSend = new System.Windows.Forms.Label();
            this.txtZfbSend = new System.Windows.Forms.TextBox();
            this.lblZfbText = new System.Windows.Forms.Label();
            this.txtZfbText = new System.Windows.Forms.TextBox();
            this.btnZfbQrCode = new System.Windows.Forms.Button();
            this.lblZfbReply = new System.Windows.Forms.Label();
            this.txtZfbReply = new System.Windows.Forms.TextBox();
            
            // WeiXin controls
            this.lblWxSend = new System.Windows.Forms.Label();
            this.txtWxSend = new System.Windows.Forms.TextBox();
            this.lblWxText = new System.Windows.Forms.Label();
            this.txtWxText = new System.Windows.Forms.TextBox();
            this.btnWxQrCode = new System.Windows.Forms.Button();
            this.lblWxReply = new System.Windows.Forms.Label();
            this.txtWxReply = new System.Windows.Forms.TextBox();
            
            // Right side (custom keyword library)
            this.tblRight = new System.Windows.Forms.TableLayoutPanel();
            this.grpCustomKeyword = new System.Windows.Forms.GroupBox();
            this.dgvKeywords = new System.Windows.Forms.DataGridView();
            this.colKeyword = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colReply = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colBlank = new System.Windows.Forms.DataGridViewTextBoxColumn();

            this.grpEditor = new System.Windows.Forms.GroupBox();
            this.tblEditor = new System.Windows.Forms.TableLayoutPanel();
            this.lblKeyword = new System.Windows.Forms.Label();
            this.txtKeyword = new System.Windows.Forms.TextBox();
            this.lblReply = new System.Windows.Forms.Label();
            this.txtReply = new System.Windows.Forms.TextBox();

            this.pnlButtons = new System.Windows.Forms.FlowLayoutPanel();
            this.btnAddModify = new System.Windows.Forms.Button();
            this.btnDelete = new System.Windows.Forms.Button();
            this.btnSave = new System.Windows.Forms.Button();
            
            ((System.ComponentModel.ISupportInitialize)(this.splitMain)).BeginInit();
            this.splitMain.Panel1.SuspendLayout();
            this.splitMain.Panel2.SuspendLayout();
            this.splitMain.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvKeywords)).BeginInit();
            this.SuspendLayout();
            
            // =========================
            // Root: SplitContainer
            // =========================
            this.splitMain.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitMain.Orientation = System.Windows.Forms.Orientation.Vertical;
            this.splitMain.SplitterWidth = 6;
            this.splitMain.Panel1.Controls.Add(this.panelLeft);
            this.splitMain.Panel2.Controls.Add(this.panelRight);
            this.splitMain.FixedPanel = System.Windows.Forms.FixedPanel.None;
            this.splitMain.BackColor = System.Drawing.SystemColors.Control;
            this.splitMain.TabStop = false;
            // IMPORTANT:
            // Do NOT set SplitterDistance/Panel2MinSize here. During InitializeComponent the control width
            // may not be finalized, and setting min sizes can indirectly cause SplitterDistance validation
            // to throw. We apply these constraints safely at runtime from code-behind.

            // =========================
            // Left container
            // =========================
            this.panelLeft.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panelLeft.Padding = new System.Windows.Forms.Padding(6);
            this.panelLeft.Controls.Add(this.tblLeft);

            this.tblLeft.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tblLeft.ColumnCount = 1;
            this.tblLeft.RowCount = 3;
            this.tblLeft.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tblLeft.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 33.3333F));
            this.tblLeft.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 33.3333F));
            this.tblLeft.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 33.3333F));
            this.tblLeft.Controls.Add(this.grpCft, 0, 0);
            this.tblLeft.Controls.Add(this.grpZfb, 0, 1);
            this.tblLeft.Controls.Add(this.grpWx, 0, 2);

            // =========================
            // Left: CaiFuTong group
            // =========================
            this.grpCft.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpCft.Text = "财付通";
            this.grpCft.Padding = new System.Windows.Forms.Padding(8, 10, 8, 8);
            this.grpCft.Controls.Add(this.tblCft);

            BuildTemplateLayout(
                this.tblCft,
                this.lblCftSend, this.txtCftSend,
                this.lblCftText, this.txtCftText,
                this.btnCftQrCode,
                this.lblCftReply, this.txtCftReply);
            this.txtCftSend.Text = "私聊前排接单";
            this.txtCftReply.Text = "财富|发财富|财付通|发财付通|财富多少|财付通给|cft|接下财富|接下财付通|财量多少|财富哪个|财富几个|财富帐户";

            // =========================
            // Left: ZhiFuBao group
            // =========================
            this.grpZfb.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpZfb.Text = "支付宝";
            this.grpZfb.Padding = new System.Windows.Forms.Padding(8, 10, 8, 8);
            this.grpZfb.Controls.Add(this.tblZfb);

            BuildTemplateLayout(
                this.tblZfb,
                this.lblZfbSend, this.txtZfbSend,
                this.lblZfbText, this.txtZfbText,
                this.btnZfbQrCode,
                this.lblZfbReply, this.txtZfbReply);
            this.txtZfbSend.Text = "私聊前排客服";
            this.txtZfbReply.Text = "支付|支付宝|发支付宝|发支付|支付多少|支付宝一下|接下支付|接支付宝多少|zfb|支付宝发下|发下支付宝|支付发来|支付宝?";

            // =========================
            // Left: WeiXin group
            // =========================
            this.grpWx.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpWx.Text = "微信";
            this.grpWx.Padding = new System.Windows.Forms.Padding(8, 10, 8, 8);
            this.grpWx.Controls.Add(this.tblWx);

            BuildTemplateLayout(
                this.tblWx,
                this.lblWxSend, this.txtWxSend,
                this.lblWxText, this.txtWxText,
                this.btnWxQrCode,
                this.lblWxReply, this.txtWxReply);
            this.txtWxSend.Text = "私聊前排客服";
            this.txtWxReply.Text = "微信|发微信|微信多少|微信号";

            // Configure common QR button text (same as original design)
            this.btnCftQrCode.Text = "导入\r\n二维码";
            this.btnZfbQrCode.Text = "导入\r\n二维码";
            this.btnWxQrCode.Text = "导入\r\n二维码";
            
            // =========================
            // Right container
            // =========================
            this.panelRight.Dock = System.Windows.Forms.DockStyle.Fill;
            // Remove outer padding so the "自定义词库" container can match the exact design size (305x179).
            this.panelRight.Padding = new System.Windows.Forms.Padding(0);
            this.panelRight.Controls.Add(this.tblRight);

            this.tblRight.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tblRight.Margin = new System.Windows.Forms.Padding(0);
            this.tblRight.ColumnCount = 1;
            this.tblRight.RowCount = 3;
            this.tblRight.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            // Match the original design: custom keyword container size is 305x179 (height fixed).
            // NOTE: The 305x179 is the DataGridView visible area; GroupBox includes a title bar and padding,
            // so the row height must be larger than 179 to preserve the inner grid size.
            this.tblRight.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 210F));
            this.tblRight.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tblRight.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 44F));

            this.tblRight.Controls.Add(this.grpCustomKeyword, 0, 0);
            this.tblRight.Controls.Add(this.grpEditor, 0, 1);
            this.tblRight.Controls.Add(this.pnlButtons, 0, 2);

            // =========================
            // Right: Custom keyword grid
            // =========================
            this.grpCustomKeyword.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpCustomKeyword.Text = "自定义词库";
            // Use predictable padding to make the inner grid match design pixels.
            this.grpCustomKeyword.Padding = new System.Windows.Forms.Padding(10, 18, 10, 10);
            this.grpCustomKeyword.Controls.Add(this.dgvKeywords);

            // Grid: keep colors consistent with system theme, but improve readability
            // IMPORTANT: Fixed grid viewport size (matches the annotated screenshot).
            // 305x179 = whole grid area including headers.
            this.dgvKeywords.Dock = System.Windows.Forms.DockStyle.None;
            this.dgvKeywords.Location = new System.Drawing.Point(10, 20);
            this.dgvKeywords.Size = new System.Drawing.Size(305, 179);
            this.dgvKeywords.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.dgvKeywords.AllowUserToAddRows = false;
            this.dgvKeywords.AllowUserToDeleteRows = false;
            this.dgvKeywords.ReadOnly = false;
            this.dgvKeywords.EditMode = System.Windows.Forms.DataGridViewEditMode.EditOnEnter;
            this.dgvKeywords.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.CellSelect;
            this.dgvKeywords.MultiSelect = false;
            this.dgvKeywords.RowHeadersVisible = false;
            this.dgvKeywords.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.dgvKeywords.BackgroundColor = System.Drawing.SystemColors.Window;
            this.dgvKeywords.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.dgvKeywords.GridColor = System.Drawing.SystemColors.ControlLight;
            this.dgvKeywords.CellBorderStyle = System.Windows.Forms.DataGridViewCellBorderStyle.Single;
            this.dgvKeywords.ColumnHeadersBorderStyle = System.Windows.Forms.DataGridViewHeaderBorderStyle.Single;
            this.dgvKeywords.EnableHeadersVisualStyles = false;
            this.dgvKeywords.ColumnHeadersDefaultCellStyle.BackColor = System.Drawing.SystemColors.Window;
            this.dgvKeywords.ColumnHeadersDefaultCellStyle.ForeColor = System.Drawing.SystemColors.ControlText;
            this.dgvKeywords.ColumnHeadersDefaultCellStyle.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
            this.dgvKeywords.ColumnHeadersDefaultCellStyle.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F, System.Drawing.FontStyle.Bold);
            this.dgvKeywords.DefaultCellStyle.BackColor = System.Drawing.SystemColors.Window;
            this.dgvKeywords.DefaultCellStyle.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F);
            this.dgvKeywords.DefaultCellStyle.SelectionBackColor = System.Drawing.Color.FromArgb(225, 235, 245);
            this.dgvKeywords.DefaultCellStyle.SelectionForeColor = System.Drawing.SystemColors.ControlText;
            this.dgvKeywords.DefaultCellStyle.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.dgvKeywords.AutoSizeRowsMode = System.Windows.Forms.DataGridViewAutoSizeRowsMode.AllCells;
            this.dgvKeywords.RowTemplate.Height = 18;
            this.dgvKeywords.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.dgvKeywords.DefaultCellStyle.Padding = new System.Windows.Forms.Padding(3, 2, 3, 2);
            
            this.colKeyword.HeaderText = "关键词";
            this.colKeyword.Name = "colKeyword";
            this.colKeyword.FillWeight = 40F;
            this.colKeyword.ReadOnly = false;
            this.colKeyword.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            this.colKeyword.DefaultCellStyle.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            
            this.colReply.HeaderText = "回复信息";
            this.colReply.Name = "colReply";
            this.colReply.FillWeight = 50F;
            this.colReply.ReadOnly = false;
            this.colReply.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            this.colReply.DefaultCellStyle.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;

            // Blank filler column to keep the worksheet look
            this.colBlank.HeaderText = "";
            this.colBlank.Name = "colBlank";
            this.colBlank.FillWeight = 10F;
            this.colBlank.ReadOnly = true;
            this.colBlank.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            
            this.dgvKeywords.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[]
            {
                this.colKeyword,
                this.colReply,
                this.colBlank
            });
            
            // =========================
            // Right: Editor area
            // =========================
            this.grpEditor.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpEditor.Text = "编辑";
            this.grpEditor.Padding = new System.Windows.Forms.Padding(8, 10, 8, 8);
            this.grpEditor.Controls.Add(this.tblEditor);

            this.tblEditor.Dock = System.Windows.Forms.DockStyle.Fill;
            // 关键词/回复输入区固定宽度（与截图一致）
            this.tblEditor.ColumnCount = 3;
            this.tblEditor.RowCount = 2;
            this.tblEditor.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 55F));
            this.tblEditor.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 240F));
            this.tblEditor.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            // Fixed heights: keyword box 55px, reply box 127px (matches annotated screenshot)
            this.tblEditor.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 55F));
            this.tblEditor.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 127F));

            this.lblKeyword.Text = "关键词";
            this.lblKeyword.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.lblKeyword.Dock = System.Windows.Forms.DockStyle.Fill;
            
            this.txtKeyword.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtKeyword.Multiline = true;
            this.txtKeyword.ScrollBars = System.Windows.Forms.ScrollBars.None;
            
            this.lblReply.Text = "回复";
            this.lblReply.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.lblReply.Dock = System.Windows.Forms.DockStyle.Fill;
            
            this.txtReply.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtReply.Multiline = true;
            this.txtReply.ScrollBars = System.Windows.Forms.ScrollBars.None;

            this.tblEditor.Controls.Add(this.lblKeyword, 0, 0);
            this.tblEditor.Controls.Add(this.txtKeyword, 1, 0);
            this.tblEditor.Controls.Add(this.lblReply, 0, 1);
            this.tblEditor.Controls.Add(this.txtReply, 1, 1);
            // Keep the fixed-width inputs aligned; do not stretch into the extra column.
            this.tblEditor.SetColumnSpan(this.txtKeyword, 1);
            this.tblEditor.SetColumnSpan(this.txtReply, 1);

            // =========================
            // Right: Action buttons
            // =========================
            this.pnlButtons.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlButtons.FlowDirection = System.Windows.Forms.FlowDirection.RightToLeft;
            this.pnlButtons.WrapContents = false;
            this.pnlButtons.Padding = new System.Windows.Forms.Padding(0, 6, 0, 0);
            
            this.btnSave.Text = "保存设置";
            this.btnSave.Size = new System.Drawing.Size(100, 28);
            
            this.btnDelete.Text = "删除关键词";
            this.btnDelete.Size = new System.Drawing.Size(90, 28);

            this.btnAddModify.Text = "修改/添加";
            this.btnAddModify.Size = new System.Drawing.Size(90, 28);

            this.pnlButtons.Controls.Add(this.btnSave);
            this.pnlButtons.Controls.Add(this.btnDelete);
            this.pnlButtons.Controls.Add(this.btnAddModify);
            
            // =========================
            // Final control setup
            // =========================
            this.Controls.Add(this.splitMain);
            this.BackColor = System.Drawing.SystemColors.Control;
            this.Size = new System.Drawing.Size(670, 450);
            
            this.splitMain.Panel1.ResumeLayout(false);
            this.splitMain.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitMain)).EndInit();
            this.splitMain.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dgvKeywords)).EndInit();
            this.ResumeLayout(false);
        }

        /// <summary>
        /// Build a consistent "template" layout:
        /// - Row 1: Send text + QR import button (button spans Row 1-2)
        /// - Row 2: Text (e.g., QR code path)
        /// - Row 3: Reply keyword library (multiline)
        /// </summary>
        private static void BuildTemplateLayout(
            System.Windows.Forms.TableLayoutPanel table,
            System.Windows.Forms.Label lblSend,
            System.Windows.Forms.TextBox txtSend,
            System.Windows.Forms.Label lblText,
            System.Windows.Forms.TextBox txtText,
            System.Windows.Forms.Button btnQr,
            System.Windows.Forms.Label lblReply,
            System.Windows.Forms.TextBox txtReply)
        {
            table.Dock = System.Windows.Forms.DockStyle.Fill;
            table.ColumnCount = 3;
            table.RowCount = 3;
            table.ColumnStyles.Clear();
            table.RowStyles.Clear();

            table.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 45F));
            table.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            table.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 72F));

            table.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 28F));
            table.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 28F));
            table.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));

            // IMPORTANT: Disable AutoSize to avoid negative maxWidth issues during early layout
            // when the parent width is not finalized yet.
            lblSend.AutoSize = false;
            lblText.AutoSize = false;
            lblReply.AutoSize = false;

            lblSend.Text = "发送";
            lblSend.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            lblSend.Dock = System.Windows.Forms.DockStyle.Fill;

            txtSend.Dock = System.Windows.Forms.DockStyle.Fill;

            lblText.Text = "文本";
            lblText.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            lblText.Dock = System.Windows.Forms.DockStyle.Fill;

            txtText.Dock = System.Windows.Forms.DockStyle.Fill;

            lblReply.Text = "回复词库";
            lblReply.TextAlign = System.Drawing.ContentAlignment.TopLeft;
            lblReply.Dock = System.Windows.Forms.DockStyle.Fill;

            txtReply.Dock = System.Windows.Forms.DockStyle.Fill;
            txtReply.Multiline = true;
            txtReply.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;

            btnQr.Dock = System.Windows.Forms.DockStyle.Fill;
            btnQr.Font = new System.Drawing.Font("Microsoft YaHei UI", 8F);

            table.Controls.Clear();
            table.Controls.Add(lblSend, 0, 0);
            table.Controls.Add(txtSend, 1, 0);
            table.Controls.Add(btnQr, 2, 0);
            table.SetRowSpan(btnQr, 2);

            table.Controls.Add(lblText, 0, 1);
            table.Controls.Add(txtText, 1, 1);

            table.Controls.Add(lblReply, 0, 2);
            table.Controls.Add(txtReply, 1, 2);
            table.SetColumnSpan(txtReply, 2);
        }

        #endregion

        // Root layout containers
        private System.Windows.Forms.SplitContainer splitMain;
        private System.Windows.Forms.Panel panelLeft;
        private System.Windows.Forms.Panel panelRight;

        // Left side layout
        private System.Windows.Forms.TableLayoutPanel tblLeft;
        private System.Windows.Forms.GroupBox grpCft;
        private System.Windows.Forms.GroupBox grpZfb;
        private System.Windows.Forms.GroupBox grpWx;
        private System.Windows.Forms.TableLayoutPanel tblCft;
        private System.Windows.Forms.TableLayoutPanel tblZfb;
        private System.Windows.Forms.TableLayoutPanel tblWx;
        
        // CaiFuTong controls
        private System.Windows.Forms.Label lblCftSend;
        private System.Windows.Forms.TextBox txtCftSend;
        private System.Windows.Forms.Label lblCftText;
        private System.Windows.Forms.TextBox txtCftText;
        private System.Windows.Forms.Button btnCftQrCode;
        private System.Windows.Forms.Label lblCftReply;
        private System.Windows.Forms.TextBox txtCftReply;
        
        // ZhiFuBao controls
        private System.Windows.Forms.Label lblZfbSend;
        private System.Windows.Forms.TextBox txtZfbSend;
        private System.Windows.Forms.Label lblZfbText;
        private System.Windows.Forms.TextBox txtZfbText;
        private System.Windows.Forms.Button btnZfbQrCode;
        private System.Windows.Forms.Label lblZfbReply;
        private System.Windows.Forms.TextBox txtZfbReply;
        
        // WeiXin controls
        private System.Windows.Forms.Label lblWxSend;
        private System.Windows.Forms.TextBox txtWxSend;
        private System.Windows.Forms.Label lblWxText;
        private System.Windows.Forms.TextBox txtWxText;
        private System.Windows.Forms.Button btnWxQrCode;
        private System.Windows.Forms.Label lblWxReply;
        private System.Windows.Forms.TextBox txtWxReply;
        
        // Right side layout
        private System.Windows.Forms.TableLayoutPanel tblRight;
        private System.Windows.Forms.GroupBox grpCustomKeyword;
        private System.Windows.Forms.DataGridView dgvKeywords;
        private System.Windows.Forms.DataGridViewTextBoxColumn colKeyword;
        private System.Windows.Forms.DataGridViewTextBoxColumn colReply;
        private System.Windows.Forms.DataGridViewTextBoxColumn colBlank;

        // Editor area
        private System.Windows.Forms.GroupBox grpEditor;
        private System.Windows.Forms.TableLayoutPanel tblEditor;
        private System.Windows.Forms.Label lblKeyword;
        private System.Windows.Forms.TextBox txtKeyword;
        private System.Windows.Forms.Label lblReply;
        private System.Windows.Forms.TextBox txtReply;

        // Actions
        private System.Windows.Forms.FlowLayoutPanel pnlButtons;
        private System.Windows.Forms.Button btnAddModify;
        private System.Windows.Forms.Button btnDelete;
        private System.Windows.Forms.Button btnSave;
    }
}
