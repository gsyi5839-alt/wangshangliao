using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using WangShangLiaoBot.Services;

namespace WangShangLiaoBot.Controls
{
    /// <summary>
    /// 消息模板管理控件 - 自定义自动回复模板
    /// </summary>
    public class MessageTemplateControl : UserControl
    {
        #region 控件

        private ListView lstTemplates;
        private TextBox txtTemplateContent;
        private Label lblCurrentKey;
        private Label lblVariableHint;
        private Button btnSave;
        private Button btnPreview;
        private Button btnResetAll;

        private string _currentKey;

        #endregion

        public MessageTemplateControl()
        {
            InitializeComponent();
            LoadTemplates();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            this.Size = new Size(900, 600);

            // 模板列表
            var lblList = new Label
            {
                Text = "模板列表:",
                Location = new Point(10, 10),
                AutoSize = true,
                Font = new Font(Font.FontFamily, 10, FontStyle.Bold)
            };
            this.Controls.Add(lblList);

            lstTemplates = new ListView
            {
                Location = new Point(10, 35),
                Size = new Size(280, 500),
                View = View.Details,
                FullRowSelect = true,
                GridLines = true
            };
            lstTemplates.Columns.Add("模板名称", 260);
            lstTemplates.SelectedIndexChanged += LstTemplates_SelectedIndexChanged;
            this.Controls.Add(lstTemplates);

            // 当前模板
            lblCurrentKey = new Label
            {
                Text = "当前模板: (请选择)",
                Location = new Point(310, 10),
                Size = new Size(400, 20),
                Font = new Font(Font.FontFamily, 10, FontStyle.Bold)
            };
            this.Controls.Add(lblCurrentKey);

            // 模板内容
            txtTemplateContent = new TextBox
            {
                Location = new Point(310, 35),
                Size = new Size(570, 300),
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                Font = new Font("Consolas", 10)
            };
            this.Controls.Add(txtTemplateContent);

            // 变量提示
            lblVariableHint = new Label
            {
                Text = "支持的变量:\n" +
                       "[艾特] - @玩家    [旺旺]/[昵称] - 玩家昵称    [余粮]/[余额] - 账户余额\n" +
                       "[玩家攻击]/[下注内容] - 下注详情    [分数]/[金额] - 金额\n" +
                       "[期数]/[期号] - 当前期号    [开奖号码]/[和值] - 开奖结果\n" +
                       "[一区] [二区] [三区] - 三个骰子值    [大小单双] - 开奖类型\n" +
                       "[豹顺对子] - 特殊类型    [龙虎豹] - 龙虎结果\n" +
                       "[客户人数]/[人数] - 下注人数    [总分数]/[总下注] - 总下注额\n" +
                       "[封盘倒计时] - 距封盘秒数    [换行] - 换行符",
                Location = new Point(310, 345),
                Size = new Size(570, 130),
                ForeColor = Color.Gray
            };
            this.Controls.Add(lblVariableHint);

            // 按钮
            btnSave = new Button
            {
                Text = "保存模板",
                Location = new Point(310, 485),
                Size = new Size(100, 35),
                BackColor = Color.FromArgb(46, 204, 113),
                ForeColor = Color.White
            };
            btnSave.Click += BtnSave_Click;
            this.Controls.Add(btnSave);

            btnPreview = new Button
            {
                Text = "预览效果",
                Location = new Point(420, 485),
                Size = new Size(100, 35)
            };
            btnPreview.Click += BtnPreview_Click;
            this.Controls.Add(btnPreview);

            btnResetAll = new Button
            {
                Text = "重置所有模板",
                Location = new Point(530, 485),
                Size = new Size(120, 35),
                BackColor = Color.FromArgb(231, 76, 60),
                ForeColor = Color.White
            };
            btnResetAll.Click += BtnResetAll_Click;
            this.Controls.Add(btnResetAll);

            // 分类标签
            var categories = new[]
            {
                ("下注相关", new[] { "下注显示", "重复下注", "余粮不足", "攻击上分有效", "超出范围", "取消下注", "模糊匹配提醒", "已封盘下注无效" }),
                ("托管相关", new[] { "托管成功", "取消托管" }),
                ("上分相关", new[] { "上分到词", "上分到词_0分", "上分到词_第二条", "上分没到词", "客户上分回复" }),
                ("下分相关", new[] { "下分查分词", "下分查分词_第二条", "下分拒绝词", "下分勿催词", "客户下分回复", "下分正在处理", "下注不能下分", "下分不能下注", "下分最少下注次数", "下分一次性回" }),
                ("封盘相关", new[] { "封盘提示", "封盘内容", "发送规矩内容" }),
                ("开奖相关", new[] { "开奖发送", "账单发送" }),
                ("查询相关", new[] { "发1_0分", "发1_有分无攻击", "发1_有分有攻击" }),
                ("回水相关", new[] { "返点_有回水回复", "返点_无回水回复", "返点_把数不达标回复" }),
                ("其他", new[] { "进群私聊玩家", "私聊尾巴_未封盘", "私聊尾巴_已封盘", "禁止点09" })
            };

            this.ResumeLayout(false);
        }

        private void LoadTemplates()
        {
            lstTemplates.Items.Clear();

            var templates = MessageTemplateService.Instance.GetAllTemplates();

            // 按分类组织
            var categories = new Dictionary<string, List<string>>
            {
                ["下注相关"] = new List<string> { "下注显示", "重复下注", "余粮不足", "攻击上分有效", "超出范围", "取消下注", "模糊匹配提醒", "已封盘下注无效" },
                ["托管相关"] = new List<string> { "托管成功", "取消托管" },
                ["上分相关"] = new List<string> { "上分到词", "上分到词_0分", "上分到词_第二条", "上分没到词", "客户上分回复" },
                ["下分相关"] = new List<string> { "下分查分词", "下分查分词_第二条", "下分拒绝词", "下分勿催词", "客户下分回复", "下分正在处理", "下注不能下分", "下分不能下注", "下分最少下注次数", "下分一次性回" },
                ["封盘相关"] = new List<string> { "封盘提示", "封盘内容", "发送规矩内容" },
                ["开奖相关"] = new List<string> { "开奖发送", "账单发送" },
                ["查询相关"] = new List<string> { "发1_0分", "发1_有分无攻击", "发1_有分有攻击" },
                ["回水相关"] = new List<string> { "返点_有回水回复", "返点_无回水回复", "返点_把数不达标回复" },
                ["其他"] = new List<string> { "进群私聊玩家", "私聊尾巴_未封盘", "私聊尾巴_已封盘", "禁止点09" }
            };

            foreach (var cat in categories)
            {
                // 添加分类标题
                var catItem = new ListViewItem($"【{cat.Key}】");
                catItem.ForeColor = Color.Blue;
                catItem.Font = new Font(lstTemplates.Font, FontStyle.Bold);
                lstTemplates.Items.Add(catItem);

                // 添加模板项
                foreach (var key in cat.Value)
                {
                    if (templates.ContainsKey(key))
                    {
                        var item = new ListViewItem($"  {key}");
                        item.Tag = key;
                        lstTemplates.Items.Add(item);
                    }
                }
            }

            // 添加自定义模板
            var customItem = new ListViewItem("【自动回复关键词】");
            customItem.ForeColor = Color.Blue;
            customItem.Font = new Font(lstTemplates.Font, FontStyle.Bold);
            lstTemplates.Items.Add(customItem);

            var keywordKeys = new[] { "自动回复_历史关键词", "自动回复_账单关键词", "自动回复_财付通关键词", "自动回复_支付宝关键词", "自动回复_微信关键词" };
            foreach (var key in keywordKeys)
            {
                if (templates.ContainsKey(key))
                {
                    var item = new ListViewItem($"  {key}");
                    item.Tag = key;
                    lstTemplates.Items.Add(item);
                }
            }
        }

        private void LstTemplates_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (lstTemplates.SelectedItems.Count == 0) return;

            var item = lstTemplates.SelectedItems[0];
            var key = item.Tag as string;

            if (string.IsNullOrEmpty(key)) return;

            _currentKey = key;
            lblCurrentKey.Text = $"当前模板: {key}";
            txtTemplateContent.Text = MessageTemplateService.Instance.GetTemplate(key);
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(_currentKey))
            {
                MessageBox.Show("请先选择一个模板！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            MessageTemplateService.Instance.SetTemplate(_currentKey, txtTemplateContent.Text);
            MessageTemplateService.Instance.SaveTemplates();
            MessageBox.Show("模板已保存！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void BtnPreview_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(txtTemplateContent.Text))
            {
                MessageBox.Show("模板内容为空！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // 使用示例数据预览
            var variables = new Dictionary<string, string>
            {
                ["艾特"] = "@测试玩家",
                ["旺旺"] = "测试玩家",
                ["昵称"] = "测试玩家",
                ["玩家ID"] = "123456",
                ["玩家攻击"] = "大100 小单50",
                ["下注内容"] = "大100 小单50",
                ["下注"] = "大100 小单50",
                ["余粮"] = "1000.00",
                ["余额"] = "1000.00",
                ["分数"] = "150.00",
                ["金额"] = "150.00",
                ["期数"] = "20240110001",
                ["期号"] = "20240110001",
                ["一区"] = "8",
                ["二区"] = "5",
                ["三区"] = "6",
                ["开奖号码"] = "19",
                ["和值"] = "19",
                ["大小"] = "大",
                ["单双"] = "单",
                ["大小单双"] = "大单",
                ["豹顺对子"] = "",
                ["龙虎豹"] = "虎",
                ["客户人数"] = "10",
                ["人数"] = "10",
                ["总分数"] = "5000.00",
                ["总下注"] = "5000.00",
                ["封盘倒计时"] = "30",
                ["留分"] = "850.00"
            };

            var preview = MessageTemplateService.Instance.RenderText(txtTemplateContent.Text, variables);

            var previewForm = new Form
            {
                Text = "预览效果",
                Size = new Size(500, 300),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false
            };

            var txtPreview = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                ReadOnly = true,
                Text = preview,
                Font = new Font("Microsoft YaHei", 10),
                BackColor = Color.FromArgb(40, 44, 52),
                ForeColor = Color.White
            };

            previewForm.Controls.Add(txtPreview);
            previewForm.ShowDialog();
        }

        private void BtnResetAll_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("确定要重置所有模板为默认值吗？\n此操作不可撤销！", "确认",
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
            {
                // 重新加载默认模板
                var service = MessageTemplateService.Instance;
                // 服务会在下次启动时重新加载默认值
                try
                {
                    var configPath = System.IO.Path.Combine(
                        DataService.Instance.DatabaseDir, "message-templates.ini");
                    if (System.IO.File.Exists(configPath))
                    {
                        System.IO.File.Delete(configPath);
                    }
                }
                catch { }

                MessageBox.Show("已重置所有模板！\n请重启程序以生效。", "提示",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
    }
}
