using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Web.Script.Serialization;

namespace WSLFramework.Models
{
    /// <summary>
    /// 机器人账号模型 - 用于登录旺商聊
    /// </summary>
    [Serializable]
    public class BotAccount
    {
        /// <summary>旺商聊账号 (手机号)</summary>
        public string Account { get; set; } = "";
        
        /// <summary>登录密码 (加密存储)</summary>
        public string PasswordEncrypted { get; set; } = "";
        
        /// <summary>机器人名称</summary>
        public string BotName { get; set; } = "机器人";
        
        /// <summary>绑定群号</summary>
        public string GroupId { get; set; } = "";
        
        /// <summary>群名称</summary>
        public string GroupName { get; set; } = "";
        
        /// <summary>记住密码</summary>
        public bool RememberPassword { get; set; } = true;
        
        /// <summary>自动登录</summary>
        public bool AutoLogin { get; set; } = false;
        
        // === 登录后获取的信息 ===
        
        /// <summary>旺旺ID</summary>
        public string Wwid { get; set; } = "";
        
        /// <summary>昵称</summary>
        public string Nickname { get; set; } = "";
        
        /// <summary>NIM Accid</summary>
        public string NimAccid { get; set; } = "";
        
        /// <summary>NIM Token</summary>
        public string NimToken { get; set; } = "";
        
        /// <summary>JWT Token</summary>
        public string JwtToken { get; set; } = "";
        
        /// <summary>最后登录时间</summary>
        public DateTime LastLoginTime { get; set; }
        
        /// <summary>是否已登录</summary>
        public bool IsLoggedIn { get; set; } = false;
        
        /// <summary>登录状态信息</summary>
        public string LoginStatus { get; set; } = "未登录";
        
        // === 密码加解密 ===
        
        /// <summary>设置明文密码 (自动加密)</summary>
        public void SetPassword(string plainPassword)
        {
            if (string.IsNullOrEmpty(plainPassword))
            {
                PasswordEncrypted = "";
                return;
            }
            
            // 简单加密 (Base64 + 混淆)
            var bytes = Encoding.UTF8.GetBytes(plainPassword);
            for (int i = 0; i < bytes.Length; i++)
            {
                bytes[i] = (byte)(bytes[i] ^ 0x5A);
            }
            PasswordEncrypted = Convert.ToBase64String(bytes);
        }
        
        /// <summary>获取明文密码 (自动解密)</summary>
        public string GetPassword()
        {
            if (string.IsNullOrEmpty(PasswordEncrypted))
                return "";
            
            try
            {
                var bytes = Convert.FromBase64String(PasswordEncrypted);
                for (int i = 0; i < bytes.Length; i++)
                {
                    bytes[i] = (byte)(bytes[i] ^ 0x5A);
                }
                return Encoding.UTF8.GetString(bytes);
            }
            catch
            {
                return "";
            }
        }
        
        /// <summary>显示名称</summary>
        public string DisplayName => string.IsNullOrEmpty(Nickname) ? BotName : Nickname;
        
        /// <summary>简短描述</summary>
        public override string ToString()
        {
            return $"{DisplayName} ({Account}) - 群:{GroupId}";
        }
    }
    
    /// <summary>
    /// 账号管理器 - 管理多个机器人账号
    /// </summary>
    public class AccountManager
    {
        private static AccountManager _instance;
        public static AccountManager Instance => _instance ?? (_instance = new AccountManager());
        
        /// <summary>账号列表</summary>
        public List<BotAccount> Accounts { get; private set; } = new List<BotAccount>();
        
        /// <summary>当前选中的账号</summary>
        public BotAccount CurrentAccount { get; set; }
        
        /// <summary>配置文件路径</summary>
        public string ConfigPath { get; set; }
        
        /// <summary>账号变化事件</summary>
        public event Action OnAccountsChanged;
        
        /// <summary>日志事件</summary>
        public event Action<string> OnLog;
        
        private readonly JavaScriptSerializer _serializer;
        
        private AccountManager()
        {
            _serializer = new JavaScriptSerializer();
            
            // 默认配置路径
            // ★★★ 使用 bot_accounts.json 避免与 ZCGDataStorage 冲突 ★★★
            var appDir = AppDomain.CurrentDomain.BaseDirectory;
            var zcgDir = Path.Combine(appDir, "zcg");
            if (!Directory.Exists(zcgDir))
                Directory.CreateDirectory(zcgDir);
            
            ConfigPath = Path.Combine(zcgDir, "bot_accounts.json");
        }
        
        /// <summary>加载账号列表</summary>
        public void Load()
        {
            try
            {
                if (File.Exists(ConfigPath))
                {
                    var json = File.ReadAllText(ConfigPath, Encoding.UTF8);
                    var data = _serializer.Deserialize<AccountListData>(json);
                    if (data != null && data.Accounts != null)
                    {
                        Accounts = data.Accounts;
                        Log($"加载了 {Accounts.Count} 个账号");
                    }
                }
                else
                {
                    Log("账号配置文件不存在，使用空列表");
                }
            }
            catch (Exception ex)
            {
                Log($"加载账号失败: {ex.Message}");
                Accounts = new List<BotAccount>();
            }
        }
        
        /// <summary>保存账号列表</summary>
        public void Save()
        {
            try
            {
                var data = new AccountListData { Accounts = Accounts };
                var json = _serializer.Serialize(data);
                File.WriteAllText(ConfigPath, json, Encoding.UTF8);
                Log($"保存了 {Accounts.Count} 个账号");
            }
            catch (Exception ex)
            {
                Log($"保存账号失败: {ex.Message}");
            }
        }
        
        /// <summary>添加账号</summary>
        public void AddAccount(BotAccount account)
        {
            // 检查是否已存在
            var existing = Accounts.Find(a => a.Account == account.Account);
            if (existing != null)
            {
                // 更新现有账号
                var index = Accounts.IndexOf(existing);
                Accounts[index] = account;
                Log($"更新账号: {account.Account}");
            }
            else
            {
                Accounts.Add(account);
                Log($"添加账号: {account.Account}");
            }
            
            Save();
            OnAccountsChanged?.Invoke();
        }
        
        /// <summary>删除账号</summary>
        public void RemoveAccount(string account)
        {
            var existing = Accounts.Find(a => a.Account == account);
            if (existing != null)
            {
                Accounts.Remove(existing);
                Log($"删除账号: {account}");
                Save();
                OnAccountsChanged?.Invoke();
            }
        }
        
        /// <summary>获取指定账号</summary>
        public BotAccount GetAccount(string account)
        {
            return Accounts.Find(a => a.Account == account);
        }
        
        /// <summary>获取自动登录账号</summary>
        public BotAccount GetAutoLoginAccount()
        {
            return Accounts.Find(a => a.AutoLogin && a.RememberPassword);
        }
        
        /// <summary>
        /// 更新账号配置（账号或群号变更时调用）
        /// </summary>
        /// <param name="oldAccount">旧账号ID（如果更换账号）</param>
        /// <param name="newAccount">新账号配置</param>
        /// <returns>是否需要重新获取凭证</returns>
        public AccountUpdateResult UpdateAccountConfig(string oldAccount, BotAccount newAccount)
        {
            var result = new AccountUpdateResult();
            
            // 检查是否更换了机器人账号
            bool accountChanged = !string.IsNullOrEmpty(oldAccount) && oldAccount != newAccount.Account;
            
            if (accountChanged)
            {
                Log($"检测到账号变更: {oldAccount} -> {newAccount.Account}");
                
                // 检查新账号是否有保存的凭证
                var existingAccount = GetAccount(newAccount.Account);
                if (existingAccount != null && !string.IsNullOrEmpty(existingAccount.NimAccid) && !string.IsNullOrEmpty(existingAccount.NimToken))
                {
                    // 新账号有凭证，复制凭证到新配置
                    newAccount.NimAccid = existingAccount.NimAccid;
                    newAccount.NimToken = existingAccount.NimToken;
                    newAccount.Wwid = existingAccount.Wwid;
                    newAccount.Nickname = existingAccount.Nickname;
                    result.HasCredentials = true;
                    Log($"新账号 {newAccount.Account} 已有保存的凭证");
                }
                else
                {
                    // 新账号没有凭证，需要重新获取
                    result.NeedCredentials = true;
                    result.Reason = "新账号没有保存的 NIM 凭证";
                    Log($"新账号 {newAccount.Account} 需要获取凭证");
                }
                
                // 删除旧账号（可选，保留凭证以便切换回来）
                // RemoveAccount(oldAccount);
                result.AccountChanged = true;
            }
            else
            {
                // 仅更新群号或其他配置
                var existingAccount = GetAccount(newAccount.Account);
                if (existingAccount != null)
                {
                    // 保留现有凭证
                    if (string.IsNullOrEmpty(newAccount.NimAccid) && !string.IsNullOrEmpty(existingAccount.NimAccid))
                    {
                        newAccount.NimAccid = existingAccount.NimAccid;
                        newAccount.NimToken = existingAccount.NimToken;
                        newAccount.Wwid = existingAccount.Wwid;
                        newAccount.Nickname = existingAccount.Nickname;
                    }
                    result.HasCredentials = !string.IsNullOrEmpty(newAccount.NimAccid);
                }
                
                // 检查群号是否变更
                if (existingAccount != null && existingAccount.GroupId != newAccount.GroupId)
                {
                    result.GroupChanged = true;
                    Log($"群号变更: {existingAccount.GroupId} -> {newAccount.GroupId}");
                }
            }
            
            // 保存更新后的账号
            AddAccount(newAccount);
            result.Success = true;
            
            return result;
        }
        
        /// <summary>
        /// 清除账号凭证（用于强制重新获取）
        /// </summary>
        public void ClearCredentials(string account)
        {
            var existing = GetAccount(account);
            if (existing != null)
            {
                existing.NimAccid = "";
                existing.NimToken = "";
                existing.JwtToken = "";
                existing.IsLoggedIn = false;
                existing.LoginStatus = "凭证已清除";
                Save();
                Log($"已清除账号 {account} 的凭证");
            }
        }
        
        /// <summary>
        /// 检查账号是否有有效凭证
        /// </summary>
        public bool HasValidCredentials(string account)
        {
            var existing = GetAccount(account);
            return existing != null && 
                   !string.IsNullOrEmpty(existing.NimAccid) && 
                   !string.IsNullOrEmpty(existing.NimToken);
        }
        
        private void Log(string msg)
        {
            OnLog?.Invoke($"[AccountManager] {msg}");
        }
        
        // 序列化辅助类
        [Serializable]
        private class AccountListData
        {
            public List<BotAccount> Accounts { get; set; }
        }
    }
    
    /// <summary>
    /// 账号更新结果
    /// </summary>
    public class AccountUpdateResult
    {
        /// <summary>操作是否成功</summary>
        public bool Success { get; set; }
        
        /// <summary>账号是否变更</summary>
        public bool AccountChanged { get; set; }
        
        /// <summary>群号是否变更</summary>
        public bool GroupChanged { get; set; }
        
        /// <summary>是否需要重新获取凭证</summary>
        public bool NeedCredentials { get; set; }
        
        /// <summary>是否已有有效凭证</summary>
        public bool HasCredentials { get; set; }
        
        /// <summary>原因说明</summary>
        public string Reason { get; set; }
        
        /// <summary>是否需要重新登录</summary>
        public bool NeedRelogin => AccountChanged || NeedCredentials;
        
        public override string ToString()
        {
            if (NeedCredentials)
                return $"需要获取凭证: {Reason}";
            if (AccountChanged)
                return "账号已变更，使用已保存的凭证";
            if (GroupChanged)
                return "群号已更新";
            return "配置已更新";
        }
    }
}
