using System;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using WangShangLiaoBot.Services;

namespace WangShangLiaoBot.Controls.RebateTool
{
    /// <summary>
    /// 夜宵设置面板 - Night snack settings panel
    /// </summary>
    public sealed class NightSnackSettingsPanel : UserControl
    {
        private readonly Color BorderColor = Color.FromArgb(180, 180, 180);

        // Top - calculation mode radios
        private RadioButton rbCountMode;      // 把数模式计算
        private RadioButton rbFlowMode;       // 流水模式计算
        private RadioButton rbWinLoseMode;    // 输赢模式计算

        // Main config container
        private GroupBox grpConfig;

        // Left - rules list
        private ListBox lstRules;

        // Right - help text (editable TextBox)
        private TextBox txtHelpRules;

        // Bottom inputs
        private Label lblMaxSingleBet;
        private TextBox txtMaxSingleBet;
        private Label lblTotalBet;
        private TextBox txtTotalBet;
        private Label lblCountPerRound;
        private TextBox txtCountPerRound;
        private Label lblJoinNightSnack;

        // Middle controls
        private CheckBox chkNotifyBillGroup;
        private Button btnSave;

        // Special settings
        private GroupBox grpSpecial;
        private CheckBox chk13_14NoFlow;
        private CheckBox chkDuiShunBaoNoFlow;
        private CheckBox chk13_14SplitNoFlow;
        private CheckBox chkHuiBenNoFlow;
        private CheckBox chkAllSingleNoFlow;
        private CheckBox chkNoComboSingleNoFlow;
        private CheckBox chkOnlySpecialCodeFlow;

        public NightSnackSettingsPanel()
        {
            SuspendLayout();
            BackColor = Color.White;
            Dock = DockStyle.Fill;
            Font = new Font("Microsoft YaHei UI", 9F);

            CreateModeRadios();
            CreateRulesSection();
            CreateMiddleControls();
            CreateSpecialSettings();

            ResumeLayout(false);

            LoadSettings();
        }

        private void CreateModeRadios()
        {
            int y = 8;
            int x = 10;
            int spacing = 120;

            rbCountMode = new RadioButton
            {
                Text = "把数模式计算",
                Location = new Point(x, y),
                AutoSize = true
            };
            Controls.Add(rbCountMode);

            rbFlowMode = new RadioButton
            {
                Text = "流水模式计算",
                Location = new Point(x + spacing, y),
                AutoSize = true,
                Checked = true
            };
            Controls.Add(rbFlowMode);

            rbWinLoseMode = new RadioButton
            {
                Text = "输赢模式计算",
                Location = new Point(x + spacing * 2, y),
                AutoSize = true
            };
            Controls.Add(rbWinLoseMode);
        }

        private void CreateRulesSection()
        {
            // Main container GroupBox
            grpConfig = new GroupBox
            {
                Text = "",
                Location = new Point(5, 35),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            Controls.Add(grpConfig);

            // Dynamic size
            grpConfig.Size = new Size(ClientSize.Width - 10, 200);
            Resize += (s, e) =>
            {
                grpConfig.Size = new Size(ClientSize.Width - 10, 200);
            };

            // Rules list on left (inside container)
            lstRules = new ListBox
            {
                Location = new Point(10, 15),
                Size = new Size(180, 140),
                BorderStyle = BorderStyle.FixedSingle,
                ScrollAlwaysVisible = true
            };
            grpConfig.Controls.Add(lstRules);

            // Default rules
            lstRules.Items.AddRange(new object[] {
                "60000=128",
                "50000=108",
                "40000=88",
                "30000=58",
                "22000=48",
                "16000=38"
            });

            // Help text on right (inside container) - editable TextBox
            txtHelpRules = new TextBox
            {
                Location = new Point(210, 15),
                Size = new Size(280, 140),
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                BorderStyle = BorderStyle.FixedSingle,
                Text = "填写规则：\r\n如总流水大于等于8888奖励58\r\n如总流水大于等于28888奖励128\r\n则填写\r\n28888=128\r\n8888=58\r\n规则请从多到少规范填写，每行一个"
            };
            grpConfig.Controls.Add(txtHelpRules);

            // Bottom inputs (inside container)
            int inputY = 160;

            lblMaxSingleBet = new Label
            {
                Text = "最大单个下注≥",
                Location = new Point(10, inputY + 3),
                AutoSize = true
            };
            grpConfig.Controls.Add(lblMaxSingleBet);

            txtMaxSingleBet = new TextBox
            {
                Location = new Point(105, inputY),
                Size = new Size(60, 23),
                Text = "10"
            };
            grpConfig.Controls.Add(txtMaxSingleBet);

            lblTotalBet = new Label
            {
                Text = "总注≥",
                Location = new Point(175, inputY + 3),
                AutoSize = true
            };
            grpConfig.Controls.Add(lblTotalBet);

            txtTotalBet = new TextBox
            {
                Location = new Point(220, inputY),
                Size = new Size(80, 23)
            };
            grpConfig.Controls.Add(txtTotalBet);

            lblCountPerRound = new Label
            {
                Text = "分算一把，把数≥",
                Location = new Point(320, inputY + 3),
                AutoSize = true
            };
            grpConfig.Controls.Add(lblCountPerRound);

            txtCountPerRound = new TextBox
            {
                Location = new Point(430, inputY),
                Size = new Size(60, 23),
                Text = "18"
            };
            grpConfig.Controls.Add(txtCountPerRound);

            lblJoinNightSnack = new Label
            {
                Text = "参与夜宵",
                Location = new Point(510, inputY + 3),
                AutoSize = true
            };
            grpConfig.Controls.Add(lblJoinNightSnack);
        }


        private void CreateMiddleControls()
        {
            int y = 245;

            chkNotifyBillGroup = new CheckBox
            {
                Text = "加入账单群内通知",
                Location = new Point(10, y),
                AutoSize = true
            };
            Controls.Add(chkNotifyBillGroup);

            btnSave = new Button
            {
                Text = "保存设置",
                Size = new Size(80, 26),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.White,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            btnSave.FlatAppearance.BorderColor = BorderColor;
            btnSave.Click += BtnSave_Click;
            Controls.Add(btnSave);

            // Position save button
            btnSave.Location = new Point(ClientSize.Width - btnSave.Width - 15, y - 3);
            Resize += (s, e) =>
            {
                btnSave.Location = new Point(ClientSize.Width - btnSave.Width - 15, y - 3);
            };
        }

        private void CreateSpecialSettings()
        {
            grpSpecial = new GroupBox
            {
                Text = "特殊设置",
                Location = new Point(5, 275),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom
            };
            Controls.Add(grpSpecial);

            // Dynamic size
            grpSpecial.Size = new Size(ClientSize.Width - 10, Math.Max(180, ClientSize.Height - 285));
            Resize += (s, e) =>
            {
                grpSpecial.Size = new Size(ClientSize.Width - 10, Math.Max(180, ClientSize.Height - 285));
            };

            int y = 20;
            int spacing = 24;

            chk13_14NoFlow = new CheckBox
            {
                Text = "开13/14，所有下注 不计算流水",
                Location = new Point(10, y),
                AutoSize = true
            };
            grpSpecial.Controls.Add(chk13_14NoFlow);

            y += spacing;
            chkDuiShunBaoNoFlow = new CheckBox
            {
                Text = "开对/顺/豹，所有下注 不计算流水",
                Location = new Point(10, y),
                AutoSize = true
            };
            grpSpecial.Controls.Add(chkDuiShunBaoNoFlow);

            y += spacing;
            chk13_14SplitNoFlow = new CheckBox
            {
                Text = "开13，小、单、小单不计算流水\r\n开14，大、双、大双不计算流水",
                Location = new Point(10, y),
                Size = new Size(280, 38),
                CheckAlign = ContentAlignment.TopLeft,
                TextAlign = ContentAlignment.TopLeft
            };
            grpSpecial.Controls.Add(chk13_14SplitNoFlow);

            y += 42;
            chkHuiBenNoFlow = new CheckBox
            {
                Text = "开回本，所有下注 不计算流水",
                Location = new Point(10, y),
                AutoSize = true
            };
            grpSpecial.Controls.Add(chkHuiBenNoFlow);

            y += spacing;
            chkAllSingleNoFlow = new CheckBox
            {
                Text = "所有单注不计入流水",
                Location = new Point(10, y),
                AutoSize = true
            };
            grpSpecial.Controls.Add(chkAllSingleNoFlow);

            y += spacing;
            chkNoComboSingleNoFlow = new CheckBox
            {
                Text = "无组合时，所有单注不计入流水",
                Location = new Point(10, y),
                AutoSize = true
            };
            grpSpecial.Controls.Add(chkNoComboSingleNoFlow);

            y += spacing;
            chkOnlySpecialCodeFlow = new CheckBox
            {
                Text = "只计算特码下注流水",
                Location = new Point(10, y),
                AutoSize = true
            };
            grpSpecial.Controls.Add(chkOnlySpecialCodeFlow);
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            SaveSettings();
            MessageBox.Show("设置已保存", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void SaveSettings()
        {
            var ds = DataService.Instance;

            // Calculation mode
            string mode = "Flow";
            if (rbCountMode.Checked) mode = "Count";
            else if (rbWinLoseMode.Checked) mode = "WinLose";
            ds.SaveSetting("NightSnack:CalcMode", mode);

            // Rules
            var sb = new StringBuilder();
            foreach (var item in lstRules.Items)
            {
                if (sb.Length > 0) sb.Append("|");
                sb.Append(item.ToString());
            }
            ds.SaveSetting("NightSnack:Rules", sb.ToString());

            // Help rules text
            ds.SaveSetting("NightSnack:HelpRules", txtHelpRules.Text.Replace("\r\n", "||"));

            // Input values
            ds.SaveSetting("NightSnack:MaxSingleBet", txtMaxSingleBet.Text);
            ds.SaveSetting("NightSnack:TotalBet", txtTotalBet.Text);
            ds.SaveSetting("NightSnack:CountPerRound", txtCountPerRound.Text);

            // Checkboxes
            ds.SaveSetting("NightSnack:NotifyBillGroup", chkNotifyBillGroup.Checked ? "1" : "0");

            // Special settings
            ds.SaveSetting("NightSnack:Sp13_14NoFlow", chk13_14NoFlow.Checked ? "1" : "0");
            ds.SaveSetting("NightSnack:SpDuiShunBaoNoFlow", chkDuiShunBaoNoFlow.Checked ? "1" : "0");
            ds.SaveSetting("NightSnack:Sp13_14SplitNoFlow", chk13_14SplitNoFlow.Checked ? "1" : "0");
            ds.SaveSetting("NightSnack:SpHuiBenNoFlow", chkHuiBenNoFlow.Checked ? "1" : "0");
            ds.SaveSetting("NightSnack:SpAllSingleNoFlow", chkAllSingleNoFlow.Checked ? "1" : "0");
            ds.SaveSetting("NightSnack:SpNoComboSingleNoFlow", chkNoComboSingleNoFlow.Checked ? "1" : "0");
            ds.SaveSetting("NightSnack:SpOnlySpecialCodeFlow", chkOnlySpecialCodeFlow.Checked ? "1" : "0");
        }

        private void LoadSettings()
        {
            var ds = DataService.Instance;

            // Calculation mode
            var mode = ds.GetSetting("NightSnack:CalcMode", "Flow");
            switch (mode)
            {
                case "Count": rbCountMode.Checked = true; break;
                case "WinLose": rbWinLoseMode.Checked = true; break;
                default: rbFlowMode.Checked = true; break;
            }

            // Rules
            var rulesStr = ds.GetSetting("NightSnack:Rules", "");
            if (!string.IsNullOrEmpty(rulesStr))
            {
                lstRules.Items.Clear();
                var rules = rulesStr.Split('|');
                foreach (var rule in rules)
                {
                    if (!string.IsNullOrWhiteSpace(rule))
                        lstRules.Items.Add(rule);
                }
            }

            // Help rules text
            var helpRulesStr = ds.GetSetting("NightSnack:HelpRules", "");
            if (!string.IsNullOrEmpty(helpRulesStr))
            {
                txtHelpRules.Text = helpRulesStr.Replace("||", "\r\n");
            }

            // Input values
            txtMaxSingleBet.Text = ds.GetSetting("NightSnack:MaxSingleBet", "10");
            txtTotalBet.Text = ds.GetSetting("NightSnack:TotalBet", "");
            txtCountPerRound.Text = ds.GetSetting("NightSnack:CountPerRound", "18");

            // Checkboxes
            chkNotifyBillGroup.Checked = ds.GetSetting("NightSnack:NotifyBillGroup", "0") == "1";

            // Special settings
            chk13_14NoFlow.Checked = ds.GetSetting("NightSnack:Sp13_14NoFlow", "0") == "1";
            chkDuiShunBaoNoFlow.Checked = ds.GetSetting("NightSnack:SpDuiShunBaoNoFlow", "0") == "1";
            chk13_14SplitNoFlow.Checked = ds.GetSetting("NightSnack:Sp13_14SplitNoFlow", "0") == "1";
            chkHuiBenNoFlow.Checked = ds.GetSetting("NightSnack:SpHuiBenNoFlow", "0") == "1";
            chkAllSingleNoFlow.Checked = ds.GetSetting("NightSnack:SpAllSingleNoFlow", "0") == "1";
            chkNoComboSingleNoFlow.Checked = ds.GetSetting("NightSnack:SpNoComboSingleNoFlow", "0") == "1";
            chkOnlySpecialCodeFlow.Checked = ds.GetSetting("NightSnack:SpOnlySpecialCodeFlow", "0") == "1";
        }

        public void RefreshData(DateTime startTime, DateTime endTime)
        {
            // Reload settings if needed
        }
    }
}

