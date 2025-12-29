using System;
using System.Drawing;
using System.Windows.Forms;
using WangShangLiaoBot.Services;

namespace WangShangLiaoBot.Controls.OtherSettings
{
    /// <summary>
    /// 敏感操作开关 - 设置面板
    /// </summary>
    public sealed class SensitiveOperationPanel : UserControl
    {
        private GroupBox grpPassword;
        private Label lblOldPassword;
        private TextBox txtOldPassword;
        private Label lblNewPassword;
        private TextBox txtNewPassword;
        private Label lblConfirmPassword;
        private TextBox txtConfirmPassword;
        private Button btnSavePassword;
        private CheckBox chkDisablePassword;

        public SensitiveOperationPanel()
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

            // GroupBox: 敏感操作密码
            grpPassword = new GroupBox
            {
                Text = "敏感操作密码",
                Location = new Point(15, 15),
                Size = new Size(350, 160)
            };
            Controls.Add(grpPassword);

            int y = 25;

            // 原密码
            lblOldPassword = new Label
            {
                Text = "原密码",
                Location = new Point(15, y + 3),
                Size = new Size(50, 20)
            };
            grpPassword.Controls.Add(lblOldPassword);

            txtOldPassword = new TextBox
            {
                Location = new Point(70, y),
                Size = new Size(200, 23),
                UseSystemPasswordChar = true
            };
            grpPassword.Controls.Add(txtOldPassword);
            y += 30;

            // 新密码
            lblNewPassword = new Label
            {
                Text = "新密码",
                Location = new Point(15, y + 3),
                Size = new Size(50, 20)
            };
            grpPassword.Controls.Add(lblNewPassword);

            txtNewPassword = new TextBox
            {
                Location = new Point(70, y),
                Size = new Size(200, 23),
                UseSystemPasswordChar = true
            };
            grpPassword.Controls.Add(txtNewPassword);
            y += 30;

            // 确认新密码
            lblConfirmPassword = new Label
            {
                Text = "新密码",
                Location = new Point(15, y + 3),
                Size = new Size(50, 20)
            };
            grpPassword.Controls.Add(lblConfirmPassword);

            txtConfirmPassword = new TextBox
            {
                Location = new Point(70, y),
                Size = new Size(200, 23),
                UseSystemPasswordChar = true
            };
            grpPassword.Controls.Add(txtConfirmPassword);
            y += 40;

            // 保存密码按钮
            btnSavePassword = new Button
            {
                Text = "保存密码",
                Location = new Point(120, y),
                Size = new Size(85, 28)
            };
            btnSavePassword.Click += BtnSavePassword_Click;
            grpPassword.Controls.Add(btnSavePassword);

            // 关闭敏感密码 复选框
            chkDisablePassword = new CheckBox
            {
                Text = "关闭敏感密码",
                Location = new Point(15, 190),
                Size = new Size(150, 20)
            };
            chkDisablePassword.CheckedChanged += ChkDisablePassword_CheckedChanged;
            Controls.Add(chkDisablePassword);

            ResumeLayout(false);
        }

        private void LoadSettings()
        {
            var s = SensitiveOperationService.Instance;
            chkDisablePassword.Checked = !s.PasswordEnabled;
            UpdatePasswordFieldsState();
        }

        private void UpdatePasswordFieldsState()
        {
            bool enabled = !chkDisablePassword.Checked;
            txtOldPassword.Enabled = enabled;
            txtNewPassword.Enabled = enabled;
            txtConfirmPassword.Enabled = enabled;
            btnSavePassword.Enabled = enabled;
        }

        private void ChkDisablePassword_CheckedChanged(object sender, EventArgs e)
        {
            UpdatePasswordFieldsState();
            
            if (chkDisablePassword.Checked)
            {
                // Disable password
                SensitiveOperationService.Instance.DisablePassword();
                txtOldPassword.Clear();
                txtNewPassword.Clear();
                txtConfirmPassword.Clear();
            }
        }

        private void BtnSavePassword_Click(object sender, EventArgs e)
        {
            var newPwd = txtNewPassword.Text;
            var confirmPwd = txtConfirmPassword.Text;
            var oldPwd = txtOldPassword.Text;

            // Validate new password
            if (string.IsNullOrEmpty(newPwd))
            {
                MessageBox.Show("请输入新密码", "提示", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Check password match
            if (newPwd != confirmPwd)
            {
                MessageBox.Show("两次输入的新密码不一致", "提示", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Try to set password
            var s = SensitiveOperationService.Instance;
            if (s.SetPassword(oldPwd, newPwd))
            {
                MessageBox.Show("密码保存成功", "提示", 
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                txtOldPassword.Clear();
                txtNewPassword.Clear();
                txtConfirmPassword.Clear();
                chkDisablePassword.Checked = false;
            }
            else
            {
                MessageBox.Show("原密码不正确", "错误", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
