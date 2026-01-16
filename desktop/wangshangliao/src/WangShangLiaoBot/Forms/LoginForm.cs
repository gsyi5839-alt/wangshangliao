using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using WangShangLiaoBot.Services;
using WangShangLiaoBot.Utils;

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

            // Fix register/recharge/change-password input issues:
            // - Blue underline line above textbox (IME composition UI)
            // - First character not visible until typing more
            // - Password textbox sometimes not showing input
            // Root cause: IME composition context attached to edit control.
            // Solution: detach IME context + switch to English input on focus (best effort).
            ImeGuard.DisableIme(txtUsername);
            ImeGuard.DisableIme(txtPassword);
            ImeGuard.DisableIme(txtRegUsername);
            ImeGuard.DisableIme(txtRegPassword);
            ImeGuard.DisableIme(txtRegSuperPassword);
            ImeGuard.DisableIme(txtRegCard);
            ImeGuard.DisableIme(txtRegBindInfo);
            ImeGuard.DisableIme(txtRegPromoter);
            ImeGuard.DisableIme(txtRechargeUsername);
            ImeGuard.DisableIme(txtRechargeCard);
            ImeGuard.DisableIme(txtChangePwdUsername);
            ImeGuard.DisableIme(txtChangePwdSuperPassword);
            ImeGuard.DisableIme(txtChangePwdNewPassword);
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

                Logger.Info($"用户 {username} 登录成功");
                
                // 【优化】立即关闭登录窗口，让主界面尽快显示
                // 公告和版本列表改为在主界面显示后异步加载
                this.DialogResult = DialogResult.OK;
                this.Close();
                
                // 【优化】公告和版本列表移到后台异步加载，不阻塞主界面
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await Task.Delay(2000); // 等待主界面显示
                        var ann = await ClientPortalService.Instance.GetAnnouncementAsync();
                        if (ann != null && !string.IsNullOrWhiteSpace(ann.content))
                        {
                            // 在 UI 线程显示
                            System.Windows.Forms.Application.OpenForms[0]?.BeginInvoke(new Action(() =>
                            {
                                MessageBox.Show(ann.content, ann.title ?? "公告", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            }));
                        }
                    }
                    catch { }
                });
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
                MessageBox.Show("请填写账号与密码", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (string.IsNullOrEmpty(superPassword) || string.IsNullOrEmpty(card))
            {
                MessageBox.Show("请填写超级密码与充值卡", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (!IsValidCardInput(card))
            {
                MessageBox.Show("充值卡格式错误，请输入18位纯数字卡密", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            btnDoRegister.Enabled = false;
            var oldText = btnDoRegister.Text;
            btnDoRegister.Text = "注册中...";
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
                MessageBox.Show($"注册失败: {ex.Message}", "提示", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnDoRegister.Enabled = true;
                btnDoRegister.Text = oldText;
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
                MessageBox.Show("请填写账号与充值卡", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (!IsValidCardInput(card))
            {
                MessageBox.Show("充值卡格式错误，请输入18位纯数字卡密", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            
            btnDoRecharge.Enabled = false;
            var oldText = btnDoRecharge.Text;
            btnDoRecharge.Text = "充值中...";
            try
            {
                var result = await ClientPortalService.Instance.RechargeAsync(username, card);
                var left = result != null && result.daysLeft.HasValue ? result.daysLeft.Value.ToString() : "未知";
                MessageBox.Show($"充值成功，剩余天数: {left}", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                tabControl.SelectedTab = tabLogin;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"充值失败: {ex.Message}", "提示", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnDoRecharge.Enabled = true;
                btnDoRecharge.Text = oldText;
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
                MessageBox.Show("请填写账号与新密码", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            
            if (string.IsNullOrEmpty(superPassword))
            {
                MessageBox.Show("请输入超级密码", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            btnDoChangePassword.Enabled = false;
            var oldText = btnDoChangePassword.Text;
            btnDoChangePassword.Text = "提交中...";
            try
            {
                await ClientPortalService.Instance.ChangePasswordAsync(username, superPassword, newPassword);
                MessageBox.Show("修改成功", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                tabControl.SelectedTab = tabLogin;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"修改失败: {ex.Message}", "提示", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnDoChangePassword.Enabled = true;
                btnDoChangePassword.Text = oldText;
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
        /// 新格式: 只需18位纯数字卡密
        /// </summary>
        private bool IsValidCardInput(string input)
        {
            try
            {
                var code = (input ?? "").Trim();
                // 卡密: 18位纯数字
                return System.Text.RegularExpressions.Regex.IsMatch(code, "^[0-9]{18}$");
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

