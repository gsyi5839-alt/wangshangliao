using System;
using System.Drawing;
using System.Windows.Forms;
using WangShangLiaoBot.Services;

namespace WangShangLiaoBot.Forms
{
    /// <summary>
    /// 登录窗口
    /// </summary>
    public partial class LoginForm : Form
    {
        public LoginForm()
        {
            InitializeComponent();
            LoadSavedCredentials();
        }
        
        /// <summary>
        /// 加载保存的凭据
        /// </summary>
        private void LoadSavedCredentials()
        {
            var config = ConfigService.Instance.Config;
            if (config.RememberUser)
            {
                txtUsername.Text = config.Username;
                chkRemember.Checked = true;
            }
        }
        
        /// <summary>
        /// 登录按钮点击
        /// </summary>
        private async void btnLogin_Click(object sender, EventArgs e)
        {
            var username = txtUsername.Text.Trim();
            var password = txtPassword.Text;
            
            if (string.IsNullOrEmpty(username))
            {
                ShowError("请输入账号");
                return;
            }
            
            if (string.IsNullOrEmpty(password))
            {
                ShowError("请输入密码");
                return;
            }
            
            // 禁用按钮，显示加载状态
            btnLogin.Enabled = false;
            btnLogin.Text = "登录中...";
            lblStatus.Text = "正在连接服务器...";
            lblStatus.ForeColor = Color.Blue;
            
            try
            {
                // Real login via server API.
                var login = await ClientPortalService.Instance.LoginAsync(username, password);

                // Save token into config for subsequent calls (e.g., lottery settings).
                var config = ConfigService.Instance.Config;
                config.Username = username;
                config.RememberUser = chkRemember.Checked;
                config.ClientToken = login.token ?? "";
                ConfigService.Instance.SaveConfig();

                // Pull private settings after login (lottery token is sensitive, do not expose publicly).
                try
                {
                    var settings = await ClientPortalService.Instance.GetPrivateSettingsAsync(config.ClientToken);
                    if (settings != null)
                    {
                        if (settings.ContainsKey("lottery_api_url"))
                            config.LotteryApiUrl = settings["lottery_api_url"] ?? "";
                        if (settings.ContainsKey("lottery_api_token"))
                            config.LotteryApiToken = settings["lottery_api_token"] ?? "";
                        ConfigService.Instance.SaveConfig();
                    }
                }
                catch (Exception ex)
                {
                    // Non-fatal: bot can still run without remote settings.
                    Logger.Error($"Fetch settings failed: {ex.Message}");
                }

                // Load announcement and version list (best effort).
                try
                {
                    var ann = await ClientPortalService.Instance.GetAnnouncementAsync();
                    if (ann != null && !string.IsNullOrWhiteSpace(ann.content))
                        MessageBox.Show(ann.content, ann.title ?? "公告", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch { }
                try
                {
                    var vers = await ClientPortalService.Instance.GetVersionsAsync(10);
                    if (vers != null && vers.Count > 0)
                    {
                        var lines = new System.Text.StringBuilder();
                        foreach (var v in vers)
                            lines.AppendLine($"{v.version}: {v.content}");
                        MessageBox.Show(lines.ToString(), "更新日志", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
                catch { }
                
                Logger.Info($"用户 {username} 登录成功");
                
                // 登录成功
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex)
            {
                ShowError($"登录失败: {ex.Message}");
            }
            finally
            {
                btnLogin.Enabled = true;
                btnLogin.Text = "登录";
            }
        }
        
        /// <summary>
        /// 注册按钮点击
        /// </summary>
        private void btnRegister_Click(object sender, EventArgs e)
        {
            tabControl.SelectedTab = tabRegister;
        }
        
        /// <summary>
        /// 充值按钮点击
        /// </summary>
        private void btnRecharge_Click(object sender, EventArgs e)
        {
            tabControl.SelectedTab = tabRecharge;
        }
        
        /// <summary>
        /// 修改密码按钮点击
        /// </summary>
        private void btnChangePassword_Click(object sender, EventArgs e)
        {
            tabControl.SelectedTab = tabChangePassword;
        }
        
        /// <summary>
        /// 执行注册
        /// </summary>
        private async void btnDoRegister_Click(object sender, EventArgs e)
        {
            var username = txtRegUsername.Text.Trim();
            var password = txtRegPassword.Text;
            var superPassword = txtRegSuperPassword.Text;
            var card = txtRegCard.Text.Trim();
            var boundInfo = txtRegBindInfo.Text.Trim();
            var promoter = txtRegPromoter.Text.Trim();
            
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                ShowError("请填写完整信息");
                return;
            }

            if (string.IsNullOrEmpty(superPassword) || string.IsNullOrEmpty(card))
            {
                ShowError("请填写超级密码与充值卡");
                return;
            }
            if (!IsValidCardInput(card))
            {
                ShowError("充值卡格式错误，请输入：26位卡号 + 空格/换行 + 18位卡密（大小写+数字）");
                return;
            }

            btnDoRegister.Enabled = false;
            try
            {
                var reg = await ClientPortalService.Instance.RegisterAsync(
                    username,
                    password,
                    superPassword,
                    card,
                    string.IsNullOrWhiteSpace(boundInfo) ? null : boundInfo,
                    string.IsNullOrWhiteSpace(promoter) ? null : promoter
                );

                // Auto-save token and username.
                var config = ConfigService.Instance.Config;
                config.Username = username;
                config.ClientToken = reg.token ?? "";
                ConfigService.Instance.SaveConfig();

                MessageBox.Show("注册成功", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                tabControl.SelectedTab = tabLogin;
            }
            catch (Exception ex)
            {
                ShowError($"注册失败: {ex.Message}");
            }
            finally
            {
                btnDoRegister.Enabled = true;
            }
        }
        
        /// <summary>
        /// 执行充值
        /// </summary>
        private async void btnDoRecharge_Click(object sender, EventArgs e)
        {
            var username = txtRechargeUsername.Text.Trim();
            var card = txtRechargeCard.Text.Trim();
            
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(card))
            {
                ShowError("请填写完整信息");
                return;
            }
            if (!IsValidCardInput(card))
            {
                ShowError("充值卡格式错误，请输入：26位卡号 + 空格/换行 + 18位卡密（大小写+数字）");
                return;
            }
            
            btnDoRecharge.Enabled = false;
            try
            {
                var result = await ClientPortalService.Instance.RechargeAsync(username, card);
                var left = result != null && result.daysLeft.HasValue ? result.daysLeft.Value.ToString() : "未知";
                MessageBox.Show($"充值成功，剩余天数: {left}", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                tabControl.SelectedTab = tabLogin;
            }
            catch (Exception ex)
            {
                ShowError($"充值失败: {ex.Message}");
            }
            finally
            {
                btnDoRecharge.Enabled = true;
            }
        }
        
        /// <summary>
        /// 执行修改密码
        /// </summary>
        private async void btnDoChangePassword_Click(object sender, EventArgs e)
        {
            var username = txtChangePwdUsername.Text.Trim();
            var superPassword = txtChangePwdSuperPassword.Text;
            var newPassword = txtChangePwdNewPassword.Text;
            
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(newPassword))
            {
                ShowError("请填写完整信息");
                return;
            }
            
            if (string.IsNullOrEmpty(superPassword))
            {
                ShowError("请输入超级密码");
                return;
            }

            btnDoChangePassword.Enabled = false;
            try
            {
                await ClientPortalService.Instance.ChangePasswordAsync(username, superPassword, newPassword);
                MessageBox.Show("修改成功", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                tabControl.SelectedTab = tabLogin;
            }
            catch (Exception ex)
            {
                ShowError($"修改失败: {ex.Message}");
            }
            finally
            {
                btnDoChangePassword.Enabled = true;
            }
        }
        
        /// <summary>
        /// 显示错误信息
        /// </summary>
        private void ShowError(string message)
        {
            lblStatus.Text = message;
            lblStatus.ForeColor = Color.Red;
        }

        /// <summary>
        /// Validate card input for register/recharge.
        /// Format: CODE(26) + PASSWORD(18), both alphanumeric and case-sensitive.
        /// </summary>
        private bool IsValidCardInput(string input)
        {
            try
            {
                var s = (input ?? "").Trim().Replace("\r", "\n").Replace("|", " ").Replace(",", " ");
                var parts = s.Split(new[] { ' ', '\n', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length < 2) return false;
                var code = parts[0];
                var pass = parts[1];
                if (!System.Text.RegularExpressions.Regex.IsMatch(code, "^[A-Za-z0-9]{26}$")) return false;
                if (!System.Text.RegularExpressions.Regex.IsMatch(pass, "^[A-Za-z0-9]{18}$")) return false;
                return true;
            }
            catch
            {
                return false;
            }
        }
        
        /// <summary>
        /// 回车键登录
        /// </summary>
        private void txtPassword_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                btnLogin_Click(sender, e);
            }
        }
    }
}

