using System;
using System.Text;
using System.Windows.Forms;
using System.IO;
using WangShangLiaoBot.Services;

namespace WangShangLiaoBot.Forms
{
    /// <summary>
    /// Bill export/import dialog form
    /// </summary>
    public partial class BillExportForm : Form
    {
        public BillExportForm()
        {
            InitializeComponent();
        }
        
        /// <summary>
        /// Initialize form components
        /// </summary>
        private void InitializeComponent()
        {
            this.txtBillContent = new TextBox();
            this.lblFormat = new Label();
            this.btnExportBill = new Button();
            this.btnClearText = new Button();
            this.btnAddText = new Button();
            this.btnImportBill = new Button();
            this.btnAnalyzeOther = new Button();
            this.btnAnalyzeNick = new Button();
            this.SuspendLayout();
            
            // ===== Main text area =====
            this.txtBillContent.Location = new System.Drawing.Point(12, 12);
            this.txtBillContent.Multiline = true;
            this.txtBillContent.ScrollBars = ScrollBars.Both;
            this.txtBillContent.Size = new System.Drawing.Size(610, 280);
            this.txtBillContent.Font = new System.Drawing.Font("Consolas", 10F);
            
            // ===== Format description label =====
            this.lblFormat.Location = new System.Drawing.Point(12, 300);
            this.lblFormat.Size = new System.Drawing.Size(200, 80);
            this.lblFormat.Text = "格式:\r\n" +
                "大师(1234567891) = 78945\r\n" +
                "大师(123456789$) = 78945\r\n" +
                "大师(12345678$$) = 7894\r\n\r\n" +
                "空格无影响";
            this.lblFormat.ForeColor = System.Drawing.Color.Black;
            
            // ===== Buttons row 1 =====
            this.btnExportBill.Text = "导出账单";
            this.btnExportBill.Location = new System.Drawing.Point(280, 300);
            this.btnExportBill.Size = new System.Drawing.Size(80, 28);
            this.btnExportBill.Click += new EventHandler(this.btnExportBill_Click);
            
            this.btnClearText.Text = "清空文本";
            this.btnClearText.Location = new System.Drawing.Point(365, 300);
            this.btnClearText.Size = new System.Drawing.Size(80, 28);
            this.btnClearText.Click += new EventHandler(this.btnClearText_Click);
            
            this.btnAddText.Text = "加入文本";
            this.btnAddText.Location = new System.Drawing.Point(450, 300);
            this.btnAddText.Size = new System.Drawing.Size(80, 28);
            this.btnAddText.Click += new EventHandler(this.btnAddText_Click);
            
            this.btnImportBill.Text = "导入账单";
            this.btnImportBill.Location = new System.Drawing.Point(535, 300);
            this.btnImportBill.Size = new System.Drawing.Size(80, 28);
            this.btnImportBill.Click += new EventHandler(this.btnImportBill_Click);
            
            // ===== Buttons row 2 =====
            this.btnAnalyzeOther.Text = "分析其他机器昵称账单";
            this.btnAnalyzeOther.Location = new System.Drawing.Point(280, 335);
            this.btnAnalyzeOther.Size = new System.Drawing.Size(160, 28);
            this.btnAnalyzeOther.Click += new EventHandler(this.btnAnalyzeOther_Click);
            
            this.btnAnalyzeNick.Text = "分析昵称账单";
            this.btnAnalyzeNick.Location = new System.Drawing.Point(450, 335);
            this.btnAnalyzeNick.Size = new System.Drawing.Size(165, 28);
            this.btnAnalyzeNick.Click += new EventHandler(this.btnAnalyzeNick_Click);
            
            // ===== Form =====
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(634, 375);
            this.Controls.Add(this.txtBillContent);
            this.Controls.Add(this.lblFormat);
            this.Controls.Add(this.btnExportBill);
            this.Controls.Add(this.btnClearText);
            this.Controls.Add(this.btnAddText);
            this.Controls.Add(this.btnImportBill);
            this.Controls.Add(this.btnAnalyzeOther);
            this.Controls.Add(this.btnAnalyzeNick);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "BillExportForm";
            this.StartPosition = FormStartPosition.CenterParent;
            this.Text = "导出账单";
            
            // Set icon
            try
            {
                var iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "app.ico");
                if (File.Exists(iconPath))
                {
                    this.Icon = new System.Drawing.Icon(iconPath);
                }
            }
            catch { }
            
            this.ResumeLayout(false);
            this.PerformLayout();
        }
        
        // ===== Event Handlers =====
        
        /// <summary>
        /// Export bill - Generate bill text from player data
        /// </summary>
        private void btnExportBill_Click(object sender, EventArgs e)
        {
            try
            {
                var players = DataService.Instance.GetAllPlayers();
                var sb = new StringBuilder();
                
                foreach (var player in players)
                {
                    if (player.Score != 0)
                    {
                        // Format: 昵称(旺旺号) = 分数
                        var nick = string.IsNullOrEmpty(player.Nickname) ? "玩家" : player.Nickname;
                        sb.AppendLine(string.Format("{0}({1}) = {2}", nick, player.WangWangId, player.Score));
                    }
                }
                
                if (sb.Length > 0)
                {
                    txtBillContent.Text = sb.ToString();
                    MessageBox.Show(string.Format("已导出 {0} 条账单记录", players.Count), "导出成功", 
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show("没有可导出的账单数据", "提示", 
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("导出失败: " + ex.Message, "错误", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        
        /// <summary>
        /// Clear text area
        /// </summary>
        private void btnClearText_Click(object sender, EventArgs e)
        {
            txtBillContent.Clear();
        }
        
        /// <summary>
        /// Add text from clipboard
        /// </summary>
        private void btnAddText_Click(object sender, EventArgs e)
        {
            try
            {
                if (Clipboard.ContainsText())
                {
                    var clipText = Clipboard.GetText();
                    txtBillContent.AppendText(clipText);
                    if (!clipText.EndsWith("\n"))
                    {
                        txtBillContent.AppendText(Environment.NewLine);
                    }
                }
                else
                {
                    MessageBox.Show("剪贴板中没有文本内容", "提示", 
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("加入文本失败: " + ex.Message, "错误", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        
        /// <summary>
        /// Import bill - Parse text and update player scores
        /// </summary>
        private void btnImportBill_Click(object sender, EventArgs e)
        {
            try
            {
                var content = txtBillContent.Text.Trim();
                if (string.IsNullOrEmpty(content))
                {
                    MessageBox.Show("请先输入或粘贴账单内容", "提示", 
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                
                var lines = content.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                var importCount = 0;
                
                foreach (var line in lines)
                {
                    // Parse format: 昵称(旺旺号) = 分数
                    var match = System.Text.RegularExpressions.Regex.Match(
                        line.Trim(), 
                        @"(.+?)\(([^)]+)\)\s*=\s*(-?\d+\.?\d*)");
                    
                    if (match.Success)
                    {
                        var nickname = match.Groups[1].Value.Trim();
                        var wangwangId = match.Groups[2].Value.Trim().Replace("$", "");
                        decimal score;
                        if (decimal.TryParse(match.Groups[3].Value, out score))
                        {
                            // Save or update player
                            var player = new Models.Player
                            {
                                WangWangId = wangwangId,
                                Nickname = nickname,
                                Score = score,
                                LastActiveTime = DateTime.Now
                            };
                            DataService.Instance.SavePlayer(player);
                            importCount++;
                        }
                    }
                }
                
                MessageBox.Show(string.Format("成功导入 {0} 条账单记录", importCount), "导入成功", 
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("导入失败: " + ex.Message, "错误", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        
        /// <summary>
        /// Analyze bill from other robots (different format)
        /// </summary>
        private void btnAnalyzeOther_Click(object sender, EventArgs e)
        {
            try
            {
                var content = txtBillContent.Text.Trim();
                if (string.IsNullOrEmpty(content))
                {
                    MessageBox.Show("请先输入要分析的账单内容", "提示", 
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                
                var lines = content.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                var sb = new StringBuilder();
                var parseCount = 0;
                
                foreach (var line in lines)
                {
                    // Try multiple formats
                    // Format 1: 昵称 = 分数
                    var match1 = System.Text.RegularExpressions.Regex.Match(
                        line.Trim(), @"(.+?)\s*[=:：]\s*(-?\d+\.?\d*)");
                    
                    if (match1.Success)
                    {
                        var nickname = match1.Groups[1].Value.Trim();
                        var score = match1.Groups[2].Value;
                        
                        // Extract ID if exists in nickname
                        var idMatch = System.Text.RegularExpressions.Regex.Match(nickname, @"\(([^)]+)\)");
                        var id = idMatch.Success ? idMatch.Groups[1].Value : "未知";
                        var name = idMatch.Success ? nickname.Replace(idMatch.Value, "").Trim() : nickname;
                        
                        sb.AppendLine(string.Format("{0}({1}) = {2}", name, id, score));
                        parseCount++;
                    }
                }
                
                if (parseCount > 0)
                {
                    txtBillContent.Text = sb.ToString();
                    MessageBox.Show(string.Format("已分析转换 {0} 条记录", parseCount), "分析完成", 
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show("未能识别账单格式", "提示", 
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("分析失败: " + ex.Message, "错误", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        
        /// <summary>
        /// Analyze nickname bill
        /// </summary>
        private void btnAnalyzeNick_Click(object sender, EventArgs e)
        {
            try
            {
                var content = txtBillContent.Text.Trim();
                if (string.IsNullOrEmpty(content))
                {
                    MessageBox.Show("请先输入要分析的账单内容", "提示", 
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                
                var lines = content.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                var sb = new StringBuilder();
                var parseCount = 0;
                
                foreach (var line in lines)
                {
                    // Format: 昵称 分数 or 昵称:分数
                    var match = System.Text.RegularExpressions.Regex.Match(
                        line.Trim(), @"^(.+?)\s+(-?\d+\.?\d*)$");
                    
                    if (match.Success)
                    {
                        var nickname = match.Groups[1].Value.Trim();
                        var score = match.Groups[2].Value;
                        sb.AppendLine(string.Format("{0}(未知) = {1}", nickname, score));
                        parseCount++;
                    }
                }
                
                if (parseCount > 0)
                {
                    txtBillContent.Text = sb.ToString();
                    MessageBox.Show(string.Format("已分析转换 {0} 条记录\n请手动补充旺旺号", parseCount), "分析完成", 
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show("未能识别账单格式", "提示", 
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("分析失败: " + ex.Message, "错误", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        
        // Controls
        private TextBox txtBillContent;
        private Label lblFormat;
        private Button btnExportBill;
        private Button btnClearText;
        private Button btnAddText;
        private Button btnImportBill;
        private Button btnAnalyzeOther;
        private Button btnAnalyzeNick;
    }
}

