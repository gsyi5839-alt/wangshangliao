using System;
using System.IO;
using System.Threading;
using System.Windows.Forms;

namespace WSLFramework
{
    static class Program
    {
        private static Mutex _mutex;
        
        [STAThread]
        static void Main()
        {
            // 确保单实例运行
            bool createdNew;
            _mutex = new Mutex(true, "WSLFramework_SingleInstance", out createdNew);
            
            if (!createdNew)
            {
                MessageBox.Show("旺商聊框架已在运行中！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            
            // 全局异常处理
            Application.ThreadException += (s, e) =>
            {
                Utils.Logger.Error($"未处理的线程异常: {e.Exception.Message}");
                MessageBox.Show($"发生错误: {e.Exception.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            };
            
            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            {
                var ex = e.ExceptionObject as Exception;
                Utils.Logger.Error($"未处理的域异常: {ex?.Message}");
            };
            
            // 初始化生产目录 (ZCG兼容)
            InitializeProductionDirectory();
            
            Utils.Logger.Info("========== 旺商聊框架启动 ==========");
            
            Application.Run(new MainForm());
            
            Utils.Logger.Info("========== 旺商聊框架退出 ==========");
        }
        
        /// <summary>
        /// 初始化生产目录结构和配置文件 (ZCG兼容)
        /// </summary>
        private static void InitializeProductionDirectory()
        {
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            
            try
            {
                // 创建目录结构
                var dirs = new[] { "plugin", "YX_Clinent", "zcg", "zcg服务端收发日志", "zcg收发日志", "旺旺号资料", "logs" };
                foreach (var dir in dirs)
                {
                    var path = Path.Combine(baseDir, dir);
                    if (!Directory.Exists(path)) Directory.CreateDirectory(path);
                }
                
                // 创建配置文件: run.cmd
                var runCmdPath = Path.Combine(baseDir, "run.cmd");
                if (!File.Exists(runCmdPath))
                {
                    File.WriteAllText(runCmdPath, @"@echo off
chcp 65001 >nul
title 旺商聊框架
echo ========================================
echo   旺商聊框架 启动中...
echo ========================================
start """" ""旺商聊框架.exe""
");
                }
                
                // 创建配置文件: config.ini
                var configPath = Path.Combine(baseDir, "config.ini");
                if (!File.Exists(configPath))
                {
                    File.WriteAllText(configPath, @"[程序配置]
程序名=旺商聊框架
非当天缓存清除=真

[nim]
版本=1

[环境]
线上环境=真
");
                }
                
                // 创建配置文件: Plugin.ini
                var pluginPath = Path.Combine(baseDir, "Plugin.ini");
                if (!File.Exists(pluginPath))
                {
                    File.WriteAllText(pluginPath, "[Plugin]\n");
                }
                
                // 创建配置文件: zcg端口.ini
                var portPath = Path.Combine(baseDir, "zcg端口.ini");
                if (!File.Exists(portPath))
                {
                    File.WriteAllText(portPath, "[端口]\n端口=14745\n");
                }
                
                // 创建zcg目录下的配置文件
                var zcgDir = Path.Combine(baseDir, "zcg");
                var loginConfigPath = Path.Combine(zcgDir, "登录配置.ini");
                if (!File.Exists(loginConfigPath))
                {
                    File.WriteAllText(loginConfigPath, "[程序配置]\n版本=1\n");
                }
                
                // 创建空数据库文件
                var dbFiles = new[] { "攻击.db", "上下分.db", "设置.db", "玩家姓名.db", "邀请记录.db", "账单.db" };
                foreach (var db in dbFiles)
                {
                    var dbPath = Path.Combine(zcgDir, db);
                    if (!File.Exists(dbPath)) File.Create(dbPath).Close();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"初始化生产目录失败: {ex.Message}");
            }
        }
    }
}
