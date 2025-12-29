using System;
using System.Windows.Forms;
using WangShangLiaoBot.Services;

namespace WangShangLiaoBot.Controls
{
    /// <summary>
    /// 基本设置控件 (右侧上方)
    /// </summary>
    public partial class BasicSettingsControl : UserControl
    {
        public BasicSettingsControl()
        {
            InitializeComponent();
        }

        public void LoadSettings()
        {
            var config = ConfigService.Instance.Config;
            txtAdminId.Text = config.AdminWangWangId;
            txtGroupId.Text = config.GroupId;
        }

        public void SaveSettings()
        {
            var config = ConfigService.Instance.Config;
            config.AdminWangWangId = txtAdminId.Text.Trim();
            config.GroupId = txtGroupId.Text.Trim();
        }

        private void btnSaveAdmin_Click(object sender, EventArgs e)
        {
            SaveSettings();
            ConfigService.Instance.SaveConfig();
            MessageBox.Show("管理号和群号已保存", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void btnViewInviteLog_Click(object sender, EventArgs e)
        {
            MessageBox.Show("查看群成员邀请记录功能开发中...", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}

