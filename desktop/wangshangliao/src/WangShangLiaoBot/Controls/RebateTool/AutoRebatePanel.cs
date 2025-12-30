using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using WangShangLiaoBot.Services;

namespace WangShangLiaoBot.Controls.RebateTool
{
    /// <summary>
    /// 自动反水面板 - Auto Rebate Panel
    /// </summary>
    public sealed class AutoRebatePanel : UserControl
    {
        private readonly Color BorderColor = Color.FromArgb(180, 180, 180);
        private readonly Color HeaderBgColor = Color.FromArgb(240, 240, 240);
        private readonly Color TabActiveColor = Color.White;
        private readonly Color TabInactiveColor = Color.FromArgb(240, 240, 240);

        // Sub-tabs
        private Panel pnlSubTabs;
        private Label lblTabAutoRebate;
        private Label lblTabRebateSettings;
        private Label lblTabReplySettings;
        private Label lblTabOtherSettings;
        private int _selectedSubTab = 0;

        // Content panels for each sub-tab
        private Panel pnlAutoRebateContent;
        private Panel pnlRebateSettingsContent;
        private Panel pnlReplySettingsContent;
        private Panel pnlOtherSettingsContent;

        // Auto Rebate tab controls
        private Label lblWangwang;
        private TextBox txtWangwang;
        private Button btnQuery;
        private Button btnExport;
        private Button btnAddBill;
        private Label lblTotalFlow;
        private Label lblProcessedFlow;
        private Label lblRewardedPoints;
        private Label lblRemainingFlow;
        private Label lblRewardablePoints;
        private DataGridView dgv;

        // Data
        private List<AutoRebateRecord> _allRecords = new List<AutoRebateRecord>();
        private DateTime _startTime = DateTime.Today;
        private DateTime _endTime = DateTime.Now;

        public AutoRebatePanel()
        {
            SuspendLayout();
            BackColor = Color.White;
            Dock = DockStyle.Fill;
            Font = new Font("Microsoft YaHei UI", 9F);

            CreateSubTabs();
            CreateAutoRebateContent();
            CreateRebateSettingsContent();
            CreateReplySettingsContent();
            CreateOtherSettingsContent();

            // 确保子标签栏在z-order的最后，这样它会先停靠并占用顶部空间
            pnlSubTabs.SendToBack();

            SelectSubTab(0);

            ResumeLayout(false);
        }

        private void CreateSubTabs()
        {
            pnlSubTabs = new Panel
            {
                Dock = DockStyle.Top,
                Height = 28,
                BackColor = TabInactiveColor
            };
            Controls.Add(pnlSubTabs);

            int x = 5;

            lblTabAutoRebate = CreateTabLabel("自动反水", x);
            lblTabAutoRebate.Click += (s, e) => SelectSubTab(0);
            pnlSubTabs.Controls.Add(lblTabAutoRebate);
            x += lblTabAutoRebate.Width + 10;

            lblTabRebateSettings = CreateTabLabel("反水设置", x);
            lblTabRebateSettings.Click += (s, e) => SelectSubTab(1);
            pnlSubTabs.Controls.Add(lblTabRebateSettings);
            x += lblTabRebateSettings.Width + 10;

            lblTabReplySettings = CreateTabLabel("回复设置", x);
            lblTabReplySettings.Click += (s, e) => SelectSubTab(2);
            pnlSubTabs.Controls.Add(lblTabReplySettings);
            x += lblTabReplySettings.Width + 10;

            lblTabOtherSettings = CreateTabLabel("其他设置", x);
            lblTabOtherSettings.Click += (s, e) => SelectSubTab(3);
            pnlSubTabs.Controls.Add(lblTabOtherSettings);
        }

        private Label CreateTabLabel(string text, int x)
        {
            return new Label
            {
                Text = text,
                Location = new Point(x, 5),
                AutoSize = true,
                Padding = new Padding(8, 3, 8, 3),
                Cursor = Cursors.Hand
            };
        }

        private void SelectSubTab(int index)
        {
            _selectedSubTab = index;

            // Update tab styles
            lblTabAutoRebate.BackColor = index == 0 ? TabActiveColor : TabInactiveColor;
            lblTabRebateSettings.BackColor = index == 1 ? TabActiveColor : TabInactiveColor;
            lblTabReplySettings.BackColor = index == 2 ? TabActiveColor : TabInactiveColor;
            lblTabOtherSettings.BackColor = index == 3 ? TabActiveColor : TabInactiveColor;

            // Show/hide content panels
            pnlAutoRebateContent.Visible = index == 0;
            pnlRebateSettingsContent.Visible = index == 1;
            pnlReplySettingsContent.Visible = index == 2;
            pnlOtherSettingsContent.Visible = index == 3;
        }

        private void CreateAutoRebateContent()
        {
            pnlAutoRebateContent = new Panel
            {
                Dock = DockStyle.Fill,
                Visible = true
            };
            Controls.Add(pnlAutoRebateContent);
            pnlAutoRebateContent.BringToFront();

            // Toolbar
            var pnlToolbar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 35,
                BackColor = Color.White
            };
            pnlAutoRebateContent.Controls.Add(pnlToolbar);

            int x = 10;

            lblWangwang = new Label
            {
                Text = "旺旺",
                Location = new Point(x, 8),
                AutoSize = true
            };
            pnlToolbar.Controls.Add(lblWangwang);
            x += 35;

            txtWangwang = new TextBox
            {
                Location = new Point(x, 5),
                Size = new Size(100, 23),
                BorderStyle = BorderStyle.FixedSingle
            };
            pnlToolbar.Controls.Add(txtWangwang);
            x += 110;

            btnQuery = new Button
            {
                Text = "查询",
                Location = new Point(x, 4),
                Size = new Size(60, 25),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.White
            };
            btnQuery.FlatAppearance.BorderColor = BorderColor;
            btnQuery.Click += BtnQuery_Click;
            pnlToolbar.Controls.Add(btnQuery);
            x += 70;

            btnExport = new Button
            {
                Text = "导出",
                Location = new Point(x, 4),
                Size = new Size(60, 25),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.White
            };
            btnExport.FlatAppearance.BorderColor = BorderColor;
            btnExport.Click += BtnExport_Click;
            pnlToolbar.Controls.Add(btnExport);
            x += 70;

            btnAddBill = new Button
            {
                Text = "加入账单",
                Location = new Point(x, 4),
                Size = new Size(80, 25),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.White
            };
            btnAddBill.FlatAppearance.BorderColor = BorderColor;
            btnAddBill.Click += BtnAddBill_Click;
            pnlToolbar.Controls.Add(btnAddBill);
            x += 100;

            // Statistics labels
            lblTotalFlow = new Label
            {
                Text = "总流水：",
                Location = new Point(x, 4),
                AutoSize = true,
                ForeColor = Color.Blue
            };
            pnlToolbar.Controls.Add(lblTotalFlow);

            lblProcessedFlow = new Label
            {
                Text = "已处理流水：",
                Location = new Point(x + 80, 4),
                AutoSize = true,
                ForeColor = Color.Blue
            };
            pnlToolbar.Controls.Add(lblProcessedFlow);

            lblRewardedPoints = new Label
            {
                Text = "已奖励分数：",
                Location = new Point(x + 180, 4),
                AutoSize = true,
                ForeColor = Color.Blue
            };
            pnlToolbar.Controls.Add(lblRewardedPoints);

            lblRemainingFlow = new Label
            {
                Text = "剩余流水：",
                Location = new Point(x, 18),
                AutoSize = true,
                ForeColor = Color.Blue
            };
            pnlToolbar.Controls.Add(lblRemainingFlow);

            lblRewardablePoints = new Label
            {
                Text = "可奖励分数：",
                Location = new Point(x + 80, 18),
                AutoSize = true,
                ForeColor = Color.Blue
            };
            pnlToolbar.Controls.Add(lblRewardablePoints);

            // DataGridView
            dgv = new DataGridView
            {
                Dock = DockStyle.Fill,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                ReadOnly = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                RowHeadersVisible = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                CellBorderStyle = DataGridViewCellBorderStyle.Single,
                GridColor = BorderColor,
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
                ColumnHeadersHeight = 24,
                ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single,
                EnableHeadersVisualStyles = false,
                RowTemplate = { Height = 20 },
                ScrollBars = ScrollBars.Both
            };

            dgv.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = HeaderBgColor,
                ForeColor = Color.Black,
                Font = new Font("Microsoft YaHei UI", 9F),
                Alignment = DataGridViewContentAlignment.MiddleCenter,
                SelectionBackColor = HeaderBgColor,
                SelectionForeColor = Color.Black
            };

            dgv.DefaultCellStyle = new DataGridViewCellStyle
            {
                Font = new Font("Microsoft YaHei UI", 9F),
                SelectionBackColor = Color.FromArgb(0, 120, 215),
                SelectionForeColor = Color.White
            };

            // Columns
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "WangwangId", HeaderText = "旺旺号", Width = 80, ReadOnly = true });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "Nickname", HeaderText = "昵称", Width = 60, ReadOnly = true });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "TotalFlow", HeaderText = "总流水", Width = 70, ReadOnly = true });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "ProcessedFlow", HeaderText = "已处理流水", Width = 80, ReadOnly = true });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "RewardedPoints", HeaderText = "已奖励分数", Width = 80, ReadOnly = true });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "RemainingFlow", HeaderText = "剩余流水", Width = 70, ReadOnly = true });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "RewardablePoints", HeaderText = "可奖励分数", Width = 80, ReadOnly = true });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "ModifyReward", HeaderText = "修改奖励", Width = 70, ReadOnly = false });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "ModifyNotes", HeaderText = "修改备注", Width = 80, ReadOnly = false });

            pnlAutoRebateContent.Controls.Add(dgv);
            dgv.BringToFront();
        }

        // Rebate Settings controls
        private CheckBox chkAutoRebate;
        private TextBox txtDefaultCount;
        private TextBox txtDefaultTotalFlow;
        private TextBox txtDefaultPercent;
        private TextBox txtAtReason;
        private TextBox txtGroupCommand;
        private TextBox txtCommandInterval;
        private Button btnSaveRebateSettings;
        private DataGridView dgvRebateSettings;
        private TextBox txtSearchWangwang;
        private Button btnSearchWangwang;
        private TextBox txtAddWangwangIds;
        private TextBox txtAddCount;
        private TextBox txtAddTotalFlow;
        private TextBox txtAddPercent;
        private Button btnAdd;
        private Button btnDelete;
        private Button btnDeleteAll;

        private void CreateRebateSettingsContent()
        {
            pnlRebateSettingsContent = new Panel
            {
                Dock = DockStyle.Fill,
                Visible = false,
                BackColor = Color.White,
                AutoScroll = true
            };
            Controls.Add(pnlRebateSettingsContent);

            int y = 10;

            // Auto rebate checkbox
            chkAutoRebate = new CheckBox
            {
                Text = "自动反水(开启后，界面反水不可用。界面反水时，需先关闭)",
                Location = new Point(10, y),
                AutoSize = true
            };
            pnlRebateSettingsContent.Controls.Add(chkAutoRebate);

            y += 28;

            // First row: 默认把数, 总流水, 返百分比
            var lblDefaultCount = new Label { Text = "默认把数≥", Location = new Point(10, y + 3), AutoSize = true };
            pnlRebateSettingsContent.Controls.Add(lblDefaultCount);

            txtDefaultCount = new TextBox { Location = new Point(80, y), Size = new Size(60, 23), BorderStyle = BorderStyle.FixedSingle };
            pnlRebateSettingsContent.Controls.Add(txtDefaultCount);

            var lblTotalFlow = new Label { Text = "总流水≥", Location = new Point(155, y + 3), AutoSize = true };
            pnlRebateSettingsContent.Controls.Add(lblTotalFlow);

            txtDefaultTotalFlow = new TextBox { Location = new Point(215, y), Size = new Size(60, 23), BorderStyle = BorderStyle.FixedSingle };
            pnlRebateSettingsContent.Controls.Add(txtDefaultTotalFlow);

            var lblPercent = new Label { Text = "返百分比", Location = new Point(290, y + 3), AutoSize = true };
            pnlRebateSettingsContent.Controls.Add(lblPercent);

            txtDefaultPercent = new TextBox { Location = new Point(355, y), Size = new Size(60, 23), BorderStyle = BorderStyle.FixedSingle };
            pnlRebateSettingsContent.Controls.Add(txtDefaultPercent);

            y += 30;

            // Second row: 艾特分理由, 群内反水命令, 命令间隔
            var lblAtReason = new Label { Text = "艾特分理由", Location = new Point(10, y + 3), AutoSize = true };
            pnlRebateSettingsContent.Controls.Add(lblAtReason);

            txtAtReason = new TextBox { Location = new Point(80, y), Size = new Size(60, 23), BorderStyle = BorderStyle.FixedSingle };
            pnlRebateSettingsContent.Controls.Add(txtAtReason);

            var lblGroupCmd = new Label { Text = "群内反水命令", Location = new Point(155, y + 3), AutoSize = true };
            pnlRebateSettingsContent.Controls.Add(lblGroupCmd);

            txtGroupCommand = new TextBox { Location = new Point(245, y), Size = new Size(80, 23), BorderStyle = BorderStyle.FixedSingle };
            pnlRebateSettingsContent.Controls.Add(txtGroupCommand);

            var lblInterval = new Label { Text = "命令间隔", Location = new Point(340, y + 3), AutoSize = true };
            pnlRebateSettingsContent.Controls.Add(lblInterval);

            txtCommandInterval = new TextBox { Location = new Point(400, y), Size = new Size(40, 23), BorderStyle = BorderStyle.FixedSingle, Text = "3" };
            pnlRebateSettingsContent.Controls.Add(txtCommandInterval);

            var lblMinute = new Label { Text = "分钟", Location = new Point(445, y + 3), AutoSize = true };
            pnlRebateSettingsContent.Controls.Add(lblMinute);

            // Save button
            btnSaveRebateSettings = new Button
            {
                Text = "保存",
                Location = new Point(520, y - 15),
                Size = new Size(60, 50),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.White
            };
            btnSaveRebateSettings.FlatAppearance.BorderColor = BorderColor;
            btnSaveRebateSettings.Click += BtnSaveRebateSettings_Click;
            pnlRebateSettingsContent.Controls.Add(btnSaveRebateSettings);

            y += 35;

            // DataGridView for special settings
            dgvRebateSettings = new DataGridView
            {
                Location = new Point(10, y),
                Size = new Size(380, 150),
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                RowHeadersVisible = false,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                ColumnHeadersHeight = 24,
                RowTemplate = { Height = 20 },
                EnableHeadersVisualStyles = false
            };

            dgvRebateSettings.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = HeaderBgColor,
                ForeColor = Color.Black,
                Font = new Font("Microsoft YaHei UI", 9F),
                Alignment = DataGridViewContentAlignment.MiddleCenter,
                SelectionBackColor = HeaderBgColor,
                SelectionForeColor = Color.Black
            };

            dgvRebateSettings.Columns.Add(new DataGridViewTextBoxColumn { Name = "WangwangId", HeaderText = "旺旺号", Width = 100 });
            dgvRebateSettings.Columns.Add(new DataGridViewTextBoxColumn { Name = "Count", HeaderText = "把数", Width = 70 });
            dgvRebateSettings.Columns.Add(new DataGridViewTextBoxColumn { Name = "TotalFlow", HeaderText = "总流水", Width = 100 });
            dgvRebateSettings.Columns.Add(new DataGridViewTextBoxColumn { Name = "Percent", HeaderText = "百分比", Width = 80 });

            pnlRebateSettingsContent.Controls.Add(dgvRebateSettings);

            // Search section on right
            var lblSearchWangwang = new Label { Text = "搜索旺旺号", Location = new Point(410, y), AutoSize = true };
            pnlRebateSettingsContent.Controls.Add(lblSearchWangwang);

            txtSearchWangwang = new TextBox
            {
                Location = new Point(410, y + 22),
                Size = new Size(120, 23),
                BorderStyle = BorderStyle.FixedSingle
            };
            pnlRebateSettingsContent.Controls.Add(txtSearchWangwang);

            btnSearchWangwang = new Button
            {
                Text = "搜索旺旺号",
                Location = new Point(410, y + 52),
                Size = new Size(100, 28),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.White
            };
            btnSearchWangwang.FlatAppearance.BorderColor = BorderColor;
            btnSearchWangwang.Click += BtnSearchWangwang_Click;
            pnlRebateSettingsContent.Controls.Add(btnSearchWangwang);

            y += 160;

            // Add section
            var lblAddWangwang = new Label { Text = "添加旺旺号", Location = new Point(10, y), AutoSize = true };
            pnlRebateSettingsContent.Controls.Add(lblAddWangwang);

            var lblAddCount = new Label { Text = "把数", Location = new Point(150, y), AutoSize = true };
            pnlRebateSettingsContent.Controls.Add(lblAddCount);

            var lblAddFlow = new Label { Text = "总流水", Location = new Point(230, y), AutoSize = true };
            pnlRebateSettingsContent.Controls.Add(lblAddFlow);

            var lblAddPercent = new Label { Text = "百分比", Location = new Point(320, y), AutoSize = true };
            pnlRebateSettingsContent.Controls.Add(lblAddPercent);

            y += 22;

            txtAddWangwangIds = new TextBox
            {
                Location = new Point(10, y),
                Size = new Size(120, 80),
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                BorderStyle = BorderStyle.FixedSingle
            };
            pnlRebateSettingsContent.Controls.Add(txtAddWangwangIds);

            txtAddCount = new TextBox { Location = new Point(150, y), Size = new Size(60, 23), BorderStyle = BorderStyle.FixedSingle };
            pnlRebateSettingsContent.Controls.Add(txtAddCount);

            txtAddTotalFlow = new TextBox { Location = new Point(230, y), Size = new Size(70, 23), BorderStyle = BorderStyle.FixedSingle };
            pnlRebateSettingsContent.Controls.Add(txtAddTotalFlow);

            txtAddPercent = new TextBox { Location = new Point(320, y), Size = new Size(60, 23), BorderStyle = BorderStyle.FixedSingle };
            pnlRebateSettingsContent.Controls.Add(txtAddPercent);

            y += 35;

            // Action buttons
            btnAdd = new Button
            {
                Text = "添加",
                Location = new Point(150, y),
                Size = new Size(60, 28),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.White
            };
            btnAdd.FlatAppearance.BorderColor = BorderColor;
            btnAdd.Click += BtnAddRebateSetting_Click;
            pnlRebateSettingsContent.Controls.Add(btnAdd);

            btnDelete = new Button
            {
                Text = "删除",
                Location = new Point(220, y),
                Size = new Size(60, 28),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.White
            };
            btnDelete.FlatAppearance.BorderColor = BorderColor;
            btnDelete.Click += BtnDeleteRebateSetting_Click;
            pnlRebateSettingsContent.Controls.Add(btnDelete);

            btnDeleteAll = new Button
            {
                Text = "全部删除",
                Location = new Point(290, y),
                Size = new Size(80, 28),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.White
            };
            btnDeleteAll.FlatAppearance.BorderColor = BorderColor;
            btnDeleteAll.Click += BtnDeleteAllRebateSetting_Click;
            pnlRebateSettingsContent.Controls.Add(btnDeleteAll);

            // Load settings
            LoadRebateSettings();
        }

        private void BtnSaveRebateSettings_Click(object sender, EventArgs e)
        {
            var ds = DataService.Instance;
            ds.SaveSetting("AutoRebate:Enabled", chkAutoRebate.Checked ? "1" : "0");
            ds.SaveSetting("AutoRebate:DefaultCount", txtDefaultCount.Text);
            ds.SaveSetting("AutoRebate:DefaultTotalFlow", txtDefaultTotalFlow.Text);
            ds.SaveSetting("AutoRebate:DefaultPercent", txtDefaultPercent.Text);
            ds.SaveSetting("AutoRebate:AtReason", txtAtReason.Text);
            ds.SaveSetting("AutoRebate:GroupCommand", txtGroupCommand.Text);
            ds.SaveSetting("AutoRebate:CommandInterval", txtCommandInterval.Text);
            MessageBox.Show("设置已保存", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void BtnSearchWangwang_Click(object sender, EventArgs e)
        {
            var search = txtSearchWangwang.Text.Trim();
            if (string.IsNullOrEmpty(search))
            {
                LoadRebateSettingsGrid();
                return;
            }

            // Filter grid
            for (int i = 0; i < dgvRebateSettings.Rows.Count; i++)
            {
                var row = dgvRebateSettings.Rows[i];
                var wangwangId = row.Cells["WangwangId"].Value?.ToString() ?? "";
                row.Visible = wangwangId.Contains(search);
            }
        }

        private void BtnAddRebateSetting_Click(object sender, EventArgs e)
        {
            var ids = txtAddWangwangIds.Text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Trim()).Where(x => !string.IsNullOrEmpty(x)).ToList();

            if (ids.Count == 0)
            {
                MessageBox.Show("请输入旺旺号", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            foreach (var id in ids)
            {
                dgvRebateSettings.Rows.Add(id, txtAddCount.Text, txtAddTotalFlow.Text, txtAddPercent.Text);
            }

            SaveRebateSettingsGrid();
            txtAddWangwangIds.Clear();
        }

        private void BtnDeleteRebateSetting_Click(object sender, EventArgs e)
        {
            if (dgvRebateSettings.SelectedRows.Count == 0)
            {
                MessageBox.Show("请选择要删除的行", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            dgvRebateSettings.Rows.Remove(dgvRebateSettings.SelectedRows[0]);
            SaveRebateSettingsGrid();
        }

        private void BtnDeleteAllRebateSetting_Click(object sender, EventArgs e)
        {
            if (dgvRebateSettings.Rows.Count == 0) return;

            var result = MessageBox.Show("确定要删除全部设置吗？", "确认", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (result == DialogResult.Yes)
            {
                dgvRebateSettings.Rows.Clear();
                SaveRebateSettingsGrid();
            }
        }

        private void LoadRebateSettings()
        {
            var ds = DataService.Instance;
            chkAutoRebate.Checked = ds.GetSetting("AutoRebate:Enabled", "0") == "1";
            txtDefaultCount.Text = ds.GetSetting("AutoRebate:DefaultCount", "");
            txtDefaultTotalFlow.Text = ds.GetSetting("AutoRebate:DefaultTotalFlow", "");
            txtDefaultPercent.Text = ds.GetSetting("AutoRebate:DefaultPercent", "");
            txtAtReason.Text = ds.GetSetting("AutoRebate:AtReason", "");
            txtGroupCommand.Text = ds.GetSetting("AutoRebate:GroupCommand", "");
            txtCommandInterval.Text = ds.GetSetting("AutoRebate:CommandInterval", "3");

            LoadRebateSettingsGrid();
        }

        private void LoadRebateSettingsGrid()
        {
            dgvRebateSettings.Rows.Clear();
            var ds = DataService.Instance;
            var data = ds.GetSetting("AutoRebate:SpecialSettings", "");
            if (!string.IsNullOrEmpty(data))
            {
                var rows = data.Split(new[] { "||" }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var row in rows)
                {
                    var parts = row.Split('|');
                    if (parts.Length >= 4)
                    {
                        dgvRebateSettings.Rows.Add(parts[0], parts[1], parts[2], parts[3]);
                    }
                }
            }
        }

        private void SaveRebateSettingsGrid()
        {
            var ds = DataService.Instance;
            var sb = new StringBuilder();
            foreach (DataGridViewRow row in dgvRebateSettings.Rows)
            {
                if (sb.Length > 0) sb.Append("||");
                sb.Append($"{row.Cells[0].Value}|{row.Cells[1].Value}|{row.Cells[2].Value}|{row.Cells[3].Value}");
            }
            ds.SaveSetting("AutoRebate:SpecialSettings", sb.ToString());
        }

        // Reply Settings controls
        private TextBox txtReplyReached;
        private TextBox txtReplyNotReached;
        private TextBox txtReplyCountNotReached;
        private TextBox txtReplyFlowNotReached;
        private Button btnSaveReplySettings;

        private void CreateReplySettingsContent()
        {
            pnlReplySettingsContent = new Panel
            {
                Dock = DockStyle.Fill,
                Visible = false,
                BackColor = Color.FromArgb(240, 240, 240),
                AutoScroll = true
            };
            Controls.Add(pnlReplySettingsContent);

            int y = 15;
            int labelX = 15;
            int inputX = 65;
            int inputWidth = 350;
            int rowHeight = 45;
            int inputHeight = 40;

            // Row 1: 回水已达标
            var lblReached = new Label
            {
                Text = "回水\n已达标",
                Location = new Point(labelX, y + 5),
                Size = new Size(45, 35),
                TextAlign = ContentAlignment.TopRight
            };
            pnlReplySettingsContent.Controls.Add(lblReached);

            txtReplyReached = new TextBox
            {
                Text = "本次回水[分数]",
                Location = new Point(inputX, y),
                Size = new Size(inputWidth, inputHeight),
                BorderStyle = BorderStyle.FixedSingle,
                Multiline = true
            };
            pnlReplySettingsContent.Controls.Add(txtReplyReached);

            y += rowHeight;

            // Row 2: 回水未达标
            var lblNotReached = new Label
            {
                Text = "回水\n未达标",
                Location = new Point(labelX, y + 5),
                Size = new Size(45, 35),
                TextAlign = ContentAlignment.TopRight
            };
            pnlReplySettingsContent.Controls.Add(lblNotReached);

            txtReplyNotReached = new TextBox
            {
                Text = "本次回水0",
                Location = new Point(inputX, y),
                Size = new Size(inputWidth, inputHeight),
                BorderStyle = BorderStyle.FixedSingle,
                Multiline = true
            };
            pnlReplySettingsContent.Controls.Add(txtReplyNotReached);

            y += rowHeight;

            // Row 3: 把数不达标
            var lblCountNotReached = new Label
            {
                Text = "把数\n不达标",
                Location = new Point(labelX, y + 5),
                Size = new Size(45, 35),
                TextAlign = ContentAlignment.TopRight
            };
            pnlReplySettingsContent.Controls.Add(lblCountNotReached);

            txtReplyCountNotReached = new TextBox
            {
                Text = "把数不足[把数]把",
                Location = new Point(inputX, y),
                Size = new Size(inputWidth, inputHeight),
                BorderStyle = BorderStyle.FixedSingle,
                Multiline = true
            };
            pnlReplySettingsContent.Controls.Add(txtReplyCountNotReached);

            y += rowHeight;

            // Row 4: 流水不达标
            var lblFlowNotReached = new Label
            {
                Text = "流水\n不达标",
                Location = new Point(labelX, y + 5),
                Size = new Size(45, 35),
                TextAlign = ContentAlignment.TopRight
            };
            pnlReplySettingsContent.Controls.Add(lblFlowNotReached);

            txtReplyFlowNotReached = new TextBox
            {
                Text = "本次回水0",
                Location = new Point(inputX, y),
                Size = new Size(inputWidth, inputHeight),
                BorderStyle = BorderStyle.FixedSingle,
                Multiline = true
            };
            pnlReplySettingsContent.Controls.Add(txtReplyFlowNotReached);

            y += rowHeight + 10;

            // Save button - centered below input boxes
            btnSaveReplySettings = new Button
            {
                Text = "保存",
                Location = new Point(inputX + (inputWidth - 70) / 2, y),
                Size = new Size(70, 28),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.White
            };
            btnSaveReplySettings.FlatAppearance.BorderColor = BorderColor;
            btnSaveReplySettings.Click += BtnSaveReplySettings_Click;
            pnlReplySettingsContent.Controls.Add(btnSaveReplySettings);

            // Load settings
            LoadReplySettings();
        }

        private void BtnSaveReplySettings_Click(object sender, EventArgs e)
        {
            var ds = DataService.Instance;
            ds.SaveSetting("AutoRebate:ReplyReached", txtReplyReached.Text);
            ds.SaveSetting("AutoRebate:ReplyNotReached", txtReplyNotReached.Text);
            ds.SaveSetting("AutoRebate:ReplyCountNotReached", txtReplyCountNotReached.Text);
            ds.SaveSetting("AutoRebate:ReplyFlowNotReached", txtReplyFlowNotReached.Text);
            MessageBox.Show("设置已保存", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void LoadReplySettings()
        {
            var ds = DataService.Instance;
            txtReplyReached.Text = ds.GetSetting("AutoRebate:ReplyReached", "本次回水[分数]");
            txtReplyNotReached.Text = ds.GetSetting("AutoRebate:ReplyNotReached", "本次回水0");
            txtReplyCountNotReached.Text = ds.GetSetting("AutoRebate:ReplyCountNotReached", "把数不足[把数]把");
            txtReplyFlowNotReached.Text = ds.GetSetting("AutoRebate:ReplyFlowNotReached", "本次回水0");
        }

        // Other Settings controls
        private CheckBox chkSpecialRule;
        private TextBox txtSpecialPlayers;
        private Button btnSaveOtherSettings;

        private void CreateOtherSettingsContent()
        {
            pnlOtherSettingsContent = new Panel
            {
                Dock = DockStyle.Fill,
                Visible = false,
                BackColor = Color.White,
                AutoScroll = true
            };
            Controls.Add(pnlOtherSettingsContent);

            int y = 10;

            // Checkbox: 回水设置-特殊规则，指定玩家生效
            chkSpecialRule = new CheckBox
            {
                Text = "回水设置-特殊规则，指定玩家生效",
                Location = new Point(10, y),
                AutoSize = true
            };
            pnlOtherSettingsContent.Controls.Add(chkSpecialRule);

            y += 28;

            // Label: 一行一个
            var lblHint = new Label
            {
                Text = "一行一个",
                Location = new Point(10, y),
                AutoSize = true
            };
            pnlOtherSettingsContent.Controls.Add(lblHint);

            y += 22;

            // Multiline TextBox for player IDs
            txtSpecialPlayers = new TextBox
            {
                Location = new Point(10, y),
                Size = new Size(180, 280),
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                BorderStyle = BorderStyle.FixedSingle
            };
            pnlOtherSettingsContent.Controls.Add(txtSpecialPlayers);

            // Save button - positioned to the right of the middle of the textbox
            btnSaveOtherSettings = new Button
            {
                Text = "保存",
                Location = new Point(220, y + 120),
                Size = new Size(70, 28),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.White
            };
            btnSaveOtherSettings.FlatAppearance.BorderColor = BorderColor;
            btnSaveOtherSettings.Click += BtnSaveOtherSettings_Click;
            pnlOtherSettingsContent.Controls.Add(btnSaveOtherSettings);

            // Load settings
            LoadOtherSettings();
        }

        private void BtnSaveOtherSettings_Click(object sender, EventArgs e)
        {
            var ds = DataService.Instance;
            ds.SaveSetting("AutoRebate:SpecialRuleEnabled", chkSpecialRule.Checked ? "1" : "0");
            ds.SaveSetting("AutoRebate:SpecialPlayers", txtSpecialPlayers.Text.Replace("\r\n", "||"));
            MessageBox.Show("设置已保存", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void LoadOtherSettings()
        {
            var ds = DataService.Instance;
            chkSpecialRule.Checked = ds.GetSetting("AutoRebate:SpecialRuleEnabled", "0") == "1";
            txtSpecialPlayers.Text = ds.GetSetting("AutoRebate:SpecialPlayers", "").Replace("||", "\r\n");
        }

        private void BtnQuery_Click(object sender, EventArgs e)
        {
            LoadRecords();
        }

        private void BtnExport_Click(object sender, EventArgs e)
        {
            if (dgv.Rows.Count == 0)
            {
                MessageBox.Show("没有数据可导出", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var fileName = $"自动反水_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
            var savePath = Path.Combine(DataService.Instance.DatabaseDir, fileName);

            try
            {
                var sb = new StringBuilder();
                sb.AppendLine("旺旺号,昵称,总流水,已处理流水,已奖励分数,剩余流水,可奖励分数,修改奖励,修改备注");
                foreach (DataGridViewRow row in dgv.Rows)
                {
                    var cells = row.Cells;
                    sb.AppendLine($"{cells[0].Value},{cells[1].Value},{cells[2].Value},{cells[3].Value},{cells[4].Value},{cells[5].Value},{cells[6].Value},{cells[7].Value},{cells[8].Value}");
                }
                File.WriteAllText(savePath, sb.ToString(), Encoding.UTF8);
                MessageBox.Show($"已导出到：\r\n{savePath}", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"导出失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnAddBill_Click(object sender, EventArgs e)
        {
            if (dgv.Rows.Count == 0)
            {
                MessageBox.Show("没有数据可加入账单", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            MessageBox.Show("已加入账单", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void LoadRecords()
        {
            _allRecords.Clear();
            var ds = DataService.Instance;

            try
            {
                // Load from 自动反水.db file
                var dbPath = Path.Combine(ds.DatabaseDir, "自动反水.db");
                if (File.Exists(dbPath))
                {
                    var lines = File.ReadAllLines(dbPath, Encoding.UTF8);
                    foreach (var line in lines)
                    {
                        if (string.IsNullOrWhiteSpace(line)) continue;
                        var parts = line.Split('|');
                        if (parts.Length >= 7)
                        {
                            var record = new AutoRebateRecord
                            {
                                WangwangId = parts[0],
                                Nickname = parts[1],
                                TotalFlow = ParseDec(parts[2], 0),
                                ProcessedFlow = ParseDec(parts[3], 0),
                                RewardedPoints = ParseDec(parts[4], 0),
                                RemainingFlow = ParseDec(parts[5], 0),
                                RewardablePoints = ParseDec(parts[6], 0)
                            };
                            _allRecords.Add(record);
                        }
                    }
                }

                // Filter by search
                var search = txtWangwang.Text.Trim();
                var filtered = _allRecords.AsEnumerable();
                if (!string.IsNullOrEmpty(search))
                {
                    filtered = filtered.Where(r =>
                        r.WangwangId.Contains(search) ||
                        r.Nickname.Contains(search));
                }

                var result = filtered.ToList();
                RenderRecords(result);
                UpdateStatistics(result);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"加载数据失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void RenderRecords(List<AutoRebateRecord> records)
        {
            dgv.Rows.Clear();
            foreach (var r in records)
            {
                dgv.Rows.Add(
                    r.WangwangId,
                    r.Nickname,
                    r.TotalFlow.ToString(CultureInfo.InvariantCulture),
                    r.ProcessedFlow.ToString(CultureInfo.InvariantCulture),
                    r.RewardedPoints.ToString(CultureInfo.InvariantCulture),
                    r.RemainingFlow.ToString(CultureInfo.InvariantCulture),
                    r.RewardablePoints.ToString(CultureInfo.InvariantCulture),
                    "",
                    ""
                );
            }
        }

        private void UpdateStatistics(List<AutoRebateRecord> records)
        {
            decimal totalFlow = records.Sum(r => r.TotalFlow);
            decimal processedFlow = records.Sum(r => r.ProcessedFlow);
            decimal rewardedPoints = records.Sum(r => r.RewardedPoints);
            decimal remainingFlow = records.Sum(r => r.RemainingFlow);
            decimal rewardablePoints = records.Sum(r => r.RewardablePoints);

            lblTotalFlow.Text = $"总流水：{totalFlow}";
            lblProcessedFlow.Text = $"已处理流水：{processedFlow}";
            lblRewardedPoints.Text = $"已奖励分数：{rewardedPoints}";
            lblRemainingFlow.Text = $"剩余流水：{remainingFlow}";
            lblRewardablePoints.Text = $"可奖励分数：{rewardablePoints}";
        }

        private static decimal ParseDec(string s, decimal defaultValue)
        {
            if (string.IsNullOrWhiteSpace(s)) return defaultValue;
            if (decimal.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var v)) return v;
            if (decimal.TryParse(s, out v)) return v;
            return defaultValue;
        }

        public void RefreshData(DateTime startTime, DateTime endTime)
        {
            _startTime = startTime;
            _endTime = endTime;
        }
    }

    internal class AutoRebateRecord
    {
        public string WangwangId { get; set; } = "";
        public string Nickname { get; set; } = "";
        public decimal TotalFlow { get; set; }
        public decimal ProcessedFlow { get; set; }
        public decimal RewardedPoints { get; set; }
        public decimal RemainingFlow { get; set; }
        public decimal RewardablePoints { get; set; }
    }
}

