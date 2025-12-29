using System;
using System.Drawing;
using System.Media;
using System.Windows.Forms;
using WangShangLiaoBot.Models;
using WangShangLiaoBot.Services;

namespace WangShangLiaoBot.Forms
{
    /// <summary>
    /// 上下分处理窗口
    /// </summary>
    public partial class ScoreForm : Form
    {
        private Form _mainForm;
        
        public ScoreForm()
        {
            InitializeComponent();
            LoadSettings();
            
            // 默认显示上分/下分页面
            ShowPanel(panelUpDown);
        }
        
        /// <summary>
        /// 设置主窗口引用（用于跟随移动）
        /// </summary>
        public void SetMainForm(Form mainForm)
        {
            _mainForm = mainForm;
            if (_mainForm != null && chkFollowMainWindow.Checked)
            {
                _mainForm.LocationChanged += MainForm_LocationChanged;
            }
        }
        
        private void MainForm_LocationChanged(object sender, EventArgs e)
        {
            if (_mainForm != null && chkFollowMainWindow.Checked)
            {
                this.Location = new Point(
                    _mainForm.Right + 10,
                    _mainForm.Top
                );
            }
        }
        
        #region 菜单切换
        
        private void ShowPanel(Panel panel)
        {
            panelUpDown.Visible = false;
            panelSettings.Visible = false;
            panelSettings2.Visible = false;
            panelText.Visible = false;
            
            panel.Visible = true;
            panel.BringToFront();
        }
        
        private void menuUpDown_Click(object sender, EventArgs e)
        {
            ShowPanel(panelUpDown);
        }
        
        private void menuSettings_Click(object sender, EventArgs e)
        {
            ShowPanel(panelSettings);
        }
        
        private void menuSettings2_Click(object sender, EventArgs e)
        {
            ShowPanel(panelSettings2);
        }
        
        private void menuText_Click(object sender, EventArgs e)
        {
            ShowPanel(panelText);
        }
        
        #endregion
        
        #region 加载和保存设置
        
        /// <summary>
        /// 加载设置
        /// </summary>
        private void LoadSettings()
        {
            var config = ConfigService.Instance.Config;
            
            // 设置页面
            txtUpKeyword.Text = config.UpScoreKeywords;
            txtDownKeyword.Text = config.DownScoreKeywords;
            txtMinRounds.Text = config.MinRoundsBeforeDownScore.ToString();
            txtMinScore.Text = config.MinScoreForSingleDown.ToString();
            chkUpMsgFeedback.Checked = config.UpScoreFeedbackToGroup;
            chkDownMsgFeedback.Checked = config.DownScoreFeedbackToGroup;
            chkUpScoreSound.Checked = config.EnableUpScoreSound;
            chkDownScoreSound.Checked = config.EnableDownScoreSound;
            txtClientDownReplyContent.Text = config.ClientDownReplyContent;
            
            // 提示文本页面
            txtNotArrivedText.Text = config.NotArrivedText;
            txtZeroArrivedText.Text = config.ZeroArrivedText;
            txtHasScoreText.Text = config.HasScoreArrivedText;
            txtNoScoreText.Text = config.CheckScoreText;
            txtReSaveText.Text = config.DontRushText;
            txtDontRushText.Text = config.RejectText;
        }
        
        /// <summary>
        /// 保存设置
        /// </summary>
        private void SaveSettings()
        {
            var config = ConfigService.Instance.Config;
            
            config.UpScoreKeywords = txtUpKeyword.Text.Trim();
            config.DownScoreKeywords = txtDownKeyword.Text.Trim();
            
            int minRounds;
            if (int.TryParse(txtMinRounds.Text, out minRounds))
            {
                config.MinRoundsBeforeDownScore = minRounds;
            }
            
            int minScore;
            if (int.TryParse(txtMinScore.Text, out minScore))
            {
                config.MinScoreForSingleDown = minScore;
            }
            
            config.UpScoreFeedbackToGroup = chkUpMsgFeedback.Checked;
            config.DownScoreFeedbackToGroup = chkDownMsgFeedback.Checked;
            config.EnableUpScoreSound = chkUpScoreSound.Checked;
            config.EnableDownScoreSound = chkDownScoreSound.Checked;
            config.ClientDownReplyContent = txtClientDownReplyContent.Text;
            
            ConfigService.Instance.SaveConfig();
            Logger.Info("上下分设置已保存");
        }
        
        /// <summary>
        /// 保存提示文本
        /// </summary>
        private void SaveTextSettings()
        {
            var config = ConfigService.Instance.Config;
            
            config.NotArrivedText = txtNotArrivedText.Text;
            config.ZeroArrivedText = txtZeroArrivedText.Text;
            config.HasScoreArrivedText = txtHasScoreText.Text;
            config.CheckScoreText = txtNoScoreText.Text;
            config.DontRushText = txtReSaveText.Text;
            config.RejectText = txtDontRushText.Text;
            
            ConfigService.Instance.SaveConfig();
            Logger.Info("提示文本设置已保存");
        }
        
        private void btnSaveSettings_Click(object sender, EventArgs e)
        {
            SaveSettings();
            MessageBox.Show("设置已保存", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        
        private void btnSaveText_Click(object sender, EventArgs e)
        {
            SaveTextSettings();
            MessageBox.Show("提示文本设置已保存", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        
        #endregion
        
        #region 上分处理
        
        /// <summary>
        /// 修改上分
        /// </summary>
        private void btnModifyUpScore_Click(object sender, EventArgs e)
        {
            if (listUpRequests.SelectedItems.Count == 0)
            {
                MessageBox.Show("请选择要修改的上分请求", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            
            int newScore;
            if (!int.TryParse(txtRequestUpScore.Text, out newScore))
            {
                MessageBox.Show("请输入有效的上分金额", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            
            var item = listUpRequests.SelectedItems[0];
            item.SubItems[2].Text = newScore.ToString();
            Logger.Info(string.Format("修改上分: {0} -> {1}", item.SubItems[1].Text, newScore));
        }
        
        /// <summary>
        /// 喊到
        /// </summary>
        private async void btnUpArrived_Click(object sender, EventArgs e)
        {
            if (listUpRequests.SelectedItems.Count == 0)
            {
                MessageBox.Show("请选择要处理的上分请求", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            
            var item = listUpRequests.SelectedItems[0];
            var reply = ProcessReplyTemplate(txtHasScoreText.Text, item);
            
            if (await ChatService.Instance.SendMessageAsync(reply))
            {
                item.BackColor = Color.LightGreen;
                Logger.Info(string.Format("上分处理完成: {0}", item.SubItems[1].Text));
            }
        }
        
        /// <summary>
        /// 喊没到
        /// </summary>
        private async void btnUpNotArrived_Click(object sender, EventArgs e)
        {
            if (listUpRequests.SelectedItems.Count == 0)
            {
                MessageBox.Show("请选择要处理的上分请求", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            
            var item = listUpRequests.SelectedItems[0];
            var reply = ProcessReplyTemplate(txtNotArrivedText.Text, item);
            
            if (await ChatService.Instance.SendMessageAsync(reply))
            {
                item.BackColor = Color.LightCoral;
                Logger.Info(string.Format("上分未到: {0}", item.SubItems[1].Text));
            }
        }
        
        /// <summary>
        /// 忽略上分
        /// </summary>
        private void btnUpIgnore_Click(object sender, EventArgs e)
        {
            if (listUpRequests.SelectedItems.Count == 0) return;
            
            var item = listUpRequests.SelectedItems[0];
            item.BackColor = Color.LightGray;
            Logger.Info(string.Format("忽略上分: {0}", item.SubItems[1].Text));
        }
        
        #endregion
        
        #region 下分处理
        
        /// <summary>
        /// 修改下分
        /// </summary>
        private void btnModifyDownScore_Click(object sender, EventArgs e)
        {
            if (listDownRequests.SelectedItems.Count == 0)
            {
                MessageBox.Show("请选择要修改的下分请求", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            
            int newScore;
            if (!int.TryParse(txtRequestDownScore.Text, out newScore))
            {
                MessageBox.Show("请输入有效的下分金额", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            
            var item = listDownRequests.SelectedItems[0];
            item.SubItems[2].Text = newScore.ToString();
            Logger.Info(string.Format("修改下分: {0} -> {1}", item.SubItems[1].Text, newScore));
        }
        
        /// <summary>
        /// 喊查
        /// </summary>
        private async void btnDownCheck_Click(object sender, EventArgs e)
        {
            if (listDownRequests.SelectedItems.Count == 0)
            {
                MessageBox.Show("请选择要处理的下分请求", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            
            var item = listDownRequests.SelectedItems[0];
            var reply = ProcessReplyTemplate(txtNoScoreText.Text, item);
            
            if (await ChatService.Instance.SendMessageAsync(reply))
            {
                item.BackColor = Color.LightGreen;
                Logger.Info(string.Format("下分查询: {0}", item.SubItems[1].Text));
            }
        }
        
        /// <summary>
        /// 拒绝下分
        /// </summary>
        private async void btnDownReject_Click(object sender, EventArgs e)
        {
            if (listDownRequests.SelectedItems.Count == 0)
            {
                MessageBox.Show("请选择要处理的下分请求", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            
            var item = listDownRequests.SelectedItems[0];
            var reply = ProcessReplyTemplate(txtDontRushText.Text, item);
            
            if (await ChatService.Instance.SendMessageAsync(reply))
            {
                item.BackColor = Color.LightCoral;
                Logger.Info(string.Format("拒绝下分: {0}", item.SubItems[1].Text));
            }
        }
        
        /// <summary>
        /// 忽略下分
        /// </summary>
        private void btnDownIgnore_Click(object sender, EventArgs e)
        {
            if (listDownRequests.SelectedItems.Count == 0) return;
            
            var item = listDownRequests.SelectedItems[0];
            item.BackColor = Color.LightGray;
            Logger.Info(string.Format("忽略下分: {0}", item.SubItems[1].Text));
        }
        
        #endregion
        
        #region 辅助方法
        
        /// <summary>
        /// 处理回复模板
        /// </summary>
        private string ProcessReplyTemplate(string template, ListViewItem item)
        {
            if (string.IsNullOrEmpty(template)) return "";

            // ListViewItem layout:
            // 0: playerId, 1: nickname, 2: score, 3: grain(reserved), 4: count
            var playerId = item?.SubItems.Count > 0 ? item.SubItems[0].Text : "";
            var nickname = item?.SubItems.Count > 1 ? item.SubItems[1].Text : "";
            var score = item?.SubItems.Count > 2 ? item.SubItems[2].Text : "";
            var grain = item?.SubItems.Count > 3 ? item.SubItems[3].Text : "";

            // Backward compatible token used in old templates
            var normalized = template.Replace("@qq", string.IsNullOrEmpty(nickname) ? "" : "@" + nickname);

            // Render via unified template engine (supports advanced variables too)
            decimal.TryParse(score, out var scoreAmount);
            decimal.TryParse(grain, out var grainAmount);

            return TemplateEngine.Render(normalized, new TemplateEngine.RenderContext
            {
                Player = DataService.Instance.GetPlayer(playerId),
                Score = new TemplateEngine.ScoreContext
                {
                    WangWangId = playerId,
                    Nickname = nickname,
                    Amount = scoreAmount,
                    Grain = grainAmount
                },
                Today = DateTime.Today
            });
        }
        
        /// <summary>
        /// 添加上分请求到列表
        /// </summary>
        public void AddUpScoreRequest(string playerId, string nickname, string score, string grain, string count)
        {
            var item = new ListViewItem(playerId);
            item.SubItems.Add(nickname);
            item.SubItems.Add(score);
            item.SubItems.Add(grain);
            item.SubItems.Add(count);
            listUpRequests.Items.Add(item);
            
            lblUpStatus.Text = string.Format("待处理上分: {0}", listUpRequests.Items.Count);
            lblUpStatus.ForeColor = Color.Red;
            
            // 播放提示音
            if (chkUpScoreSound.Checked)
            {
                PlaySound();
            }
            
            // 自动显示窗口
            if (chkAutoShowWindow.Checked && !this.Visible)
            {
                this.Show();
            }
        }
        
        /// <summary>
        /// 添加下分请求到列表
        /// </summary>
        public void AddDownScoreRequest(string playerId, string nickname, string score, string grain, string count)
        {
            var item = new ListViewItem(playerId);
            item.SubItems.Add(nickname);
            item.SubItems.Add(score);
            item.SubItems.Add(grain);
            item.SubItems.Add(count);
            listDownRequests.Items.Add(item);
            
            lblDownStatus.Text = string.Format("待处理下分: {0}", listDownRequests.Items.Count);
            lblDownStatus.ForeColor = Color.Red;
            
            // 播放提示音
            if (chkDownScoreSound.Checked)
            {
                PlaySound();
            }
            
            // 自动显示窗口
            if (chkAutoShowWindow.Checked && !this.Visible)
            {
                this.Show();
            }
        }
        
        /// <summary>
        /// 播放提示音
        /// </summary>
        private void PlaySound()
        {
            try
            {
                SystemSounds.Asterisk.Play();
            }
            catch { }
        }
        
        #endregion
        
        #region 快捷键
        
        /// <summary>
        /// F11 隐藏/显示窗口
        /// </summary>
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.F11)
            {
                this.Visible = !this.Visible;
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }
        
        #endregion
        
        #region 窗口关闭处理
        
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            // 不真正关闭，只是隐藏
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                this.Hide();
            }
            base.OnFormClosing(e);
        }
        
        #endregion
    }
}
