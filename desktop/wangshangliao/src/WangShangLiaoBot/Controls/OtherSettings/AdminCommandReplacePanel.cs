using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using WangShangLiaoBot.Services;

namespace WangShangLiaoBot.Controls.OtherSettings
{
    /// <summary>
    /// 管理命令替换 - 设置面板
    /// </summary>
    public sealed class AdminCommandReplacePanel : UserControl
    {
        private GroupBox grpMain;
        private DataGridView dgvReplacements;
        private Label lblKeyword;
        private TextBox txtKeyword;
        private Label lblReplacement;
        private TextBox txtReplacement;
        private Button btnAddUpdate;
        private Button btnDelete;

        private BindingList<ReplacementItem> _items;

        public AdminCommandReplacePanel()
        {
            BackColor = SystemColors.Control;
            Font = new Font("Microsoft YaHei UI", 9F);
            Dock = DockStyle.Fill;
            AutoScroll = true;

            InitializeUI();
            LoadData();
        }

        private void InitializeUI()
        {
            SuspendLayout();

            // Main GroupBox
            grpMain = new GroupBox
            {
                Text = "管理员命令关键词替换",
                Location = new Point(10, 10),
                Size = new Size(560, 320)
            };
            Controls.Add(grpMain);

            // DataGridView for replacements
            dgvReplacements = new DataGridView
            {
                Location = new Point(15, 25),
                Size = new Size(530, 140),
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                RowHeadersVisible = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = SystemColors.Window,
                BorderStyle = BorderStyle.FixedSingle,
                CellBorderStyle = DataGridViewCellBorderStyle.Single,
                GridColor = Color.LightGray
            };
            dgvReplacements.SelectionChanged += DgvReplacements_SelectionChanged;

            // Add columns
            dgvReplacements.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Keyword",
                HeaderText = "关键词",
                DataPropertyName = "Keyword",
                FillWeight = 50
            });
            dgvReplacements.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Replacement",
                HeaderText = "替换为",
                DataPropertyName = "Replacement",
                FillWeight = 50
            });

            grpMain.Controls.Add(dgvReplacements);

            int y = 180;

            // Keyword input
            lblKeyword = new Label
            {
                Text = "关键词",
                Location = new Point(15, y + 5),
                AutoSize = true
            };
            grpMain.Controls.Add(lblKeyword);

            txtKeyword = new TextBox
            {
                Location = new Point(65, y),
                Size = new Size(350, 25),
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                Height = 45
            };
            grpMain.Controls.Add(txtKeyword);
            y += 55;

            // Replacement input
            lblReplacement = new Label
            {
                Text = "替换为",
                Location = new Point(15, y + 5),
                AutoSize = true
            };
            grpMain.Controls.Add(lblReplacement);

            txtReplacement = new TextBox
            {
                Location = new Point(65, y),
                Size = new Size(350, 23)
            };
            grpMain.Controls.Add(txtReplacement);
            y += 40;

            // Buttons
            btnAddUpdate = new Button
            {
                Text = "修改/添加",
                Location = new Point(100, y),
                Size = new Size(85, 28)
            };
            btnAddUpdate.Click += BtnAddUpdate_Click;
            grpMain.Controls.Add(btnAddUpdate);

            btnDelete = new Button
            {
                Text = "删除关键词",
                Location = new Point(200, y),
                Size = new Size(85, 28)
            };
            btnDelete.Click += BtnDelete_Click;
            grpMain.Controls.Add(btnDelete);

            ResumeLayout(false);
        }

        private void LoadData()
        {
            _items = new BindingList<ReplacementItem>();
            
            var data = AdminCommandReplaceService.Instance.GetAll();
            foreach (var kv in data)
            {
                _items.Add(new ReplacementItem { Keyword = kv.Key, Replacement = kv.Value });
            }

            dgvReplacements.DataSource = _items;
        }

        private void DgvReplacements_SelectionChanged(object sender, EventArgs e)
        {
            if (dgvReplacements.SelectedRows.Count > 0)
            {
                var row = dgvReplacements.SelectedRows[0];
                txtKeyword.Text = row.Cells["Keyword"].Value?.ToString() ?? "";
                txtReplacement.Text = row.Cells["Replacement"].Value?.ToString() ?? "";
            }
        }

        private void BtnAddUpdate_Click(object sender, EventArgs e)
        {
            var keyword = txtKeyword.Text.Trim();
            var replacement = txtReplacement.Text;

            if (string.IsNullOrEmpty(keyword))
            {
                MessageBox.Show("请输入关键词", "提示", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            AdminCommandReplaceService.Instance.AddOrUpdate(keyword, replacement);
            
            // Update UI
            var existing = FindItem(keyword);
            if (existing != null)
            {
                existing.Replacement = replacement;
            }
            else
            {
                _items.Add(new ReplacementItem { Keyword = keyword, Replacement = replacement });
            }

            dgvReplacements.Refresh();
            MessageBox.Show("保存成功", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void BtnDelete_Click(object sender, EventArgs e)
        {
            var keyword = txtKeyword.Text.Trim();
            
            if (string.IsNullOrEmpty(keyword))
            {
                // Try to get from selected row
                if (dgvReplacements.SelectedRows.Count > 0)
                {
                    keyword = dgvReplacements.SelectedRows[0].Cells["Keyword"].Value?.ToString();
                }
            }

            if (string.IsNullOrEmpty(keyword))
            {
                MessageBox.Show("请选择要删除的关键词", "提示", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var result = MessageBox.Show($"确定要删除关键词 \"{keyword}\" 吗？", "确认删除",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                if (AdminCommandReplaceService.Instance.Remove(keyword))
                {
                    var item = FindItem(keyword);
                    if (item != null)
                    {
                        _items.Remove(item);
                    }
                    txtKeyword.Clear();
                    txtReplacement.Clear();
                    MessageBox.Show("删除成功", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }

        private ReplacementItem FindItem(string keyword)
        {
            foreach (var item in _items)
            {
                if (string.Equals(item.Keyword, keyword, StringComparison.OrdinalIgnoreCase))
                    return item;
            }
            return null;
        }

        /// <summary>Inner class for data binding</summary>
        private class ReplacementItem
        {
            public string Keyword { get; set; }
            public string Replacement { get; set; }
        }
    }
}
