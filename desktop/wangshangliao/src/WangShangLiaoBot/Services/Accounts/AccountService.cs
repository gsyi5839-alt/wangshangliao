using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using WangShangLiaoBot.Models;

namespace WangShangLiaoBot.Services
{
    /// <summary>
    /// 账号管理服务
    /// </summary>
    public class AccountService
    {
        private static AccountService _instance;
        private static readonly object _lock = new object();
        
        /// <summary>单例实例</summary>
        public static AccountService Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                            _instance = new AccountService();
                    }
                }
                return _instance;
            }
        }
        
        private List<BotAccount> _accounts;
        private string _dataFile;
        private int _nextId = 1;
        
        /// <summary>账号列表</summary>
        public List<BotAccount> Accounts 
        { 
            get { return _accounts; } 
        }
        
        /// <summary>当前选中的账号</summary>
        public BotAccount CurrentAccount { get; set; }
        
        /// <summary>账号变更事件</summary>
        public event Action OnAccountsChanged;
        
        /// <summary>日志事件</summary>
        public event Action<string> OnLog;
        
        private AccountService()
        {
            _accounts = new List<BotAccount>();
            var dataDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data");
            if (!Directory.Exists(dataDir))
                Directory.CreateDirectory(dataDir);
            _dataFile = Path.Combine(dataDir, "accounts.xml");
            
            LoadAccounts();
        }
        
        /// <summary>
        /// 加载账号列表
        /// </summary>
        public void LoadAccounts()
        {
            try
            {
                if (File.Exists(_dataFile))
                {
                    var serializer = new XmlSerializer(typeof(List<BotAccount>));
                    using (var reader = new StreamReader(_dataFile))
                    {
                        _accounts = (List<BotAccount>)serializer.Deserialize(reader);
                    }
                    
                    // Calculate next ID
                    foreach (var acc in _accounts)
                    {
                        if (acc.Id >= _nextId)
                            _nextId = acc.Id + 1;
                    }
                    
                    Log(string.Format("已加载 {0} 个账号", _accounts.Count));
                }
            }
            catch (Exception ex)
            {
                Log(string.Format("加载账号失败: {0}", ex.Message));
                _accounts = new List<BotAccount>();
            }
        }
        
        /// <summary>
        /// 保存账号列表
        /// </summary>
        public void SaveAccounts()
        {
            try
            {
                var serializer = new XmlSerializer(typeof(List<BotAccount>));
                using (var writer = new StreamWriter(_dataFile))
                {
                    serializer.Serialize(writer, _accounts);
                }
                Log("账号列表已保存");
            }
            catch (Exception ex)
            {
                Log(string.Format("保存账号失败: {0}", ex.Message));
            }
        }
        
        /// <summary>
        /// 添加账号
        /// </summary>
        public BotAccount AddAccount(string nickname, string wangwangId, string groupId = "", 
            string phone = "", string password = "")
        {
            var account = new BotAccount
            {
                Id = _nextId++,
                Nickname = nickname,
                WangWangId = wangwangId,
                GroupId = groupId,
                Phone = phone,
                Password = password,
                Status = AccountStatus.Offline
            };
            
            _accounts.Add(account);
            SaveAccounts();
            OnAccountsChanged?.Invoke();
            
            Log(string.Format("添加账号: {0} ({1})", nickname, wangwangId));
            return account;
        }
        
        /// <summary>
        /// 删除账号
        /// </summary>
        public bool RemoveAccount(int id)
        {
            var account = _accounts.Find(a => a.Id == id);
            if (account != null)
            {
                _accounts.Remove(account);
                SaveAccounts();
                OnAccountsChanged?.Invoke();
                
                Log(string.Format("删除账号: {0}", account.Nickname));
                return true;
            }
            return false;
        }
        
        /// <summary>
        /// 更新账号
        /// </summary>
        public void UpdateAccount(BotAccount account)
        {
            var existing = _accounts.Find(a => a.Id == account.Id);
            if (existing != null)
            {
                var index = _accounts.IndexOf(existing);
                _accounts[index] = account;
                SaveAccounts();
                OnAccountsChanged?.Invoke();
            }
        }
        
        /// <summary>
        /// 获取账号
        /// </summary>
        public BotAccount GetAccount(int id)
        {
            return _accounts.Find(a => a.Id == id);
        }
        
        /// <summary>
        /// 设置账号状态
        /// </summary>
        public void SetAccountStatus(int id, AccountStatus status)
        {
            var account = GetAccount(id);
            if (account != null)
            {
                account.Status = status;
                if (status == AccountStatus.Online)
                {
                    account.LastLoginTime = DateTime.Now;
                }
                SaveAccounts();
                OnAccountsChanged?.Invoke();
            }
        }
        
        /// <summary>
        /// 设置自动登录
        /// </summary>
        public void SetAutoLogin(int id, bool autoLogin)
        {
            var account = GetAccount(id);
            if (account != null)
            {
                account.AutoLogin = autoLogin;
                SaveAccounts();
                OnAccountsChanged?.Invoke();
                
                Log(string.Format("{0} 自动登录: {1}", account.Nickname, autoLogin ? "开启" : "关闭"));
            }
        }
        
        /// <summary>
        /// 复制账号
        /// </summary>
        public BotAccount CopyAccount(int id)
        {
            var source = GetAccount(id);
            if (source != null)
            {
                var copy = new BotAccount
                {
                    Id = _nextId++,
                    Nickname = source.Nickname + "_副本",
                    WangWangId = source.WangWangId,
                    GroupId = source.GroupId,
                    Phone = source.Phone,
                    Password = source.Password,
                    DebugPort = source.DebugPort + 1,
                    ExePath = source.ExePath,
                    AutoLogin = false,
                    Status = AccountStatus.Offline
                };
                
                _accounts.Add(copy);
                SaveAccounts();
                OnAccountsChanged?.Invoke();
                
                Log(string.Format("复制账号: {0}", copy.Nickname));
                return copy;
            }
            return null;
        }
        
        /// <summary>
        /// 获取所有自动登录的账号
        /// </summary>
        public List<BotAccount> GetAutoLoginAccounts()
        {
            return _accounts.FindAll(a => a.AutoLogin);
        }
        
        private void Log(string message)
        {
            Logger.Info(string.Format("[AccountService] {0}", message));
            OnLog?.Invoke(message);
        }
    }
}

