using System;
using System.Drawing;
using System.Windows.Forms;
using WangShangLiaoBot.Services;

namespace WangShangLiaoBot.Controls
{
    /// <summary>
    /// 封盘设置控件 - 管理封盘定时任务
    /// </summary>
    public class SealingSettingsControl : UserControl
    {
        #region 控件

        private GroupBox grpLotteryType;
        private RadioButton rbCanada28;
        private RadioButton rbBit28;
        private RadioButton rbBeijing28;

        private GroupBox grpReminder;
        private CheckBox chkReminderEnabled;
        private Label lblReminderSeconds;
        private NumericUpDown nudReminderSeconds;
        private Label lblReminderContent;
        private TextBox txtReminderContent;

        private GroupBox grpSealing;
        private Label lblSealingSeconds;
        private NumericUpDown nudSealingSeconds;
        private Label lblSealingContent;
        private TextBox txtSealingContent;

        private GroupBox grpRule;
        private CheckBox chkRuleEnabled;
        private Label lblRuleSeconds;
        private NumericUpDown nudRuleSeconds;
        private Label lblRuleContent;
        private TextBox txtRuleContent;

        private GroupBox grpMute;
        private CheckBox chkAutoMute;
        private Label lblMuteSeconds;
        private NumericUpDown nudMuteSeconds;

        private GroupBox grpStatus;
        private Label lblCurrentPeriod;
        private Label lblCurrentPeriodValue;
        private Label lblNextDraw;
        private Label lblNextDrawValue;
        private Label lblCurrentState;
        private Label lblCurrentStateValue;
        private Label lblCountdown;
        private Label lblCountdownValue;

        private Button btnSave;
        private Button btnStart;
        private Button btnStop;

        private Timer statusTimer;

        #endregion

        public SealingSettingsControl()
        {
            InitializeComponent();
            LoadSettings();
            InitStatusTimer();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            this.Size = new Size(800, 600);
            this.AutoScroll = true;

            int y = 10;

            // 彩种选择
            grpLotteryType = new GroupBox
            {
                Text = "彩种选择",
                Location = new Point(10, y),
                Size = new Size(380, 60)
            };

            rbCanada28 = new RadioButton
            {
                Text = "加拿大28 (3.5分/期)",
                Location = new Point(20, 25),
                AutoSize = true,
                Checked = true
            };
            rbCanada28.CheckedChanged += OnLotteryTypeChanged;

            rbBit28 = new RadioButton
            {
                Text = "比特28 (1分/期)",
                Location = new Point(160, 25),
                AutoSize = true
            };
            rbBit28.CheckedChanged += OnLotteryTypeChanged;

            rbBeijing28 = new RadioButton
            {
                Text = "北京28 (5分/期)",
                Location = new Point(280, 25),
                AutoSize = true
            };

            grpLotteryType.Controls.AddRange(new Control[] { rbCanada28, rbBit28, rbBeijing28 });
            this.Controls.Add(grpLotteryType);

            y += 70;

            // 提醒设置
            grpReminder = new GroupBox
            {
                Text = "封盘提醒",
                Location = new Point(10, y),
                Size = new Size(380, 130)
            };

            chkReminderEnabled = new CheckBox
            {
                Text = "启用封盘提醒",
                Location = new Point(20, 25),
                AutoSize = true,
                Checked = true
            };

            lblReminderSeconds = new Label { Text = "提前秒数:", Location = new Point(20, 55), AutoSize = true };
            nudReminderSeconds = new NumericUpDown
            {
                Location = new Point(90, 52),
                Size = new Size(80, 23),
                Minimum = 1,
                Maximum = 300,
                Value = 60
            };

            lblReminderContent = new Label { Text = "提醒内容:", Location = new Point(20, 85), AutoSize = true };
            txtReminderContent = new TextBox
            {
                Location = new Point(90, 82),
                Size = new Size(270, 23),
                Text = "--距离封盘时间还有[封盘倒计时]秒--"
            };

            grpReminder.Controls.AddRange(new Control[] {
                chkReminderEnabled, lblReminderSeconds, nudReminderSeconds,
                lblReminderContent, txtReminderContent
            });
            this.Controls.Add(grpReminder);

            // 封盘设置
            grpSealing = new GroupBox
            {
                Text = "封盘设置",
                Location = new Point(400, y),
                Size = new Size(380, 130)
            };

            lblSealingSeconds = new Label { Text = "封盘秒数:", Location = new Point(20, 25), AutoSize = true };
            nudSealingSeconds = new NumericUpDown
            {
                Location = new Point(90, 22),
                Size = new Size(80, 23),
                Minimum = 1,
                Maximum = 120,
                Value = 20
            };

            lblSealingContent = new Label { Text = "封盘内容:", Location = new Point(20, 55), AutoSize = true };
            txtSealingContent = new TextBox
            {
                Location = new Point(90, 52),
                Size = new Size(270, 60),
                Multiline = true,
                Text = "========封盘线=======\n以上有钱的都接\n=====庄显为准======="
            };

            grpSealing.Controls.AddRange(new Control[] {
                lblSealingSeconds, nudSealingSeconds,
                lblSealingContent, txtSealingContent
            });
            this.Controls.Add(grpSealing);

            y += 140;

            // 规则设置
            grpRule = new GroupBox
            {
                Text = "规则发送",
                Location = new Point(10, y),
                Size = new Size(380, 130)
            };

            chkRuleEnabled = new CheckBox
            {
                Text = "启用规则发送",
                Location = new Point(20, 25),
                AutoSize = true,
                Checked = true
            };

            lblRuleSeconds = new Label { Text = "发送秒数:", Location = new Point(20, 55), AutoSize = true };
            nudRuleSeconds = new NumericUpDown
            {
                Location = new Point(90, 52),
                Size = new Size(80, 23),
                Minimum = 1,
                Maximum = 60,
                Value = 1
            };

            lblRuleContent = new Label { Text = "规则内容:", Location = new Point(20, 85), AutoSize = true };
            txtRuleContent = new TextBox
            {
                Location = new Point(90, 82),
                Size = new Size(270, 23),
                Text = "本群如遇卡奖情况，十分钟官网没开奖，本期无效！"
            };

            grpRule.Controls.AddRange(new Control[] {
                chkRuleEnabled, lblRuleSeconds, nudRuleSeconds,
                lblRuleContent, txtRuleContent
            });
            this.Controls.Add(grpRule);

            // 禁言设置
            grpMute = new GroupBox
            {
                Text = "自动禁言",
                Location = new Point(400, y),
                Size = new Size(380, 130)
            };

            chkAutoMute = new CheckBox
            {
                Text = "启用自动禁言",
                Location = new Point(20, 25),
                AutoSize = true,
                Checked = true
            };

            lblMuteSeconds = new Label { Text = "提前秒数:", Location = new Point(20, 55), AutoSize = true };
            nudMuteSeconds = new NumericUpDown
            {
                Location = new Point(90, 52),
                Size = new Size(80, 23),
                Minimum = 0,
                Maximum = 60,
                Value = 5
            };

            grpMute.Controls.AddRange(new Control[] {
                chkAutoMute, lblMuteSeconds, nudMuteSeconds
            });
            this.Controls.Add(grpMute);

            y += 140;

            // 状态显示
            grpStatus = new GroupBox
            {
                Text = "运行状态",
                Location = new Point(10, y),
                Size = new Size(770, 100)
            };

            lblCurrentPeriod = new Label { Text = "当前期号:", Location = new Point(20, 25), AutoSize = true };
            lblCurrentPeriodValue = new Label { Text = "未启动", Location = new Point(100, 25), AutoSize = true, ForeColor = Color.Blue };

            lblNextDraw = new Label { Text = "开奖时间:", Location = new Point(200, 25), AutoSize = true };
            lblNextDrawValue = new Label { Text = "--:--:--", Location = new Point(280, 25), AutoSize = true, ForeColor = Color.Blue };

            lblCurrentState = new Label { Text = "当前状态:", Location = new Point(400, 25), AutoSize = true };
            lblCurrentStateValue = new Label { Text = "未启动", Location = new Point(480, 25), AutoSize = true, ForeColor = Color.Gray };

            lblCountdown = new Label { Text = "倒计时:", Location = new Point(600, 25), AutoSize = true };
            lblCountdownValue = new Label { Text = "--", Location = new Point(680, 25), AutoSize = true, ForeColor = Color.Red, Font = new Font(Font.FontFamily, 12, FontStyle.Bold) };

            grpStatus.Controls.AddRange(new Control[] {
                lblCurrentPeriod, lblCurrentPeriodValue,
                lblNextDraw, lblNextDrawValue,
                lblCurrentState, lblCurrentStateValue,
                lblCountdown, lblCountdownValue
            });
            this.Controls.Add(grpStatus);

            y += 110;

            // 按钮
            btnSave = new Button
            {
                Text = "保存设置",
                Location = new Point(10, y),
                Size = new Size(100, 35)
            };
            btnSave.Click += BtnSave_Click;
            this.Controls.Add(btnSave);

            btnStart = new Button
            {
                Text = "启动服务",
                Location = new Point(120, y),
                Size = new Size(100, 35),
                BackColor = Color.FromArgb(46, 204, 113),
                ForeColor = Color.White
            };
            btnStart.Click += BtnStart_Click;
            this.Controls.Add(btnStart);

            btnStop = new Button
            {
                Text = "停止服务",
                Location = new Point(230, y),
                Size = new Size(100, 35),
                BackColor = Color.FromArgb(231, 76, 60),
                ForeColor = Color.White,
                Enabled = false
            };
            btnStop.Click += BtnStop_Click;
            this.Controls.Add(btnStop);

            this.ResumeLayout(false);
        }

        private void LoadSettings()
        {
            var config = SealingService.Instance.GetConfig();

            switch (config.LotteryType)
            {
                case LotteryType.Canada28: rbCanada28.Checked = true; break;
                case LotteryType.Bit28: rbBit28.Checked = true; break;
                case LotteryType.Beijing28: rbBeijing28.Checked = true; break;
            }

            chkReminderEnabled.Checked = config.ReminderEnabled;
            nudReminderSeconds.Value = config.ReminderSeconds;
            txtReminderContent.Text = config.ReminderContent;

            nudSealingSeconds.Value = config.SealingSeconds;
            txtSealingContent.Text = config.SealingContent;

            chkRuleEnabled.Checked = config.RuleEnabled;
            nudRuleSeconds.Value = config.RuleSeconds;
            txtRuleContent.Text = config.RuleContent;

            chkAutoMute.Checked = config.AutoMute;
            nudMuteSeconds.Value = config.MuteBeforeSeconds;
        }

        private void SaveSettings()
        {
            var config = new SealingConfig
            {
                LotteryType = rbCanada28.Checked ? LotteryType.Canada28 :
                              rbBit28.Checked ? LotteryType.Bit28 : LotteryType.Beijing28,

                ReminderEnabled = chkReminderEnabled.Checked,
                ReminderSeconds = (int)nudReminderSeconds.Value,
                ReminderContent = txtReminderContent.Text,

                SealingSeconds = (int)nudSealingSeconds.Value,
                SealingContent = txtSealingContent.Text,

                RuleEnabled = chkRuleEnabled.Checked,
                RuleSeconds = (int)nudRuleSeconds.Value,
                RuleContent = txtRuleContent.Text,

                AutoMute = chkAutoMute.Checked,
                MuteBeforeSeconds = (int)nudMuteSeconds.Value
            };

            config.ApplyLotteryTypeDefaults();
            SealingService.Instance.SaveConfig(config);
        }

        private void OnLotteryTypeChanged(object sender, EventArgs e)
        {
            if (rbCanada28.Checked)
            {
                nudReminderSeconds.Value = 60;
                nudSealingSeconds.Value = 20;
            }
            else if (rbBit28.Checked)
            {
                nudReminderSeconds.Value = 10;
                nudSealingSeconds.Value = 5;
            }
            else if (rbBeijing28.Checked)
            {
                nudReminderSeconds.Value = 60;
                nudSealingSeconds.Value = 20;
            }
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            SaveSettings();
            MessageBox.Show("设置已保存！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void BtnStart_Click(object sender, EventArgs e)
        {
            SaveSettings();

            // 计算下一期开奖时间 (简化示例)
            var now = DateTime.Now;
            var config = SealingService.Instance.GetConfig();
            var interval = config.DrawIntervalSeconds;

            // 计算当前期号和下一期开奖时间
            var baseTime = new DateTime(now.Year, now.Month, now.Day, 0, 0, 0);
            var secondsToday = (int)(now - baseTime).TotalSeconds;
            var currentPeriodNum = secondsToday / interval;
            var nextDrawTime = baseTime.AddSeconds((currentPeriodNum + 1) * interval);
            var period = now.ToString("yyyyMMdd") + currentPeriodNum.ToString("D3");

            SealingService.Instance.Start(period, nextDrawTime);

            btnStart.Enabled = false;
            btnStop.Enabled = true;

            UpdateStatus();
        }

        private void BtnStop_Click(object sender, EventArgs e)
        {
            SealingService.Instance.Stop();

            btnStart.Enabled = true;
            btnStop.Enabled = false;

            lblCurrentStateValue.Text = "已停止";
            lblCurrentStateValue.ForeColor = Color.Gray;
        }

        private void InitStatusTimer()
        {
            statusTimer = new Timer { Interval = 1000 };
            statusTimer.Tick += (s, e) => UpdateStatus();
            statusTimer.Start();
        }

        private void UpdateStatus()
        {
            try
            {
                var period = SealingService.Instance.GetCurrentPeriod();
                var state = SealingService.Instance.GetCurrentState();
                var seconds = SealingService.Instance.GetSecondsToNext();

                lblCurrentPeriodValue.Text = period ?? "未启动";
                lblCountdownValue.Text = seconds > 0 ? $"{seconds}秒" : "--";

                switch (state)
                {
                    case SealingState.Accepting:
                        lblCurrentStateValue.Text = "接受下注";
                        lblCurrentStateValue.ForeColor = Color.Green;
                        break;
                    case SealingState.Reminded:
                        lblCurrentStateValue.Text = "已提醒";
                        lblCurrentStateValue.ForeColor = Color.Orange;
                        break;
                    case SealingState.Sealed:
                        lblCurrentStateValue.Text = "已封盘";
                        lblCurrentStateValue.ForeColor = Color.Red;
                        break;
                    case SealingState.RuleSent:
                        lblCurrentStateValue.Text = "已发规则";
                        lblCurrentStateValue.ForeColor = Color.Purple;
                        break;
                    case SealingState.WaitingResult:
                        lblCurrentStateValue.Text = "等待开奖";
                        lblCurrentStateValue.ForeColor = Color.Blue;
                        break;
                }
            }
            catch { }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                statusTimer?.Stop();
                statusTimer?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
