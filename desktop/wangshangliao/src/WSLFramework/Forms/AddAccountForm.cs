using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Threading.Tasks;
using System.Windows.Forms;
using WSLFramework.Models;
using WSLFramework.Services;
using WSLFramework.Utils;

namespace WSLFramework.Forms
{
    /// <summary>
    /// 添加旺商聊机器人账号对话框
    /// 支持从已登录的旺商聊客户端获取真实信息
    /// </summary>
    public class AddAccountForm : Form
    {
        private TextBox txtAccount;
        private TextBox txtBotName;
        private TextBox txtPassword;
        private ComboBox cboGroupId;
        private TextBox txtGroupIdManual;
        private CheckBox chkRememberPassword;
        private Button btnOk;
        private Button btnCancel;
        private Button btnRefreshGroups;
        private Label lblStatus;
        private Label lblNickname;
        private RadioButton rbSelectGroup;
        private RadioButton rbManualGroup;
        private Panel pnlGroupSelect;
        private Panel pnlGroupManual;

        /// <summary>编辑模式</summary>
        public bool IsEditMode { get; set; } = false;

        /// <summary>结果账号</summary>
        public BotAccount ResultAccount { get; private set; }

        /// <summary>从 CDP 获取的用户信息</summary>
        private WslUserInfo _cdpUserInfo;

        public AddAccountForm(BotAccount existingAccount = null)
        {
            InitializeComponent();

            if (existingAccount != null)
            {
                IsEditMode = true;
                this.Text = "编辑旺商聊机器人账号";
                LoadAccount(existingAccount);
            }
            else
            {
                // 新增账号时，尝试从 CDP 获取信息
                _ = TryLoadFromCDPAsync();
            }
        }

        private void InitializeComponent()
        {
            this.Text = "添加旺商聊机器人账号";
            this.Size = new Size(420, 420);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.None;
            this.BackColor = Color.White;
            this.Font = new Font("Microsoft YaHei UI", 9F);
            this.ShowInTaskbar = false;

            // 主面板
            var mainPanel = new Panel { Dock = DockStyle.Fill };

            // 标题栏
            var titlePanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 45,
                BackColor = Color.FromArgb(76, 175, 80)
            };

            // 标题栏渐变
            titlePanel.Paint += (s, e) =>
            {
                using (var brush = new LinearGradientBrush(
                    titlePanel.ClientRectangle,
                    Color.FromArgb(102, 187, 106),
                    Color.FromArgb(76, 175, 80),
                    LinearGradientMode.Horizontal))
                {
                    e.Graphics.FillRectangle(brush, titlePanel.ClientRectangle);
                }
            };

            var lblTitle = new Label
            {
                Text = "🐰 添加旺商聊机器人",
                Font = new Font("Microsoft YaHei UI", 11F, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize = true,
                Location = new Point(15, 12),
                BackColor = Color.Transparent
            };
            titlePanel.Controls.Add(lblTitle);

            // 内容面板
            var contentPanel = new Panel
            {
                Location = new Point(0, 45),
                Size = new Size(420, 375)
            };

            int y = 15;
            int labelWidth = 90;
            int textWidth = 280;
            int x = 15;

            // === 状态提示 ===
            lblStatus = new Label
            {
                Text = "⏳ 正在检测旺商聊...",
                Location = new Point(x, y),
                Size = new Size(390, 25),
                ForeColor = Color.Gray,
                Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Italic)
            };
            contentPanel.Controls.Add(lblStatus);
            y += 30;

            // === 检测到的昵称 ===
            lblNickname = new Label
            {
                Text = "",
                Location = new Point(x, y),
                Size = new Size(390, 25),
                ForeColor = Color.FromArgb(76, 175, 80),
                Font = new Font("Microsoft YaHei UI", 10F, FontStyle.Bold),
                Visible = false
            };
            contentPanel.Controls.Add(lblNickname);
            y += 30;

            // 旺商聊账号
            var lblAccount = new Label
            {
                Text = "旺商聊账号:",
                Location = new Point(x, y + 3),
                Size = new Size(labelWidth, 20),
                TextAlign = ContentAlignment.MiddleRight
            };
            txtAccount = new TextBox
            {
                Location = new Point(x + labelWidth + 5, y),
                Size = new Size(textWidth, 23)
            };
            contentPanel.Controls.Add(lblAccount);
            contentPanel.Controls.Add(txtAccount);
            y += 30;

            // 机器人名称
            var lblBotName = new Label
            {
                Text = "机器人名称:",
                Location = new Point(x, y + 3),
                Size = new Size(labelWidth, 20),
                TextAlign = ContentAlignment.MiddleRight
            };
            txtBotName = new TextBox
            {
                Location = new Point(x + labelWidth + 5, y),
                Size = new Size(textWidth, 23),
                Text = "机器人"
            };
            contentPanel.Controls.Add(lblBotName);
            contentPanel.Controls.Add(txtBotName);
            y += 30;

            // 登录密码
            var lblPassword = new Label
            {
                Text = "登录密码:",
                Location = new Point(x, y + 3),
                Size = new Size(labelWidth, 20),
                TextAlign = ContentAlignment.MiddleRight
            };
            txtPassword = new TextBox
            {
                Location = new Point(x + labelWidth + 5, y),
                Size = new Size(textWidth, 23),
                PasswordChar = '●'
            };
            contentPanel.Controls.Add(lblPassword);
            contentPanel.Controls.Add(txtPassword);
            y += 35;

            // === 群号选择方式 ===
            var lblGroupMethod = new Label
            {
                Text = "绑定群号:",
                Location = new Point(x, y + 3),
                Size = new Size(labelWidth, 20),
                TextAlign = ContentAlignment.MiddleRight
            };
            contentPanel.Controls.Add(lblGroupMethod);

            rbSelectGroup = new RadioButton
            {
                Text = "从列表选择",
                Location = new Point(x + labelWidth + 5, y),
                AutoSize = true,
                Checked = true
            };
            rbSelectGroup.CheckedChanged += (s, e) => UpdateGroupInputMode();

            rbManualGroup = new RadioButton
            {
                Text = "手动输入",
                Location = new Point(x + labelWidth + 120, y),
                AutoSize = true
            };
            rbManualGroup.CheckedChanged += (s, e) => UpdateGroupInputMode();

            contentPanel.Controls.Add(rbSelectGroup);
            contentPanel.Controls.Add(rbManualGroup);
            y += 28;

            // 群选择面板（下拉框）
            pnlGroupSelect = new Panel
            {
                Location = new Point(x + labelWidth + 5, y),
                Size = new Size(textWidth, 28)
            };

            cboGroupId = new ComboBox
            {
                Location = new Point(0, 0),
                Size = new Size(textWidth - 35, 23),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cboGroupId.Items.Add("-- 请先登录旺商聊 --");
            cboGroupId.SelectedIndex = 0;

            btnRefreshGroups = new Button
            {
                Text = "🔄",
                Location = new Point(textWidth - 30, 0),
                Size = new Size(30, 23),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnRefreshGroups.FlatAppearance.BorderSize = 1;
            btnRefreshGroups.Click += async (s, e) => await RefreshGroupsAsync();

            pnlGroupSelect.Controls.Add(cboGroupId);
            pnlGroupSelect.Controls.Add(btnRefreshGroups);
            contentPanel.Controls.Add(pnlGroupSelect);

            // 手动输入面板
            pnlGroupManual = new Panel
            {
                Location = new Point(x + labelWidth + 5, y),
                Size = new Size(textWidth, 28),
                Visible = false
            };

            txtGroupIdManual = new TextBox
            {
                Location = new Point(0, 0),
                Size = new Size(textWidth, 23)
            };
            pnlGroupManual.Controls.Add(txtGroupIdManual);
            contentPanel.Controls.Add(pnlGroupManual);
            y += 35;

            // 提示信息
            var lblTip = new Label
            {
                Text = "💡 提示：请先打开旺商聊并登录，然后点击🔄刷新获取群列表",
                Location = new Point(x, y),
                Size = new Size(390, 20),
                ForeColor = Color.Gray,
                Font = new Font("Microsoft YaHei UI", 8F)
            };
            contentPanel.Controls.Add(lblTip);
            y += 25;

            // 记住密码
            chkRememberPassword = new CheckBox
            {
                Text = "记住密码",
                Location = new Point(x + labelWidth + 5, y),
                AutoSize = true,
                Checked = true
            };
            contentPanel.Controls.Add(chkRememberPassword);
            y += 40;

            // 按钮
            btnOk = new Button
            {
                Text = "确定",
                Location = new Point(140, y),
                Size = new Size(80, 32),
                BackColor = Color.FromArgb(76, 175, 80),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnOk.FlatAppearance.BorderSize = 0;
            btnOk.Click += BtnOk_Click;

            btnCancel = new Button
            {
                Text = "取消",
                Location = new Point(230, y),
                Size = new Size(80, 32),
                BackColor = Color.FromArgb(224, 224, 224),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnCancel.FlatAppearance.BorderSize = 0;
            btnCancel.Click += (s, e) => { this.DialogResult = DialogResult.Cancel; this.Close(); };

            contentPanel.Controls.Add(btnOk);
            contentPanel.Controls.Add(btnCancel);

            mainPanel.Controls.Add(contentPanel);
            mainPanel.Controls.Add(titlePanel);

            this.Controls.Add(mainPanel);

            // 添加边框
            this.Paint += (s, e) =>
            {
                using (var pen = new Pen(Color.FromArgb(76, 175, 80), 2))
                {
                    e.Graphics.DrawRectangle(pen, 0, 0, this.Width - 1, this.Height - 1);
                }
            };

            // 窗口拖动
            titlePanel.MouseDown += (s, e) =>
            {
                if (e.Button == MouseButtons.Left)
                {
                    ReleaseCapture();
                    SendMessage(this.Handle, 0xA1, 0x2, 0);
                }
            };
        }

        /// <summary>
        /// 切换群号输入模式
        /// </summary>
        private void UpdateGroupInputMode()
        {
            pnlGroupSelect.Visible = rbSelectGroup.Checked;
            pnlGroupManual.Visible = rbManualGroup.Checked;
        }

        /// <summary>
        /// 尝试从 CDP 获取信息
        /// </summary>
        private async Task TryLoadFromCDPAsync()
        {
            try
            {
                lblStatus.Text = "⏳ 正在检测旺商聊...";
                lblStatus.ForeColor = Color.Gray;

                var cdp = CDPService.Instance;
                cdp.OnLog += msg => Logger.Info(msg);

                var connected = await cdp.CheckConnectionAsync();

                if (!connected)
                {
                    lblStatus.Text = "⚠️ 未检测到旺商聊（请用调试模式启动旺商聊）";
                    lblStatus.ForeColor = Color.Orange;
                    return;
                }

                // 获取用户信息
                _cdpUserInfo = await cdp.GetCurrentUserAsync();

                if (_cdpUserInfo != null && !string.IsNullOrEmpty(_cdpUserInfo.Wwid))
                {
                    lblStatus.Text = "✅ 已检测到旺商聊登录";
                    lblStatus.ForeColor = Color.FromArgb(76, 175, 80);

                    // 使用 AccountId 作为精确的账号名称
                    var displayAccountName = !string.IsNullOrEmpty(_cdpUserInfo.AccountId) 
                        ? _cdpUserInfo.AccountId 
                        : _cdpUserInfo.Wwid;
                    lblNickname.Text = $"👤 当前登录: {displayAccountName} (昵称: {_cdpUserInfo.Nickname})";
                    lblNickname.Visible = true;

                    // 自动填充信息 - 使用 AccountId 作为机器人名称
                    txtAccount.Text = _cdpUserInfo.Wwid;
                    txtBotName.Text = displayAccountName;

                    // 获取群列表
                    await RefreshGroupsAsync();
                }
                else
                {
                    lblStatus.Text = "⚠️ 旺商聊未登录，请先登录旺商聊";
                    lblStatus.ForeColor = Color.Orange;
                }
            }
            catch (Exception ex)
            {
                lblStatus.Text = $"❌ 检测失败: {ex.Message}";
                lblStatus.ForeColor = Color.Red;
                Logger.Error($"TryLoadFromCDPAsync: {ex}");
            }
        }

        /// <summary>
        /// 刷新群列表
        /// </summary>
        private async Task RefreshGroupsAsync()
        {
            try
            {
                btnRefreshGroups.Enabled = false;
                btnRefreshGroups.Text = "...";

                var cdp = CDPService.Instance;
                var groups = await cdp.GetGroupListAsync();

                cboGroupId.Items.Clear();

                if (groups.Count > 0)
                {
                    foreach (var g in groups)
                    {
                        cboGroupId.Items.Add(g);
                    }
                    cboGroupId.SelectedIndex = 0;

                    lblStatus.Text = $"✅ 获取到 {groups.Count} 个群";
                    lblStatus.ForeColor = Color.FromArgb(76, 175, 80);
                }
                else
                {
                    cboGroupId.Items.Add("-- 未找到群，请检查旺商聊登录 --");
                    cboGroupId.SelectedIndex = 0;

                    lblStatus.Text = "⚠️ 未获取到群列表";
                    lblStatus.ForeColor = Color.Orange;
                }
            }
            catch (Exception ex)
            {
                lblStatus.Text = $"❌ 获取群列表失败: {ex.Message}";
                lblStatus.ForeColor = Color.Red;

                cboGroupId.Items.Clear();
                cboGroupId.Items.Add("-- 获取失败，请重试 --");
                cboGroupId.SelectedIndex = 0;
            }
            finally
            {
                btnRefreshGroups.Enabled = true;
                btnRefreshGroups.Text = "🔄";
            }
        }

        private void LoadAccount(BotAccount account)
        {
            txtAccount.Text = account.Account;
            txtBotName.Text = account.BotName;
            txtPassword.Text = account.GetPassword();
            chkRememberPassword.Checked = account.RememberPassword;

            // 手动输入已有群号
            rbManualGroup.Checked = true;
            txtGroupIdManual.Text = account.GroupId;

            // 编辑模式下账号不可修改
            txtAccount.ReadOnly = true;
            txtAccount.BackColor = Color.FromArgb(240, 240, 240);

            lblStatus.Text = "📝 编辑模式";
            lblStatus.ForeColor = Color.Blue;
        }

        private void BtnOk_Click(object sender, EventArgs e)
        {
            // 验证输入
            if (string.IsNullOrWhiteSpace(txtAccount.Text))
            {
                MessageBox.Show("请输入旺商聊账号（WWID）", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtAccount.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(txtPassword.Text))
            {
                MessageBox.Show("请输入登录密码", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtPassword.Focus();
                return;
            }

            // 获取群号
            string groupId = "";
            if (rbSelectGroup.Checked)
            {
                var selected = cboGroupId.SelectedItem as WslGroupInfo;
                if (selected == null)
                {
                    MessageBox.Show("请选择要绑定的群", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                groupId = selected.GroupId;
            }
            else
            {
                groupId = txtGroupIdManual.Text.Trim();
                if (string.IsNullOrWhiteSpace(groupId))
                {
                    MessageBox.Show("请输入绑定群号", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtGroupIdManual.Focus();
                    return;
                }
            }

            // 创建结果
            ResultAccount = new BotAccount
            {
                Account = txtAccount.Text.Trim(),
                BotName = txtBotName.Text.Trim(),
                GroupId = groupId,
                RememberPassword = chkRememberPassword.Checked
            };

            // 如果有 CDP 信息，使用真实的 Wwid 和 Nickname
            if (_cdpUserInfo != null)
            {
                ResultAccount.Wwid = _cdpUserInfo.Wwid;
                ResultAccount.Nickname = _cdpUserInfo.Nickname;
                ResultAccount.NimAccid = _cdpUserInfo.NimId;
                ResultAccount.NimToken = _cdpUserInfo.NimToken;
            }

            if (chkRememberPassword.Checked)
            {
                ResultAccount.SetPassword(txtPassword.Text);
            }

            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        // P/Invoke
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool ReleaseCapture();
    }
}
