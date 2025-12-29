using System;
using System.Drawing;
using System.Windows.Forms;
using WangShangLiaoBot.Services;

namespace WangShangLiaoBot.Controls
{
    /// <summary>
    /// Odds settings control - manages game play odds configuration
    /// Contains sub-tabs: Classic, Tail Ball, Dragon Tiger, Three Army, Position Ball, Other
    /// </summary>
    public partial class OddsSettingsControl : UserControl
    {
        /// <summary>
        /// Constructor - initializes the control
        /// </summary>
        public OddsSettingsControl()
        {
            InitializeComponent();
            InitializeTabContents();
        }
        
        /// <summary>
        /// Initialize all tab page contents
        /// </summary>
        private void InitializeTabContents()
        {
            // Tab 1: Classic Play (经典玩法)
            InitializeClassicPlayTab();
            
            // Tab 2: Tail Ball Play (尾球玩法)
            InitializeTailBallTab();
            
            // Tab 3: Dragon Tiger Play (龙虎玩法)
            InitializeDragonTigerTab();
            
            // Tab 4: Three Army Play (三军玩法)
            InitializeThreeArmyTab();
            
            // Tab 5: Position Ball Play (定位球玩法)
            InitializePositionBallTab();
            
            // Tab 6: Other Play (其他玩法)
            InitializeOtherTab();
        }
        
        #region Classic Play Tab (经典玩法)
        
        /// <summary>
        /// Initialize Classic Play tab content
        /// </summary>
        private void InitializeClassicPlayTab()
        {
            var panel = new Panel { Dock = DockStyle.Fill, AutoScroll = true };
            
            // Left section - Size settings (大小单双)
            var grpSize = CreateGroupBox("大-小-单-双超", 5, 5, 200, 200);
            
            // Radio buttons
            var rbSum = new RadioButton { Text = "算总注", Location = new Point(10, 22), Size = new Size(70, 20), Checked = true };
            var rbSingle = new RadioButton { Text = "算单注", Location = new Point(85, 22), Size = new Size(70, 20) };
            grpSize.Controls.AddRange(new Control[] { rbSum, rbSingle });
            
            // Numeric inputs for betting limits
            AddLabelAndNumeric(grpSize, "", 10, 50, 10000, 0);
            AddLabelAndNumeric(grpSize, "", 70, 50, 0, 0);
            AddLabelAndNumeric(grpSize, "赔率", 130, 50, 0, 0);
            
            AddLabelAndNumeric(grpSize, "", 10, 78, 10001, 0);
            AddLabelAndNumeric(grpSize, "", 70, 78, 60000, 0);
            AddLabelAndNumeric(grpSize, "赔率", 130, 78, 0, 0);
            
            AddLabelAndNumeric(grpSize, "", 10, 106, 60001, 0);
            AddLabelAndNumeric(grpSize, "", 70, 106, 0, 0);
            AddLabelAndNumeric(grpSize, "赔率", 130, 106, 0, 0);
            
            AddLabelAndNumeric(grpSize, "", 10, 134, 600001, 0);
            AddLabelAndNumeric(grpSize, "", 70, 134, 0, 0);
            AddLabelAndNumeric(grpSize, "赔率", 130, 134, 0, 0);
            
            panel.Controls.Add(grpSize);
            
            // Middle section - Leopard/Pair settings
            var grpLeopard = CreateGroupBox("豹/顺/对子", 5, 210, 200, 180);
            
            var chkOpen = new CheckBox { Text = "开/关", Location = new Point(10, 22), Size = new Size(60, 20) };
            var chkPairReturn = new CheckBox { Text = "对子回本", Location = new Point(10, 45), Size = new Size(80, 20), Checked = true };
            var chkSequenceReturn = new CheckBox { Text = "顺子回本", Location = new Point(100, 45), Size = new Size(80, 20), Checked = true };
            var chkLeopardReturn = new CheckBox { Text = "豹子回本", Location = new Point(10, 68), Size = new Size(80, 20) };
            var chkLeopardPass = new CheckBox { Text = "豹子通杀", Location = new Point(100, 68), Size = new Size(80, 20) };
            
            grpLeopard.Controls.AddRange(new Control[] { chkOpen, chkPairReturn, chkSequenceReturn, chkLeopardReturn, chkLeopardPass });
            panel.Controls.Add(grpLeopard);
            
            // Right section - Odds settings
            var grpOdds = CreateGroupBox("赔率设置", 210, 5, 150, 200);
            
            AddLabelAndTextBox(grpOdds, "大小单双赔率", 10, 25, "1.8");
            AddLabelAndTextBox(grpOdds, "大单小双赔率", 10, 55, "");
            AddLabelAndTextBox(grpOdds, "大双小单赔率", 10, 85, "5");
            AddLabelAndTextBox(grpOdds, "极大极小赔率", 10, 115, "");
            AddLabelAndTextBox(grpOdds, "特码数字赔率", 10, 145, "9");
            
            panel.Controls.Add(grpOdds);
            
            // Extreme number section
            var grpExtreme = CreateGroupBox("极数", 365, 5, 100, 80);
            AddLabelAndTextBox(grpExtreme, "极大", 10, 22, "22");
            AddLabelAndTextBox(grpExtreme, "极小", 60, 22, "27");
            AddLabelAndTextBox(grpExtreme, "极小", 10, 50, "0");
            AddLabelAndTextBox(grpExtreme, "", 60, 50, "5");
            panel.Controls.Add(grpExtreme);
            
            // Single number odds settings
            var grpSingleOdds = CreateGroupBox("单独数字赔率设置", 365, 90, 200, 200);
            
            // Create a grid of number odds
            var dgv = new DataGridView
            {
                Location = new Point(10, 22),
                Size = new Size(180, 140),
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = Color.White
            };
            dgv.Columns.Add("Num", "数字");
            dgv.Columns.Add("Odds", "赔率");
            
            for (int i = 0; i <= 9; i++)
            {
                dgv.Rows.Add(i.ToString(), "9");
            }
            
            var chkSingleOdds = new CheckBox { Text = "单独数字赔率", Location = new Point(10, 168), Size = new Size(120, 20) };
            
            grpSingleOdds.Controls.AddRange(new Control[] { dgv, chkSingleOdds });
            panel.Controls.Add(grpSingleOdds);
            
            // Save button
            var btnSave = new Button { Text = "保存设置", Location = new Point(470, 260), Size = new Size(90, 30) };
            btnSave.Click += (s, e) => MessageBox.Show("经典玩法设置已保存", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            panel.Controls.Add(btnSave);
            
            tabClassic.Controls.Add(panel);
        }
        
        #endregion
        
        #region Tail Ball Tab (尾球玩法)
        
        /// <summary>
        /// Initialize Tail Ball tab content
        /// </summary>
        private void InitializeTailBallTab()
        {
            var panel = new Panel { Dock = DockStyle.Fill, AutoScroll = true };
            
            var lbl = new Label
            {
                Text = "尾球玩法设置\n\n此功能开发中...",
                Location = new Point(20, 20),
                Size = new Size(400, 100),
                Font = new Font("Microsoft YaHei UI", 12F)
            };
            panel.Controls.Add(lbl);
            
            var btnSave = new Button { Text = "保存设置", Location = new Point(20, 150), Size = new Size(90, 30) };
            panel.Controls.Add(btnSave);
            
            tabTailBall.Controls.Add(panel);
        }
        
        #endregion
        
        #region Dragon Tiger Tab (龙虎玩法)
        
        /// <summary>
        /// Initialize Dragon Tiger tab content
        /// </summary>
        private void InitializeDragonTigerTab()
        {
            var panel = new Panel { Dock = DockStyle.Fill, AutoScroll = true };
            
            var lbl = new Label
            {
                Text = "龙虎玩法设置\n\n此功能开发中...",
                Location = new Point(20, 20),
                Size = new Size(400, 100),
                Font = new Font("Microsoft YaHei UI", 12F)
            };
            panel.Controls.Add(lbl);
            
            var btnSave = new Button { Text = "保存设置", Location = new Point(20, 150), Size = new Size(90, 30) };
            panel.Controls.Add(btnSave);
            
            tabDragonTiger.Controls.Add(panel);
        }
        
        #endregion
        
        #region Three Army Tab (三军玩法)
        
        /// <summary>
        /// Initialize Three Army tab content
        /// </summary>
        private void InitializeThreeArmyTab()
        {
            var panel = new Panel { Dock = DockStyle.Fill, AutoScroll = true };
            
            var lbl = new Label
            {
                Text = "三军玩法设置\n\n此功能开发中...",
                Location = new Point(20, 20),
                Size = new Size(400, 100),
                Font = new Font("Microsoft YaHei UI", 12F)
            };
            panel.Controls.Add(lbl);
            
            var btnSave = new Button { Text = "保存设置", Location = new Point(20, 150), Size = new Size(90, 30) };
            panel.Controls.Add(btnSave);
            
            tabThreeArmy.Controls.Add(panel);
        }
        
        #endregion
        
        #region Position Ball Tab (定位球玩法)
        
        /// <summary>
        /// Initialize Position Ball tab content
        /// </summary>
        private void InitializePositionBallTab()
        {
            var panel = new Panel { Dock = DockStyle.Fill, AutoScroll = true };
            
            // Enable checkbox
            var chkEnable = new CheckBox { Text = "定位球玩法 开启/关闭", Location = new Point(10, 10), Size = new Size(180, 25) };
            panel.Controls.Add(chkEnable);
            
            // Single bet odds
            var lblSingle = new Label { Text = "单注赔率:", Location = new Point(10, 45), Size = new Size(70, 20) };
            var txtSingleOdds = new TextBox { Location = new Point(85, 43), Size = new Size(50, 23) };
            var lblSingleRange = new Label { Text = "下注范围", Location = new Point(150, 45), Size = new Size(60, 20) };
            var txtSingleMin = new TextBox { Location = new Point(215, 43), Size = new Size(50, 23) };
            var lblSingleDash = new Label { Text = "-", Location = new Point(270, 45), Size = new Size(15, 20) };
            var txtSingleMax = new TextBox { Location = new Point(285, 43), Size = new Size(50, 23) };
            panel.Controls.AddRange(new Control[] { lblSingle, txtSingleOdds, lblSingleRange, txtSingleMin, lblSingleDash, txtSingleMax });
            
            // Combo bet odds
            var lblCombo = new Label { Text = "组合赔率:", Location = new Point(10, 75), Size = new Size(70, 20) };
            var txtComboOdds = new TextBox { Location = new Point(85, 73), Size = new Size(50, 23) };
            var lblComboRange = new Label { Text = "下注范围", Location = new Point(150, 75), Size = new Size(60, 20) };
            var txtComboMin = new TextBox { Location = new Point(215, 73), Size = new Size(50, 23) };
            var lblComboDash = new Label { Text = "-", Location = new Point(270, 75), Size = new Size(15, 20) };
            var txtComboMax = new TextBox { Location = new Point(285, 73), Size = new Size(50, 23) };
            panel.Controls.AddRange(new Control[] { lblCombo, txtComboOdds, lblComboRange, txtComboMin, lblComboDash, txtComboMax });
            
            // Special code odds
            var lblSpecial = new Label { Text = "特码赔率:", Location = new Point(10, 105), Size = new Size(70, 20) };
            var txtSpecialOdds = new TextBox { Location = new Point(85, 103), Size = new Size(50, 23) };
            var lblSpecialRange = new Label { Text = "下注范围", Location = new Point(150, 105), Size = new Size(60, 20) };
            var txtSpecialMin = new TextBox { Location = new Point(215, 103), Size = new Size(50, 23) };
            var lblSpecialDash = new Label { Text = "-", Location = new Point(270, 105), Size = new Size(15, 20) };
            var txtSpecialMax = new TextBox { Location = new Point(285, 103), Size = new Size(50, 23) };
            panel.Controls.AddRange(new Control[] { lblSpecial, txtSpecialOdds, lblSpecialRange, txtSpecialMin, lblSpecialDash, txtSpecialMax });
            
            // Save button
            var btnSave = new Button { Text = "保存设置", Location = new Point(120, 145), Size = new Size(90, 30) };
            btnSave.Click += (s, e) => MessageBox.Show("定位球玩法设置已保存", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            panel.Controls.Add(btnSave);
            
            // Format description
            var lblDesc = new Label
            {
                Text = "下注格式说明:\n" +
                       "1/大/100    1球大，下注100\n" +
                       "2/4/100    2球4，下注100\n" +
                       "3/456/100  3球4、5、6，各下注100\n" +
                       "哈1/大      全下1/大，哈可以放后面 1/大哈",
                Location = new Point(10, 190),
                Size = new Size(350, 120),
                Font = new Font("Microsoft YaHei UI", 9F)
            };
            panel.Controls.Add(lblDesc);
            
            tabPositionBall.Controls.Add(panel);
        }
        
        #endregion
        
        #region Other Tab (其他玩法)
        
        /// <summary>
        /// Initialize Other tab content
        /// </summary>
        private void InitializeOtherTab()
        {
            var panel = new Panel { Dock = DockStyle.Fill, AutoScroll = true };
            
            var lbl = new Label
            {
                Text = "其他玩法设置\n\n此功能开发中...",
                Location = new Point(20, 20),
                Size = new Size(400, 100),
                Font = new Font("Microsoft YaHei UI", 12F)
            };
            panel.Controls.Add(lbl);
            
            var btnSave = new Button { Text = "保存设置", Location = new Point(20, 150), Size = new Size(90, 30) };
            panel.Controls.Add(btnSave);
            
            tabOther.Controls.Add(panel);
        }
        
        #endregion
        
        #region Helper Methods
        
        /// <summary>
        /// Create a GroupBox with specified parameters
        /// </summary>
        private GroupBox CreateGroupBox(string text, int x, int y, int width, int height)
        {
            return new GroupBox
            {
                Text = text,
                Location = new Point(x, y),
                Size = new Size(width, height)
            };
        }
        
        /// <summary>
        /// Add a label and numeric updown to a container
        /// </summary>
        private void AddLabelAndNumeric(Control container, string labelText, int x, int y, decimal value, decimal increment)
        {
            if (!string.IsNullOrEmpty(labelText))
            {
                var lbl = new Label { Text = labelText, Location = new Point(x, y + 3), Size = new Size(40, 20) };
                container.Controls.Add(lbl);
                x += 45;
            }
            
            var num = new NumericUpDown
            {
                Location = new Point(x, y),
                Size = new Size(55, 23),
                Maximum = 9999999,
                Value = Math.Min(value, 9999999),
                Increment = increment > 0 ? increment : 1
            };
            container.Controls.Add(num);
        }
        
        /// <summary>
        /// Add a label and textbox to a container
        /// </summary>
        private void AddLabelAndTextBox(Control container, string labelText, int x, int y, string value)
        {
            if (!string.IsNullOrEmpty(labelText))
            {
                var lbl = new Label { Text = labelText, Location = new Point(x, y + 3), Size = new Size(85, 20), Font = new Font("Microsoft YaHei UI", 8F) };
                container.Controls.Add(lbl);
            }
            
            var txt = new TextBox
            {
                Location = new Point(x + 90, y),
                Size = new Size(45, 23),
                Text = value
            };
            container.Controls.Add(txt);
        }
        
        #endregion
    }
}

