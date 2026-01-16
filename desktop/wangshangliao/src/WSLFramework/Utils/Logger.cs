using System;
using System.IO;
using System.Text;

namespace WSLFramework.Utils
{
    /// <summary>
    /// 日志工具类 - 增强版
    /// 支持异常堆栈记录、日志文件轮转
    /// </summary>
    public static class Logger
    {
        private static readonly object _lock = new object();
        private static string _logPath;
        private static string _errorLogPath;
        private static DateTime _currentLogDate;
        
        /// <summary>是否启用调试日志</summary>
        public static bool EnableDebug { get; set; } = false;
        
        /// <summary>最大日志文件大小(字节)</summary>
        public const long MAX_LOG_SIZE = 10 * 1024 * 1024; // 10MB
        
        public static event Action<string, LogLevel> OnLog;
        
        public enum LogLevel
        {
            Debug,
            Info,
            Warn,
            Error
        }
        
        static Logger()
        {
            InitializeLogPaths();
        }
        
        private static void InitializeLogPaths()
        {
            var logDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
            if (!Directory.Exists(logDir))
                Directory.CreateDirectory(logDir);
            
            _currentLogDate = DateTime.Now.Date;
            _logPath = Path.Combine(logDir, $"framework_{DateTime.Now:yyyyMMdd}.log");
            _errorLogPath = Path.Combine(logDir, $"error_{DateTime.Now:yyyyMMdd}.log");
        }
        
        public static void Debug(string message)
        {
            if (EnableDebug)
                Log(message, LogLevel.Debug);
        }
        
        public static void Info(string message) => Log(message, LogLevel.Info);
        public static void Warn(string message) => Log(message, LogLevel.Warn);
        public static void Warning(string message) => Log(message, LogLevel.Warn);
        public static void Error(string message) => Log(message, LogLevel.Error);
        
        /// <summary>
        /// 记录异常日志 (包含完整堆栈信息)
        /// </summary>
        /// <param name="context">上下文描述</param>
        /// <param name="ex">异常对象</param>
        public static void Error(string context, Exception ex)
        {
            if (ex == null)
            {
                Error(context);
                return;
            }
            
            var sb = new StringBuilder();
            sb.AppendLine($"{context}");
            sb.AppendLine($"  [异常类型] {ex.GetType().FullName}");
            sb.AppendLine($"  [异常消息] {ex.Message}");
            
            // 记录内部异常
            var innerEx = ex.InnerException;
            var depth = 1;
            while (innerEx != null && depth <= 5) // 最多5层
            {
                sb.AppendLine($"  [内部异常{depth}] {innerEx.GetType().Name}: {innerEx.Message}");
                innerEx = innerEx.InnerException;
                depth++;
            }
            
            // 记录堆栈
            if (!string.IsNullOrEmpty(ex.StackTrace))
            {
                sb.AppendLine($"  [堆栈跟踪]");
                foreach (var line in ex.StackTrace.Split('\n'))
                {
                    var trimmed = line.Trim();
                    if (!string.IsNullOrEmpty(trimmed))
                    {
                        sb.AppendLine($"    {trimmed}");
                    }
                }
            }
            
            var message = sb.ToString();
            Log(message, LogLevel.Error);
            
            // 同时写入错误日志文件
            WriteToErrorLog(message);
        }
        
        /// <summary>
        /// 记录警告日志 (带异常)
        /// </summary>
        public static void Warn(string context, Exception ex)
        {
            if (ex == null)
            {
                Warn(context);
                return;
            }
            
            var message = $"{context}: [{ex.GetType().Name}] {ex.Message}";
            Log(message, LogLevel.Warn);
        }
        
        public static void Log(string message, LogLevel level = LogLevel.Info)
        {
            // 检查日期变化，轮转日志文件
            if (DateTime.Now.Date != _currentLogDate)
            {
                lock (_lock)
                {
                    if (DateTime.Now.Date != _currentLogDate)
                    {
                        InitializeLogPaths();
                    }
                }
            }
            
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            var logMessage = $"[{timestamp}] [{level,-5}] {message}";
            
            lock (_lock)
            {
                try
                {
                    // 检查文件大小，必要时轮转
                    CheckAndRotateLog();
                    
                    File.AppendAllText(_logPath, logMessage + Environment.NewLine);
                }
                catch { }
            }
            
            OnLog?.Invoke(logMessage, level);
        }
        
        /// <summary>
        /// 写入错误专用日志
        /// </summary>
        private static void WriteToErrorLog(string message)
        {
            lock (_lock)
            {
                try
                {
                    var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                    var logMessage = $"[{timestamp}]\n{message}\n{"".PadLeft(80, '-')}\n";
                    File.AppendAllText(_errorLogPath, logMessage);
                }
                catch { }
            }
        }
        
        /// <summary>
        /// 检查并轮转日志文件
        /// </summary>
        private static void CheckAndRotateLog()
        {
            try
            {
                if (File.Exists(_logPath))
                {
                    var fileInfo = new FileInfo(_logPath);
                    if (fileInfo.Length > MAX_LOG_SIZE)
                    {
                        var dir = Path.GetDirectoryName(_logPath);
                        var name = Path.GetFileNameWithoutExtension(_logPath);
                        var ext = Path.GetExtension(_logPath);
                        var rotatedPath = Path.Combine(dir, $"{name}_{DateTime.Now:HHmmss}{ext}");
                        
                        File.Move(_logPath, rotatedPath);
                    }
                }
            }
            catch { }
        }
        
        /// <summary>
        /// 清理过期日志文件 (保留最近7天)
        /// </summary>
        public static void CleanupOldLogs(int keepDays = 7)
        {
            try
            {
                var logDir = Path.GetDirectoryName(_logPath);
                var cutoff = DateTime.Now.AddDays(-keepDays);
                
                foreach (var file in Directory.GetFiles(logDir, "*.log"))
                {
                    var fileInfo = new FileInfo(file);
                    if (fileInfo.CreationTime < cutoff)
                    {
                        File.Delete(file);
                        Info($"已清理过期日志: {Path.GetFileName(file)}");
                    }
                }
            }
            catch (Exception ex)
            {
                Warn($"清理日志失败: {ex.Message}");
            }
        }
    }
}
