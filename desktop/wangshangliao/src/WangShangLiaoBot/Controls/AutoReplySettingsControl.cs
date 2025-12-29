using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using WangShangLiaoBot.Models;
using WangShangLiaoBot.Services;

namespace WangShangLiaoBot.Controls
{
    /// <summary>
    /// Auto reply settings control - manages keyword auto-reply configuration
    /// </summary>
    public partial class AutoReplySettingsControl : UserControl
    {
        /// <summary>
        /// Constructor - initializes the control
        /// </summary>
        public AutoReplySettingsControl()
        {
            InitializeComponent();
            InitializeEvents();
            InitializeSafeLayout();
            LoadSettings();
        }

        /// <summary>
        /// Initialize layout behaviors that must be applied AFTER the control is sized.
        /// This prevents runtime crashes caused by setting SplitContainer.SplitterDistance too early.
        /// </summary>
        private void InitializeSafeLayout()
        {
            // Apply once when handle is created (control is attached to a parent and sized).
            this.HandleCreated += (s, e) => ApplyRightPanelLayout();
            // Re-apply when resized to keep the right panel width consistent with the design constraints.
            this.SizeChanged += (s, e) => ApplyRightPanelLayout();

            // Best-effort: apply immediately if possible.
            ApplyRightPanelLayout();
        }

        /// <summary>
        /// Keep the right panel wide enough for fixed-size widgets (grid 305x179, inputs 240x55/127),
        /// while clamping SplitterDistance to valid range to avoid:
        /// "SplitterDistance must be between Panel1MinSize and Width - Panel2MinSize".
        /// </summary>
        private void ApplyRightPanelLayout()
        {
            try
            {
                if (splitMain == null) return;

                // When not yet laid out, Width can be very small. Never set SplitterDistance
                // unless the SplitContainer has a valid [min,max] interval.
                var splitter = Math.Max(0, splitMain.SplitterWidth);
                var total = Math.Max(0, splitMain.Width);
                if (total <= splitter + 1) return;

                // Desired minimum width for right panel to host fixed-size controls comfortably.
                const int desiredRightMin = 340;

                // SplitContainer constraints:
                // SplitterDistance must be within:
                //   [Panel1MinSize, Width - Panel2MinSize - SplitterWidth]
                var minDistance = Math.Max(0, splitMain.Panel1MinSize);

                // Panel2MinSize must also not exceed the available space after Panel1MinSize.
                var maxPanel2Min = Math.Max(0, total - splitter - minDistance);
                splitMain.Panel2MinSize = Math.Min(desiredRightMin, maxPanel2Min);

                var maxDistance = total - splitMain.Panel2MinSize - splitter;

                // If no valid range exists, skip (avoid throwing at runtime).
                if (maxDistance < minDistance) return;

                // Keep right panel at its minimum (design-locked): move splitter as far right as allowed.
                var distance = maxDistance;
                if (distance < minDistance) distance = minDistance;
                if (distance > maxDistance) distance = maxDistance;

                if (splitMain.SplitterDistance != distance)
                    splitMain.SplitterDistance = distance;
            }
            catch
            {
                // Never crash the app due to layout issues; keep best-effort behavior.
            }
        }
        
        /// <summary>
        /// Initialize event handlers
        /// </summary>
        private void InitializeEvents()
        {
            // DataGridView selection changed - fill input fields
            dgvKeywords.SelectionChanged += DgvKeywords_SelectionChanged;
            
            // Button click events
            btnAddModify.Click += BtnAddModify_Click;
            btnDelete.Click += BtnDelete_Click;
            btnSave.Click += BtnSave_Click;
            
            // QR Code import buttons
            btnCftQrCode.Click += BtnCftQrCode_Click;
            btnZfbQrCode.Click += BtnZfbQrCode_Click;
            btnWxQrCode.Click += BtnWxQrCode_Click;
        }
        
        /// <summary>
        /// Load settings from config
        /// </summary>
        public void LoadSettings()
        {
            try
            {
                var config = ConfigService.Instance.Config;
                
                // Load reply templates from config
                LoadReplyTemplates();
                
                // Load keyword rules to DataGridView
                RefreshKeywordGrid();
            }
            catch (Exception ex)
            {
                Logger.Error($"Load auto reply settings error: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Load reply templates from config
        /// </summary>
        private void LoadReplyTemplates()
        {
            var config = ConfigService.Instance.Config;
            
            // Load CaiFuTong templates
            txtCftSend.Text = config.CftSendText;
            txtCftText.Text = config.CftText;
            txtCftReply.Text = config.CftReplyKeywords;
            
            // Load ZhiFuBao templates
            txtZfbSend.Text = config.ZfbSendText;
            txtZfbText.Text = config.ZfbText;
            txtZfbReply.Text = config.ZfbReplyKeywords;
            
            // Load WeiXin templates
            txtWxSend.Text = config.WxSendText;
            txtWxText.Text = config.WxText;
            txtWxReply.Text = config.WxReplyKeywords;
        }
        
        /// <summary>
        /// Refresh keyword grid from config
        /// </summary>
        private void RefreshKeywordGrid()
        {
            dgvKeywords.Rows.Clear();
            
            var config = ConfigService.Instance.Config;
            foreach (var rule in config.KeywordRules)
            {
                // Third column is a blank filler column for the worksheet grid look.
                dgvKeywords.Rows.Add(rule.Keyword, rule.Reply, "");
            }

            // Keep the grid looking like a worksheet (show empty rows with grid lines)
            EnsureGridMinimumRows(10);
        }

        /// <summary>
        /// Ensure the DataGridView has at least N visible rows to match the design's worksheet look.
        /// </summary>
        private void EnsureGridMinimumRows(int minRows)
        {
            if (minRows <= 0) return;

            while (dgvKeywords.Rows.Count < minRows)
            {
                dgvKeywords.Rows.Add("", "", "");
            }
        }
        
        /// <summary>
        /// DataGridView selection changed - fill input fields
        /// </summary>
        private void DgvKeywords_SelectionChanged(object sender, EventArgs e)
        {
            if (dgvKeywords.CurrentRow != null && !dgvKeywords.CurrentRow.IsNewRow)
            {
                txtKeyword.Text = dgvKeywords.CurrentRow.Cells["colKeyword"].Value?.ToString() ?? "";
                txtReply.Text = dgvKeywords.CurrentRow.Cells["colReply"].Value?.ToString() ?? "";
            }
        }
        
        /// <summary>
        /// Add or modify keyword rule
        /// </summary>
        private void BtnAddModify_Click(object sender, EventArgs e)
        {
            var keyword = txtKeyword.Text.Trim();
            var reply = txtReply.Text.Trim();
            
            if (string.IsNullOrEmpty(keyword))
            {
                MessageBox.Show("请输入关键词！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            
            if (string.IsNullOrEmpty(reply))
            {
                MessageBox.Show("请输入回复内容！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            
            var config = ConfigService.Instance.Config;
            
            // Check if keyword exists
            var existingRule = config.KeywordRules.FirstOrDefault(r => r.Keyword == keyword);
            if (existingRule != null)
            {
                // Update existing
                existingRule.Reply = reply;
                existingRule.Enabled = true;
            }
            else
            {
                // Add new
                config.KeywordRules.Add(new KeywordReplyRule
                {
                    Keyword = keyword,
                    Reply = reply,
                    Enabled = true
                });
            }
            
            // Refresh grid
            RefreshKeywordGrid();
            
            // Clear input
            txtKeyword.Clear();
            txtReply.Clear();
            
            MessageBox.Show("关键词已添加/修改！", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        
        /// <summary>
        /// Delete keyword rule
        /// </summary>
        private void BtnDelete_Click(object sender, EventArgs e)
        {
            var keyword = txtKeyword.Text.Trim();
            
            // Try to get keyword from current selection if input is empty
            if (string.IsNullOrEmpty(keyword) && dgvKeywords.CurrentRow != null && !dgvKeywords.CurrentRow.IsNewRow)
            {
                keyword = dgvKeywords.CurrentRow.Cells["colKeyword"].Value?.ToString();
            }
            
            if (string.IsNullOrEmpty(keyword))
            {
                MessageBox.Show("请选择要删除的关键词！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            
            var result = MessageBox.Show($"确定要删除关键词 \"{keyword}\" 吗？", 
                "确认删除", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            
            if (result == DialogResult.Yes)
            {
                AutoReplyService.Instance.RemoveKeywordRule(keyword);
                RefreshKeywordGrid();
                txtKeyword.Clear();
                txtReply.Clear();
                MessageBox.Show("关键词已删除！", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
        
        /// <summary>
        /// Save all settings
        /// </summary>
        private void BtnSave_Click(object sender, EventArgs e)
        {
            try
            {
                var config = ConfigService.Instance.Config;
                
                // Save CaiFuTong templates
                config.CftSendText = txtCftSend.Text.Trim();
                config.CftText = txtCftText.Text.Trim();
                config.CftReplyKeywords = txtCftReply.Text.Trim();
                
                // Save ZhiFuBao templates
                config.ZfbSendText = txtZfbSend.Text.Trim();
                config.ZfbText = txtZfbText.Text.Trim();
                config.ZfbReplyKeywords = txtZfbReply.Text.Trim();
                
                // Save WeiXin templates
                config.WxSendText = txtWxSend.Text.Trim();
                config.WxText = txtWxText.Text.Trim();
                config.WxReplyKeywords = txtWxReply.Text.Trim();

                // Save custom keyword rules from the editable grid
                SaveKeywordRulesFromGrid();
                
                // Save config
                ConfigService.Instance.SaveConfig();
                
                MessageBox.Show("回复设置已保存！", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"保存失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Save keyword rules from the editable worksheet grid into config.
        /// </summary>
        private void SaveKeywordRulesFromGrid()
        {
            var config = ConfigService.Instance.Config;
            config.KeywordRules.Clear();

            foreach (DataGridViewRow row in dgvKeywords.Rows)
            {
                if (row.IsNewRow) continue;

                var keyword = row.Cells["colKeyword"].Value?.ToString()?.Trim() ?? "";
                var reply = row.Cells["colReply"].Value?.ToString()?.Trim() ?? "";

                // Skip empty worksheet rows
                if (string.IsNullOrWhiteSpace(keyword) && string.IsNullOrWhiteSpace(reply))
                    continue;

                // Keyword is required for a rule
                if (string.IsNullOrWhiteSpace(keyword))
                    continue;

                config.KeywordRules.Add(new KeywordReplyRule
                {
                    Keyword = keyword,
                    Reply = reply,
                    Enabled = true
                });
            }
        }
        
        // Note: Custom keyword rules are modified via "修改/添加" and "删除关键词".
        // The grid is read-only and used only for display/selection.
        
        /// <summary>
        /// Import QR code for CaiFuTong
        /// </summary>
        private void BtnCftQrCode_Click(object sender, EventArgs e)
        {
            ImportQrCode(txtCftText);
        }
        
        /// <summary>
        /// Import QR code for ZhiFuBao
        /// </summary>
        private void BtnZfbQrCode_Click(object sender, EventArgs e)
        {
            ImportQrCode(txtZfbText);
        }
        
        /// <summary>
        /// Import QR code for WeiXin
        /// </summary>
        private void BtnWxQrCode_Click(object sender, EventArgs e)
        {
            ImportQrCode(txtWxText);
        }
        
        /// <summary>
        /// Common QR code import function
        /// </summary>
        private void ImportQrCode(TextBox targetTextBox)
        {
            using (var dialog = new OpenFileDialog())
            {
                dialog.Filter = "图片文件|*.png;*.jpg;*.jpeg;*.bmp|所有文件|*.*";
                dialog.Title = "选择二维码图片";
                
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    targetTextBox.Text = dialog.FileName;
                    MessageBox.Show("二维码图片已导入！", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }
    }
}
