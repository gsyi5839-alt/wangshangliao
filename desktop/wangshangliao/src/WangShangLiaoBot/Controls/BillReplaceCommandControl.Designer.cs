namespace WangShangLiaoBot.Controls
{
    partial class BillReplaceCommandControl
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

        private void InitializeComponent()
        {
            this.lblTitle = new System.Windows.Forms.Label();
            this.txtReplaceHelp = new System.Windows.Forms.RichTextBox();

            this.SuspendLayout();

            // lblTitle - Section header label for robot commands
            this.lblTitle.Text = "机器人命令↓";
            this.lblTitle.Location = new System.Drawing.Point(5, 5);
            this.lblTitle.Size = new System.Drawing.Size(90, 15);

            // txtReplaceHelp - RichTextBox displaying all robot commands from 命令.md
            this.txtReplaceHelp.ReadOnly = true;
            this.txtReplaceHelp.Location = new System.Drawing.Point(5, 23);
            this.txtReplaceHelp.Size = new System.Drawing.Size(315, 100);
            this.txtReplaceHelp.BackColor = System.Drawing.SystemColors.Window;
            this.txtReplaceHelp.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.txtReplaceHelp.ScrollBars = System.Windows.Forms.RichTextBoxScrollBars.ForcedVertical;
            this.txtReplaceHelp.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right | System.Windows.Forms.AnchorStyles.Bottom;
            this.txtReplaceHelp.Text =
                "开始程序/开始游戏/开机\r\n" +
                "停止程序/停止游戏/关机\r\n" +
                "\r\n" +
                "今天[类型]=旺旺号\r\n" +
                "昨天[类型]=旺旺号\r\n" +
                "2020-02-17至2020-02-18[类型]=旺旺号\r\n" +
                "2020-02-17-8-30至2020-02-18-12-30[类型]=旺旺号(自定义时、分)\r\n" +
                "[类型]可为：下注、上下分、艾特分、统计、百分比，或者不填=全部显示\r\n" +
                "如：今天百分比=旺旺号\r\n" +
                "\r\n" +
                "今天数据=旺旺号\r\n" +
                "昨天数据=旺旺号\r\n" +
                "2020-02-17数据=旺旺号\r\n" +
                "回水工具-统计查询右边列表\r\n" +
                "\r\n" +
                "今天期盈利、昨天期盈利、2020-02-17期盈利\r\n" +
                "今天盈利、昨天盈利、2020-02-17盈利\r\n" +
                "加黑名单旺旺号、减黑名单旺旺号\r\n" +
                "改名旺旺号=新名片、旺旺号改名=新名片\r\n" +
                "旺旺号+-=分数理由，如123456+100红包，可多行，但理由需要相同\r\n" +
                "查询邀请旺旺号、查询被邀请旺旺号\r\n" +
                "\r\n" +
                "换机器人，如：私聊框架机器人 换机器人 ，即可把当前机器人设置机器人\r\n" +
                "\r\n" +
                "开启私聊命令处理上下分后，需管理员与机器人是好友可用\r\n" +
                "查看上分|查看下分(不支持命令+数字)\r\n" +
                "到的|没到|全到|忽略查钱\r\n" +
                "回钱|拒绝|全回|忽略回钱\r\n" +
                "支持命令+数字\r\n" +
                "如到的2，处理第2条，全到5，处理前5条\r\n" +
                "直接发命令，如到的，处理第1条\r\n" +
                "\r\n" +
                "********************************************\r\n" +
                "\r\n" +
                "直接艾特，等于这个人在群里发1\r\n" +
                "\r\n" +
                "艾特分数到、艾特分数查\r\n" +
                "如：@旺旺1000到、@旺旺1000查\r\n" +
                "\r\n" +
                "@旺旺+分数，如@123456+100，不需要理由，记录到艾特上下分\r\n" +
                "与私聊123456+100理由差不多\r\n" +
                "\r\n" +
                "@旺旺改名=新名片、改名@旺旺=新名片\r\n" +
                "@旺旺加黑名单、@旺旺减黑名单\r\n" +
                "@旺旺踢\r\n" +
                "@旺旺解禁 @旺旺禁言";

            // BillReplaceCommandControl - Main container
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.lblTitle);
            this.Controls.Add(this.txtReplaceHelp);
            this.Size = new System.Drawing.Size(325, 130);

            this.ResumeLayout(false);
            this.PerformLayout();
        }

        private System.Windows.Forms.Label lblTitle;
        private System.Windows.Forms.RichTextBox txtReplaceHelp;
    }
}
