using System;
using System.Net;
using System.Windows.Forms;
using WangShangLiaoBot.Forms;
using WangShangLiaoBot.Services;

namespace WangShangLiaoBot
{
    /// <summary>
    /// 旺商聊自动化机器人 - 程序入口
    /// </summary>
    static class Program
    {
        /// <summary>
        /// 应用程序的主入口点
        /// </summary>
        [STAThread]
        static void Main()
        {
            // Ensure TLS1.2 is enabled for HTTPS API calls on older Windows environments.
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;

            // 启用视觉样式
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            
            // 设置未处理异常处理
            Application.ThreadException += (s, e) =>
            {
                Logger.Error($"未处理的线程异常: {e.Exception.Message}");
                MessageBox.Show($"发生错误: {e.Exception.Message}", "错误", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            };
            
            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            {
                var ex = e.ExceptionObject as Exception;
                Logger.Error($"未处理的域异常: {ex?.Message}");
            };
            
            // 初始化日志
            Logger.Info("========== 旺商聊机器人启动 ==========");
            
            // 加载配置
            ConfigService.Instance.LoadConfig();
            
            // 显示登录窗口
            using (var loginForm = new LoginForm())
            {
                if (loginForm.ShowDialog() == DialogResult.OK)
                {
                    // 登录成功，显示主窗口
                    Application.Run(new MainForm());
                }
            }
            
            Logger.Info("========== 旺商聊机器人退出 ==========");
        }
    }
}

