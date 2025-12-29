using System;
using System.Drawing;
using System.Windows.Forms;
using WangShangLiaoBot.Services;

namespace WangShangLiaoBot.Controls
{
    /// <summary>
    /// 群名片设置控件
    /// </summary>
    public sealed class CardSettingsControl : UserControl
    {
        private GroupBox grpCardSettings;
        
        // Row 1: 提醒发送选项
        private CheckBox chkNotifyToGroup;
        private CheckBox chkNotifyToAdmin;
        
        // Description labels
        private Label lblDesc1;
        private Label lblDesc2;
        
        // Row 2: 锁名片开关
        private CheckBox chkLockCard;
        
        // Row 3: 改名次数踢出
        private CheckBox chkKickOnRename;
        private NumericUpDown nudRenameLimit;
        private Label lblRenameKickSuffix;
        
        // Row 4: 进群改名片不提醒
        private CheckBox chkNoNotifyOnJoin;
        
        // Buttons
        private Button btnHowToModify;
        private Button btnBatchRename;
        private Button btnSave;

        public CardSettingsControl()
        {
            BackColor = SystemColors.Control;
            Font = new Font("Microsoft YaHei UI", 9F);
            
            MinimumSize = new Size(400, 320);
            Size = new Size(500, 350);
            AutoScroll = true;
            Dock = DockStyle.Fill;
            
            InitializeUI();
            LoadSettings();
        }

        private void InitializeUI()
        {
            SuspendLayout();

            // ================= GroupBox: 群名片设置 =================
            grpCardSettings = new GroupBox
            {
                Text = "群名片设置",
                Location = new Point(10, 10),
                Size = new Size(460, 300),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom
            };
            Controls.Add(grpCardSettings);

            // Row 1: 提醒发送选项
            chkNotifyToGroup = new CheckBox
            {
                Text = "提醒发送到群里",
                Location = new Point(15, 25),
                Size = new Size(130, 20),
                Checked = true
            };
            grpCardSettings.Controls.Add(chkNotifyToGroup);

            chkNotifyToAdmin = new CheckBox
            {
                Text = "提醒发送到管理号",
                Location = new Point(160, 25),
                Size = new Size(140, 20)
            };
            grpCardSettings.Controls.Add(chkNotifyToAdmin);

            // Description line 1
            lblDesc1 = new Label
            {
                Text = "群成员群名片变动时提醒 推荐开启锁群名片",
                Location = new Point(15, 52),
                AutoSize = true,
                ForeColor = Color.Gray
            };
            grpCardSettings.Controls.Add(lblDesc1);

            // Description line 2
            lblDesc2 = new Label
            {
                Text = "有效防止骗子改名冒充账号\n修改名片命令 修改名片旺旺号=要修改的名片",
                Location = new Point(15, 70),
                Size = new Size(400, 35),
                ForeColor = Color.Gray
            };
            grpCardSettings.Controls.Add(lblDesc2);

            // Row 2: 锁名片开关
            chkLockCard = new CheckBox
            {
                Text = "锁名片开关(不推荐关闭)",
                Location = new Point(15, 110),
                Size = new Size(200, 20),
                Checked = true
            };
            grpCardSettings.Controls.Add(chkLockCard);

            // Row 3: 改名次数踢出
            chkKickOnRename = new CheckBox
            {
                Text = "改名次数超过",
                Location = new Point(15, 138),
                Size = new Size(100, 20),
                Checked = true
            };
            grpCardSettings.Controls.Add(chkKickOnRename);

            nudRenameLimit = new NumericUpDown
            {
                Location = new Point(120, 136),
                Size = new Size(50, 23),
                Minimum = 1,
                Maximum = 99,
                Value = 3
            };
            grpCardSettings.Controls.Add(nudRenameLimit);

            lblRenameKickSuffix = new Label
            {
                Text = "次踢出并拉黑",
                Location = new Point(175, 140),
                AutoSize = true
            };
            grpCardSettings.Controls.Add(lblRenameKickSuffix);

            // Row 4: 进群改名片不提醒
            chkNoNotifyOnJoin = new CheckBox
            {
                Text = "进群改名片不提醒",
                Location = new Point(15, 166),
                Size = new Size(150, 20)
            };
            grpCardSettings.Controls.Add(chkNoNotifyOnJoin);

            // Button: 开启锁名片后怎么修改名片?
            btnHowToModify = new Button
            {
                Text = "开启锁名片后怎么修改名片?",
                Location = new Point(15, 200),
                Size = new Size(200, 28)
            };
            btnHowToModify.Click += BtnHowToModify_Click;
            grpCardSettings.Controls.Add(btnHowToModify);

            // Button: 一键修改全群成员为两字中文名
            btnBatchRename = new Button
            {
                Text = "一键修改全群成员为两字中文名",
                Location = new Point(15, 235),
                Size = new Size(220, 28)
            };
            btnBatchRename.Click += BtnBatchRename_Click;
            grpCardSettings.Controls.Add(btnBatchRename);

            // Button: 保存设置
            btnSave = new Button
            {
                Text = "保存设置",
                Location = new Point(180, 270),
                Size = new Size(100, 28)
            };
            btnSave.Click += BtnSave_Click;
            grpCardSettings.Controls.Add(btnSave);

            ResumeLayout(false);
        }

        private void LoadSettings()
        {
            var s = CardSettingsService.Instance;
            chkNotifyToGroup.Checked = s.NotifyToGroup;
            chkNotifyToAdmin.Checked = s.NotifyToAdmin;
            chkLockCard.Checked = s.LockCardEnabled;
            chkKickOnRename.Checked = s.KickOnRenameEnabled;
            nudRenameLimit.Value = ClampValue(s.RenameLimit, nudRenameLimit);
            chkNoNotifyOnJoin.Checked = s.NoNotifyOnJoin;
        }

        private decimal ClampValue(int v, NumericUpDown nud)
        {
            if (v < (int)nud.Minimum) return nud.Minimum;
            if (v > (int)nud.Maximum) return nud.Maximum;
            return v;
        }

        private void BtnHowToModify_Click(object sender, EventArgs e)
        {
            var msg = "开启锁名片后修改名片的方法：\n\n" +
                      "1. 在群聊中发送命令：\n" +
                      "   修改名片旺旺号=新名片\n\n" +
                      "例如：修改名片12345678=小明\n\n" +
                      "2. 管理员可以通过管理号发送修改命令";
            MessageBox.Show(msg, "修改名片说明", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private async void BtnBatchRename_Click(object sender, EventArgs e)
        {
            var result = MessageBox.Show(
                "确定要将全群成员名片修改为两字中文名吗？\n\n" +
                "此操作会将所有成员的群名片改为随机两字中文名。\n" +
                "建议先备份当前成员列表。",
                "确认批量修改",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (result != DialogResult.Yes) return;

            btnBatchRename.Enabled = false;
            btnBatchRename.Text = "正在修改...";

            try
            {
                if (!ChatService.Instance.IsConnected)
                {
                    MessageBox.Show("请先连接旺商聊！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // TODO: 实现批量修改名片功能
                // 需要调用 ChatService 的修改群名片方法
                await System.Threading.Tasks.Task.Delay(1000); // 模拟操作

                MessageBox.Show("批量修改名片功能开发中...\n\n需要配合旺商聊的群名片修改API实现。", 
                    "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"操作失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnBatchRename.Enabled = true;
                btnBatchRename.Text = "一键修改全群成员为两字中文名";
            }
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            var s = CardSettingsService.Instance;
            s.NotifyToGroup = chkNotifyToGroup.Checked;
            s.NotifyToAdmin = chkNotifyToAdmin.Checked;
            s.LockCardEnabled = chkLockCard.Checked;
            s.KickOnRenameEnabled = chkKickOnRename.Checked;
            s.RenameLimit = (int)nudRenameLimit.Value;
            s.NoNotifyOnJoin = chkNoNotifyOnJoin.Checked;

            MessageBox.Show("设置已保存", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}

