using System;
using System.Drawing;
using System.Windows.Forms;
using WangShangLiaoBot.Services;

namespace WangShangLiaoBot.Controls.OtherSettings
{
    /// <summary>
    /// 开奖延迟 - 设置面板
    /// </summary>
    public sealed class LotteryDelayPanel : UserControl
    {
        // Left side controls
        private CheckBox chkDelayEnabled;
        private RadioButton rbCountdown;
        private RadioButton rbLottery;
        private NumericUpDown nudPcEgg;
        private NumericUpDown nudCanada;
        private NumericUpDown nudBit;
        private NumericUpDown nudBeijing;

        // Right side controls
        private CheckBox chkAutoAdjustTime;
        private NumericUpDown nudAutoAdjustInterval;

        private Button btnSave;

        public LotteryDelayPanel()
        {
            BackColor = SystemColors.Control;
            Font = new Font("Microsoft YaHei UI", 9F);
            Dock = DockStyle.Fill;
            AutoScroll = true;

            InitializeUI();
            LoadSettings();
        }

        private void InitializeUI()
        {
            SuspendLayout();

            int leftX = 15;
            int rightX = 300;
            int y = 15;

            // Left side: Delay settings
            chkDelayEnabled = new CheckBox
            {
                Text = "延迟 开/关",
                Location = new Point(leftX, y),
                Size = new Size(85, 20)
            };
            Controls.Add(chkDelayEnabled);

            rbCountdown = new RadioButton
            {
                Text = "倒计时",
                Location = new Point(leftX + 100, y),
                Size = new Size(70, 20),
                Checked = true
            };
            Controls.Add(rbCountdown);

            rbLottery = new RadioButton
            {
                Text = "开奖",
                Location = new Point(leftX + 175, y),
                Size = new Size(60, 20)
            };
            Controls.Add(rbLottery);

            // Right side: Auto adjust time
            chkAutoAdjustTime = new CheckBox
            {
                Text = "自动调整时间 开/关",
                Location = new Point(rightX, y),
                Size = new Size(140, 20)
            };
            Controls.Add(chkAutoAdjustTime);
            y += 30;

            // Lottery delay inputs
            CreateLotteryRow("PC蛋蛋", leftX, y, out nudPcEgg);
            
            // Auto adjust interval
            var lblEvery = new Label
            {
                Text = "每过",
                Location = new Point(rightX, y + 3),
                AutoSize = true
            };
            Controls.Add(lblEvery);

            nudAutoAdjustInterval = new NumericUpDown
            {
                Location = new Point(rightX + 35, y),
                Size = new Size(45, 23),
                Minimum = 1,
                Maximum = 48,
                Value = 12
            };
            Controls.Add(nudAutoAdjustInterval);

            var lblIntervalSuffix = new Label
            {
                Text = "小时，自动联网调整一次时间",
                Location = new Point(rightX + 85, y + 3),
                AutoSize = true
            };
            Controls.Add(lblIntervalSuffix);
            y += 28;

            CreateLotteryRow("加拿大", leftX, y, out nudCanada);
            y += 28;

            CreateLotteryRow("比  特", leftX, y, out nudBit);
            y += 28;

            CreateLotteryRow("北  京", leftX, y, out nudBeijing);
            y += 35;

            // Left side notes
            var lblNote1 = new Label
            {
                Text = "增加一定的倒计时，以抵消等待时间",
                Location = new Point(leftX, y),
                AutoSize = true,
                ForeColor = Color.Gray
            };
            Controls.Add(lblNote1);

            // Right side notes
            var lblRightNote1 = new Label
            {
                Text = "每隔一段时间，测试电脑时间都准确，就不用开启。",
                Location = new Point(rightX, y),
                AutoSize = true,
                ForeColor = Color.Gray
            };
            Controls.Add(lblRightNote1);
            y += 20;

            var lblNote2 = new Label
            {
                Text = "请务必保证开奖前封盘，出现过秒的情况",
                Location = new Point(leftX, y),
                AutoSize = true,
                ForeColor = Color.Gray
            };
            Controls.Add(lblNote2);

            var lblRightNote2 = new Label
            {
                Text = "每次联网获取，都可能因网络卡顿，造成机器卡顿",
                Location = new Point(rightX, y),
                AutoSize = true,
                ForeColor = Color.Gray
            };
            Controls.Add(lblRightNote2);
            y += 25;

            var lblNote3 = new Label
            {
                Text = "注：倒计时=下期开奖时间-当前时间，",
                Location = new Point(leftX, y),
                AutoSize = true,
                ForeColor = Color.Gray
            };
            Controls.Add(lblNote3);

            var lblRightNote3 = new Label
            {
                Text = "开启后，每次启动游戏，都会自动调整一次",
                Location = new Point(rightX, y),
                AutoSize = true,
                ForeColor = Color.Gray
            };
            Controls.Add(lblRightNote3);
            y += 18;

            var lblNote4 = new Label
            {
                Text = "如果电脑时间慢了，也相当于延长倒计时",
                Location = new Point(leftX + 20, y),
                AutoSize = true,
                ForeColor = Color.Gray
            };
            Controls.Add(lblNote4);

            var lblRightNote4 = new Label
            {
                Text = "之后间隔一段时间调整一次，推荐间隔12小时调整一次",
                Location = new Point(rightX, y),
                AutoSize = true,
                ForeColor = Color.Gray
            };
            Controls.Add(lblRightNote4);
            y += 35;

            // Save button (right aligned)
            btnSave = new Button
            {
                Text = "保存设置",
                Location = new Point(480, y),
                Size = new Size(85, 28)
            };
            btnSave.Click += BtnSave_Click;
            Controls.Add(btnSave);

            ResumeLayout(false);
        }

        private void CreateLotteryRow(string name, int x, int y, out NumericUpDown nud)
        {
            var lbl = new Label
            {
                Text = name,
                Location = new Point(x, y + 3),
                Size = new Size(55, 20)
            };
            Controls.Add(lbl);

            nud = new NumericUpDown
            {
                Location = new Point(x + 55, y),
                Size = new Size(50, 23),
                Minimum = 0,
                Maximum = 300,
                Value = 0
            };
            Controls.Add(nud);

            var lblSuffix = new Label
            {
                Text = "秒",
                Location = new Point(x + 110, y + 3),
                AutoSize = true
            };
            Controls.Add(lblSuffix);
        }

        private void LoadSettings()
        {
            var s = LotteryDelayService.Instance;
            
            chkDelayEnabled.Checked = s.DelayEnabled;
            rbCountdown.Checked = s.IsCountdownMode;
            rbLottery.Checked = !s.IsCountdownMode;
            
            nudPcEgg.Value = Math.Max(0, Math.Min(300, s.PcEggDelay));
            nudCanada.Value = Math.Max(0, Math.Min(300, s.CanadaDelay));
            nudBit.Value = Math.Max(0, Math.Min(300, s.BitDelay));
            nudBeijing.Value = Math.Max(0, Math.Min(300, s.BeijingDelay));

            chkAutoAdjustTime.Checked = s.AutoAdjustTimeEnabled;
            nudAutoAdjustInterval.Value = Math.Max(1, Math.Min(48, s.AutoAdjustIntervalHours));
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            var s = LotteryDelayService.Instance;
            
            s.DelayEnabled = chkDelayEnabled.Checked;
            s.IsCountdownMode = rbCountdown.Checked;
            
            s.PcEggDelay = (int)nudPcEgg.Value;
            s.CanadaDelay = (int)nudCanada.Value;
            s.BitDelay = (int)nudBit.Value;
            s.BeijingDelay = (int)nudBeijing.Value;

            s.AutoAdjustTimeEnabled = chkAutoAdjustTime.Checked;
            s.AutoAdjustIntervalHours = (int)nudAutoAdjustInterval.Value;

            MessageBox.Show("开奖延迟设置已保存", "提示", 
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}
