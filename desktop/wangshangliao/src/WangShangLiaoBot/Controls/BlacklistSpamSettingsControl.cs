using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using WangShangLiaoBot.Models.Spam;
using WangShangLiaoBot.Services;
using WangShangLiaoBot.Services.Spam;

namespace WangShangLiaoBot.Controls
{
    /// <summary>
    /// Blacklist / Spam detection settings control.
    /// Layout strictly follows the provided design screenshot.
    /// </summary>
    public sealed class BlacklistSpamSettingsControl : UserControl
    {
        // ===== Left: Blacklist =====
        private GroupBox grpBlacklist;
        private DataGridView dgvBlacklist;
        private CheckBox chkAutoBlacklistBotKick;
        private CheckBox chkAutoBlacklistAdminKick;
        private ListBox lstCandidates;
        private Button btnClearAll;
        private Button btnAddBlacklist;
        private Button btnRemoveBlacklist;

        // ===== Right: Spam =====
        private GroupBox grpSpam;
        private Label lblRuleHint;
        private Label lblCharsMute;
        private NumericUpDown nudCharsMute;
        private Label lblCharsMuteSuffix;
        private Label lblCharsKick;
        private NumericUpDown nudCharsKick;
        private Label lblCharsKickSuffix;
        private Label lblLinesMute;
        private NumericUpDown nudLinesMute;
        private Label lblLinesMuteSuffix;
        private CheckBox chkImageEmoji;
        private NumericUpDown nudImageKickCount;
        private Label lblImageKickSuffix;
        private CheckBox chkBillAtMute;
        private CheckBox chkFollowBotAutoRecall;
        private DataGridView dgvKeywords;
        private Label lblKeyword;
        private TextBox txtKeyword;
        private GroupBox grpAction;
        private RadioButton rbMute;
        private RadioButton rbKick;
        private Button btnAddKeyword;
        private Button btnRemoveKeyword;
        private Label lblMuteMinutes;
        private NumericUpDown nudMuteMinutes;
        private Label lblMuteMinutesSuffix;
        private Button btnSave;

        public BlacklistSpamSettingsControl()
        {
            BackColor = SystemColors.Control;
            Font = new Font("Microsoft YaHei UI", 9F);
            
            // Set minimum size to ensure full display (match original design)
            MinimumSize = new Size(680, 430);
            Size = new Size(680, 430);
            AutoScroll = true;
            Dock = DockStyle.Fill;
            
            InitializeUi();
            LoadData();
        }

        private void InitializeUi()
        {
            SuspendLayout();

            // ================= LEFT: Blacklist GroupBox =================
            grpBlacklist = new GroupBox
            {
                Text = "黑名单",
                Location = new Point(5, 5),
                Size = new Size(250, 330),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Bottom
            };
            Controls.Add(grpBlacklist);

            // Blacklist DataGridView (left side of group)
            // Enable grid lines (dashed style) and editable cells
            dgvBlacklist = new DataGridView
            {
                Location = new Point(8, 18),
                Size = new Size(110, 220),
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                RowHeadersVisible = false,
                MultiSelect = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                BackgroundColor = Color.White,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize,
                // Grid line settings
                GridColor = Color.LightGray,
                CellBorderStyle = DataGridViewCellBorderStyle.Single,
                BorderStyle = BorderStyle.FixedSingle,
                // Enable editing
                ReadOnly = false,
                EditMode = DataGridViewEditMode.EditOnKeystrokeOrF2
            };
            // Column: Index (read-only)
            var colIndex = new DataGridViewTextBoxColumn
            {
                Name = "colIndex",
                HeaderText = "序",
                FillWeight = 25,
                ReadOnly = true,
                DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter }
            };
            // Column: Blacklist ID (editable)
            var colId = new DataGridViewTextBoxColumn
            {
                Name = "colId",
                HeaderText = "黑名单号",
                FillWeight = 75,
                ReadOnly = false,
                DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleRight }
            };
            dgvBlacklist.Columns.Add(colIndex);
            dgvBlacklist.Columns.Add(colId);
            dgvBlacklist.CellEndEdit += DgvBlacklist_CellEndEdit;
            grpBlacklist.Controls.Add(dgvBlacklist);

            // CheckBoxes (right side of blacklist grid)
            chkAutoBlacklistBotKick = new CheckBox
            {
                Text = "被机器人踢出自动加黑名单",
                Location = new Point(122, 18),
                Size = new Size(125, 36),
                Checked = true
            };
            grpBlacklist.Controls.Add(chkAutoBlacklistBotKick);

            chkAutoBlacklistAdminKick = new CheckBox
            {
                Text = "被群管理踢出自动加黑名单",
                Location = new Point(122, 55),
                Size = new Size(125, 36)
            };
            grpBlacklist.Controls.Add(chkAutoBlacklistAdminKick);

            // Candidates ListBox (shows kicked users waiting to be added)
            lstCandidates = new ListBox
            {
                Location = new Point(122, 95),
                Size = new Size(120, 143)
            };
            grpBlacklist.Controls.Add(lstCandidates);

            // Buttons at bottom
            btnClearAll = new Button
            {
                Text = "全部删除",
                Location = new Point(8, 248),
                Size = new Size(75, 28)
            };
            btnClearAll.Click += (s, e) => ClearAllBlacklist();
            grpBlacklist.Controls.Add(btnClearAll);

            btnAddBlacklist = new Button
            {
                Text = "添加黑名单",
                Location = new Point(140, 248),
                Size = new Size(100, 28)
            };
            btnAddBlacklist.Click += (s, e) => AddSelectedCandidateToBlacklist();
            grpBlacklist.Controls.Add(btnAddBlacklist);

            btnRemoveBlacklist = new Button
            {
                Text = "移除黑名单",
                Location = new Point(8, 283),
                Size = new Size(75, 28)
            };
            btnRemoveBlacklist.Click += (s, e) => RemoveSelectedBlacklist();
            grpBlacklist.Controls.Add(btnRemoveBlacklist);

            // ================= RIGHT: Spam GroupBox =================
            grpSpam = new GroupBox
            {
                Text = "刷屏检测",
                Location = new Point(260, 5),
                Size = new Size(415, 420),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom
            };
            Controls.Add(grpSpam);

            // Rule hint label
            lblRuleHint = new Label
            {
                Text = "一个汉字=2字符   一个字母=1字符",
                Location = new Point(12, 22),
                AutoSize = true
            };
            grpSpam.Controls.Add(lblRuleHint);

            // Row 1: Characters mute threshold
            lblCharsMute = new Label
            {
                Text = "发言字符超过",
                Location = new Point(12, 48),
                AutoSize = true
            };
            grpSpam.Controls.Add(lblCharsMute);

            nudCharsMute = new NumericUpDown
            {
                Location = new Point(95, 45),
                Size = new Size(50, 23),
                Minimum = 1,
                Maximum = 9999,
                Value = 60
            };
            grpSpam.Controls.Add(nudCharsMute);

            lblCharsMuteSuffix = new Label
            {
                Text = "字符禁言",
                Location = new Point(150, 48),
                AutoSize = true
            };
            grpSpam.Controls.Add(lblCharsMuteSuffix);

            // Row 2: Characters kick threshold
            lblCharsKick = new Label
            {
                Text = "发言字符超过",
                Location = new Point(12, 74),
                AutoSize = true
            };
            grpSpam.Controls.Add(lblCharsKick);

            nudCharsKick = new NumericUpDown
            {
                Location = new Point(95, 71),
                Size = new Size(50, 23),
                Minimum = 1,
                Maximum = 9999,
                Value = 80
            };
            grpSpam.Controls.Add(nudCharsKick);

            lblCharsKickSuffix = new Label
            {
                Text = "字符踢出",
                Location = new Point(150, 74),
                AutoSize = true
            };
            grpSpam.Controls.Add(lblCharsKickSuffix);

            // Row 3: Lines mute threshold
            lblLinesMute = new Label
            {
                Text = "发言行数超过",
                Location = new Point(12, 100),
                AutoSize = true
            };
            grpSpam.Controls.Add(lblLinesMute);

            nudLinesMute = new NumericUpDown
            {
                Location = new Point(95, 97),
                Size = new Size(50, 23),
                Minimum = 1,
                Maximum = 99,
                Value = 4
            };
            grpSpam.Controls.Add(nudLinesMute);

            lblLinesMuteSuffix = new Label
            {
                Text = "行禁言",
                Location = new Point(150, 100),
                AutoSize = true
            };
            grpSpam.Controls.Add(lblLinesMuteSuffix);

            // Row 4: Image/Emoji checkbox + kick count
            chkImageEmoji = new CheckBox
            {
                Text = "图片表情禁言，超过",
                Location = new Point(12, 126),
                Size = new Size(135, 20),
                Checked = true
            };
            grpSpam.Controls.Add(chkImageEmoji);

            nudImageKickCount = new NumericUpDown
            {
                Location = new Point(150, 124),
                Size = new Size(45, 23),
                Minimum = 1,
                Maximum = 999,
                Value = 6
            };
            grpSpam.Controls.Add(nudImageKickCount);

            lblImageKickSuffix = new Label
            {
                Text = "次踢出",
                Location = new Point(200, 126),
                AutoSize = true
            };
            grpSpam.Controls.Add(lblImageKickSuffix);

            // Row 5: Bill @分 mute checkbox
            chkBillAtMute = new CheckBox
            {
                Text = "账单0分除了上分，其他发言一律禁言",
                Location = new Point(12, 150),
                Size = new Size(250, 20)
            };
            grpSpam.Controls.Add(chkBillAtMute);

            // Row 6: Auto recall checkbox
            chkFollowBotAutoRecall = new CheckBox
            {
                Text = "违规自动撤回",
                Location = new Point(12, 174),
                Size = new Size(120, 20)
            };
            grpSpam.Controls.Add(chkFollowBotAutoRecall);

            // Keywords DataGridView
            // Enable grid lines (dashed style) and editable cells
            dgvKeywords = new DataGridView
            {
                Location = new Point(10, 200),
                Size = new Size(175, 180),
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                RowHeadersVisible = false,
                MultiSelect = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                BackgroundColor = Color.White,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize,
                // Grid line settings
                GridColor = Color.LightGray,
                CellBorderStyle = DataGridViewCellBorderStyle.Single,
                BorderStyle = BorderStyle.FixedSingle,
                // Enable editing
                ReadOnly = false,
                EditMode = DataGridViewEditMode.EditOnKeystrokeOrF2
            };
            // Column: Keyword (editable)
            var colKeyword = new DataGridViewTextBoxColumn
            {
                Name = "colKeyword",
                HeaderText = "关键词",
                FillWeight = 60,
                ReadOnly = false,
                DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleLeft }
            };
            // Column: Action (editable with ComboBox)
            var colAction = new DataGridViewComboBoxColumn
            {
                Name = "colAction",
                HeaderText = "处理",
                FillWeight = 40,
                FlatStyle = FlatStyle.Flat,
                DisplayStyle = DataGridViewComboBoxDisplayStyle.DropDownButton
            };
            colAction.Items.AddRange("禁言", "踢出");
            dgvKeywords.Columns.Add(colKeyword);
            dgvKeywords.Columns.Add(colAction);
            dgvKeywords.CellEndEdit += DgvKeywords_CellEndEdit;
            grpSpam.Controls.Add(dgvKeywords);

            // Right side: Keyword input area
            lblKeyword = new Label
            {
                Text = "关键词",
                Location = new Point(195, 200),
                AutoSize = true
            };
            grpSpam.Controls.Add(lblKeyword);

            txtKeyword = new TextBox
            {
                Location = new Point(245, 197),
                Size = new Size(155, 23)
            };
            grpSpam.Controls.Add(txtKeyword);

            // Action GroupBox with radio buttons
            grpAction = new GroupBox
            {
                Text = "处理",
                Location = new Point(195, 228),
                Size = new Size(130, 45)
            };
            grpSpam.Controls.Add(grpAction);

            rbMute = new RadioButton
            {
                Text = "禁言",
                Location = new Point(10, 18),
                Size = new Size(55, 20),
                Checked = true
            };
            grpAction.Controls.Add(rbMute);

            rbKick = new RadioButton
            {
                Text = "踢出",
                Location = new Point(70, 18),
                Size = new Size(55, 20)
            };
            grpAction.Controls.Add(rbKick);

            // Add/Remove keyword buttons
            btnAddKeyword = new Button
            {
                Text = "添加关键词",
                Location = new Point(330, 230),
                Size = new Size(80, 28)
            };
            btnAddKeyword.Click += (s, e) => AddKeywordRule();
            grpSpam.Controls.Add(btnAddKeyword);

            btnRemoveKeyword = new Button
            {
                Text = "删除关键词",
                Location = new Point(330, 265),
                Size = new Size(80, 28)
            };
            btnRemoveKeyword.Click += (s, e) => RemoveKeywordRule();
            grpSpam.Controls.Add(btnRemoveKeyword);

            // Bottom: Mute duration
            lblMuteMinutes = new Label
            {
                Text = "禁言",
                Location = new Point(10, 390),
                AutoSize = true
            };
            grpSpam.Controls.Add(lblMuteMinutes);

            nudMuteMinutes = new NumericUpDown
            {
                Location = new Point(45, 387),
                Size = new Size(55, 23),
                Minimum = 1,
                Maximum = 9999,
                Value = 10
            };
            grpSpam.Controls.Add(nudMuteMinutes);

            lblMuteMinutesSuffix = new Label
            {
                Text = "分钟",
                Location = new Point(105, 390),
                AutoSize = true
            };
            grpSpam.Controls.Add(lblMuteMinutesSuffix);

            // Save button (bottom right)
            btnSave = new Button
            {
                Text = "保存设置",
                Location = new Point(310, 385),
                Size = new Size(95, 30)
            };
            btnSave.Click += (s, e) => SaveAll();
            grpSpam.Controls.Add(btnSave);

            ResumeLayout(false);
        }

        private void LoadData()
        {
            // Load blacklist
            RefreshBlacklistGrid();

            // Load candidates (users kicked by bot, waiting to be added)
            RefreshCandidates();

            // Load spam settings
            var s = SpamSettingsService.Instance;
            chkAutoBlacklistBotKick.Checked = s.AutoAddBlacklistOnBotKick;
            chkAutoBlacklistAdminKick.Checked = s.AutoAddBlacklistOnAdminKick;
            nudCharsMute.Value = ClampValue(s.MaxCharsMute, nudCharsMute);
            nudCharsKick.Value = ClampValue(s.MaxCharsKick, nudCharsKick);
            nudLinesMute.Value = ClampValue(s.MaxLinesMute, nudLinesMute);
            chkImageEmoji.Checked = s.ImageEmojiEnabled;
            nudImageKickCount.Value = ClampValue(s.ImageEmojiKickCount, nudImageKickCount);
            nudMuteMinutes.Value = ClampValue(s.MuteMinutes, nudMuteMinutes);
            chkBillAtMute.Checked = s.BillAtMute;
            chkFollowBotAutoRecall.Checked = s.FollowBotAutoRecall;

            // Load keyword rules
            RefreshKeywordGrid();
        }

        private decimal ClampValue(int v, NumericUpDown nud)
        {
            if (v < (int)nud.Minimum) return nud.Minimum;
            if (v > (int)nud.Maximum) return nud.Maximum;
            return v;
        }

        private void RefreshBlacklistGrid()
        {
            dgvBlacklist.Rows.Clear();
            var list = ConfigService.Instance.Config?.Blacklist ?? new List<string>();
            for (int i = 0; i < list.Count; i++)
            {
                dgvBlacklist.Rows.Add((i + 1).ToString(), list[i]);
            }
        }

        private void RefreshCandidates()
        {
            lstCandidates.Items.Clear();
            foreach (var id in SpamSettingsService.Instance.LoadBlacklistCandidates())
            {
                lstCandidates.Items.Add(id);
            }
        }

        private void ClearAllBlacklist()
        {
            var cfg = ConfigService.Instance.Config;
            if (cfg == null) return;

            var result = MessageBox.Show(
                "确定要全部删除黑名单吗？",
                "确认",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result != DialogResult.Yes) return;

            cfg.Blacklist = new List<string>();
            ConfigService.Instance.SaveConfig();
            RefreshBlacklistGrid();
        }

        private void RemoveSelectedBlacklist()
        {
            var cfg = ConfigService.Instance.Config;
            if (cfg == null || cfg.Blacklist == null) return;
            if (dgvBlacklist.SelectedRows.Count == 0)
            {
                MessageBox.Show("请先选择要移除的黑名单", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var id = dgvBlacklist.SelectedRows[0].Cells[1].Value?.ToString() ?? "";
            if (string.IsNullOrEmpty(id)) return;

            cfg.Blacklist.Remove(id);
            ConfigService.Instance.SaveConfig();
            RefreshBlacklistGrid();
        }

        private void AddSelectedCandidateToBlacklist()
        {
            var cfg = ConfigService.Instance.Config;
            if (cfg == null) return;
            if (cfg.Blacklist == null) cfg.Blacklist = new List<string>();

            var id = lstCandidates.SelectedItem?.ToString();
            id = (id ?? "").Trim();

            if (string.IsNullOrEmpty(id))
            {
                MessageBox.Show("请先选择要添加的用户", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (!cfg.Blacklist.Contains(id))
            {
                cfg.Blacklist.Add(id);
            ConfigService.Instance.SaveConfig();
            RefreshBlacklistGrid();
            }
        }

        private void RefreshKeywordGrid()
        {
            dgvKeywords.Rows.Clear();
            var rules = SpamSettingsService.Instance.LoadKeywordRules();
            foreach (var r in rules)
            {
                dgvKeywords.Rows.Add(r.Keyword, r.Action == SpamAction.Kick ? "踢出" : "禁言");
            }
        }

        private void AddKeywordRule()
        {
            var keyword = (txtKeyword.Text ?? "").Trim();
            if (string.IsNullOrEmpty(keyword))
            {
                MessageBox.Show("请输入关键词", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var action = rbKick.Checked ? SpamAction.Kick : SpamAction.Mute;
            var rules = SpamSettingsService.Instance.LoadKeywordRules();

            // Upsert: update if exists, add if new
            var existing = rules.FirstOrDefault(x =>
                string.Equals((x.Keyword ?? "").Trim(), keyword, StringComparison.OrdinalIgnoreCase));

            if (existing == null)
            {
                rules.Add(new SpamKeywordRule { Keyword = keyword, Action = action });
            }
            else
            {
                existing.Action = action;
            }

            SpamSettingsService.Instance.SaveKeywordRules(rules);
            RefreshKeywordGrid();
            txtKeyword.Clear();
        }

        private void RemoveKeywordRule()
        {
            if (dgvKeywords.SelectedRows.Count == 0)
            {
                MessageBox.Show("请先选择要删除的关键词", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var keyword = dgvKeywords.SelectedRows[0].Cells[0].Value?.ToString() ?? "";
            keyword = keyword.Trim();
            if (string.IsNullOrEmpty(keyword)) return;

            var rules = SpamSettingsService.Instance.LoadKeywordRules();
            rules = rules.Where(r =>
                !string.Equals((r.Keyword ?? "").Trim(), keyword, StringComparison.OrdinalIgnoreCase)).ToList();

            SpamSettingsService.Instance.SaveKeywordRules(rules);
            RefreshKeywordGrid();
        }

        private void SaveAll()
        {
            var s = SpamSettingsService.Instance;
            s.AutoAddBlacklistOnBotKick = chkAutoBlacklistBotKick.Checked;
            s.AutoAddBlacklistOnAdminKick = chkAutoBlacklistAdminKick.Checked;
            s.MaxCharsMute = (int)nudCharsMute.Value;
            s.MaxCharsKick = (int)nudCharsKick.Value;
            s.MaxLinesMute = (int)nudLinesMute.Value;
            s.ImageEmojiEnabled = chkImageEmoji.Checked;
            s.ImageEmojiKickCount = (int)nudImageKickCount.Value;
            s.MuteMinutes = (int)nudMuteMinutes.Value;
            s.BillAtMute = chkBillAtMute.Checked;
            s.FollowBotAutoRecall = chkFollowBotAutoRecall.Checked;

            MessageBox.Show("设置已保存", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        /// <summary>
        /// Handle blacklist grid cell edit - save changes to config
        /// </summary>
        private void DgvBlacklist_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            if (e.ColumnIndex != 1) return; // Only handle "黑名单号" column

            var cfg = ConfigService.Instance.Config;
            if (cfg == null || cfg.Blacklist == null) return;

            // Rebuild blacklist from grid
            var newList = new List<string>();
            for (int i = 0; i < dgvBlacklist.Rows.Count; i++)
            {
                var id = dgvBlacklist.Rows[i].Cells[1].Value?.ToString()?.Trim() ?? "";
                if (!string.IsNullOrEmpty(id))
                {
                    newList.Add(id);
    }
}
            cfg.Blacklist = newList;
            ConfigService.Instance.SaveConfig();
        }

        /// <summary>
        /// Handle keywords grid cell edit - save changes to file
        /// </summary>
        private void DgvKeywords_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            // Rebuild keyword rules from grid
            var rules = new List<SpamKeywordRule>();
            for (int i = 0; i < dgvKeywords.Rows.Count; i++)
            {
                var keyword = dgvKeywords.Rows[i].Cells[0].Value?.ToString()?.Trim() ?? "";
                var actionStr = dgvKeywords.Rows[i].Cells[1].Value?.ToString() ?? "禁言";
                
                if (!string.IsNullOrEmpty(keyword))
                {
                    rules.Add(new SpamKeywordRule
                    {
                        Keyword = keyword,
                        Action = actionStr == "踢出" ? SpamAction.Kick : SpamAction.Mute
                    });
                }
            }
            SpamSettingsService.Instance.SaveKeywordRules(rules);
        }
    }
}
