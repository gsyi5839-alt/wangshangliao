using System;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Text;
using System.Windows.Forms;
using WangShangLiaoBot.Services;

namespace WangShangLiaoBot.Controls.RebateTool
{
    /// <summary>
    /// 回水设置面板 - 按设计图实现，自适应布局
    /// </summary>
    public sealed class RebateSettingsPanel : UserControl
    {
        private readonly Color BorderColor = Color.FromArgb(180, 180, 180);

        // 计算模式单选按钮
        private RadioButton rbCombineRatio;     // 组合比例计算
        private RadioButton rbBetCount;         // 下注次数计算
        private RadioButton rbBetFlow;          // 下注流水计算
        private RadioButton rbBetLoss;          // 下注输分计算

        // 主配置区域
        private GroupBox grpConfig;
        private TextBox txtMinBetCount;         // 下注次数大于
        private RebateRangeRow[] rangeRows;     // 8行范围配置
        private RadioButton rbPercent;          // 百分比
        private RadioButton rbFixedValue;       // 固定值
        private CheckBox chkKillComboNotCount;  // 杀组合不算入下注输分
        private CheckBox chkMultiComboNotCount; // 多组合不算入下注输分

        // 底部
        private CheckBox chkNotifyBillGroup;    // 加入账单群内通知
        private Button btnSave;                 // 保存设置

        // 特殊设置区域
        private GroupBox grpSpecial;
        private CheckBox chk13_14NoFlow;        // 开13/14，所有下注 不计算流水
        private CheckBox chkDuiShunBaoNoFlow;   // 开对/顺/豹，所有下注 不计算流水
        private CheckBox chk13_14SplitNoFlow;   // 开13，小、单、小单不计算流水 / 开14，大、双、大双不计算流水
        private CheckBox chkHuiBenNoFlow;       // 开回本，所有下注 不计算流水
        private CheckBox chkAllSingleNoFlow;    // 所有单注不计入流水
        private CheckBox chkNoComboSingleNoFlow;// 无组合时，所有单注不计入流水
        private CheckBox chkOnlySpecialCodeFlow;// 只计算特码下注流水

        public RebateSettingsPanel()
        {
            SuspendLayout();
            BackColor = Color.White;
            Dock = DockStyle.Fill;
            Font = new Font("Microsoft YaHei UI", 9F);

            CreateModeRadios();
            CreateConfigGroup();
            CreateSpecialSettings();
            CreateBottomControls();

            ResumeLayout(false);

            // 加载保存的设置
            LoadSettings();
        }

        private void CreateModeRadios()
        {
            int y = 8;
            int x = 10;
            int spacing = 115;

            rbCombineRatio = new RadioButton
            {
                Text = "组合比例计算",
                Location = new Point(x, y),
                AutoSize = true
            };
            Controls.Add(rbCombineRatio);

            rbBetCount = new RadioButton
            {
                Text = "下注次数计算",
                Location = new Point(x + spacing, y),
                AutoSize = true
            };
            Controls.Add(rbBetCount);

            rbBetFlow = new RadioButton
            {
                Text = "下注流水计算",
                Location = new Point(x + spacing * 2, y),
                AutoSize = true
            };
            Controls.Add(rbBetFlow);

            rbBetLoss = new RadioButton
            {
                Text = "下注输分计算",
                Location = new Point(x + spacing * 3, y),
                AutoSize = true,
                Checked = true  // 默认选中
            };
            Controls.Add(rbBetLoss);
        }

        private void CreateConfigGroup()
        {
            grpConfig = new GroupBox
            {
                Location = new Point(5, 32),
                Text = "",
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            Controls.Add(grpConfig);

            // Dynamic width to fill available space, increased height
            grpConfig.Size = new Size(ClientSize.Width - 10, 210);
            Resize += (s, e) => grpConfig.Width = ClientSize.Width - 10;

            // 左侧：下注次数大于
            var lblMinBet = new Label
            {
                Text = "下注次数大于",
                Location = new Point(8, 18),
                AutoSize = true
            };
            grpConfig.Controls.Add(lblMinBet);

            txtMinBetCount = new TextBox
            {
                Location = new Point(90, 15),
                Size = new Size(35, 23),
                Text = "1"
            };
            grpConfig.Controls.Add(txtMinBetCount);

            // 配置行标题
            var lblRange1 = new Label { Text = "下注输分", Location = new Point(140, 18), AutoSize = true };
            var lblDash1 = new Label { Text = "-", Location = new Point(260, 18), AutoSize = true };
            var lblRate1 = new Label { Text = "回水百分之", Location = new Point(340, 18), AutoSize = true };
            grpConfig.Controls.Add(lblRange1);
            grpConfig.Controls.Add(lblDash1);
            grpConfig.Controls.Add(lblRate1);

            // 右侧配置 - 与输入区域保持足够间距
            rbPercent = new RadioButton
            {
                Text = "百分比",
                Location = new Point(540, 8),
                AutoSize = true,
                Checked = true
            };
            grpConfig.Controls.Add(rbPercent);

            rbFixedValue = new RadioButton
            {
                Text = "固定值",
                Location = new Point(610, 8),
                AutoSize = true
            };
            grpConfig.Controls.Add(rbFixedValue);

            // 创建8行范围配置
            rangeRows = new RebateRangeRow[8];
            int rowY = 38;
            int rowSpacing = 17;

            for (int i = 0; i < 8; i++)
            {
                rangeRows[i] = new RebateRangeRow();
                rangeRows[i].CreateControls(grpConfig, rowY + i * rowSpacing, i == 0);

                // 设置默认值（前两行）
                if (i == 0)
                {
                    rangeRows[i].SetValues("50", "500", "8");
                }
                else if (i == 1)
                {
                    rangeRows[i].SetValues("500", "1000000", "10");
                }
            }

            // 右侧复选框 - 与输入区域保持足够间距
            chkKillComboNotCount = new CheckBox
            {
                Text = "杀组合不算入下注输分",
                Location = new Point(540, 35),
                AutoSize = true
            };
            grpConfig.Controls.Add(chkKillComboNotCount);

            chkMultiComboNotCount = new CheckBox
            {
                Text = "多组合不算入下注输分",
                Location = new Point(540, 58),
                AutoSize = true
            };
            grpConfig.Controls.Add(chkMultiComboNotCount);
        }

        private void CreateBottomControls()
        {
            // 加入账单群内通知 - fixed position below config group
            chkNotifyBillGroup = new CheckBox
            {
                Text = "加入账单群内通知",
                Location = new Point(5, 248),
                AutoSize = true
            };
            Controls.Add(chkNotifyBillGroup);

            // 保存设置按钮 - anchored to right, aligned with grpConfig right edge
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

            // Position save button - align with right edge of grpConfig
            btnSave.Location = new Point(ClientSize.Width - btnSave.Width - 10, 245);
            Resize += (s, e) =>
            {
                btnSave.Location = new Point(ClientSize.Width - btnSave.Width - 10, 245);
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

            // Dynamic size to fill available space
            grpSpecial.Size = new Size(ClientSize.Width - 10, ClientSize.Height - 285);
            Resize += (s, e) => grpSpecial.Size = new Size(ClientSize.Width - 10, Math.Max(180, ClientSize.Height - 285));

            int y = 18;
            int spacing = 21;

            chk13_14NoFlow = new CheckBox
            {
                Text = "开13/14，所有下注 不计算流水",
                Location = new Point(8, y),
                AutoSize = true
            };
            grpSpecial.Controls.Add(chk13_14NoFlow);

            y += spacing;
            chkDuiShunBaoNoFlow = new CheckBox
            {
                Text = "开对/顺/豹，所有下注 不计算流水",
                Location = new Point(8, y),
                AutoSize = true
            };
            grpSpecial.Controls.Add(chkDuiShunBaoNoFlow);

            y += spacing;
            chk13_14SplitNoFlow = new CheckBox
            {
                Text = "开13，小、单、小单不计算流水\r\n开14，大、双、大双不计算流水",
                Location = new Point(8, y),
                Size = new Size(250, 34)
            };
            grpSpecial.Controls.Add(chk13_14SplitNoFlow);

            y += 36;
            chkHuiBenNoFlow = new CheckBox
            {
                Text = "开回本，所有下注 不计算流水",
                Location = new Point(8, y),
                AutoSize = true
            };
            grpSpecial.Controls.Add(chkHuiBenNoFlow);

            y += spacing;
            chkAllSingleNoFlow = new CheckBox
            {
                Text = "所有单注不计入流水",
                Location = new Point(8, y),
                AutoSize = true
            };
            grpSpecial.Controls.Add(chkAllSingleNoFlow);

            y += spacing;
            chkNoComboSingleNoFlow = new CheckBox
            {
                Text = "无组合时，所有单注不计入流水",
                Location = new Point(8, y),
                AutoSize = true
            };
            grpSpecial.Controls.Add(chkNoComboSingleNoFlow);

            y += spacing;
            chkOnlySpecialCodeFlow = new CheckBox
            {
                Text = "只计算特码下注流水",
                Location = new Point(8, y),
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

            // 计算模式
            string mode = "BetLoss";
            if (rbCombineRatio.Checked) mode = "CombineRatio";
            else if (rbBetCount.Checked) mode = "BetCount";
            else if (rbBetFlow.Checked) mode = "BetFlow";
            ds.SaveSetting("RebateTool:CalcMode", mode);

            // 下注次数大于
            ds.SaveSetting("RebateTool:MinBetCount", txtMinBetCount.Text);

            // 百分比/固定值
            ds.SaveSetting("RebateTool:RateType", rbPercent.Checked ? "Percent" : "Fixed");

            // 复选框
            ds.SaveSetting("RebateTool:KillComboNotCount", chkKillComboNotCount.Checked ? "1" : "0");
            ds.SaveSetting("RebateTool:MultiComboNotCount", chkMultiComboNotCount.Checked ? "1" : "0");
            ds.SaveSetting("RebateTool:NotifyBillGroup", chkNotifyBillGroup.Checked ? "1" : "0");

            // 范围配置行
            for (int i = 0; i < rangeRows.Length; i++)
            {
                var row = rangeRows[i];
                ds.SaveSetting($"RebateTool:Range{i}:Min", row.GetMin());
                ds.SaveSetting($"RebateTool:Range{i}:Max", row.GetMax());
                ds.SaveSetting($"RebateTool:Range{i}:Rate", row.GetRate());
            }

            // 特殊设置
            ds.SaveSetting("RebateTool:Sp13_14NoFlow", chk13_14NoFlow.Checked ? "1" : "0");
            ds.SaveSetting("RebateTool:SpDuiShunBaoNoFlow", chkDuiShunBaoNoFlow.Checked ? "1" : "0");
            ds.SaveSetting("RebateTool:Sp13_14SplitNoFlow", chk13_14SplitNoFlow.Checked ? "1" : "0");
            ds.SaveSetting("RebateTool:SpHuiBenNoFlow", chkHuiBenNoFlow.Checked ? "1" : "0");
            ds.SaveSetting("RebateTool:SpAllSingleNoFlow", chkAllSingleNoFlow.Checked ? "1" : "0");
            ds.SaveSetting("RebateTool:SpNoComboSingleNoFlow", chkNoComboSingleNoFlow.Checked ? "1" : "0");
            ds.SaveSetting("RebateTool:SpOnlySpecialCodeFlow", chkOnlySpecialCodeFlow.Checked ? "1" : "0");
        }

        private void LoadSettings()
        {
            var ds = DataService.Instance;

            // 计算模式
            var mode = ds.GetSetting("RebateTool:CalcMode", "BetLoss");
            switch (mode)
            {
                case "CombineRatio": rbCombineRatio.Checked = true; break;
                case "BetCount": rbBetCount.Checked = true; break;
                case "BetFlow": rbBetFlow.Checked = true; break;
                default: rbBetLoss.Checked = true; break;
            }

            // 下注次数大于
            txtMinBetCount.Text = ds.GetSetting("RebateTool:MinBetCount", "1");

            // 百分比/固定值
            var rateType = ds.GetSetting("RebateTool:RateType", "Percent");
            rbPercent.Checked = (rateType == "Percent");
            rbFixedValue.Checked = (rateType == "Fixed");

            // 复选框
            chkKillComboNotCount.Checked = ds.GetSetting("RebateTool:KillComboNotCount", "0") == "1";
            chkMultiComboNotCount.Checked = ds.GetSetting("RebateTool:MultiComboNotCount", "0") == "1";
            chkNotifyBillGroup.Checked = ds.GetSetting("RebateTool:NotifyBillGroup", "0") == "1";

            // 范围配置行
            for (int i = 0; i < rangeRows.Length; i++)
            {
                var min = ds.GetSetting($"RebateTool:Range{i}:Min", "");
                var max = ds.GetSetting($"RebateTool:Range{i}:Max", "");
                var rate = ds.GetSetting($"RebateTool:Range{i}:Rate", "");
                
                // 如果没有保存的值，使用默认值
                if (i == 0 && string.IsNullOrEmpty(min))
                {
                    min = "50"; max = "500"; rate = "8";
                }
                else if (i == 1 && string.IsNullOrEmpty(min))
                {
                    min = "500"; max = "1000000"; rate = "10";
                }
                
                rangeRows[i].SetValues(min, max, rate);
            }

            // 特殊设置
            chk13_14NoFlow.Checked = ds.GetSetting("RebateTool:Sp13_14NoFlow", "0") == "1";
            chkDuiShunBaoNoFlow.Checked = ds.GetSetting("RebateTool:SpDuiShunBaoNoFlow", "0") == "1";
            chk13_14SplitNoFlow.Checked = ds.GetSetting("RebateTool:Sp13_14SplitNoFlow", "0") == "1";
            chkHuiBenNoFlow.Checked = ds.GetSetting("RebateTool:SpHuiBenNoFlow", "0") == "1";
            chkAllSingleNoFlow.Checked = ds.GetSetting("RebateTool:SpAllSingleNoFlow", "0") == "1";
            chkNoComboSingleNoFlow.Checked = ds.GetSetting("RebateTool:SpNoComboSingleNoFlow", "0") == "1";
            chkOnlySpecialCodeFlow.Checked = ds.GetSetting("RebateTool:SpOnlySpecialCodeFlow", "0") == "1";
        }

        public void RefreshData(DateTime startTime, DateTime endTime)
        {
            // 重新加载设置（如有必要）
        }
    }

    /// <summary>
    /// 回水范围配置行
    /// </summary>
    internal sealed class RebateRangeRow
    {
        private Label lblPrefix;
        private TextBox txtMin;
        private Label lblDash;
        private TextBox txtMax;
        private Label lblRatePrefix;
        private TextBox txtRate;

        public void CreateControls(Control parent, int y, bool showLabel)
        {
            int x = 140;

            lblPrefix = new Label
            {
                Text = "下注输分",
                Location = new Point(x, y + 2),
                AutoSize = true,
                Visible = !showLabel  // 第一行已有标题，后面行显示
            };
            parent.Controls.Add(lblPrefix);

            txtMin = new TextBox
            {
                Location = new Point(x + 52, y),
                Size = new Size(60, 21)
            };
            parent.Controls.Add(txtMin);

            lblDash = new Label
            {
                Text = "-",
                Location = new Point(x + 117, y + 2),
                AutoSize = true
            };
            parent.Controls.Add(lblDash);

            txtMax = new TextBox
            {
                Location = new Point(x + 130, y),
                Size = new Size(70, 21)
            };
            parent.Controls.Add(txtMax);

            lblRatePrefix = new Label
            {
                Text = "回水百分之",
                Location = new Point(x + 208, y + 2),
                AutoSize = true
            };
            parent.Controls.Add(lblRatePrefix);

            txtRate = new TextBox
            {
                Location = new Point(x + 278, y),
                Size = new Size(40, 21)
            };
            parent.Controls.Add(txtRate);
        }

        public void SetValues(string min, string max, string rate)
        {
            if (txtMin != null) txtMin.Text = min;
            if (txtMax != null) txtMax.Text = max;
            if (txtRate != null) txtRate.Text = rate;
        }

        public string GetMin() => txtMin?.Text ?? "";
        public string GetMax() => txtMax?.Text ?? "";
        public string GetRate() => txtRate?.Text ?? "";
    }
}
