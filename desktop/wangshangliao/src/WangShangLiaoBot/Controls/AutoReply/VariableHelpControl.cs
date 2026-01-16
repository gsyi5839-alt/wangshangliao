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
        private const string VariableHelpText = @"======== 变量说明 ========
任何可以自定义回复的地方，都可以使用以下变量

═══════════════════════════════════════════════════════════════════
【基础变量】
═══════════════════════════════════════════════════════════════════
[换行]        换行符
[时间]        当前时间 (HH:mm:ss)
[日期]        当前日期 (yyyy-MM-dd)
[时]          当前小时
[分]          当前分钟
[秒]          当前秒数

═══════════════════════════════════════════════════════════════════
【玩家变量】
═══════════════════════════════════════════════════════════════════
[艾特]        @玩家昵称，用于艾特玩家
[公主艾特]    同[艾特]
[昵称]        玩家昵称
[旺旺]        玩家旺旺号前4位
[玩家号]      同[旺旺]
[分数]        玩家当前分数
[总分]        玩家总分
[余粮]        玩家留分/余粮
[留分]        同[余粮]

═══════════════════════════════════════════════════════════════════
【下注变量】
═══════════════════════════════════════════════════════════════════
[玩家]        玩家攻击内容，没有时显示[未攻击]
[下注]        玩家攻击内容，没有时显示[未攻击]
[下注2]       玩家攻击内容，没有时不显示
[下注3]       玩家攻击(中文格式)，如[小单2000]
[下注分]      下注金额数字总和
[封盘倒计时]  封盘倒计时秒数

═══════════════════════════════════════════════════════════════════
【开奖变量】
═══════════════════════════════════════════════════════════════════
[期数]        当前期号(粗体)
[期数2]       当前期号(普通)
[开奖号码]    开奖号码和值
[一区]        第一个号码
[二区]        第二个号码
[三区]        第三个号码
[在区]        同[一区]
[开奖时间]    开奖时间(粗体)
[开奖时间2]   开奖时间(普通)
[大小单双]    大小单双结果(如DD/XS)
[大小单双2]   大小单双结果(小写)
[大小单双3]   大小单双结果(中文，如大单)
[豹顺对子]    豹子/顺子/对子/半杂
[龙虎豹]      龙/虎/豹(L/H/B)
[09回本]      0或9时显示X回本

═══════════════════════════════════════════════════════════════════
【统计变量】
═══════════════════════════════════════════════════════════════════
[最近下注]    玩家最近几期的下注记录
[今天统计]    盈利、补亏、回粮、流水、期数
[今天统计2]   盈利、流水
[今天统计盈利] 今日总盈利
[今天统计流水] 今日总流水
[今天统计期数] 今日总期数
[客户人数]    客户总人数
[总分数]      所有客户分数总和

═══════════════════════════════════════════════════════════════════
【账单变量】(用于发送账单)
═══════════════════════════════════════════════════════════════════
[下注核对]    所有玩家下注核对表
              格式: 昵称(旺旺前4位) 下注前分数 - [下注内容] 下注后分数
[下注核对2]   同上(中文格式)
[账单]        账单内容(不含ID)
[账单2]       账单内容(含ID)
[中奖玩家]    中奖玩家列表

═══════════════════════════════════════════════════════════════════
【历史记录变量】
═══════════════════════════════════════════════════════════════════
[开奖历史]    最近15期开奖记录
[龙虎历史]    龙虎历史(英文)
[龙虎历史2]   龙虎历史(中文)
[尾球历史]    尾球历史
[豹顺对历史]  豹顺对历史
[边历史]      边/中历史
[开奖图]      开奖趋势
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

