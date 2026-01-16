using System;
using System.Drawing;
using System.Windows.Forms;
using WangShangLiaoBot.Models;
using WangShangLiaoBot.Services;

namespace WangShangLiaoBot.Forms
{
    /// <summary>
    /// 账号列表窗体
    /// </summary>
    public partial class AccountListForm : Form
    {
        private ListView listAccounts;
        private Button btnAdd;
        private Button btnAddGroup;
        private Button btnLogin;
        private Button btnLogout;
        private Button btnDelete;
        private Button btnCopy;
        private Button btnAutoLogin;
        private Button btnCancelAuto;
        private Button btnRefresh;
        private Label lblStatus;
        
        public AccountListForm()
        {
            InitializeComponent();
            LoadAccounts();
            
            // Subscribe to account changes
            AccountService.Instance.OnAccountsChanged += LoadAccounts;
        }
        
        private void InitializeComponent()
        {
            this.Text = "账号列表";
            this.Size = new Size(700, 450);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            
            // ListView for accounts
            listAccounts = new ListView();
            listAccounts.View = View.Details;
            listAccounts.FullRowSelect = true;
            listAccounts.GridLines = true;
            listAccounts.Location = new Point(10, 10);
            listAccounts.Size = new Size(560, 350);
            listAccounts.Columns.Add("ID", 40);
            listAccounts.Columns.Add("昵称", 100);
            listAccounts.Columns.Add("旺商号", 90);
            listAccounts.Columns.Add("群号", 90);
            listAccounts.Columns.Add("状态", 70);
            listAccounts.Columns.Add("自动", 50);
            listAccounts.Columns.Add("账号", 100);
            listAccounts.DoubleClick += ListAccounts_DoubleClick;
            this.Controls.Add(listAccounts);
            
            // Buttons panel
            int btnX = 580;
            int btnY = 10;
            int btnW = 95;
            int btnH = 28;
            int gap = 35;
            
            btnAdd = new Button();
            btnAdd.Text = "添加账户";
            btnAdd.Location = new Point(btnX, btnY);
            btnAdd.Size = new Size(btnW, btnH);
            btnAdd.Click += BtnAdd_Click;
            this.Controls.Add(btnAdd);
            
            btnAddGroup = new Button();
            btnAddGroup.Text = "添加群机器人";
            btnAddGroup.Location = new Point(btnX, btnY + gap);
            btnAddGroup.Size = new Size(btnW, btnH);
            btnAddGroup.Click += BtnAddGroup_Click;
            this.Controls.Add(btnAddGroup);
            
            btnLogin = new Button();
            btnLogin.Text = "登录";
            btnLogin.Location = new Point(btnX, btnY + gap * 2);
            btnLogin.Size = new Size(btnW, btnH);
            btnLogin.Click += BtnLogin_Click;
            this.Controls.Add(btnLogin);
            
            btnLogout = new Button();
            btnLogout.Text = "退出登录";
            btnLogout.Location = new Point(btnX, btnY + gap * 3);
            btnLogout.Size = new Size(btnW, btnH);
            btnLogout.Click += BtnLogout_Click;
            this.Controls.Add(btnLogout);
            
            btnDelete = new Button();
            btnDelete.Text = "删除账户";
            btnDelete.Location = new Point(btnX, btnY + gap * 4);
            btnDelete.Size = new Size(btnW, btnH);
            btnDelete.ForeColor = Color.Red;
            btnDelete.Click += BtnDelete_Click;
            this.Controls.Add(btnDelete);
            
            btnCopy = new Button();
            btnCopy.Text = "复制账户";
            btnCopy.Location = new Point(btnX, btnY + gap * 5);
            btnCopy.Size = new Size(btnW, btnH);
            btnCopy.Click += BtnCopy_Click;
            this.Controls.Add(btnCopy);
            
            btnAutoLogin = new Button();
            btnAutoLogin.Text = "设置自动登录";
            btnAutoLogin.Location = new Point(btnX, btnY + gap * 6);
            btnAutoLogin.Size = new Size(btnW, btnH);
            btnAutoLogin.Click += BtnAutoLogin_Click;
            this.Controls.Add(btnAutoLogin);
            
            btnCancelAuto = new Button();
            btnCancelAuto.Text = "取消自动登录";
            btnCancelAuto.Location = new Point(btnX, btnY + gap * 7);
            btnCancelAuto.Size = new Size(btnW, btnH);
            btnCancelAuto.Click += BtnCancelAuto_Click;
            this.Controls.Add(btnCancelAuto);
            
            btnRefresh = new Button();
            btnRefresh.Text = "刷新列表";
            btnRefresh.Location = new Point(btnX, btnY + gap * 8);
            btnRefresh.Size = new Size(btnW, btnH);
            btnRefresh.Click += (s, e) => LoadAccounts();
            this.Controls.Add(btnRefresh);
            
            // Status label
            lblStatus = new Label();
            lblStatus.Text = "账号总数: 0";
            lblStatus.Location = new Point(10, 370);
            lblStatus.Size = new Size(300, 20);
            this.Controls.Add(lblStatus);
        }
        
        /// <summary>
        /// Load accounts to list
        /// </summary>
        private void LoadAccounts()
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(LoadAccounts));
                return;
            }
            
            listAccounts.Items.Clear();
            
            foreach (var acc in AccountService.Instance.Accounts)
            {
                var item = new ListViewItem(acc.Id.ToString());
                item.SubItems.Add(acc.Nickname ?? "");
                item.SubItems.Add(acc.WangWangId ?? "");
                item.SubItems.Add(acc.GroupId ?? "");
                item.SubItems.Add(GetStatusText(acc.Status));
                item.SubItems.Add(acc.AutoLogin ? "√" : "×");
                item.SubItems.Add(acc.Phone ?? "");
                item.Tag = acc;
                
                // Set color based on status
                switch (acc.Status)
                {
                    case AccountStatus.Online:
                        item.ForeColor = Color.Green;
                        break;
                    case AccountStatus.Logging:
                        item.ForeColor = Color.Orange;
                        break;
                    case AccountStatus.Failed:
                        item.ForeColor = Color.Red;
                        break;
                    default:
                        item.ForeColor = Color.Gray;
                        break;
                }
                
                listAccounts.Items.Add(item);
            }
            
            lblStatus.Text = string.Format("账号总数: {0}", AccountService.Instance.Accounts.Count);
        }
        
        private string GetStatusText(AccountStatus status)
        {
            switch (status)
            {
                case AccountStatus.Online: return "登录成功";
                case AccountStatus.Logging: return "登录中";
                case AccountStatus.Failed: return "登录失败";
                default: return "离线";
            }
        }
        
        /// <summary>
        /// Get selected account
        /// </summary>
        private BotAccount GetSelectedAccount()
        {
            if (listAccounts.SelectedItems.Count > 0)
            {
                return listAccounts.SelectedItems[0].Tag as BotAccount;
            }
            return null;
        }
        
        /// <summary>
        /// Double click to edit
        /// </summary>
        private void ListAccounts_DoubleClick(object sender, EventArgs e)
        {
            var account = GetSelectedAccount();
            if (account != null)
            {
                ShowEditDialog(account);
            }
        }
        
        /// <summary>
        /// Add account
        /// </summary>
        private void BtnAdd_Click(object sender, EventArgs e)
        {
            ShowAddDialog(false);
        }
        
        /// <summary>
        /// Add group robot
        /// </summary>
        private void BtnAddGroup_Click(object sender, EventArgs e)
        {
            ShowAddDialog(true);
        }
        
        /// <summary>
        /// Show add dialog with auto-fetch from WangShangLiao (simplified)
        /// </summary>
        private void ShowAddDialog(bool isGroupRobot)
        {
            using (var dialog = new Form())
            {
                dialog.Text = isGroupRobot ? "添加群机器人" : "添加账户";
                dialog.Size = new Size(380, 240);
                dialog.StartPosition = FormStartPosition.CenterParent;
                dialog.FormBorderStyle = FormBorderStyle.FixedDialog;
                dialog.MaximizeBox = false;
                dialog.MinimizeBox = false;
                
                int y = 15;
                int gap = 35;
                
                // Auto-fetch button
                var btnFetch = new Button 
                { 
                    Text = "从旺商聊获取", 
                    Location = new Point(20, y), 
                    Size = new Size(120, 28),
                    BackColor = Color.LightGreen
                };
                var lblFetchStatus = new Label 
                { 
                    Text = "点击自动获取", 
                    Location = new Point(150, y + 5), 
                    Size = new Size(200, 20),
                    ForeColor = Color.Gray
                };
                dialog.Controls.Add(btnFetch);
                dialog.Controls.Add(lblFetchStatus);
                
                y += gap + 5;
                var lblWwid = new Label { Text = "旺商号:", Location = new Point(20, y), Size = new Size(60, 20) };
                var txtWwid = new TextBox { Location = new Point(90, y - 3), Size = new Size(250, 21) };
                dialog.Controls.Add(lblWwid);
                dialog.Controls.Add(txtWwid);
                
                y += gap;
                var lblGroup = new Label { Text = "绑定群:", Location = new Point(20, y), Size = new Size(60, 20) };
                var cmbGroup = new ComboBox { Location = new Point(90, y - 3), Size = new Size(250, 21), DropDownStyle = ComboBoxStyle.DropDownList };
                dialog.Controls.Add(lblGroup);
                dialog.Controls.Add(cmbGroup);
                
                y += gap;
                var lblPwd = new Label { Text = "密码:", Location = new Point(20, y), Size = new Size(60, 20) };
                var txtPwd = new TextBox { Location = new Point(90, y - 3), Size = new Size(250, 21), PasswordChar = '*' };
                dialog.Controls.Add(lblPwd);
                dialog.Controls.Add(txtPwd);
                
                // Store group data for later use
                var groupDataList = new System.Collections.Generic.List<Models.GroupInfo>();
                
                // Fetch button click handler
                btnFetch.Click += async (s, args) =>
                {
                    btnFetch.Enabled = false;
                    lblFetchStatus.Text = "正在获取...";
                    lblFetchStatus.ForeColor = Color.Orange;
                    
                    try
                    {
                        // 检查副框架连接状态
                        var frameworkClient = Services.HPSocket.FrameworkClient.Instance;
                        if (!frameworkClient.IsConnected)
                        {
                            lblFetchStatus.Text = "请先连接副框架";
                            lblFetchStatus.ForeColor = Color.Red;
                            btnFetch.Enabled = true;
                            return;
                        }
                        
                        var info = await ChatService.Instance.GetFullAccountInfoAsync();
                        
                        if (info != null)
                        {
                            txtWwid.Text = info.AccountId;
                            
                            // Fill group dropdown
                            cmbGroup.Items.Clear();
                            groupDataList.Clear();
                            
                            foreach (var g in info.Groups)
                            {
                                cmbGroup.Items.Add($"{g.GroupName} (ID: {g.GroupId})");
                                groupDataList.Add(g);
                            }
                            
                            if (cmbGroup.Items.Count > 0)
                            {
                                cmbGroup.SelectedIndex = 0;
                            }
                            
                            lblFetchStatus.Text = $"✓ 获取成功！({info.Groups.Count}个群)";
                            lblFetchStatus.ForeColor = Color.Green;
                        }
                        else
                        {
                            lblFetchStatus.Text = "获取失败，请确保已登录";
                            lblFetchStatus.ForeColor = Color.Red;
                        }
                    }
                    catch (Exception ex)
                    {
                        lblFetchStatus.Text = "获取异常: " + ex.Message;
                        lblFetchStatus.ForeColor = Color.Red;
                    }
                    
                    btnFetch.Enabled = true;
                };
                
                y += gap + 10;
                var btnOK = new Button { Text = "确定", Location = new Point(160, y), Size = new Size(80, 28), DialogResult = DialogResult.OK };
                var btnCancel = new Button { Text = "取消", Location = new Point(250, y), Size = new Size(80, 28), DialogResult = DialogResult.Cancel };
                dialog.Controls.Add(btnOK);
                dialog.Controls.Add(btnCancel);
                dialog.AcceptButton = btnOK;
                dialog.CancelButton = btnCancel;
                
                if (dialog.ShowDialog(this) == DialogResult.OK)
                {
                    if (string.IsNullOrWhiteSpace(txtWwid.Text))
                    {
                        MessageBox.Show("请输入旺商号", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                    
                    // Get selected group ID
                    string groupId = "";
                    if (cmbGroup.SelectedIndex >= 0 && cmbGroup.SelectedIndex < groupDataList.Count)
                    {
                        groupId = groupDataList[cmbGroup.SelectedIndex].GroupId.ToString();
                    }
                    else if (!string.IsNullOrWhiteSpace(cmbGroup.Text))
                    {
                        // Extract ID from text if manually entered
                        var match = System.Text.RegularExpressions.Regex.Match(cmbGroup.Text, @"\d+");
                        if (match.Success)
                        {
                            groupId = match.Value;
                        }
                    }
                    
                    // Use accountId as nickname
                    AccountService.Instance.AddAccount(
                        txtWwid.Text,  // Use accountId as nickname
                        txtWwid.Text,
                        groupId,
                        "",            // No phone
                        txtPwd.Text    // Password
                    );
                    
                    MessageBox.Show("添加成功！", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }
        
        /// <summary>
        /// Show edit dialog
        /// </summary>
        private void ShowEditDialog(BotAccount account)
        {
            using (var dialog = new Form())
            {
                dialog.Text = "编辑账户";
                dialog.Size = new Size(350, 280);
                dialog.StartPosition = FormStartPosition.CenterParent;
                dialog.FormBorderStyle = FormBorderStyle.FixedDialog;
                dialog.MaximizeBox = false;
                dialog.MinimizeBox = false;
                
                int y = 15;
                int gap = 30;
                
                var lblNick = new Label { Text = "昵称:", Location = new Point(20, y), Size = new Size(60, 20) };
                var txtNick = new TextBox { Text = account.Nickname, Location = new Point(90, y - 3), Size = new Size(220, 21) };
                dialog.Controls.Add(lblNick);
                dialog.Controls.Add(txtNick);
                
                y += gap;
                var lblWwid = new Label { Text = "旺商号:", Location = new Point(20, y), Size = new Size(60, 20) };
                var txtWwid = new TextBox { Text = account.WangWangId, Location = new Point(90, y - 3), Size = new Size(220, 21) };
                dialog.Controls.Add(lblWwid);
                dialog.Controls.Add(txtWwid);
                
                y += gap;
                var lblGroup = new Label { Text = "群号:", Location = new Point(20, y), Size = new Size(60, 20) };
                var txtGroup = new TextBox { Text = account.GroupId, Location = new Point(90, y - 3), Size = new Size(220, 21) };
                dialog.Controls.Add(lblGroup);
                dialog.Controls.Add(txtGroup);
                
                y += gap;
                var lblPhone = new Label { Text = "账号:", Location = new Point(20, y), Size = new Size(60, 20) };
                var txtPhone = new TextBox { Text = account.Phone, Location = new Point(90, y - 3), Size = new Size(220, 21) };
                dialog.Controls.Add(lblPhone);
                dialog.Controls.Add(txtPhone);
                
                y += gap;
                var lblPwd = new Label { Text = "密码:", Location = new Point(20, y), Size = new Size(60, 20) };
                var txtPwd = new TextBox { Text = account.Password, Location = new Point(90, y - 3), Size = new Size(220, 21), PasswordChar = '*' };
                dialog.Controls.Add(lblPwd);
                dialog.Controls.Add(txtPwd);
                
                y += gap + 10;
                var btnOK = new Button { Text = "保存", Location = new Point(140, y), Size = new Size(80, 28), DialogResult = DialogResult.OK };
                var btnCancel = new Button { Text = "取消", Location = new Point(230, y), Size = new Size(80, 28), DialogResult = DialogResult.Cancel };
                dialog.Controls.Add(btnOK);
                dialog.Controls.Add(btnCancel);
                dialog.AcceptButton = btnOK;
                dialog.CancelButton = btnCancel;
                
                if (dialog.ShowDialog(this) == DialogResult.OK)
                {
                    account.Nickname = txtNick.Text;
                    account.WangWangId = txtWwid.Text;
                    account.GroupId = txtGroup.Text;
                    account.Phone = txtPhone.Text;
                    account.Password = txtPwd.Text;
                    
                    AccountService.Instance.UpdateAccount(account);
                    MessageBox.Show("账户信息已更新！", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }
        
        /// <summary>
        /// Login account and connect to group chat
        /// </summary>
        private async void BtnLogin_Click(object sender, EventArgs e)
        {
            var account = GetSelectedAccount();
            if (account == null)
            {
                MessageBox.Show("请先选择一个账户", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            
            AccountService.Instance.SetAccountStatus(account.Id, AccountStatus.Logging);
            lblStatus.Text = string.Format("正在连接: {0}...", account.Nickname);
            
            try
            {
                // Connect via ChatService
                var success = await ChatService.Instance.LaunchAndConnectAsync();
                
                if (success)
                {
                    lblStatus.Text = string.Format("已连接，正在切换到群聊...");
                    
                    // If account has a group ID, switch to that group chat
                    if (!string.IsNullOrEmpty(account.GroupId))
                    {
                        long groupId;
                        if (long.TryParse(account.GroupId, out groupId))
                        {
                            var switchSuccess = await ChatService.Instance.SwitchToGroupChatAsync(groupId);
                            
                            if (switchSuccess)
                            {
                                AccountService.Instance.SetAccountStatus(account.Id, AccountStatus.Online);
                                AccountService.Instance.CurrentAccount = account;
                                lblStatus.Text = string.Format("✓ 已连接到群: {0}", account.GroupId);
                                MessageBox.Show(
                                    string.Format("登录成功！\n已连接到群聊 ID: {0}", account.GroupId), 
                                    "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            }
                            else
                            {
                                AccountService.Instance.SetAccountStatus(account.Id, AccountStatus.Online);
                                AccountService.Instance.CurrentAccount = account;
                                lblStatus.Text = string.Format("已连接，但切换群聊失败");
                                MessageBox.Show(
                                    "连接成功，但切换到指定群聊失败。\n请手动在旺商聊中打开群聊。", 
                                    "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            }
                        }
                        else
                        {
                            // Invalid group ID format
                            AccountService.Instance.SetAccountStatus(account.Id, AccountStatus.Online);
                            AccountService.Instance.CurrentAccount = account;
                            lblStatus.Text = string.Format("登录成功: {0}", account.Nickname);
                            MessageBox.Show("登录成功！\n群号格式无效，请手动切换群聊。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        }
                    }
                    else
                    {
                        // No group ID configured
                        AccountService.Instance.SetAccountStatus(account.Id, AccountStatus.Online);
                        AccountService.Instance.CurrentAccount = account;
                        lblStatus.Text = string.Format("登录成功: {0}", account.Nickname);
                        MessageBox.Show("登录成功！\n提示: 该账户未配置群号，请在旺商聊中手动选择群聊。", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
                else
                {
                    AccountService.Instance.SetAccountStatus(account.Id, AccountStatus.Failed);
                    lblStatus.Text = string.Format("连接失败: {0}", account.Nickname);
                    MessageBox.Show("连接旺商聊失败！\n请确保旺商聊正在运行且已启用调试端口。", "失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                AccountService.Instance.SetAccountStatus(account.Id, AccountStatus.Failed);
                lblStatus.Text = string.Format("登录异常: {0}", ex.Message);
                MessageBox.Show("登录异常: " + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        
        /// <summary>
        /// Logout account
        /// </summary>
        private async void BtnLogout_Click(object sender, EventArgs e)
        {
            var account = GetSelectedAccount();
            if (account == null)
            {
                MessageBox.Show("请先选择一个账户", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            
            await ChatService.Instance.DisconnectAsync();
            AccountService.Instance.SetAccountStatus(account.Id, AccountStatus.Offline);
            
            if (AccountService.Instance.CurrentAccount?.Id == account.Id)
            {
                AccountService.Instance.CurrentAccount = null;
            }
            
            lblStatus.Text = string.Format("已退出登录: {0}", account.Nickname);
        }
        
        /// <summary>
        /// Delete account
        /// </summary>
        private void BtnDelete_Click(object sender, EventArgs e)
        {
            var account = GetSelectedAccount();
            if (account == null)
            {
                MessageBox.Show("请先选择一个账户", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            
            var result = MessageBox.Show(
                string.Format("确定要删除账户 \"{0}\" 吗？", account.Nickname),
                "确认删除",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);
            
            if (result == DialogResult.Yes)
            {
                AccountService.Instance.RemoveAccount(account.Id);
                MessageBox.Show("账户已删除", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
        
        /// <summary>
        /// Copy account
        /// </summary>
        private void BtnCopy_Click(object sender, EventArgs e)
        {
            var account = GetSelectedAccount();
            if (account == null)
            {
                MessageBox.Show("请先选择一个账户", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            
            AccountService.Instance.CopyAccount(account.Id);
            MessageBox.Show("账户已复制", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        
        /// <summary>
        /// Set auto login
        /// </summary>
        private void BtnAutoLogin_Click(object sender, EventArgs e)
        {
            var account = GetSelectedAccount();
            if (account == null)
            {
                MessageBox.Show("请先选择一个账户", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            
            AccountService.Instance.SetAutoLogin(account.Id, true);
            MessageBox.Show("已设置自动登录", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        
        /// <summary>
        /// Cancel auto login
        /// </summary>
        private void BtnCancelAuto_Click(object sender, EventArgs e)
        {
            var account = GetSelectedAccount();
            if (account == null)
            {
                MessageBox.Show("请先选择一个账户", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            
            AccountService.Instance.SetAutoLogin(account.Id, false);
            MessageBox.Show("已取消自动登录", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            AccountService.Instance.OnAccountsChanged -= LoadAccounts;
            base.OnFormClosing(e);
        }
    }
}

