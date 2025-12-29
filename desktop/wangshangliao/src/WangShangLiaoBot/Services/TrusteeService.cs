using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using WangShangLiaoBot.Models;

namespace WangShangLiaoBot.Services
{
    /// <summary>
    /// 托管服务 - 管理玩家托管下注
    /// </summary>
    public sealed class TrusteeService
    {
        private static TrusteeService _instance;
        public static TrusteeService Instance => _instance ?? (_instance = new TrusteeService());

        private readonly List<TrusteeItem> _items = new List<TrusteeItem>();
        private readonly string _dataFile;
        private readonly object _lock = new object();

        /// <summary>托管开关</summary>
        public bool IsEnabled
        {
            get => GetBool("Trustee:Enabled", false);
            set => SetBool("Trustee:Enabled", value);
        }

        /// <summary>托管列表变化事件</summary>
        public event Action OnListChanged;

        private TrusteeService()
        {
            var dataDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "数据库");
            if (!Directory.Exists(dataDir)) Directory.CreateDirectory(dataDir);
            _dataFile = Path.Combine(dataDir, "trustee.txt");
            Load();
        }

        /// <summary>
        /// 获取所有托管项
        /// </summary>
        public List<TrusteeItem> GetAll()
        {
            lock (_lock)
            {
                return _items.Where(x => x.IsActive).ToList();
            }
        }

        /// <summary>
        /// 添加或更新托管
        /// </summary>
        public bool AddOrUpdate(string wangWangId, string content, string nickName = null)
        {
            if (string.IsNullOrWhiteSpace(wangWangId) || string.IsNullOrWhiteSpace(content))
                return false;

            lock (_lock)
            {
                var existing = _items.FirstOrDefault(x => 
                    x.WangWangId.Equals(wangWangId, StringComparison.OrdinalIgnoreCase));
                
                if (existing != null)
                {
                    existing.Content = content;
                    existing.NickName = nickName ?? existing.NickName;
                    existing.IsActive = true;
                }
                else
                {
                    _items.Add(new TrusteeItem
                    {
                        WangWangId = wangWangId,
                        NickName = nickName ?? wangWangId,
                        Content = content,
                        IsActive = true
                    });
                }
                Save();
                OnListChanged?.Invoke();
                return true;
            }
        }

        /// <summary>
        /// 删除托管
        /// </summary>
        public bool Remove(string wangWangId)
        {
            if (string.IsNullOrWhiteSpace(wangWangId)) return false;

            lock (_lock)
            {
                var item = _items.FirstOrDefault(x => 
                    x.WangWangId.Equals(wangWangId, StringComparison.OrdinalIgnoreCase));
                if (item != null)
                {
                    item.IsActive = false;
                    Save();
                    OnListChanged?.Invoke();
                    return true;
                }
                return false;
            }
        }

        /// <summary>
        /// 清空所有托管
        /// </summary>
        public void ClearAll()
        {
            lock (_lock)
            {
                _items.Clear();
                Save();
                OnListChanged?.Invoke();
            }
        }

        /// <summary>
        /// 通过聊天消息处理托管命令
        /// </summary>
        public (bool Handled, string Reply) ProcessCommand(string senderId, string senderName, string message)
        {
            if (!IsEnabled) return (false, null);
            if (string.IsNullOrWhiteSpace(message)) return (false, null);

            // 取消托管命令
            if (message.Contains("取消托管"))
            {
                var removed = Remove(senderId);
                return (true, removed ? "托管已取消" : "您没有托管记录");
            }

            // 托管命令: "Ja100托管" 或 "托管大100"
            var match = Regex.Match(message, @"^(.+?)托管$|^托管(.+)$", RegexOptions.IgnoreCase);
            if (match.Success)
            {
                var content = match.Groups[1].Success ? match.Groups[1].Value : match.Groups[2].Value;
                content = content.Trim();
                if (!string.IsNullOrEmpty(content))
                {
                    AddOrUpdate(senderId, content, senderName);
                    return (true, $"托管成功：{content}，下局开奖自动生效");
                }
            }

            return (false, null);
        }

        /// <summary>
        /// 获取所有托管内容（用于自动下注）
        /// </summary>
        public List<string> GetAllBetContents()
        {
            lock (_lock)
            {
                return _items.Where(x => x.IsActive)
                    .Select(x => x.Content)
                    .ToList();
            }
        }

        /// <summary>
        /// 导出为聊天格式
        /// </summary>
        public string ExportChatFormat()
        {
            lock (_lock)
            {
                var sb = new StringBuilder();
                foreach (var item in _items.Where(x => x.IsActive))
                {
                    sb.AppendLine(item.ToChatFormat());
                }
                return sb.ToString();
            }
        }

        /// <summary>
        /// 标记托管失败（超额、余额不足等）
        /// </summary>
        public void MarkFailed(string wangWangId, string reason)
        {
            Remove(wangWangId);
            Logger.Warn($"[Trustee] {wangWangId} 托管失败: {reason}");
        }

        private void Load()
        {
            lock (_lock)
            {
                _items.Clear();
                if (!File.Exists(_dataFile)) return;

                try
                {
                    var lines = File.ReadAllLines(_dataFile, Encoding.UTF8);
                    foreach (var line in lines)
                    {
                        var item = TrusteeItem.Parse(line);
                        if (item != null) _items.Add(item);
                    }
                    Logger.Info($"[Trustee] Loaded {_items.Count} items");
                }
                catch (Exception ex)
                {
                    Logger.Error($"[Trustee] Load error: {ex.Message}");
                }
            }
        }

        private void Save()
        {
            try
            {
                var lines = _items.Where(x => x.IsActive).Select(x => x.ToString());
                File.WriteAllLines(_dataFile, lines, Encoding.UTF8);
            }
            catch (Exception ex)
            {
                Logger.Error($"[Trustee] Save error: {ex.Message}");
            }
        }

        private bool GetBool(string key, bool defaultValue)
        {
            var s = DataService.Instance.GetSetting(key, defaultValue ? "1" : "0");
            return s == "1" || bool.TryParse(s, out var b) && b;
        }

        private void SetBool(string key, bool value)
        {
            DataService.Instance.SaveSetting(key, value ? "1" : "0");
        }
    }
}

