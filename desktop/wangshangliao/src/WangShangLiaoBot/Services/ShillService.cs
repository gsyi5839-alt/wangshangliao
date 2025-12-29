using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace WangShangLiaoBot.Services
{
    /// <summary>
    /// 托号服务 - 管理托号列表和自动操作
    /// </summary>
    public sealed class ShillService
    {
        private static ShillService _instance;
        public static ShillService Instance => _instance ?? (_instance = new ShillService());

        private readonly List<string> _shillList = new List<string>();
        private readonly string _dataFile;
        private readonly object _lock = new object();

        /// <summary>托号列表变化事件</summary>
        public event Action OnListChanged;

        private ShillService()
        {
            var dataDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "数据库");
            if (!Directory.Exists(dataDir)) Directory.CreateDirectory(dataDir);
            _dataFile = Path.Combine(dataDir, "shill-list.txt");
            Load();
        }

        // ================= Settings =================

        /// <summary>托查钱自动上分</summary>
        public bool AutoAddScoreOnQuery
        {
            get => GetBool("Shill:AutoAddScoreOnQuery", true);
            set => SetBool("Shill:AutoAddScoreOnQuery", value);
        }

        /// <summary>托查钱延迟最小秒</summary>
        public int QueryDelayMin
        {
            get => GetInt("Shill:QueryDelayMin", 6);
            set => SetInt("Shill:QueryDelayMin", value);
        }

        /// <summary>托查钱延迟最大秒</summary>
        public int QueryDelayMax
        {
            get => GetInt("Shill:QueryDelayMax", 10);
            set => SetInt("Shill:QueryDelayMax", value);
        }

        /// <summary>托回钱自动下分</summary>
        public bool AutoSubScoreOnReturn
        {
            get => GetBool("Shill:AutoSubScoreOnReturn", true);
            set => SetBool("Shill:AutoSubScoreOnReturn", value);
        }

        /// <summary>托回钱延迟最小秒</summary>
        public int ReturnDelayMin
        {
            get => GetInt("Shill:ReturnDelayMin", 15);
            set => SetInt("Shill:ReturnDelayMin", value);
        }

        /// <summary>托回钱延迟最大秒</summary>
        public int ReturnDelayMax
        {
            get => GetInt("Shill:ReturnDelayMax", 25);
            set => SetInt("Shill:ReturnDelayMax", value);
        }

        /// <summary>自动同意托加群</summary>
        public bool AutoAcceptJoinGroup
        {
            get => GetBool("Shill:AutoAcceptJoinGroup", true);
            set => SetBool("Shill:AutoAcceptJoinGroup", value);
        }

        /// <summary>托插件远程获取账单</summary>
        public bool RemoteFetchBill
        {
            get => GetBool("Shill:RemoteFetchBill", true);
            set => SetBool("Shill:RemoteFetchBill", value);
        }

        /// <summary>账单地址</summary>
        public string BillAddress
        {
            get => GetString("Shill:BillAddress", "");
            set => SetString("Shill:BillAddress", value);
        }

        // ================= Shill List Operations =================

        /// <summary>获取所有托号</summary>
        public List<string> GetAll()
        {
            lock (_lock)
            {
                return _shillList.ToList();
            }
        }

        /// <summary>添加托号</summary>
        public int Add(string shillId)
        {
            if (string.IsNullOrWhiteSpace(shillId)) return 0;

            lock (_lock)
            {
                var id = shillId.Trim();
                if (!_shillList.Contains(id))
                {
                    _shillList.Add(id);
                    Save();
                    OnListChanged?.Invoke();
                    return 1;
                }
                return 0;
            }
        }

        /// <summary>批量添加托号（多行）</summary>
        public int AddMultiple(string multiLineIds)
        {
            if (string.IsNullOrWhiteSpace(multiLineIds)) return 0;

            var lines = multiLineIds.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            int added = 0;
            lock (_lock)
            {
                foreach (var line in lines)
                {
                    var id = line.Trim();
                    if (!string.IsNullOrEmpty(id) && !_shillList.Contains(id))
                    {
                        _shillList.Add(id);
                        added++;
                    }
                }
                if (added > 0)
                {
                    Save();
                    OnListChanged?.Invoke();
                }
            }
            return added;
        }

        /// <summary>移除托号</summary>
        public bool Remove(string shillId)
        {
            if (string.IsNullOrWhiteSpace(shillId)) return false;

            lock (_lock)
            {
                var removed = _shillList.Remove(shillId.Trim());
                if (removed)
                {
                    Save();
                    OnListChanged?.Invoke();
                }
                return removed;
            }
        }

        /// <summary>清空所有托号</summary>
        public void ClearAll()
        {
            lock (_lock)
            {
                _shillList.Clear();
                Save();
                OnListChanged?.Invoke();
            }
        }

        /// <summary>检查是否为托号</summary>
        public bool IsShill(string accountId)
        {
            if (string.IsNullOrWhiteSpace(accountId)) return false;
            lock (_lock)
            {
                return _shillList.Contains(accountId.Trim());
            }
        }

        /// <summary>导出托号列表</summary>
        public string Export()
        {
            lock (_lock)
            {
                return string.Join(Environment.NewLine, _shillList);
            }
        }

        /// <summary>生成随机延迟（查钱）</summary>
        public int GetQueryDelay()
        {
            var rnd = new Random();
            return rnd.Next(QueryDelayMin, QueryDelayMax + 1);
        }

        /// <summary>生成随机延迟（回钱）</summary>
        public int GetReturnDelay()
        {
            var rnd = new Random();
            return rnd.Next(ReturnDelayMin, ReturnDelayMax + 1);
        }

        // ================= Persistence =================

        private void Load()
        {
            lock (_lock)
            {
                _shillList.Clear();
                if (!File.Exists(_dataFile)) return;

                try
                {
                    var lines = File.ReadAllLines(_dataFile, Encoding.UTF8);
                    foreach (var line in lines)
                    {
                        var id = line.Trim();
                        if (!string.IsNullOrEmpty(id))
                            _shillList.Add(id);
                    }
                    Logger.Info($"[Shill] Loaded {_shillList.Count} shill accounts");
                }
                catch (Exception ex)
                {
                    Logger.Error($"[Shill] Load error: {ex.Message}");
                }
            }
        }

        private void Save()
        {
            try
            {
                File.WriteAllLines(_dataFile, _shillList, Encoding.UTF8);
            }
            catch (Exception ex)
            {
                Logger.Error($"[Shill] Save error: {ex.Message}");
            }
        }

        // ================= Setting Helpers =================

        private bool GetBool(string key, bool defaultValue)
        {
            var s = DataService.Instance.GetSetting(key, defaultValue ? "1" : "0");
            return s == "1" || (bool.TryParse(s, out var b) && b);
        }

        private void SetBool(string key, bool value)
        {
            DataService.Instance.SaveSetting(key, value ? "1" : "0");
        }

        private int GetInt(string key, int defaultValue)
        {
            var s = DataService.Instance.GetSetting(key, defaultValue.ToString());
            return int.TryParse(s, out var v) ? v : defaultValue;
        }

        private void SetInt(string key, int value)
        {
            DataService.Instance.SaveSetting(key, value.ToString());
        }

        private string GetString(string key, string defaultValue)
        {
            return DataService.Instance.GetSetting(key, defaultValue);
        }

        private void SetString(string key, string value)
        {
            DataService.Instance.SaveSetting(key, value ?? "");
        }
    }
}

