using System;
using System.Drawing;
using System.Windows.Forms;
using WangShangLiaoBot.Services;

namespace WangShangLiaoBot.Controls
{
    /// <summary>
    /// 托设置控件 - 包含二级Tab: 托号 / 私聊版托
    /// </summary>
    public sealed class ShillSettingsControl : UserControl
    {
        private TabControl tabShill;
        private TabPage tabShillList;
        private TabPage tabPrivateShill;
        
        // Tab1: 托号 controls
        private ShillListPanel _shillListPanel;
        
        // Tab2: 私聊版托 controls
        private PrivateShillPanel _privateShillPanel;

        public ShillSettingsControl()
        {
            BackColor = SystemColors.Control;
            Font = new Font("Microsoft YaHei UI", 9F);
            MinimumSize = new Size(600, 420);
            Size = new Size(650, 450);
            
            InitializeUI();
        }

        private void InitializeUI()
        {
            SuspendLayout();

            // Main TabControl
            tabShill = new TabControl
            {
                Dock = DockStyle.Fill
            };
            Controls.Add(tabShill);

            // Tab 1: 托号
            tabShillList = new TabPage
            {
                Text = "托号",
                Padding = new Padding(5)
            };
            tabShill.TabPages.Add(tabShillList);

            _shillListPanel = new ShillListPanel
            {
                Dock = DockStyle.Fill
            };
            tabShillList.Controls.Add(_shillListPanel);

            // Tab 2: 私聊版托
            tabPrivateShill = new TabPage
            {
                Text = "私聊版托",
                Padding = new Padding(5)
            };
            tabShill.TabPages.Add(tabPrivateShill);

            _privateShillPanel = new PrivateShillPanel
            {
                Dock = DockStyle.Fill
            };
            tabPrivateShill.Controls.Add(_privateShillPanel);

            ResumeLayout(false);
        }
    }

    /// <summary>
    /// 托号列表面板
    /// </summary>
    internal sealed class ShillListPanel : UserControl
    {
        private GroupBox grpShill;
        private DataGridView dgvShillList;
        private Label lblDesc;
        private CheckBox chkAutoAddScore;
        private NumericUpDown nudQueryDelayMin, nudQueryDelayMax;
        private CheckBox chkAutoSubScore;
        private NumericUpDown nudReturnDelayMin, nudReturnDelayMax;
        private CheckBox chkAutoAcceptJoin;
        private Button btnAdd, btnExport, btnRemove, btnRemoveAll;
        private CheckBox chkRemoteFetch;
        private Button btnCopyAddress;
        private Button btnSave;

        public ShillListPanel()
        {
            InitializeUI();
            LoadData();
            ShillService.Instance.OnListChanged += RefreshList;
        }

        private void InitializeUI()
        {
            SuspendLayout();

            grpShill = new GroupBox
            {
                Text = "托号",
                Location = new Point(5, 5),
                Size = new Size(560, 370)
            };
            Controls.Add(grpShill);

            // Left: DataGridView
            dgvShillList = new DataGridView
            {
                Location = new Point(15, 22),
                Size = new Size(170, 200),
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = true,
                RowHeadersVisible = false,
                BackgroundColor = SystemColors.Window,
                BorderStyle = BorderStyle.FixedSingle,
                CellBorderStyle = DataGridViewCellBorderStyle.Single,
                GridColor = Color.LightGray
            };
            dgvShillList.Columns.Add("Index", "序");
            dgvShillList.Columns.Add("ShillId", "托名单");
            dgvShillList.Columns["Index"].Width = 35;
            dgvShillList.Columns["ShillId"].Width = 120;
            grpShill.Controls.Add(dgvShillList);

            // Right: Settings
            int rightX = 200;
            lblDesc = new Label
            {
                Text = "如添加多个托号\n一行一个",
                Location = new Point(rightX, 22),
                Size = new Size(120, 35),
                ForeColor = Color.Gray
            };
            grpShill.Controls.Add(lblDesc);

            chkAutoAddScore = new CheckBox
            {
                Text = "托查钱自动上分  延迟",
                Location = new Point(rightX, 60),
                Size = new Size(140, 20)
            };
            grpShill.Controls.Add(chkAutoAddScore);

            nudQueryDelayMin = new NumericUpDown
            {
                Location = new Point(rightX + 145, 58),
                Size = new Size(45, 23),
                Minimum = 1, Maximum = 60, Value = 6
            };
            grpShill.Controls.Add(nudQueryDelayMin);

            var lbl1 = new Label { Text = "-", Location = new Point(rightX + 192, 62), AutoSize = true };
            grpShill.Controls.Add(lbl1);

            nudQueryDelayMax = new NumericUpDown
            {
                Location = new Point(rightX + 205, 58),
                Size = new Size(45, 23),
                Minimum = 1, Maximum = 120, Value = 10
            };
            grpShill.Controls.Add(nudQueryDelayMax);

            var lbl2 = new Label { Text = "秒喊到", Location = new Point(rightX + 252, 62), AutoSize = true };
            grpShill.Controls.Add(lbl2);

            chkAutoSubScore = new CheckBox
            {
                Text = "托回钱自动下分  延迟",
                Location = new Point(rightX, 88),
                Size = new Size(140, 20)
            };
            grpShill.Controls.Add(chkAutoSubScore);

            nudReturnDelayMin = new NumericUpDown
            {
                Location = new Point(rightX + 145, 86),
                Size = new Size(45, 23),
                Minimum = 1, Maximum = 120, Value = 15
            };
            grpShill.Controls.Add(nudReturnDelayMin);

            var lbl3 = new Label { Text = "-", Location = new Point(rightX + 192, 90), AutoSize = true };
            grpShill.Controls.Add(lbl3);

            nudReturnDelayMax = new NumericUpDown
            {
                Location = new Point(rightX + 205, 86),
                Size = new Size(45, 23),
                Minimum = 1, Maximum = 180, Value = 25
            };
            grpShill.Controls.Add(nudReturnDelayMax);

            var lbl4 = new Label { Text = "秒喊查", Location = new Point(rightX + 252, 90), AutoSize = true };
            grpShill.Controls.Add(lbl4);

            chkAutoAcceptJoin = new CheckBox
            {
                Text = "自动同意托加群",
                Location = new Point(rightX, 116),
                Size = new Size(150, 20)
            };
            grpShill.Controls.Add(chkAutoAcceptJoin);

            // Buttons
            btnAdd = new Button { Text = "添加托", Location = new Point(rightX, 148), Size = new Size(80, 26) };
            btnAdd.Click += BtnAdd_Click;
            grpShill.Controls.Add(btnAdd);

            btnExport = new Button { Text = "导出托", Location = new Point(rightX + 90, 148), Size = new Size(80, 26) };
            btnExport.Click += BtnExport_Click;
            grpShill.Controls.Add(btnExport);

            btnRemove = new Button { Text = "移除托", Location = new Point(rightX, 180), Size = new Size(80, 26) };
            btnRemove.Click += BtnRemove_Click;
            grpShill.Controls.Add(btnRemove);

            btnRemoveAll = new Button { Text = "移除所有托", Location = new Point(rightX + 90, 180), Size = new Size(90, 26) };
            btnRemoveAll.Click += BtnRemoveAll_Click;
            grpShill.Controls.Add(btnRemoveAll);

            // Bottom
            chkRemoteFetch = new CheckBox
            {
                Text = "托插件远程获取账单",
                Location = new Point(15, 235),
                Size = new Size(160, 20)
            };
            grpShill.Controls.Add(chkRemoteFetch);

            btnCopyAddress = new Button
            {
                Text = "复制账单地址",
                Location = new Point(180, 232),
                Size = new Size(100, 26)
            };
            btnCopyAddress.Click += BtnCopyAddress_Click;
            grpShill.Controls.Add(btnCopyAddress);

            btnSave = new Button
            {
                Text = "保存设置",
                Location = new Point(440, 330),
                Size = new Size(100, 28)
            };
            btnSave.Click += BtnSave_Click;
            grpShill.Controls.Add(btnSave);

            ResumeLayout(false);
        }

        private void LoadData()
        {
            var s = ShillService.Instance;
            chkAutoAddScore.Checked = s.AutoAddScoreOnQuery;
            nudQueryDelayMin.Value = Clamp(s.QueryDelayMin, nudQueryDelayMin);
            nudQueryDelayMax.Value = Clamp(s.QueryDelayMax, nudQueryDelayMax);
            chkAutoSubScore.Checked = s.AutoSubScoreOnReturn;
            nudReturnDelayMin.Value = Clamp(s.ReturnDelayMin, nudReturnDelayMin);
            nudReturnDelayMax.Value = Clamp(s.ReturnDelayMax, nudReturnDelayMax);
            chkAutoAcceptJoin.Checked = s.AutoAcceptJoinGroup;
            chkRemoteFetch.Checked = s.RemoteFetchBill;
            RefreshList();
        }

        private decimal Clamp(int v, NumericUpDown nud)
        {
            if (v < (int)nud.Minimum) return nud.Minimum;
            if (v > (int)nud.Maximum) return nud.Maximum;
            return v;
        }

        private void RefreshList()
        {
            if (InvokeRequired) { Invoke(new Action(RefreshList)); return; }
            dgvShillList.Rows.Clear();
            var list = ShillService.Instance.GetAll();
            for (int i = 0; i < list.Count; i++)
                dgvShillList.Rows.Add(i + 1, list[i]);
        }

        private void BtnAdd_Click(object sender, EventArgs e)
        {
            using (var dlg = new Form())
            {
                dlg.Text = "添加托号";
                dlg.Size = new Size(300, 250);
                dlg.StartPosition = FormStartPosition.CenterParent;
                dlg.FormBorderStyle = FormBorderStyle.FixedDialog;
                dlg.MaximizeBox = dlg.MinimizeBox = false;

                var lbl = new Label { Text = "请输入托号（每行一个）:", Location = new Point(10, 10), AutoSize = true };
                dlg.Controls.Add(lbl);

                var txt = new TextBox { Location = new Point(10, 35), Size = new Size(260, 120), Multiline = true, ScrollBars = ScrollBars.Vertical };
                dlg.Controls.Add(txt);

                var btn = new Button { Text = "确定", Location = new Point(100, 165), Size = new Size(80, 28), DialogResult = DialogResult.OK };
                dlg.Controls.Add(btn);
                dlg.AcceptButton = btn;

                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    var added = ShillService.Instance.AddMultiple(txt.Text);
                    MessageBox.Show(added > 0 ? $"成功添加 {added} 个托号" : "没有新增托号", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }

        private void BtnExport_Click(object sender, EventArgs e)
        {
            var content = ShillService.Instance.Export();
            if (string.IsNullOrWhiteSpace(content)) { MessageBox.Show("托号列表为空", "提示"); return; }
            try { Clipboard.SetText(content); MessageBox.Show("托号列表已复制到剪切板", "导出成功"); }
            catch (Exception ex) { MessageBox.Show($"复制失败：{ex.Message}", "错误"); }
        }

        private void BtnRemove_Click(object sender, EventArgs e)
        {
            if (dgvShillList.SelectedRows.Count == 0) { MessageBox.Show("请先选择要移除的托号", "提示"); return; }
            if (MessageBox.Show($"确定移除选中的 {dgvShillList.SelectedRows.Count} 个托号？", "确认", MessageBoxButtons.YesNo) != DialogResult.Yes) return;
            foreach (DataGridViewRow row in dgvShillList.SelectedRows)
            {
                var id = row.Cells["ShillId"].Value?.ToString();
                if (!string.IsNullOrEmpty(id)) ShillService.Instance.Remove(id);
            }
        }

        private void BtnRemoveAll_Click(object sender, EventArgs e)
        {
            if (dgvShillList.Rows.Count == 0) { MessageBox.Show("托号列表已为空", "提示"); return; }
            if (MessageBox.Show("确定移除所有托号？", "确认", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
            {
                ShillService.Instance.ClearAll();
                MessageBox.Show("已清空所有托号", "提示");
            }
        }

        private void BtnCopyAddress_Click(object sender, EventArgs e)
        {
            var addr = ShillService.Instance.BillAddress;
            if (string.IsNullOrWhiteSpace(addr))
            {
                addr = $"http://localhost:8080/bill/{Guid.NewGuid():N}";
                ShillService.Instance.BillAddress = addr;
            }
            try { Clipboard.SetText(addr); MessageBox.Show($"账单地址已复制:\n{addr}", "复制成功"); }
            catch (Exception ex) { MessageBox.Show($"复制失败：{ex.Message}", "错误"); }
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            var s = ShillService.Instance;
            s.AutoAddScoreOnQuery = chkAutoAddScore.Checked;
            s.QueryDelayMin = (int)nudQueryDelayMin.Value;
            s.QueryDelayMax = (int)nudQueryDelayMax.Value;
            s.AutoSubScoreOnReturn = chkAutoSubScore.Checked;
            s.ReturnDelayMin = (int)nudReturnDelayMin.Value;
            s.ReturnDelayMax = (int)nudReturnDelayMax.Value;
            s.AutoAcceptJoinGroup = chkAutoAcceptJoin.Checked;
            s.RemoteFetchBill = chkRemoteFetch.Checked;
            MessageBox.Show("设置已保存", "提示");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) ShillService.Instance.OnListChanged -= RefreshList;
            base.Dispose(disposing);
        }
    }

    /// <summary>
    /// 私聊版托面板
    /// </summary>
    internal sealed class PrivateShillPanel : UserControl
    {
        private CheckBox chkEnabled;
        private NumericUpDown nudAfterLotteryDelay;
        private TextBox txtBeforeSealTime;
        private TextBox txtShillList;
        private NumericUpDown nudBet1Min, nudBet1Max, nudBet2Min, nudBet2Max;
        private NumericUpDown nudBet3Min, nudBet3Max, nudBet4Min, nudBet4Max;
        private TextBox txtBet1, txtBet2, txtBet3, txtBet4;
        private NumericUpDown nudScoreLow, nudScoreHigh;
        private TextBox txtScoreLowMsg, txtScoreHighMsg;
        private NumericUpDown nudSpeedMin, nudSpeedMax;
        private Button btnSave;

        public PrivateShillPanel()
        {
            AutoScroll = true;
            InitializeUI();
            LoadData();
        }

        private void InitializeUI()
        {
            SuspendLayout();
            int y = 10;

            chkEnabled = new CheckBox
            {
                Text = "私聊版托开关(开启后艾特会变成昵称发送)",
                Location = new Point(15, y),
                Size = new Size(350, 20)
            };
            Controls.Add(chkEnabled);
            y += 28;

            // After lottery delay
            var lbl1 = new Label { Text = "开奖后", Location = new Point(15, y + 3), AutoSize = true };
            Controls.Add(lbl1);
            nudAfterLotteryDelay = new NumericUpDown { Location = new Point(60, y), Size = new Size(55, 23), Minimum = 1, Maximum = 120, Value = 20 };
            Controls.Add(nudAfterLotteryDelay);
            var lbl2 = new Label { Text = "秒内不下注", Location = new Point(120, y + 3), AutoSize = true };
            Controls.Add(lbl2);
            y += 26;

            // Before seal
            var lbl3 = new Label { Text = "封盘前", Location = new Point(15, y + 3), AutoSize = true };
            Controls.Add(lbl3);
            txtBeforeSealTime = new TextBox { Location = new Point(60, y), Size = new Size(55, 23), Text = "wan20" };
            Controls.Add(txtBeforeSealTime);
            var lbl4 = new Label { Text = "秒停止下注", Location = new Point(120, y + 3), AutoSize = true };
            Controls.Add(lbl4);
            y += 32;

            // Shill list (left)
            var lblList = new Label { Text = "一行一个托", Location = new Point(15, y), AutoSize = true };
            Controls.Add(lblList);
            txtShillList = new TextBox { Location = new Point(15, y + 18), Size = new Size(95, 230), Multiline = true, ScrollBars = ScrollBars.Vertical };
            Controls.Add(txtShillList);

            // Bet settings (right)
            int rx = 125;
            var lblBet = new Label { Text = "下注设置:", Location = new Point(rx, y), AutoSize = true };
            Controls.Add(lblBet);
            y += 20;
            CreateBetRow(rx, ref y, out nudBet1Min, out nudBet1Max, out txtBet1, 50, 400);
            CreateBetRow(rx, ref y, out nudBet2Min, out nudBet2Max, out txtBet2, 401, 800);
            CreateBetRow(rx, ref y, out nudBet3Min, out nudBet3Max, out txtBet3, 801, 1300);
            CreateBetRow(rx, ref y, out nudBet4Min, out nudBet4Max, out txtBet4, 1301, 10000);

            // Score settings
            y += 5;
            var lblScore = new Label { Text = "上下分设置:", Location = new Point(rx, y), AutoSize = true };
            Controls.Add(lblScore);
            y += 20;

            var lblLow1 = new Label { Text = "分数低于", Location = new Point(rx, y + 3), AutoSize = true };
            Controls.Add(lblLow1);
            nudScoreLow = new NumericUpDown { Location = new Point(rx + 55, y), Size = new Size(55, 23), Minimum = 0, Maximum = 99999, Value = 100 };
            Controls.Add(nudScoreLow);
            var lblLow2 = new Label { Text = "分发送:", Location = new Point(rx + 115, y + 3), AutoSize = true };
            Controls.Add(lblLow2);
            txtScoreLowMsg = new TextBox { Location = new Point(rx + 165, y), Size = new Size(200, 23) };
            Controls.Add(txtScoreLowMsg);
            y += 26;

            var lblHigh1 = new Label { Text = "分数高于", Location = new Point(rx, y + 3), AutoSize = true };
            Controls.Add(lblHigh1);
            nudScoreHigh = new NumericUpDown { Location = new Point(rx + 55, y), Size = new Size(55, 23), Minimum = 0, Maximum = 999999, Value = 5999 };
            Controls.Add(nudScoreHigh);
            var lblHigh2 = new Label { Text = "分发送:", Location = new Point(rx + 115, y + 3), AutoSize = true };
            Controls.Add(lblHigh2);
            txtScoreHighMsg = new TextBox { Location = new Point(rx + 165, y), Size = new Size(200, 23) };
            Controls.Add(txtScoreHighMsg);
            y += 30;

            // Speed settings
            var lblSpd = new Label { Text = "速度设置:", Location = new Point(rx, y), AutoSize = true };
            Controls.Add(lblSpd);
            y += 20;
            nudSpeedMin = new NumericUpDown { Location = new Point(rx, y), Size = new Size(50, 23), Minimum = 1, Maximum = 60, Value = 3 };
            Controls.Add(nudSpeedMin);
            var lblS1 = new Label { Text = "到", Location = new Point(rx + 55, y + 3), AutoSize = true };
            Controls.Add(lblS1);
            nudSpeedMax = new NumericUpDown { Location = new Point(rx + 75, y), Size = new Size(50, 23), Minimum = 1, Maximum = 120, Value = 10 };
            Controls.Add(nudSpeedMax);
            var lblS2 = new Label { Text = "秒随机选择一个托行动", Location = new Point(rx + 130, y + 3), AutoSize = true };
            Controls.Add(lblS2);

            // Save button
            btnSave = new Button { Text = "保存设置", Location = new Point(460, 340), Size = new Size(90, 28) };
            btnSave.Click += BtnSave_Click;
            Controls.Add(btnSave);

            ResumeLayout(false);
        }

        private void CreateBetRow(int x, ref int y, out NumericUpDown min, out NumericUpDown max, out TextBox txt, int minDef, int maxDef)
        {
            min = new NumericUpDown { Location = new Point(x, y), Size = new Size(50, 23), Minimum = 0, Maximum = 99999, Value = minDef };
            Controls.Add(min);
            var lbl1 = new Label { Text = "到", Location = new Point(x + 53, y + 3), AutoSize = true };
            Controls.Add(lbl1);
            max = new NumericUpDown { Location = new Point(x + 70, y), Size = new Size(55, 23), Minimum = 0, Maximum = 99999, Value = maxDef };
            Controls.Add(max);
            var lbl2 = new Label { Text = "分下注:", Location = new Point(x + 128, y + 3), AutoSize = true };
            Controls.Add(lbl2);
            txt = new TextBox { Location = new Point(x + 180, y), Size = new Size(185, 23) };
            Controls.Add(txt);
            y += 26;
        }

        private void LoadData()
        {
            var s = PrivateShillService.Instance;
            chkEnabled.Checked = s.Enabled;
            nudAfterLotteryDelay.Value = Clamp(s.AfterLotteryDelay, nudAfterLotteryDelay);
            txtBeforeSealTime.Text = s.BeforeSealTime;
            txtShillList.Text = s.ShillListText;
            nudBet1Min.Value = s.BetRange1Min; nudBet1Max.Value = s.BetRange1Max; txtBet1.Text = s.BetRange1Bets;
            nudBet2Min.Value = s.BetRange2Min; nudBet2Max.Value = s.BetRange2Max; txtBet2.Text = s.BetRange2Bets;
            nudBet3Min.Value = s.BetRange3Min; nudBet3Max.Value = s.BetRange3Max; txtBet3.Text = s.BetRange3Bets;
            nudBet4Min.Value = s.BetRange4Min; nudBet4Max.Value = s.BetRange4Max; txtBet4.Text = s.BetRange4Bets;
            nudScoreLow.Value = s.ScoreLowThreshold; txtScoreLowMsg.Text = s.ScoreLowMessages;
            nudScoreHigh.Value = s.ScoreHighThreshold; txtScoreHighMsg.Text = s.ScoreHighMessages;
            nudSpeedMin.Value = Clamp(s.SpeedMin, nudSpeedMin);
            nudSpeedMax.Value = Clamp(s.SpeedMax, nudSpeedMax);
        }

        private decimal Clamp(int v, NumericUpDown nud)
        {
            if (v < (int)nud.Minimum) return nud.Minimum;
            if (v > (int)nud.Maximum) return nud.Maximum;
            return v;
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            var s = PrivateShillService.Instance;
            s.Enabled = chkEnabled.Checked;
            s.AfterLotteryDelay = (int)nudAfterLotteryDelay.Value;
            s.BeforeSealTime = txtBeforeSealTime.Text.Trim();
            s.ShillListText = txtShillList.Text;
            s.BetRange1Min = (int)nudBet1Min.Value; s.BetRange1Max = (int)nudBet1Max.Value; s.BetRange1Bets = txtBet1.Text.Trim();
            s.BetRange2Min = (int)nudBet2Min.Value; s.BetRange2Max = (int)nudBet2Max.Value; s.BetRange2Bets = txtBet2.Text.Trim();
            s.BetRange3Min = (int)nudBet3Min.Value; s.BetRange3Max = (int)nudBet3Max.Value; s.BetRange3Bets = txtBet3.Text.Trim();
            s.BetRange4Min = (int)nudBet4Min.Value; s.BetRange4Max = (int)nudBet4Max.Value; s.BetRange4Bets = txtBet4.Text.Trim();
            s.ScoreLowThreshold = (int)nudScoreLow.Value; s.ScoreLowMessages = txtScoreLowMsg.Text.Trim();
            s.ScoreHighThreshold = (int)nudScoreHigh.Value; s.ScoreHighMessages = txtScoreHighMsg.Text.Trim();
            s.SpeedMin = (int)nudSpeedMin.Value; s.SpeedMax = (int)nudSpeedMax.Value;
            MessageBox.Show("设置已保存", "提示");
        }
    }
}
