using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Timers;
using WangShangLiaoBot.Models;
using WangShangLiaoBot.Services;
using WangShangLiaoBot.Services.HPSocket;
using WangShangLiaoBot.Forms.Settings;
using WangShangLiaoBot.Controls;
using WangShangLiaoBot.Controls.BetProcess;
using WangShangLiaoBot.Controls.Odds;

namespace WangShangLiaoBot.Forms
{
    public partial class MainForm : Form
    {
        private void RebateTool_OnOperationLogRequested(object sender, EventArgs e)
        {
            try
            {
                var logDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "运行日志");
                Directory.CreateDirectory(logDir);
                var file = Path.Combine(logDir, $"{DateTime.Now:yyyy-MM-dd}.log");
                if (!File.Exists(file)) File.WriteAllText(file, "", Encoding.UTF8);

                Process.Start(new ProcessStartInfo
                {
                    FileName = "notepad.exe",
                    Arguments = $"\"{file}\"",
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"打开操作记录失败: {ex.Message}", "回水工具", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void RebateTool_OnClearDataRequested(object sender, EventArgs e)
        {
            try
            {
                var confirm = MessageBox.Show(
                    "将执行以下清理：\n\n" +
                    "1) 删除 Data\\数据库\\Bets 下所有下注/结算文件\n" +
                    "2) 清空 Data\\设置.ini 中 Daily:* 统计缓存（不影响玩家资料/其他设置）\n\n" +
                    "确定继续吗？",
                    "确认清除回水工具数据",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);

                if (confirm != DialogResult.Yes) return;

                // 1) delete Bets folder
                var betsDir = Path.Combine(DataService.Instance.DatabaseDir, "Bets");
                if (Directory.Exists(betsDir))
                    Directory.Delete(betsDir, recursive: true);

                // 2) remove Daily:* from settings ini
                var settingsIni = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "设置.ini");
                if (File.Exists(settingsIni))
                {
                    var lines = File.ReadAllLines(settingsIni, Encoding.UTF8);
                    var kept = lines.Where(l => !l.StartsWith("Daily:", StringComparison.OrdinalIgnoreCase)).ToArray();
                    File.WriteAllLines(settingsIni, kept, Encoding.UTF8);
                }

                MessageBox.Show("清除完成。", "回水工具", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"清除失败: {ex.Message}", "回水工具", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

    }
}
