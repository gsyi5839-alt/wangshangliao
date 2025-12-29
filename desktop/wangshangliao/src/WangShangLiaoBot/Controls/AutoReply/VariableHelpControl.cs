using System;
using System.IO;
using System.Windows.Forms;

namespace WangShangLiaoBot.Controls.AutoReply
{
    /// <summary>
    /// Variable help control
    /// Displays all available variables for message templates
    /// </summary>
    public partial class VariableHelpControl : UserControl
    {
        /// <summary>
        /// Variable help text content
        /// </summary>
        private const string VariableHelpText = @"[阐述上，任何可以自定义回复的地方，都可以用以下变量]
回复需要添加条件，如回复玩家>号[旺旺]人，称呼显示艾特用[艾特]等
[昵称]，[公主艾特], 别1回复消息需要配[在区][二区][三区]等，则在每期结果后更新

*专用变量，则只在待定回复设置里可用

************************************************************************************
[换行]        自动替换为  换行
[艾特]        发消息玩家名
[玩家号]      玩家旺旺号前4位，如1234
[昵称]        玩家昵称，如辣辣鑫
[玩家]        玩家攻击，如100DA，没有攻击时，显示未攻击
[下注]        玩家攻击，如100DA，没有攻击时，不显示
[下注2]       玩家攻击，如100DA，没有攻击时，不显示
[下注3]       玩家攻击，如大/大/100，没有攻击时，不显示
[余粮]        自动替换为  玩家下注分，如100
[留分]        自动替换为  攻击后分数，如100，同[余粮]

************************************************************************************

[最近下注]    显示玩家最近几期的下注
[今天统计]    显示玩家最近的盈利、补亏、回粮、流水、期数
[今天统计2]   显示玩家最近的盈利、流水
[今天统计盈利] 显示玩家最近的总盈利
[今天统计期数] 显示玩家最近的总期数

************************************************************************************

[下注核对]
所有玩家的攻击数据，格式如下
旺堂 (1234) 123456 - [2000XD 20000DS]  60000
名字 (旺旺前4位) 下注前积分 - [下注内容]_下注后积分
";
        
        public VariableHelpControl()
        {
            InitializeComponent();
            LoadHelpText();
        }
        
        /// <summary>
        /// Load help text to display
        /// </summary>
        private void LoadHelpText()
        {
            txtVariableHelp.Text = VariableHelpText;
        }
        
        #region Event Handlers
        
        /// <summary>
        /// Handle export button click - save help text to file
        /// </summary>
        private void btnExport_Click(object sender, EventArgs e)
        {
            try
            {
                using (var dialog = new SaveFileDialog())
                {
                    dialog.Filter = "文本文件|*.txt|所有文件|*.*";
                    dialog.Title = "导出变量说明";
                    dialog.FileName = "变量说明.txt";
                    dialog.DefaultExt = "txt";
                    
                    if (dialog.ShowDialog() == DialogResult.OK)
                    {
                        File.WriteAllText(dialog.FileName, VariableHelpText);
                        MessageBox.Show($"已导出到：{dialog.FileName}", "导出成功", 
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"导出失败：{ex.Message}", "导出错误", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        
        #endregion
    }
}

