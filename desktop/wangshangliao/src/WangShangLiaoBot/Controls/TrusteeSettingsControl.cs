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
    /// 托管设置控件 - 管理玩家自动托管下注
    /// </summary>
    public sealed class TrusteeSettingsControl : UserControl
    {
        private GroupBox grpTrustee;
        
        // Top: Enable switch
        private CheckBox chkEnable;
        
        // Input row
        private Label lblWangWangId;
        private TextBox txtWangWangId;
        private Label lblContent;
        private TextBox txtContent;
        private Button btnAddOrUpdate;
        private Button btnDelete;
        
        // Action buttons
        private Button btnRefresh;
        private Button btnClearAll;
        private Button btnExport;
        
        // Data grid
        private DataGridView dgvTrustee;
        
        // Description labels
        private Label lblDesc1;
        private Label lblDesc2;
        private Label lblDesc3;

        public TrusteeSettingsControl()
        {
            BackColor = SystemColors.Control;
            Font = new Font("Microsoft YaHei UI", 9F);
            MinimumSize = new Size(500, 400);
            Size = new Size(600, 450);
            AutoScroll = true;
            Dock = DockStyle.Fill;
            
            InitializeUI();
            LoadData();
            
            // Subscribe to list change events
            TrusteeService.Instance.OnListChanged += RefreshList;
        }

        private void InitializeUI()
        {
            SuspendLayout();

            // ================= GroupBox: 托管设置 =================
            grpTrustee = new GroupBox
            {
                Text = "托管设置",
                Location = new Point(10, 10),
                Size = new Size(570, 420),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom
            };
            Controls.Add(grpTrustee);

            // Row 1: Enable switch
            chkEnable = new CheckBox
            {
                Text = "玩家托管开关",
                Location = new Point(15, 25),
                Size = new Size(150, 20)
            };
            chkEnable.CheckedChanged += ChkEnable_CheckedChanged;
            grpTrustee.Controls.Add(chkEnable);

            // Row 2: Input fields
            lblWangWangId = new Label
            {
                Text = "旺旺号",
                Location = new Point(15, 55),
                AutoSize = true
            };
            grpTrustee.Controls.Add(lblWangWangId);

            txtWangWangId = new TextBox
            {
                Location = new Point(60, 52),
                Size = new Size(100, 23)
            };
            grpTrustee.Controls.Add(txtWangWangId);

            lblContent = new Label
            {
                Text = "内容",
                Location = new Point(170, 55),
                AutoSize = true
            };
            grpTrustee.Controls.Add(lblContent);

            txtContent = new TextBox
            {
                Location = new Point(205, 52),
                Size = new Size(150, 23)
            };
            grpTrustee.Controls.Add(txtContent);

            btnAddOrUpdate = new Button
            {
                Text = "修改/添加",
                Location = new Point(365, 51),
                Size = new Size(80, 25)
            };
            btnAddOrUpdate.Click += BtnAddOrUpdate_Click;
            grpTrustee.Controls.Add(btnAddOrUpdate);

            btnDelete = new Button
            {
                Text = "删除托管",
                Location = new Point(450, 51),
                Size = new Size(80, 25)
            };
            btnDelete.Click += BtnDelete_Click;
            grpTrustee.Controls.Add(btnDelete);

            // Row 3: Action buttons
            btnRefresh = new Button
            {
                Text = "刷新列表",
                Location = new Point(15, 85),
                Size = new Size(80, 25)
            };
            btnRefresh.Click += BtnRefresh_Click;
            grpTrustee.Controls.Add(btnRefresh);

            btnClearAll = new Button
            {
                Text = "全部删除",
                Location = new Point(100, 85),
                Size = new Size(80, 25)
            };
            btnClearAll.Click += BtnClearAll_Click;
            grpTrustee.Controls.Add(btnClearAll);

            btnExport = new Button
            {
                Text = "导出聊天格式",
                Location = new Point(185, 85),
                Size = new Size(100, 25)
            };
            btnExport.Click += BtnExport_Click;
            grpTrustee.Controls.Add(btnExport);

            // DataGridView
            dgvTrustee = new DataGridView
            {
                Location = new Point(15, 118),
                Size = new Size(540, 180),
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
                GridColor = Color.LightGray,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom
            };
            dgvTrustee.Columns.Add("Index", "序号");
            dgvTrustee.Columns.Add("WangWangId", "旺旺号");
            dgvTrustee.Columns.Add("NickName", "姓名");
            dgvTrustee.Columns.Add("Content", "托管内容");
            dgvTrustee.Columns["Index"].Width = 50;
            dgvTrustee.Columns["WangWangId"].Width = 100;
            dgvTrustee.Columns["NickName"].Width = 100;
            dgvTrustee.Columns["Content"].Width = 250;
            dgvTrustee.SelectionChanged += DgvTrustee_SelectionChanged;
            grpTrustee.Controls.Add(dgvTrustee);

            // Description labels
            lblDesc1 = new Label
            {
                Text = "说明：玩家发送如\"Ja100托管\"进行托管，发送\"取消托管\"进行取消",
                Location = new Point(15, 305),
                AutoSize = true,
                ForeColor = Color.Red
            };
            grpTrustee.Controls.Add(lblDesc1);

            lblDesc2 = new Label
            {
                Text = "下局开奖自动生效，如果托管失败，则自动取消托管（超额、余额不足等）",
                Location = new Point(15, 325),
                AutoSize = true,
                ForeColor = Color.Red
            };
            grpTrustee.Controls.Add(lblDesc2);

            lblDesc3 = new Label
            {
                Text = "如果出现需要重新开奖，则 导出聊天格式，自动复制到剪切板\n粘贴到号入下注即可",
                Location = new Point(15, 350),
                Size = new Size(500, 40),
                ForeColor = Color.Red
            };
            grpTrustee.Controls.Add(lblDesc3);

            ResumeLayout(false);
        }

        private void LoadData()
        {
            chkEnable.Checked = TrusteeService.Instance.IsEnabled;
            RefreshList();
        }

        private void RefreshList()
        {
            if (InvokeRequired)
            {
                Invoke(new Action(RefreshList));
                return;
            }

            dgvTrustee.Rows.Clear();
            var items = TrusteeService.Instance.GetAll();
            for (int i = 0; i < items.Count; i++)
            {
                var item = items[i];
                item.Index = i + 1;
                dgvTrustee.Rows.Add(item.Index, item.WangWangId, item.NickName, item.Content);
            }
        }

        private void ChkEnable_CheckedChanged(object sender, EventArgs e)
        {
            TrusteeService.Instance.IsEnabled = chkEnable.Checked;
        }

        private void BtnAddOrUpdate_Click(object sender, EventArgs e)
        {
            var wangWangId = txtWangWangId.Text.Trim();
            var content = txtContent.Text.Trim();

            if (string.IsNullOrEmpty(wangWangId))
            {
                MessageBox.Show("请输入旺旺号", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtWangWangId.Focus();
                return;
            }

            if (string.IsNullOrEmpty(content))
            {
                MessageBox.Show("请输入托管内容", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtContent.Focus();
                return;
            }

            TrusteeService.Instance.AddOrUpdate(wangWangId, content);
            txtWangWangId.Clear();
            txtContent.Clear();
            MessageBox.Show("添加/修改成功", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void BtnDelete_Click(object sender, EventArgs e)
        {
            var wangWangId = txtWangWangId.Text.Trim();
            
            // If input is empty, try to get from selected row
            if (string.IsNullOrEmpty(wangWangId) && dgvTrustee.SelectedRows.Count > 0)
            {
                wangWangId = dgvTrustee.SelectedRows[0].Cells["WangWangId"].Value?.ToString() ?? "";
            }

            if (string.IsNullOrEmpty(wangWangId))
            {
                MessageBox.Show("请先选择要删除的记录或输入旺旺号", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var result = MessageBox.Show(
                $"确定要删除 {wangWangId} 的托管记录吗？",
                "确认删除",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result != DialogResult.Yes) return;

            if (TrusteeService.Instance.Remove(wangWangId))
            {
                txtWangWangId.Clear();
                txtContent.Clear();
                MessageBox.Show("删除成功", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show("未找到该旺旺号的托管记录", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void BtnRefresh_Click(object sender, EventArgs e)
        {
            RefreshList();
        }

        private void BtnClearAll_Click(object sender, EventArgs e)
        {
            if (dgvTrustee.Rows.Count == 0)
            {
                MessageBox.Show("列表已为空", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var result = MessageBox.Show(
                "确定要删除所有托管记录吗？\n此操作不可撤销！",
                "确认删除",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (result == DialogResult.Yes)
            {
                TrusteeService.Instance.ClearAll();
                MessageBox.Show("已清空所有托管记录", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void BtnExport_Click(object sender, EventArgs e)
        {
            var chatFormat = TrusteeService.Instance.ExportChatFormat();
            if (string.IsNullOrWhiteSpace(chatFormat))
            {
                MessageBox.Show("没有托管记录可导出", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                Clipboard.SetText(chatFormat);
                MessageBox.Show(
                    "已复制到剪切板！\n可直接粘贴到聊天窗口进行下注。",
                    "导出成功",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"复制失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void DgvTrustee_SelectionChanged(object sender, EventArgs e)
        {
            if (dgvTrustee.SelectedRows.Count > 0)
            {
                var row = dgvTrustee.SelectedRows[0];
                txtWangWangId.Text = row.Cells["WangWangId"].Value?.ToString() ?? "";
                txtContent.Text = row.Cells["Content"].Value?.ToString() ?? "";
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                TrusteeService.Instance.OnListChanged -= RefreshList;
            }
            base.Dispose(disposing);
        }
    }
}

