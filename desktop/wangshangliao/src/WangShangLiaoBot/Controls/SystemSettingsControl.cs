using System;
using System.Drawing;
using System.Windows.Forms;
using WangShangLiaoBot.Services;

namespace WangShangLiaoBot.Controls
{
    /// <summary>
    /// 系统设置 - 管理面板
    /// </summary>
    public sealed class SystemSettingsControl : UserControl
    {
        private GroupBox grpGeneral;
        private CheckBox chkAutoStart;
        private CheckBox chkMinimizeToTray;
        private CheckBox chkShowNotification;
        private CheckBox chkLogEnabled;

        private GroupBox grpConnection;
        private Label lblConnectTimeout;
        private NumericUpDown nudConnectTimeout;
        private Label lblRetryCount;
        private NumericUpDown nudRetryCount;

        private GroupBox grpBackup;
        private CheckBox chkAutoBackup;
        private Label lblBackupInterval;
        private NumericUpDown nudBackupInterval;
        private Button btnBackupNow;

        private Button btnSave;
        private Button btnResetDefault;

        public SystemSettingsControl()
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

            int y = 10;

            // General settings group
            grpGeneral = new GroupBox
            {
                Text = "常规设置",
                Location = new Point(10, y),
                Size = new Size(300, 140)
            };
            Controls.Add(grpGeneral);

            chkAutoStart = new CheckBox
            {
                Text = "开机自动启动",
                Location = new Point(15, 25),
                AutoSize = true
            };
            grpGeneral.Controls.Add(chkAutoStart);

            chkMinimizeToTray = new CheckBox
            {
                Text = "最小化到系统托盘",
                Location = new Point(15, 50),
                AutoSize = true
            };
            grpGeneral.Controls.Add(chkMinimizeToTray);

            chkShowNotification = new CheckBox
            {
                Text = "显示通知消息",
                Location = new Point(15, 75),
                AutoSize = true
            };
            grpGeneral.Controls.Add(chkShowNotification);

            chkLogEnabled = new CheckBox
            {
                Text = "启用日志记录",
                Location = new Point(15, 100),
                AutoSize = true
            };
            grpGeneral.Controls.Add(chkLogEnabled);

            // Connection settings group
            grpConnection = new GroupBox
            {
                Text = "连接设置",
                Location = new Point(320, y),
                Size = new Size(250, 140)
            };
            Controls.Add(grpConnection);

            lblConnectTimeout = new Label
            {
                Text = "连接超时（秒）:",
                Location = new Point(15, 30),
                AutoSize = true
            };
            grpConnection.Controls.Add(lblConnectTimeout);

            nudConnectTimeout = new NumericUpDown
            {
                Location = new Point(120, 27),
                Size = new Size(60, 23),
                Minimum = 5,
                Maximum = 120,
                Value = 30
            };
            grpConnection.Controls.Add(nudConnectTimeout);

            lblRetryCount = new Label
            {
                Text = "重试次数:",
                Location = new Point(15, 65),
                AutoSize = true
            };
            grpConnection.Controls.Add(lblRetryCount);

            nudRetryCount = new NumericUpDown
            {
                Location = new Point(120, 62),
                Size = new Size(60, 23),
                Minimum = 0,
                Maximum = 10,
                Value = 3
            };
            grpConnection.Controls.Add(nudRetryCount);

            y += 160;

            // Backup settings group
            grpBackup = new GroupBox
            {
                Text = "数据备份",
                Location = new Point(10, y),
                Size = new Size(300, 100)
            };
            Controls.Add(grpBackup);

            chkAutoBackup = new CheckBox
            {
                Text = "启用自动备份",
                Location = new Point(15, 25),
                AutoSize = true
            };
            grpBackup.Controls.Add(chkAutoBackup);

            lblBackupInterval = new Label
            {
                Text = "备份间隔（小时）:",
                Location = new Point(15, 55),
                AutoSize = true
            };
            grpBackup.Controls.Add(lblBackupInterval);

            nudBackupInterval = new NumericUpDown
            {
                Location = new Point(130, 52),
                Size = new Size(60, 23),
                Minimum = 1,
                Maximum = 72,
                Value = 24
            };
            grpBackup.Controls.Add(nudBackupInterval);

            btnBackupNow = new Button
            {
                Text = "立即备份",
                Location = new Point(200, 50),
                Size = new Size(80, 28)
            };
            btnBackupNow.Click += BtnBackupNow_Click;
            grpBackup.Controls.Add(btnBackupNow);

            y += 120;

            // Buttons
            btnSave = new Button
            {
                Text = "保存设置",
                Location = new Point(100, y),
                Size = new Size(85, 30)
            };
            btnSave.Click += BtnSave_Click;
            Controls.Add(btnSave);

            btnResetDefault = new Button
            {
                Text = "恢复默认",
                Location = new Point(200, y),
                Size = new Size(85, 30)
            };
            btnResetDefault.Click += BtnResetDefault_Click;
            Controls.Add(btnResetDefault);

            ResumeLayout(false);
        }

        private void LoadSettings()
        {
            var ds = DataService.Instance;
            
            chkAutoStart.Checked = ds.GetSetting("System:AutoStart", "0") == "1";
            chkMinimizeToTray.Checked = ds.GetSetting("System:MinimizeToTray", "1") == "1";
            chkShowNotification.Checked = ds.GetSetting("System:ShowNotification", "1") == "1";
            chkLogEnabled.Checked = ds.GetSetting("System:LogEnabled", "1") == "1";

            int timeout;
            if (int.TryParse(ds.GetSetting("System:ConnectTimeout", "30"), out timeout))
                nudConnectTimeout.Value = Math.Max(5, Math.Min(120, timeout));

            int retry;
            if (int.TryParse(ds.GetSetting("System:RetryCount", "3"), out retry))
                nudRetryCount.Value = Math.Max(0, Math.Min(10, retry));

            chkAutoBackup.Checked = ds.GetSetting("System:AutoBackup", "1") == "1";

            int backupInterval;
            if (int.TryParse(ds.GetSetting("System:BackupInterval", "24"), out backupInterval))
                nudBackupInterval.Value = Math.Max(1, Math.Min(72, backupInterval));
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            var ds = DataService.Instance;
            
            ds.SaveSetting("System:AutoStart", chkAutoStart.Checked ? "1" : "0");
            ds.SaveSetting("System:MinimizeToTray", chkMinimizeToTray.Checked ? "1" : "0");
            ds.SaveSetting("System:ShowNotification", chkShowNotification.Checked ? "1" : "0");
            ds.SaveSetting("System:LogEnabled", chkLogEnabled.Checked ? "1" : "0");
            ds.SaveSetting("System:ConnectTimeout", ((int)nudConnectTimeout.Value).ToString());
            ds.SaveSetting("System:RetryCount", ((int)nudRetryCount.Value).ToString());
            ds.SaveSetting("System:AutoBackup", chkAutoBackup.Checked ? "1" : "0");
            ds.SaveSetting("System:BackupInterval", ((int)nudBackupInterval.Value).ToString());

            MessageBox.Show("系统设置已保存", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void BtnResetDefault_Click(object sender, EventArgs e)
        {
            var result = MessageBox.Show("确定要恢复默认设置吗？", "确认",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                chkAutoStart.Checked = false;
                chkMinimizeToTray.Checked = true;
                chkShowNotification.Checked = true;
                chkLogEnabled.Checked = true;
                nudConnectTimeout.Value = 30;
                nudRetryCount.Value = 3;
                chkAutoBackup.Checked = true;
                nudBackupInterval.Value = 24;
            }
        }

        private void BtnBackupNow_Click(object sender, EventArgs e)
        {
            try
            {
                // Perform manual backup
                var backupDir = System.IO.Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory, "Data", "备份");
                if (!System.IO.Directory.Exists(backupDir))
                    System.IO.Directory.CreateDirectory(backupDir);
                    
                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                var sourceDir = System.IO.Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory, "Data", "数据库");
                    
                if (System.IO.Directory.Exists(sourceDir))
                {
                    var destDir = System.IO.Path.Combine(backupDir, $"backup_{timestamp}");
                    System.IO.Directory.CreateDirectory(destDir);
                    
                    foreach (var file in System.IO.Directory.GetFiles(sourceDir))
                    {
                        var fileName = System.IO.Path.GetFileName(file);
                        System.IO.File.Copy(file, System.IO.Path.Combine(destDir, fileName), true);
                    }
                }
                
                MessageBox.Show("数据备份完成", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"备份失败: {ex.Message}", "错误", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}

