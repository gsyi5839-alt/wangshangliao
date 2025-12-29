using System;
using System.IO;
using System.Text;

namespace WangShangLiaoBot.Services
{
    /// <summary>
    /// 日志工具类
    /// </summary>
    public static class Logger
    {
        private static readonly object _lock = new object();
        private static string _logPath;
        
        /// <summary>日志文件路径</summary>
        public static string LogPath
        {
            get
            {
                if (string.IsNullOrEmpty(_logPath))
                {
                    var dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
                    if (!Directory.Exists(dir))
                        Directory.CreateDirectory(dir);
                    _logPath = Path.Combine(dir, $"{DateTime.Now:yyyy-MM-dd}.log");
                }
                return _logPath;
            }
        }
        
        /// <summary>日志变更事件</summary>
        public static event Action<string, LogLevel> OnLog;
        
        /// <summary>
        /// 记录信息日志
        /// </summary>
        public static void Info(string message)
        {
            Log(message, LogLevel.Info);
        }
        
        /// <summary>
        /// 记录警告日志
        /// </summary>
        public static void Warn(string message)
        {
            Log(message, LogLevel.Warn);
        }
        
        /// <summary>
        /// 记录错误日志
        /// </summary>
        public static void Error(string message)
        {
            Log(message, LogLevel.Error);
        }
        
        /// <summary>
        /// 记录调试日志
        /// </summary>
        public static void Debug(string message)
        {
            #if DEBUG
            Log(message, LogLevel.Debug);
            #endif
        }
        
        /// <summary>
        /// 记录日志
        /// </summary>
        private static void Log(string message, LogLevel level)
        {
            var logLine = $"[{DateTime.Now:HH:mm:ss}] [{level}] {message}";
            
            // 写入文件
            lock (_lock)
            {
                try
                {
                    File.AppendAllText(LogPath, logLine + Environment.NewLine, Encoding.UTF8);
                }
                catch { /* 忽略文件写入错误 */ }
            }
            
            // 触发事件
            OnLog?.Invoke(logLine, level);
            
            // 输出到控制台
            Console.WriteLine(logLine);
        }
    }
    
    /// <summary>
    /// 日志级别
    /// </summary>
    public enum LogLevel
    {
        Debug,
        Info,
        Warn,
        Error
    }
}

