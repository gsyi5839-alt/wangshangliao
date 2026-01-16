using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading;
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
        /// 主框架进程
        /// </summary>
        private static Process _frameworkProcess;
        
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
            
            // 程序退出时关闭主框架
            Application.ApplicationExit += (s, e) =>
            {
                StopFramework();
            };
            
            // 初始化日志
            Logger.Info("========== 旺商聊机器人启动 ==========");
            
            // 加载配置
            ConfigService.Instance.LoadConfig();
            
            // 检查是否跳过登录（开发模式 或 命令行参数 -skipLogin）
            bool skipLogin = false;
            
            // 检查命令行参数
            var args = Environment.GetCommandLineArgs();
            foreach (var arg in args)
            {
                if (arg.ToLower() == "-skiplogin" || arg.ToLower() == "--skiplogin" || arg.ToLower() == "-dev")
                {
                    skipLogin = true;
                    Logger.Info("检测到跳过登录参数，直接启动主界面");
                    break;
                }
            }
            
            // 检查配置文件是否设置了跳过登录
            if (!skipLogin && ConfigService.Instance.Config != null && ConfigService.Instance.Config.SkipLogin)
            {
                skipLogin = true;
                Logger.Info("配置文件设置跳过登录，直接启动主界面");
            }
            
            if (skipLogin)
            {
                // 跳过登录，启动主框架后显示主窗口
                StartFramework();
                Application.Run(new MainForm());
            }
            else
            {
                // 显示登录窗口
                using (var loginForm = new LoginForm())
                {
                    if (loginForm.ShowDialog() == DialogResult.OK)
                    {
                        // 登录成功，先启动主框架，再显示主窗口
                        Logger.Info("登录成功，正在启动主框架...");
                        StartFramework();
                        
                        // 等待主框架启动
                        Thread.Sleep(1000);
                        
                        // 显示主窗口
                        Application.Run(new MainForm());
                    }
                }
            }
            
            Logger.Info("========== 旺商聊机器人退出 ==========");
        }
        
        /// <summary>
        /// 启动主框架 (旺商聊框架)
        /// </summary>
        private static void StartFramework()
        {
            try
            {
                // 检查主框架是否已在运行
                var existing = Process.GetProcessesByName("旺商聊框架");
                if (existing.Length > 0)
                {
                    Logger.Info("主框架已在运行中");
                    return;
                }
                
                // 查找主框架路径
                var baseDir = AppDomain.CurrentDomain.BaseDirectory;
                var frameworkPaths = new[]
                {
                    Path.Combine(baseDir, "旺商聊框架.exe"),
                    Path.Combine(baseDir, "..", "WSLFramework", "bin", "Debug", "旺商聊框架.exe"),
                    Path.Combine(baseDir, "..", "..", "..", "WSLFramework", "bin", "Debug", "旺商聊框架.exe"),
                };
                
                string frameworkPath = null;
                foreach (var path in frameworkPaths)
                {
                    var fullPath = Path.GetFullPath(path);
                    if (File.Exists(fullPath))
                    {
                        frameworkPath = fullPath;
                        break;
                    }
                }
                
                if (string.IsNullOrEmpty(frameworkPath))
                {
                    Logger.Error("未找到主框架 (旺商聊框架.exe)");
                    return;
                }
                
                Logger.Info($"启动主框架: {frameworkPath}");
                
                _frameworkProcess = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = frameworkPath,
                        WorkingDirectory = Path.GetDirectoryName(frameworkPath),
                        UseShellExecute = true
                    }
                };
                
                _frameworkProcess.Start();
                Logger.Info($"✓ 主框架已启动 (PID: {_frameworkProcess.Id})");
            }
            catch (Exception ex)
            {
                Logger.Error($"启动主框架失败: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 停止主框架
        /// </summary>
        private static void StopFramework()
        {
            try
            {
                if (_frameworkProcess != null && !_frameworkProcess.HasExited)
                {
                    Logger.Info("正在关闭主框架...");
                    _frameworkProcess.CloseMainWindow();
                    
                    // 等待3秒
                    if (!_frameworkProcess.WaitForExit(3000))
                    {
                        _frameworkProcess.Kill();
                    }
                    
                    Logger.Info("✓ 主框架已关闭");
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"关闭主框架失败: {ex.Message}");
            }
        }
    }
}

