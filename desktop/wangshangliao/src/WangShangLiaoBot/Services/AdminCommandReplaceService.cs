using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace WangShangLiaoBot.Services
{
    /// <summary>
    /// 管理命令替换服务 - 管理关键词到命令的替换规则
    /// </summary>
    public sealed class AdminCommandReplaceService
    {
        private static AdminCommandReplaceService _instance;
        public static AdminCommandReplaceService Instance => 
            _instance ?? (_instance = new AdminCommandReplaceService());

        private readonly string _filePath;
        private Dictionary<string, string> _replacements;

        private AdminCommandReplaceService()
        {
            _filePath = Path.Combine(DataService.Instance.DatabaseDir, "admin-command-replace.txt");
            _replacements = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            Load();
        }

        /// <summary>获取所有替换规则</summary>
        public Dictionary<string, string> GetAll()
        {
            return new Dictionary<string, string>(_replacements);
        }

        /// <summary>添加或更新替换规则</summary>
        public void AddOrUpdate(string keyword, string replacement)
        {
            if (string.IsNullOrWhiteSpace(keyword)) return;
            _replacements[keyword] = replacement ?? "";
            Save();
        }

        /// <summary>删除替换规则</summary>
        public bool Remove(string keyword)
        {
            if (string.IsNullOrWhiteSpace(keyword)) return false;
            if (_replacements.Remove(keyword))
            {
                Save();
                return true;
            }
            return false;
        }

        /// <summary>清空所有规则</summary>
        public void Clear()
        {
            _replacements.Clear();
            Save();
        }

        /// <summary>应用替换规则到消息</summary>
        public string ApplyReplacement(string message)
        {
            if (string.IsNullOrEmpty(message)) return message;
            
            var result = message;
            foreach (var kv in _replacements)
            {
                if (!string.IsNullOrEmpty(kv.Key) && result.Contains(kv.Key))
                {
                    result = result.Replace(kv.Key, kv.Value);
                }
            }
            return result;
        }

        /// <summary>检查消息是否匹配任何替换关键词</summary>
        public bool HasMatch(string message)
        {
            if (string.IsNullOrEmpty(message)) return false;
            return _replacements.Keys.Any(k => 
                !string.IsNullOrEmpty(k) && message.Contains(k));
        }

        /// <summary>获取替换后的命令（精确匹配）</summary>
        public string GetReplacement(string keyword)
        {
            if (string.IsNullOrWhiteSpace(keyword)) return null;
            return _replacements.TryGetValue(keyword, out var r) ? r : null;
        }

        private void Load()
        {
            _replacements.Clear();
            if (!File.Exists(_filePath)) return;

            try
            {
                var lines = File.ReadAllLines(_filePath);
                foreach (var line in lines)
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    var idx = line.IndexOf('\t');
                    if (idx > 0)
                    {
                        var keyword = line.Substring(0, idx);
                        var replacement = idx < line.Length - 1 ? line.Substring(idx + 1) : "";
                        _replacements[keyword] = replacement;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Load admin-command-replace error: {ex.Message}");
            }
        }

        private void Save()
        {
            try
            {
                var dir = Path.GetDirectoryName(_filePath);
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

                var lines = _replacements.Select(kv => $"{kv.Key}\t{kv.Value}");
                File.WriteAllLines(_filePath, lines);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Save admin-command-replace error: {ex.Message}");
            }
        }
    }
}

