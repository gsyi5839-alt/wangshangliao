using System;
using System.Drawing;
using System.Windows.Forms;
using WSLFramework.Services;

namespace WSLFramework.Forms
{
    /// <summary>
    /// 托管设置窗口 - 管理托管名单和自动上下分设置
    /// 对应招财狗的托管设置功能
    /// </summary>
    public class TrusteeForm : Form
    {
        private PlayerService _playerService;
        
        // 控件
        private ListView lvTrustees;
        private TextBox txtTrusteeId;
        private Button btnAddTrustee;
        private Button btnRemoveTrustee;
        private Button btnExportTrustees;
#pragma warning disable CS0169 // 保留给导入托功能
        private Button btnImportTrustees;
#pragma warning restore CS0169
        
        // 设置控件
        private CheckBox chkAutoUp;
        private NumericUpDown numUpDelayMin;
        private NumericUpDown numUpDelayMax;
        private CheckBox chkAutoDown;
        private NumericUpDown numDownDelayMin;
        private NumericUpDown numDownDelayMax;
        private CheckBox chkAutoAccept;
        private Button btnSaveSettings;
        
        public TrusteeForm(PlayerService playerService)
        {
            _playerService = playerService;
            InitializeComponent();
            LoadData();
        }
        
        private void InitializeComponent()
        {
            this.Text = "托管设置";
            this.Size = new Size(600, 500);
            this.StartPosition = FormStartPosition.CenterParent;
            this.Font = new Font("Microsoft YaHei UI", 9F);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            
            // 左侧 - 托管名单
            var gbTrustees = new GroupBox
            {
                Text = "托名单",
                Location = new Point(10, 10),
                Size = new Size(280, 440)
            };
            
            lvTrustees = new ListView
            {
                Location = new Point(10, 25),
                Size = new Size(260, 320),
                View = View.Details,
                FullRowSelect = true,
                GridLines = true
            };
            lvTrustees.Columns.Add("序", 40);
            lvTrustees.Columns.Add("托名单", 200);
            
            txtTrusteeId = new TextBox
            {
                Location = new Point(10, 355),
                Size = new Size(160, 25)
            };
            txtTrusteeId.GotFocus += (s, e) => { if (txtTrusteeId.Text == "输入玩家ID") txtTrusteeId.Text = ""; };
            txtTrusteeId.LostFocus += (s, e) => { if (string.IsNullOrEmpty(txtTrusteeId.Text)) txtTrusteeId.Text = "输入玩家ID"; };
            txtTrusteeId.Text = "输入玩家ID";
            txtTrusteeId.ForeColor = Color.Gray;
            
            btnAddTrustee = new Button
            {
                Text = "添加托",
                Location = new Point(180, 352),
                Size = new Size(90, 30),
                FlatStyle = FlatStyle.Flat
            };
            btnAddTrustee.Click += BtnAddTrustee_Click;
            
            btnRemoveTrustee = new Button
            {
                Text = "移除托",
                Location = new Point(10, 390),
                Size = new Size(80, 30),
                FlatStyle = FlatStyle.Flat
            };
            btnRemoveTrustee.Click += BtnRemoveTrustee_Click;
            
            var btnRemoveAll = new Button
            {
                Text = "移除所有托",
                Location = new Point(100, 390),
                Size = new Size(90, 30),
                FlatStyle = FlatStyle.Flat
            };
            btnRemoveAll.Click += (s, e) =>
            {
                if (MessageBox.Show("确定要移除所有托管吗？", "确认", MessageBoxButtons.YesNo) == DialogResult.Yes)
                {
                    foreach (ListViewItem item in lvTrustees.Items)
                    {
                        _playerService.RemoveTrustee(item.SubItems[1].Text);
                    }
                    LoadTrustees();
                }
            };
            
            btnExportTrustees = new Button
            {
                Text = "导出托",
                Location = new Point(200, 390),
                Size = new Size(70, 30),
                FlatStyle = FlatStyle.Flat
            };
            
            gbTrustees.Controls.Add(lvTrustees);
            gbTrustees.Controls.Add(txtTrusteeId);
            gbTrustees.Controls.Add(btnAddTrustee);
            gbTrustees.Controls.Add(btnRemoveTrustee);
            gbTrustees.Controls.Add(btnRemoveAll);
            gbTrustees.Controls.Add(btnExportTrustees);
            
            // 右侧 - 设置
            var gbSettings = new GroupBox
            {
                Text = "自动设置",
                Location = new Point(300, 10),
                Size = new Size(280, 200)
            };
            
            chkAutoUp = new CheckBox
            {
                Text = "托查钱自动上分",
                Location = new Point(10, 25),
                AutoSize = true,
                Checked = _playerService.AutoUpEnabled
            };
            
            var lblUpDelay = new Label
            {
                Text = "延迟",
                Location = new Point(170, 27),
                AutoSize = true
            };
            
            numUpDelayMin = new NumericUpDown
            {
                Location = new Point(200, 23),
                Size = new Size(40, 25),
                Value = _playerService.AutoUpDelayMin,
                Minimum = 1,
                Maximum = 60
            };
            
            var lblUpDelayTo = new Label
            {
                Text = "-",
                Location = new Point(242, 27),
                AutoSize = true
            };
            
            numUpDelayMax = new NumericUpDown
            {
                Location = new Point(252, 23),
                Size = new Size(40, 25),
                Value = _playerService.AutoUpDelayMax,
                Minimum = 1,
                Maximum = 60
            };
            
            var lblUpDelaySec = new Label
            {
                Text = "秒喊到",
                Location = new Point(294, 27),
                AutoSize = true
            };
            
            chkAutoDown = new CheckBox
            {
                Text = "托回钱自动下分",
                Location = new Point(10, 60),
                AutoSize = true,
                Checked = _playerService.AutoDownEnabled
            };
            
            var lblDownDelay = new Label
            {
                Text = "延迟",
                Location = new Point(170, 62),
                AutoSize = true
            };
            
            numDownDelayMin = new NumericUpDown
            {
                Location = new Point(200, 58),
                Size = new Size(40, 25),
                Value = _playerService.AutoDownDelayMin,
                Minimum = 1,
                Maximum = 120
            };
            
            var lblDownDelayTo = new Label
            {
                Text = "-",
                Location = new Point(242, 62),
                AutoSize = true
            };
            
            numDownDelayMax = new NumericUpDown
            {
                Location = new Point(252, 58),
                Size = new Size(40, 25),
                Value = _playerService.AutoDownDelayMax,
                Minimum = 1,
                Maximum = 120
            };
            
            var lblDownDelaySec = new Label
            {
                Text = "秒喊查",
                Location = new Point(294, 62),
                AutoSize = true
            };
            
            chkAutoAccept = new CheckBox
            {
                Text = "自动同意托加群",
                Location = new Point(10, 95),
                AutoSize = true,
                Checked = _playerService.AutoAcceptTrustee
            };
            
            gbSettings.Controls.Add(chkAutoUp);
            gbSettings.Controls.Add(lblUpDelay);
            gbSettings.Controls.Add(numUpDelayMin);
            gbSettings.Controls.Add(lblUpDelayTo);
            gbSettings.Controls.Add(numUpDelayMax);
            gbSettings.Controls.Add(lblUpDelaySec);
            gbSettings.Controls.Add(chkAutoDown);
            gbSettings.Controls.Add(lblDownDelay);
            gbSettings.Controls.Add(numDownDelayMin);
            gbSettings.Controls.Add(lblDownDelayTo);
            gbSettings.Controls.Add(numDownDelayMax);
            gbSettings.Controls.Add(lblDownDelaySec);
            gbSettings.Controls.Add(chkAutoAccept);
            
            // 远程获取账单
            var chkRemoteBill = new CheckBox
            {
                Text = "托插件远程获取账单",
                Location = new Point(300, 220),
                AutoSize = true
            };
            
            var btnCopyAddress = new Button
            {
                Text = "复制账单地址",
                Location = new Point(450, 216),
                Size = new Size(100, 28),
                FlatStyle = FlatStyle.Flat
            };
            
            // 私聊喊托
            var btnPrivateChat = new Button
            {
                Text = "私聊喊托",
                Location = new Point(300, 260),
                Size = new Size(100, 35),
                FlatStyle = FlatStyle.Flat
            };
            
            // 保存设置按钮
            btnSaveSettings = new Button
            {
                Text = "保存设置",
                Location = new Point(450, 410),
                Size = new Size(100, 35),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(76, 175, 80),
                ForeColor = Color.White
            };
            btnSaveSettings.FlatAppearance.BorderSize = 0;
            btnSaveSettings.Click += BtnSaveSettings_Click;
            
            this.Controls.Add(gbTrustees);
            this.Controls.Add(gbSettings);
            this.Controls.Add(chkRemoteBill);
            this.Controls.Add(btnCopyAddress);
            this.Controls.Add(btnPrivateChat);
            this.Controls.Add(btnSaveSettings);
        }
        
        private void LoadData()
        {
            LoadTrustees();
        }
        
        private void LoadTrustees()
        {
            lvTrustees.Items.Clear();
            var trustees = _playerService.GetTrustees();
            
            for (int i = 0; i < trustees.Count; i++)
            {
                var item = new ListViewItem((i + 1).ToString());
                item.SubItems.Add(trustees[i]);
                lvTrustees.Items.Add(item);
            }
        }
        
        private void BtnAddTrustee_Click(object sender, EventArgs e)
        {
            var id = txtTrusteeId.Text.Trim();
            if (string.IsNullOrEmpty(id))
            {
                MessageBox.Show("请输入玩家ID");
                return;
            }
            
            _playerService.AddTrustee(id);
            txtTrusteeId.Clear();
            LoadTrustees();
        }
        
        private void BtnRemoveTrustee_Click(object sender, EventArgs e)
        {
            if (lvTrustees.SelectedItems.Count > 0)
            {
                var id = lvTrustees.SelectedItems[0].SubItems[1].Text;
                _playerService.RemoveTrustee(id);
                LoadTrustees();
            }
        }
        
        private void BtnSaveSettings_Click(object sender, EventArgs e)
        {
            _playerService.AutoUpEnabled = chkAutoUp.Checked;
            _playerService.AutoUpDelayMin = (int)numUpDelayMin.Value;
            _playerService.AutoUpDelayMax = (int)numUpDelayMax.Value;
            _playerService.AutoDownEnabled = chkAutoDown.Checked;
            _playerService.AutoDownDelayMin = (int)numDownDelayMin.Value;
            _playerService.AutoDownDelayMax = (int)numDownDelayMax.Value;
            _playerService.AutoAcceptTrustee = chkAutoAccept.Checked;
            
            _playerService.SaveData();
            MessageBox.Show("设置已保存");
        }
    }
}
