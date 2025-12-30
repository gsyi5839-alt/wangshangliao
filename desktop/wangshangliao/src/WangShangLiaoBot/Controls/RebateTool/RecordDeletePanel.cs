using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using WangShangLiaoBot.Services;

namespace WangShangLiaoBot.Controls.RebateTool
{
    /// <summary>
    /// 记录删除面板 - Record Delete Panel
    /// </summary>
    public sealed class RecordDeletePanel : UserControl
    {
        private readonly Color BorderColor = Color.FromArgb(180, 180, 180);

        // Controls
        private Label lblTitle;
        private TextBox txtWangwangIds;
        private Button btnDelete;

        public RecordDeletePanel()
        {
            SuspendLayout();
            BackColor = Color.White;
            Dock = DockStyle.Fill;
            Font = new Font("Microsoft YaHei UI", 9F);

            CreateControls();

            ResumeLayout(false);
        }

        private void CreateControls()
        {
            // Title label
            lblTitle = new Label
            {
                Text = "一行一个旺旺号",
                Location = new Point(10, 10),
                AutoSize = true
            };
            Controls.Add(lblTitle);

            // Multiline TextBox for Wangwang IDs
            txtWangwangIds = new TextBox
            {
                Location = new Point(10, 35),
                Size = new Size(200, 300),
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                BorderStyle = BorderStyle.FixedSingle,
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left
            };
            Controls.Add(txtWangwangIds);

            // Delete button
            btnDelete = new Button
            {
                Text = "删除数据",
                Location = new Point(230, 35),
                Size = new Size(80, 28),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.White
            };
            btnDelete.FlatAppearance.BorderColor = BorderColor;
            btnDelete.Click += BtnDelete_Click;
            Controls.Add(btnDelete);

            // Handle resize
            Resize += (s, e) =>
            {
                txtWangwangIds.Height = Math.Max(100, ClientSize.Height - 50);
            };
        }

        private void BtnDelete_Click(object sender, EventArgs e)
        {
            var text = txtWangwangIds.Text.Trim();
            if (string.IsNullOrEmpty(text))
            {
                MessageBox.Show("请输入要删除的旺旺号", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var ids = text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                          .Select(x => x.Trim())
                          .Where(x => !string.IsNullOrEmpty(x))
                          .ToList();

            if (ids.Count == 0)
            {
                MessageBox.Show("请输入要删除的旺旺号", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var result = MessageBox.Show(
                $"确定要删除以下 {ids.Count} 个旺旺号的相关记录吗？\r\n\r\n此操作不可恢复！",
                "确认删除",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (result != DialogResult.Yes) return;

            try
            {
                int deletedCount = 0;
                var ds = DataService.Instance;
                var dbDir = ds.DatabaseDir;

                // Delete from various .db files
                string[] dbFiles = {
                    "上下分.db",
                    "艾特分.db",
                    "每期盈利.db",
                    "庄家盈利.db",
                    "邀请记录.db"
                };

                foreach (var dbFile in dbFiles)
                {
                    var dbPath = Path.Combine(dbDir, dbFile);
                    if (File.Exists(dbPath))
                    {
                        var lines = File.ReadAllLines(dbPath, Encoding.UTF8);
                        var newLines = lines.Where(line =>
                        {
                            if (string.IsNullOrWhiteSpace(line)) return true;
                            var parts = line.Split('|');
                            if (parts.Length > 1)
                            {
                                // Check if any ID matches
                                return !ids.Any(id => parts.Any(p => p.Contains(id)));
                            }
                            return true;
                        }).ToList();

                        if (newLines.Count < lines.Length)
                        {
                            deletedCount += (lines.Length - newLines.Count);
                            File.WriteAllLines(dbPath, newLines, Encoding.UTF8);
                        }
                    }
                }

                // Delete from Bets folder
                var betsDir = Path.Combine(dbDir, "Bets");
                if (Directory.Exists(betsDir))
                {
                    foreach (var dateDir in Directory.GetDirectories(betsDir))
                    {
                        foreach (var id in ids)
                        {
                            var playerFile = Path.Combine(dateDir, $"{id}.txt");
                            if (File.Exists(playerFile))
                            {
                                File.Delete(playerFile);
                                deletedCount++;
                            }
                        }
                    }
                }

                // Delete from settings (Daily entries)
                var settingsPath = Path.Combine(ds.DatabaseDir, "..", "设置.ini");
                if (File.Exists(settingsPath))
                {
                    var lines = File.ReadAllLines(settingsPath, Encoding.UTF8);
                    var newLines = lines.Where(line =>
                    {
                        if (string.IsNullOrWhiteSpace(line)) return true;
                        return !ids.Any(id => line.Contains($":{id}:"));
                    }).ToList();

                    if (newLines.Count < lines.Length)
                    {
                        deletedCount += (lines.Length - newLines.Count);
                        File.WriteAllLines(settingsPath, newLines, Encoding.UTF8);
                    }
                }

                MessageBox.Show($"删除完成！\r\n共删除 {deletedCount} 条相关记录", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                txtWangwangIds.Clear();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"删除失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public void RefreshData(DateTime startTime, DateTime endTime)
        {
            // No data to refresh
        }
    }
}

