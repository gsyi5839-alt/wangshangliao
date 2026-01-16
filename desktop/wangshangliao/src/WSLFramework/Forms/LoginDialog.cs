using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace WSLFramework.Forms
{
    /// <summary>
    /// 旺商聊账号登录对话框
    /// </summary>
    public class LoginDialog : Form
    {
        private TextBox txtAccount;
        private TextBox txtNickname;
        private TextBox txtPassword;
        private TextBox txtGroupId;
        private Button btnOK;
        private Button btnCancel;
        private CheckBox chkRemember;
        
        /// <summary>
        /// 获取输入的账号
        /// </summary>
        public string Account => txtAccount.Text.Trim();
        
        /// <summary>
        /// 获取输入的机器人名称
        /// </summary>
        public string Nickname => txtNickname.Text.Trim();
        
        /// <summary>
        /// 获取输入的密码
        /// </summary>
        public string Password => txtPassword.Text;
        
        /// <summary>
        /// 获取输入的群聊号
        /// </summary>
        public string GroupId => txtGroupId.Text.Trim();
        
        /// <summary>
        /// 是否记住密码
        /// </summary>
        public bool RememberPassword => chkRemember.Checked;
        
        public LoginDialog()
        {
            InitializeComponent();
            LoadSavedCredentials();
        }
        
        private void InitializeComponent()
        {
            this.Text = "添加旺商聊机器人账号";
            this.Size = new Size(380, 320);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Font = new Font("Microsoft YaHei UI", 9F);
            this.BackColor = Color.White;
            
            // 标题面板
            var pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 45,
                BackColor = Color.FromArgb(76, 175, 80)
            };
            
            pnlHeader.Paint += (s, e) =>
            {
                using (var brush = new LinearGradientBrush(
                    pnlHeader.ClientRectangle,
                    Color.FromArgb(102, 187, 106),
                    Color.FromArgb(76, 175, 80),
                    LinearGradientMode.Horizontal))
                {
                    e.Graphics.FillRectangle(brush, pnlHeader.ClientRectangle);
                }
            };
            
            var lblTitle = new Label
            {
                Text = "🤖 添加旺商聊机器人",
                Font = new Font("Microsoft YaHei UI", 12F, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize = true,
                Location = new Point(15, 12),
                BackColor = Color.Transparent
            };
            pnlHeader.Controls.Add(lblTitle);
            
            // 账号
            var lblAccount = new Label
            {
                Text = "旺商聊账号:",
                Location = new Point(20, 60),
                AutoSize = true
            };
            
            txtAccount = new TextBox
            {
                Location = new Point(110, 57),
                Size = new Size(230, 25)
            };
            
            // 机器人名称
            var lblNickname = new Label
            {
                Text = "机器人名称:",
                Location = new Point(20, 95),
                AutoSize = true
            };
            
            txtNickname = new TextBox
            {
                Location = new Point(110, 92),
                Size = new Size(230, 25)
            };
            
            // 密码
            var lblPassword = new Label
            {
                Text = "登录密码:",
                Location = new Point(20, 130),
                AutoSize = true
            };
            
            txtPassword = new TextBox
            {
                Location = new Point(110, 127),
                Size = new Size(230, 25),
                PasswordChar = '●'
            };
            
            // 群聊号
            var lblGroupId = new Label
            {
                Text = "绑定群号:",
                Location = new Point(20, 165),
                AutoSize = true
            };
            
            txtGroupId = new TextBox
            {
                Location = new Point(110, 162),
                Size = new Size(230, 25)
            };
            
            // 记住密码
            chkRemember = new CheckBox
            {
                Text = "记住密码",
                Location = new Point(110, 195),
                AutoSize = true,
                Checked = true
            };
            
            // 确定按钮
            btnOK = new Button
            {
                Text = "确定",
                Size = new Size(90, 32),
                Location = new Point(140, 230),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(76, 175, 80),
                ForeColor = Color.White,
                Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold),
                DialogResult = DialogResult.OK
            };
            btnOK.FlatAppearance.BorderSize = 0;
            btnOK.Click += BtnOK_Click;
            
            // 取消按钮
            btnCancel = new Button
            {
                Text = "取消",
                Size = new Size(90, 32),
                Location = new Point(250, 230),
                FlatStyle = FlatStyle.Flat,
                DialogResult = DialogResult.Cancel
            };
            btnCancel.FlatAppearance.BorderColor = Color.Gray;
            
            this.Controls.Add(pnlHeader);
            this.Controls.Add(lblAccount);
            this.Controls.Add(txtAccount);
            this.Controls.Add(lblNickname);
            this.Controls.Add(txtNickname);
            this.Controls.Add(lblPassword);
            this.Controls.Add(txtPassword);
            this.Controls.Add(lblGroupId);
            this.Controls.Add(txtGroupId);
            this.Controls.Add(chkRemember);
            this.Controls.Add(btnOK);
            this.Controls.Add(btnCancel);
            
            this.AcceptButton = btnOK;
            this.CancelButton = btnCancel;
        }
        
        private void BtnOK_Click(object sender, EventArgs e)
        {
            // 验证输入
            if (string.IsNullOrWhiteSpace(txtAccount.Text))
            {
                MessageBox.Show("请输入旺商聊账号", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtAccount.Focus();
                this.DialogResult = DialogResult.None;
                return;
            }
            
            if (string.IsNullOrWhiteSpace(txtPassword.Text))
            {
                MessageBox.Show("请输入登录密码", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtPassword.Focus();
                this.DialogResult = DialogResult.None;
                return;
            }
            
            if (string.IsNullOrWhiteSpace(txtGroupId.Text))
            {
                MessageBox.Show("请输入绑定群号", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtGroupId.Focus();
                this.DialogResult = DialogResult.None;
                return;
            }
            
            // 保存凭证
            if (chkRemember.Checked)
            {
                SaveCredentials();
            }
            
            this.DialogResult = DialogResult.OK;
        }
        
        /// <summary>
        /// 加载保存的凭证
        /// </summary>
        private void LoadSavedCredentials()
        {
            try
            {
                var configPath = System.IO.Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory, "config.ini");
                    
                if (System.IO.File.Exists(configPath))
                {
                    var lines = System.IO.File.ReadAllLines(configPath, System.Text.Encoding.UTF8);
                    foreach (var line in lines)
                    {
                        if (line.StartsWith("Account="))
                            txtAccount.Text = line.Substring(8);
                        else if (line.StartsWith("Nickname="))
                            txtNickname.Text = line.Substring(9);
                        else if (line.StartsWith("Password="))
                            txtPassword.Text = DecodePassword(line.Substring(9));
                        else if (line.StartsWith("GroupId="))
                            txtGroupId.Text = line.Substring(8);
                    }
                }
            }
            catch { }
        }
        
        /// <summary>
        /// 保存凭证
        /// </summary>
        private void SaveCredentials()
        {
            try
            {
                var configPath = System.IO.Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory, "config.ini");
                    
                var content = $"Account={txtAccount.Text}\n" +
                             $"Nickname={txtNickname.Text}\n" +
                             $"Password={EncodePassword(txtPassword.Text)}\n" +
                             $"GroupId={txtGroupId.Text}\n";
                             
                System.IO.File.WriteAllText(configPath, content, System.Text.Encoding.UTF8);
            }
            catch { }
        }
        
        /// <summary>
        /// 简单密码编码（Base64）
        /// </summary>
        private string EncodePassword(string password)
        {
            if (string.IsNullOrEmpty(password)) return "";
            var bytes = System.Text.Encoding.UTF8.GetBytes(password);
            return Convert.ToBase64String(bytes);
        }
        
        /// <summary>
        /// 简单密码解码
        /// </summary>
        private string DecodePassword(string encoded)
        {
            if (string.IsNullOrEmpty(encoded)) return "";
            try
            {
                var bytes = Convert.FromBase64String(encoded);
                return System.Text.Encoding.UTF8.GetString(bytes);
            }
            catch
            {
                return "";
            }
        }
    }
}
