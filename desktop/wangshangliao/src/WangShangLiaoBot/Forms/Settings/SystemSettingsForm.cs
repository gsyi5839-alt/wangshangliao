using System;
using System.Drawing;
using System.Windows.Forms;
using WangShangLiaoBot.Models;
using WangShangLiaoBot.Services;

namespace WangShangLiaoBot.Forms.Settings
{
    /// <summary>
    /// 系统设置窗口
    /// </summary>
    public class SystemSettingsForm : Form
    {
        private TabControl tabMain;
        private TextBox txtLogLevel;
        private CheckBox chkAutoStart;
        private CheckBox chkMinimizeToTray;
        private NumericUpDown nudAutoSaveInterval;
        private Button btnSave;
        private Button btnCancel;

        public SystemSettingsForm()
        {
            InitializeComponent();
            LoadSettings();
        }

        private void InitializeComponent()
        {
            this.Text = "系统设置";
            this.Size = new Size(500, 400);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            tabMain = new TabControl();
            tabMain.Dock = DockStyle.Fill;

            // 常规设置标签页
            var tabGeneral = new TabPage("常规设置");
            InitializeGeneralTab(tabGeneral);
            tabMain.TabPages.Add(tabGeneral);

            // 高级设置标签页
            var tabAdvanced = new TabPage("高级设置");
            InitializeAdvancedTab(tabAdvanced);
            tabMain.TabPages.Add(tabAdvanced);

            // 底部按钮面板
            var pnlButtons = new Panel();
            pnlButtons.Dock = DockStyle.Bottom;
            pnlButtons.Height = 50;
            pnlButtons.Padding = new Padding(10);

            btnSave = new Button();
            btnSave.Text = "保存";
            btnSave.Size = new Size(80, 30);
            btnSave.Location = new Point(this.ClientSize.Width - 190, 10);
            btnSave.Click += BtnSave_Click;
            pnlButtons.Controls.Add(btnSave);

            btnCancel = new Button();
            btnCancel.Text = "取消";
            btnCancel.Size = new Size(80, 30);
            btnCancel.Location = new Point(this.ClientSize.Width - 100, 10);
            btnCancel.DialogResult = DialogResult.Cancel;
            pnlButtons.Controls.Add(btnCancel);

            this.Controls.Add(tabMain);
            this.Controls.Add(pnlButtons);
            this.AcceptButton = btnSave;
            this.CancelButton = btnCancel;
        }

        private void InitializeGeneralTab(TabPage tab)
        {
            var y = 20;
            var lblWidth = 120;
            // controlWidth 用于后续控件扩展
            _ = 200; // 占位

            // 自动启动
            chkAutoStart = new CheckBox();
            chkAutoStart.Text = "开机自动启动";
            chkAutoStart.Location = new Point(20, y);
            chkAutoStart.AutoSize = true;
            tab.Controls.Add(chkAutoStart);
            y += 35;

            // 最小化到托盘
            chkMinimizeToTray = new CheckBox();
            chkMinimizeToTray.Text = "最小化到系统托盘";
            chkMinimizeToTray.Location = new Point(20, y);
            chkMinimizeToTray.AutoSize = true;
            tab.Controls.Add(chkMinimizeToTray);
            y += 35;

            // 自动保存间隔
            var lblAutoSave = new Label();
            lblAutoSave.Text = "自动保存间隔(秒):";
            lblAutoSave.Location = new Point(20, y + 3);
            lblAutoSave.Size = new Size(lblWidth, 20);
            tab.Controls.Add(lblAutoSave);

            nudAutoSaveInterval = new NumericUpDown();
            nudAutoSaveInterval.Location = new Point(20 + lblWidth, y);
            nudAutoSaveInterval.Size = new Size(80, 25);
            nudAutoSaveInterval.Minimum = 10;
            nudAutoSaveInterval.Maximum = 600;
            nudAutoSaveInterval.Value = 60;
            tab.Controls.Add(nudAutoSaveInterval);
        }

        private void InitializeAdvancedTab(TabPage tab)
        {
            var y = 20;

            // 日志级别
            var lblLogLevel = new Label();
            lblLogLevel.Text = "日志级别:";
            lblLogLevel.Location = new Point(20, y + 3);
            lblLogLevel.Size = new Size(100, 20);
            tab.Controls.Add(lblLogLevel);

            txtLogLevel = new TextBox();
            txtLogLevel.Location = new Point(130, y);
            txtLogLevel.Size = new Size(150, 25);
            txtLogLevel.Text = "Info";
            tab.Controls.Add(txtLogLevel);
            y += 40;

            // 提示信息
            var lblTip = new Label();
            lblTip.Text = "提示: 日志级别可选值 Debug, Info, Warning, Error";
            lblTip.Location = new Point(20, y);
            lblTip.Size = new Size(400, 20);
            lblTip.ForeColor = Color.Gray;
            tab.Controls.Add(lblTip);
        }

        private void LoadSettings()
        {
            try
            {
                var config = AppConfig.Instance;
                // 加载配置到UI控件
                chkAutoStart.Checked = false; // 默认值
                chkMinimizeToTray.Checked = true;
                nudAutoSaveInterval.Value = 60;
            }
            catch (Exception ex)
            {
                Logger.Error($"[SystemSettings] Load error: {ex.Message}");
            }
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            try
            {
                // 保存配置
                var config = AppConfig.Instance;
                config.Save();

                MessageBox.Show("设置已保存", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex)
            {
                Logger.Error($"[SystemSettings] Save error: {ex.Message}");
                MessageBox.Show($"保存设置失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
