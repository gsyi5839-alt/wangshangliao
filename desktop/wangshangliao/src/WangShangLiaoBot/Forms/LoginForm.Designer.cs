namespace WangShangLiaoBot.Forms
{
    partial class LoginForm
    {
        private System.ComponentModel.IContainer components = null;
        
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }
        
        #region Windows Form Designer generated code
        
        private void InitializeComponent()
        {
            this.tabControl = new System.Windows.Forms.TabControl();
            this.tabLogin = new System.Windows.Forms.TabPage();
            this.tabRegister = new System.Windows.Forms.TabPage();
            this.tabRecharge = new System.Windows.Forms.TabPage();
            this.tabChangePassword = new System.Windows.Forms.TabPage();
            
            // 登录页控件
            this.lblUsername = new System.Windows.Forms.Label();
            this.lblPassword = new System.Windows.Forms.Label();
            this.txtUsername = new System.Windows.Forms.TextBox();
            this.txtPassword = new System.Windows.Forms.TextBox();
            this.chkRemember = new System.Windows.Forms.CheckBox();
            this.btnLogin = new System.Windows.Forms.Button();
            this.lblStatus = new System.Windows.Forms.Label();
            this.lblTitle = new System.Windows.Forms.Label();
            this.linkRegister = new System.Windows.Forms.LinkLabel();
            this.linkRecharge = new System.Windows.Forms.LinkLabel();
            this.linkChangePassword = new System.Windows.Forms.LinkLabel();
            
            // 注册页控件
            this.lblRegUsername = new System.Windows.Forms.Label();
            this.lblRegPassword = new System.Windows.Forms.Label();
            this.lblRegSuperPassword = new System.Windows.Forms.Label();
            this.lblRegCard = new System.Windows.Forms.Label();
            this.lblRegBindInfo = new System.Windows.Forms.Label();
            this.lblRegPromoter = new System.Windows.Forms.Label();
            this.txtRegUsername = new System.Windows.Forms.TextBox();
            this.txtRegPassword = new System.Windows.Forms.TextBox();
            this.txtRegSuperPassword = new System.Windows.Forms.TextBox();
            this.txtRegCard = new System.Windows.Forms.TextBox();
            this.txtRegBindInfo = new System.Windows.Forms.TextBox();
            this.txtRegPromoter = new System.Windows.Forms.TextBox();
            this.btnDoRegister = new System.Windows.Forms.Button();
            
            // 充值页控件
            this.lblRechargeUsername = new System.Windows.Forms.Label();
            this.lblRechargeCard = new System.Windows.Forms.Label();
            this.txtRechargeUsername = new System.Windows.Forms.TextBox();
            this.txtRechargeCard = new System.Windows.Forms.TextBox();
            this.btnDoRecharge = new System.Windows.Forms.Button();
            
            // 修改密码页控件
            this.lblChangePwdUsername = new System.Windows.Forms.Label();
            this.lblChangePwdSuperPassword = new System.Windows.Forms.Label();
            this.lblChangePwdNewPassword = new System.Windows.Forms.Label();
            this.txtChangePwdUsername = new System.Windows.Forms.TextBox();
            this.txtChangePwdSuperPassword = new System.Windows.Forms.TextBox();
            this.txtChangePwdNewPassword = new System.Windows.Forms.TextBox();
            this.btnDoChangePassword = new System.Windows.Forms.Button();
            
            this.tabControl.SuspendLayout();
            this.tabLogin.SuspendLayout();
            this.tabRegister.SuspendLayout();
            this.tabRecharge.SuspendLayout();
            this.tabChangePassword.SuspendLayout();
            this.SuspendLayout();
            
            // ===== TabControl =====
            this.tabControl.Controls.Add(this.tabLogin);
            this.tabControl.Controls.Add(this.tabRegister);
            this.tabControl.Controls.Add(this.tabRecharge);
            this.tabControl.Controls.Add(this.tabChangePassword);
            this.tabControl.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabControl.Location = new System.Drawing.Point(0, 0);
            this.tabControl.Name = "tabControl";
            this.tabControl.SelectedIndex = 0;
            this.tabControl.Size = new System.Drawing.Size(450, 380);
            
            // ===== 登录页 =====
            this.tabLogin.Text = "登录";
            this.tabLogin.Padding = new System.Windows.Forms.Padding(20);
            this.tabLogin.Controls.Add(this.lblTitle);
            this.tabLogin.Controls.Add(this.lblUsername);
            this.tabLogin.Controls.Add(this.txtUsername);
            this.tabLogin.Controls.Add(this.lblPassword);
            this.tabLogin.Controls.Add(this.txtPassword);
            this.tabLogin.Controls.Add(this.chkRemember);
            this.tabLogin.Controls.Add(this.btnLogin);
            this.tabLogin.Controls.Add(this.lblStatus);
            this.tabLogin.Controls.Add(this.linkRegister);
            this.tabLogin.Controls.Add(this.linkRecharge);
            this.tabLogin.Controls.Add(this.linkChangePassword);
            
            // 标题
            this.lblTitle.Text = "旺商聊自动化机器人";
            this.lblTitle.Font = new System.Drawing.Font("微软雅黑", 16F, System.Drawing.FontStyle.Bold);
            this.lblTitle.ForeColor = System.Drawing.Color.FromArgb(51, 51, 51);
            this.lblTitle.Location = new System.Drawing.Point(105, 25);
            this.lblTitle.Size = new System.Drawing.Size(240, 35);
            this.lblTitle.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            
            // 账号
            this.lblUsername.Text = "账号:";
            this.lblUsername.Location = new System.Drawing.Point(55, 85);
            this.lblUsername.AutoSize = true;
            
            this.txtUsername.Location = new System.Drawing.Point(160, 82);
            this.txtUsername.Size = new System.Drawing.Size(210, 25);
            this.txtUsername.ImeMode = System.Windows.Forms.ImeMode.Off;
            
            // 密码
            this.lblPassword.Text = "密码:";
            this.lblPassword.Location = new System.Drawing.Point(55, 125);
            this.lblPassword.AutoSize = true;
            
            this.txtPassword.Location = new System.Drawing.Point(160, 122);
            this.txtPassword.Size = new System.Drawing.Size(210, 25);
            this.txtPassword.PasswordChar = '*';
            this.txtPassword.ImeMode = System.Windows.Forms.ImeMode.Off;
            this.txtPassword.KeyDown += new System.Windows.Forms.KeyEventHandler(this.txtPassword_KeyDown);
            
            // 记住用户
            this.chkRemember.Text = "记住用户";
            this.chkRemember.Location = new System.Drawing.Point(160, 162);
            this.chkRemember.Size = new System.Drawing.Size(100, 25);
            
            // 登录按钮
            this.btnLogin.Text = "登录";
            this.btnLogin.Location = new System.Drawing.Point(160, 200);
            this.btnLogin.Size = new System.Drawing.Size(210, 35);
            this.btnLogin.BackColor = System.Drawing.Color.FromArgb(0, 122, 204);
            this.btnLogin.ForeColor = System.Drawing.Color.White;
            this.btnLogin.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnLogin.Click += new System.EventHandler(this.btnLogin_Click);
            
            // 状态标签
            this.lblStatus.Location = new System.Drawing.Point(65, 250);
            this.lblStatus.Size = new System.Drawing.Size(330, 25);
            this.lblStatus.ForeColor = System.Drawing.Color.Gray;
            
            // 链接
            this.linkRegister.Text = "注册账号";
            this.linkRegister.Location = new System.Drawing.Point(95, 295);
            this.linkRegister.Size = new System.Drawing.Size(70, 20);
            this.linkRegister.Click += new System.EventHandler(this.btnRegister_Click);
            
            this.linkRecharge.Text = "续费充值";
            this.linkRecharge.Location = new System.Drawing.Point(195, 295);
            this.linkRecharge.Size = new System.Drawing.Size(70, 20);
            this.linkRecharge.Click += new System.EventHandler(this.btnRecharge_Click);
            
            this.linkChangePassword.Text = "修改密码";
            this.linkChangePassword.Location = new System.Drawing.Point(295, 295);
            this.linkChangePassword.Size = new System.Drawing.Size(70, 20);
            this.linkChangePassword.Click += new System.EventHandler(this.btnChangePassword_Click);
            
            // ===== 注册页 =====
            this.tabRegister.Text = "注册";
            this.tabRegister.Padding = new System.Windows.Forms.Padding(20);
            this.tabRegister.Controls.Add(this.lblRegUsername);
            this.tabRegister.Controls.Add(this.txtRegUsername);
            this.tabRegister.Controls.Add(this.lblRegPassword);
            this.tabRegister.Controls.Add(this.txtRegPassword);
            this.tabRegister.Controls.Add(this.lblRegSuperPassword);
            this.tabRegister.Controls.Add(this.txtRegSuperPassword);
            this.tabRegister.Controls.Add(this.lblRegCard);
            this.tabRegister.Controls.Add(this.txtRegCard);
            this.tabRegister.Controls.Add(this.lblRegBindInfo);
            this.tabRegister.Controls.Add(this.txtRegBindInfo);
            this.tabRegister.Controls.Add(this.lblRegPromoter);
            this.tabRegister.Controls.Add(this.txtRegPromoter);
            this.tabRegister.Controls.Add(this.btnDoRegister);
            
            // 账号
            this.lblRegUsername.Text = "账号:";
            this.lblRegUsername.Location = new System.Drawing.Point(55, 35);
            this.lblRegUsername.AutoSize = true;
            
            this.txtRegUsername.Location = new System.Drawing.Point(160, 32);
            this.txtRegUsername.Size = new System.Drawing.Size(210, 25);
            this.txtRegUsername.ImeMode = System.Windows.Forms.ImeMode.Off;
            
            // 密码
            this.lblRegPassword.Text = "密码:";
            this.lblRegPassword.Location = new System.Drawing.Point(55, 70);
            this.lblRegPassword.AutoSize = true;
            
            this.txtRegPassword.Location = new System.Drawing.Point(160, 67);
            this.txtRegPassword.Size = new System.Drawing.Size(210, 25);
            this.txtRegPassword.PasswordChar = '*';
            this.txtRegPassword.ImeMode = System.Windows.Forms.ImeMode.Off;
            
            // 超级密码
            this.lblRegSuperPassword.Text = "超级密码:";
            this.lblRegSuperPassword.Location = new System.Drawing.Point(55, 105);
            this.lblRegSuperPassword.AutoSize = true;
            
            this.txtRegSuperPassword.Location = new System.Drawing.Point(160, 102);
            this.txtRegSuperPassword.Size = new System.Drawing.Size(210, 25);
            this.txtRegSuperPassword.PasswordChar = '*';
            this.txtRegSuperPassword.ImeMode = System.Windows.Forms.ImeMode.Off;
            
            // 卡密
            this.lblRegCard.Text = "卡密(18位):";
            this.lblRegCard.Location = new System.Drawing.Point(55, 140);
            this.lblRegCard.AutoSize = true;
            
            this.txtRegCard.Location = new System.Drawing.Point(160, 137);
            this.txtRegCard.Size = new System.Drawing.Size(210, 25);
            this.txtRegCard.Multiline = false;
            this.txtRegCard.MaxLength = 18;
            this.txtRegCard.ImeMode = System.Windows.Forms.ImeMode.Off;

            // 绑定信息（可空）
            this.lblRegBindInfo.Text = "绑定信息:";
            this.lblRegBindInfo.Location = new System.Drawing.Point(55, 175);
            this.lblRegBindInfo.AutoSize = true;
            
            this.txtRegBindInfo.Location = new System.Drawing.Point(160, 172);
            this.txtRegBindInfo.Size = new System.Drawing.Size(210, 25);
            this.txtRegBindInfo.ImeMode = System.Windows.Forms.ImeMode.Off;

            // 推广员账号（可空）
            this.lblRegPromoter.Text = "推广员:";
            this.lblRegPromoter.Location = new System.Drawing.Point(55, 210);
            this.lblRegPromoter.AutoSize = true;
            
            this.txtRegPromoter.Location = new System.Drawing.Point(160, 207);
            this.txtRegPromoter.Size = new System.Drawing.Size(210, 25);
            this.txtRegPromoter.ImeMode = System.Windows.Forms.ImeMode.Off;
            
            this.btnDoRegister.Text = "注册";
            this.btnDoRegister.Location = new System.Drawing.Point(160, 255);
            this.btnDoRegister.Size = new System.Drawing.Size(210, 35);
            this.btnDoRegister.BackColor = System.Drawing.Color.FromArgb(40, 167, 69);
            this.btnDoRegister.ForeColor = System.Drawing.Color.White;
            this.btnDoRegister.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnDoRegister.Click += new System.EventHandler(this.btnDoRegister_Click);
            
            // ===== 充值页 =====
            this.tabRecharge.Text = "续费充值";
            this.tabRecharge.Padding = new System.Windows.Forms.Padding(20);
            this.tabRecharge.Controls.Add(this.lblRechargeUsername);
            this.tabRecharge.Controls.Add(this.txtRechargeUsername);
            this.tabRecharge.Controls.Add(this.lblRechargeCard);
            this.tabRecharge.Controls.Add(this.txtRechargeCard);
            this.tabRecharge.Controls.Add(this.btnDoRecharge);
            
            // 账号
            this.lblRechargeUsername.Text = "账号:";
            this.lblRechargeUsername.Location = new System.Drawing.Point(55, 55);
            this.lblRechargeUsername.AutoSize = true;
            
            this.txtRechargeUsername.Location = new System.Drawing.Point(160, 52);
            this.txtRechargeUsername.Size = new System.Drawing.Size(210, 25);
            this.txtRechargeUsername.ImeMode = System.Windows.Forms.ImeMode.Off;
            
            // 卡密
            this.lblRechargeCard.Text = "卡密(18位):";
            this.lblRechargeCard.Location = new System.Drawing.Point(55, 100);
            this.lblRechargeCard.AutoSize = true;
            
            this.txtRechargeCard.Location = new System.Drawing.Point(160, 97);
            this.txtRechargeCard.Size = new System.Drawing.Size(210, 25);
            this.txtRechargeCard.Multiline = false;
            this.txtRechargeCard.MaxLength = 18;
            this.txtRechargeCard.ImeMode = System.Windows.Forms.ImeMode.Off;
            
            // 充值按钮
            this.btnDoRecharge.Text = "充值";
            this.btnDoRecharge.Location = new System.Drawing.Point(160, 150);
            this.btnDoRecharge.Size = new System.Drawing.Size(210, 35);
            this.btnDoRecharge.BackColor = System.Drawing.Color.FromArgb(255, 193, 7);
            this.btnDoRecharge.ForeColor = System.Drawing.Color.Black;
            this.btnDoRecharge.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnDoRecharge.Click += new System.EventHandler(this.btnDoRecharge_Click);
            
            // ===== 修改密码页 =====
            this.tabChangePassword.Text = "修改密码";
            this.tabChangePassword.Padding = new System.Windows.Forms.Padding(20);
            this.tabChangePassword.Controls.Add(this.lblChangePwdUsername);
            this.tabChangePassword.Controls.Add(this.txtChangePwdUsername);
            this.tabChangePassword.Controls.Add(this.lblChangePwdSuperPassword);
            this.tabChangePassword.Controls.Add(this.txtChangePwdSuperPassword);
            this.tabChangePassword.Controls.Add(this.lblChangePwdNewPassword);
            this.tabChangePassword.Controls.Add(this.txtChangePwdNewPassword);
            this.tabChangePassword.Controls.Add(this.btnDoChangePassword);
            
            // 账号
            this.lblChangePwdUsername.Text = "账号:";
            this.lblChangePwdUsername.Location = new System.Drawing.Point(55, 55);
            this.lblChangePwdUsername.AutoSize = true;
            
            this.txtChangePwdUsername.Location = new System.Drawing.Point(160, 52);
            this.txtChangePwdUsername.Size = new System.Drawing.Size(210, 25);
            this.txtChangePwdUsername.ImeMode = System.Windows.Forms.ImeMode.Off;
            
            // 超级密码
            this.lblChangePwdSuperPassword.Text = "超级密码:";
            this.lblChangePwdSuperPassword.Location = new System.Drawing.Point(55, 100);
            this.lblChangePwdSuperPassword.AutoSize = true;
            
            this.txtChangePwdSuperPassword.Location = new System.Drawing.Point(160, 97);
            this.txtChangePwdSuperPassword.Size = new System.Drawing.Size(210, 25);
            this.txtChangePwdSuperPassword.PasswordChar = '*';
            this.txtChangePwdSuperPassword.ImeMode = System.Windows.Forms.ImeMode.Off;
            
            // 新密码
            this.lblChangePwdNewPassword.Text = "新密码:";
            this.lblChangePwdNewPassword.Location = new System.Drawing.Point(55, 145);
            this.lblChangePwdNewPassword.AutoSize = true;
            
            this.txtChangePwdNewPassword.Location = new System.Drawing.Point(160, 142);
            this.txtChangePwdNewPassword.Size = new System.Drawing.Size(210, 25);
            this.txtChangePwdNewPassword.PasswordChar = '*';
            this.txtChangePwdNewPassword.ImeMode = System.Windows.Forms.ImeMode.Off;
            
            // 修改密码按钮
            this.btnDoChangePassword.Text = "修改密码";
            this.btnDoChangePassword.Location = new System.Drawing.Point(160, 195);
            this.btnDoChangePassword.Size = new System.Drawing.Size(210, 35);
            this.btnDoChangePassword.BackColor = System.Drawing.Color.FromArgb(108, 117, 125);
            this.btnDoChangePassword.ForeColor = System.Drawing.Color.White;
            this.btnDoChangePassword.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnDoChangePassword.Click += new System.EventHandler(this.btnDoChangePassword_Click);
            
            // ===== Form =====
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(450, 380);
            this.Controls.Add(this.tabControl);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.Name = "LoginForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "旺商聊机器人 - 登录";
            this.ImeMode = System.Windows.Forms.ImeMode.Off;
            this.DoubleBuffered = true;
            
            // 设置窗体图标
            try
            {
                var iconPath = System.IO.Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "Resources", "app.ico");
                if (System.IO.File.Exists(iconPath))
                {
                    this.Icon = new System.Drawing.Icon(iconPath);
                }
            }
            catch { }
            
            this.tabControl.ResumeLayout(false);
            this.tabLogin.ResumeLayout(false);
            this.tabRegister.ResumeLayout(false);
            this.tabRecharge.ResumeLayout(false);
            this.tabChangePassword.ResumeLayout(false);
            this.ResumeLayout(false);
        }
        
        #endregion
        
        private System.Windows.Forms.TabControl tabControl;
        private System.Windows.Forms.TabPage tabLogin;
        private System.Windows.Forms.TabPage tabRegister;
        private System.Windows.Forms.TabPage tabRecharge;
        private System.Windows.Forms.TabPage tabChangePassword;
        
        // 登录页
        private System.Windows.Forms.Label lblTitle;
        private System.Windows.Forms.Label lblUsername;
        private System.Windows.Forms.Label lblPassword;
        private System.Windows.Forms.TextBox txtUsername;
        private System.Windows.Forms.TextBox txtPassword;
        private System.Windows.Forms.CheckBox chkRemember;
        private System.Windows.Forms.Button btnLogin;
        private System.Windows.Forms.Label lblStatus;
        private System.Windows.Forms.LinkLabel linkRegister;
        private System.Windows.Forms.LinkLabel linkRecharge;
        private System.Windows.Forms.LinkLabel linkChangePassword;
        
        // 注册页
        private System.Windows.Forms.Label lblRegUsername;
        private System.Windows.Forms.Label lblRegPassword;
        private System.Windows.Forms.Label lblRegSuperPassword;
        private System.Windows.Forms.Label lblRegCard;
        private System.Windows.Forms.Label lblRegBindInfo;
        private System.Windows.Forms.Label lblRegPromoter;
        private System.Windows.Forms.TextBox txtRegUsername;
        private System.Windows.Forms.TextBox txtRegPassword;
        private System.Windows.Forms.TextBox txtRegSuperPassword;
        private System.Windows.Forms.TextBox txtRegCard;
        private System.Windows.Forms.TextBox txtRegBindInfo;
        private System.Windows.Forms.TextBox txtRegPromoter;
        private System.Windows.Forms.Button btnDoRegister;
        
        // 充值页
        private System.Windows.Forms.Label lblRechargeUsername;
        private System.Windows.Forms.Label lblRechargeCard;
        private System.Windows.Forms.TextBox txtRechargeUsername;
        private System.Windows.Forms.TextBox txtRechargeCard;
        private System.Windows.Forms.Button btnDoRecharge;
        
        // 修改密码页
        private System.Windows.Forms.Label lblChangePwdUsername;
        private System.Windows.Forms.Label lblChangePwdSuperPassword;
        private System.Windows.Forms.Label lblChangePwdNewPassword;
        private System.Windows.Forms.TextBox txtChangePwdUsername;
        private System.Windows.Forms.TextBox txtChangePwdSuperPassword;
        private System.Windows.Forms.TextBox txtChangePwdNewPassword;
        private System.Windows.Forms.Button btnDoChangePassword;
    }
}

