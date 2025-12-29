using System;
using System.Drawing;
using System.Windows.Forms;
using WangShangLiaoBot.Services;

namespace WangShangLiaoBot.Controls.OtherSettings
{
    /// <summary>
    /// 结算时间 - 设置面板
    /// </summary>
    public sealed class SettlementTimePanel : UserControl
    {
        private Label lblSettlementTime;
        private DateTimePicker dtpSettlementTime;
        private CheckBox chkPlayerQuery;
        private NumericUpDown nudQueryInterval;
        private Label lblIntervalSuffix;
        private Button btnSave;

        public SettlementTimePanel()
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

            int y = 20;

            // 结算时间
            lblSettlementTime = new Label
            {
                Text = "结算时间",
                Location = new Point(15, y + 3),
                AutoSize = true
            };
            Controls.Add(lblSettlementTime);

            dtpSettlementTime = new DateTimePicker
            {
                Location = new Point(75, y),
                Size = new Size(90, 23),
                Format = DateTimePickerFormat.Time,
                ShowUpDown = true
            };
            Controls.Add(dtpSettlementTime);
            y += 35;

            // 玩家查询今天数据
            chkPlayerQuery = new CheckBox
            {
                Text = "玩家查询今天数据",
                Location = new Point(15, y),
                Size = new Size(130, 20)
            };
            Controls.Add(chkPlayerQuery);

            nudQueryInterval = new NumericUpDown
            {
                Location = new Point(150, y - 2),
                Size = new Size(50, 23),
                Minimum = 1,
                Maximum = 60,
                Value = 10
            };
            Controls.Add(nudQueryInterval);

            lblIntervalSuffix = new Label
            {
                Text = "分钟一次",
                Location = new Point(205, y + 2),
                AutoSize = true
            };
            Controls.Add(lblIntervalSuffix);
            y += 150;

            // 保存设置按钮
            btnSave = new Button
            {
                Text = "保存设置",
                Location = new Point(200, y),
                Size = new Size(85, 28)
            };
            btnSave.Click += BtnSave_Click;
            Controls.Add(btnSave);

            ResumeLayout(false);
        }

        private void LoadSettings()
        {
            var s = SettlementTimeService.Instance;
            
            // Set time picker value
            var time = s.SettlementTime;
            dtpSettlementTime.Value = DateTime.Today.Add(time);
            
            chkPlayerQuery.Checked = s.PlayerQueryEnabled;
            nudQueryInterval.Value = Math.Max(1, Math.Min(60, s.PlayerQueryIntervalMinutes));
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            var s = SettlementTimeService.Instance;
            
            // Save settlement time
            s.SettlementTime = dtpSettlementTime.Value.TimeOfDay;
            s.PlayerQueryEnabled = chkPlayerQuery.Checked;
            s.PlayerQueryIntervalMinutes = (int)nudQueryInterval.Value;

            MessageBox.Show("结算时间设置已保存", "提示", 
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}
