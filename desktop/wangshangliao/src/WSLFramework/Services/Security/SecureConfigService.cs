using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Web.Script.Serialization;
using WSLFramework.Utils;

namespace WSLFramework.Services.Security
{
    /// <summary>
    /// 安全配置服务 - 敏感信息加密存储
    /// 解决硬编码密钥的安全问题
    /// </summary>
    public class SecureConfigService
    {
        #region 单例模式

        private static readonly Lazy<SecureConfigService> _instance =
            new Lazy<SecureConfigService>(() => new SecureConfigService());

        public static SecureConfigService Instance => _instance.Value;

        #endregion

        #region 常量

        /// <summary>配置文件名</summary>
        private const string CONFIG_FILE = "secure_config.dat";

        /// <summary>默认密钥派生盐值 (应从环境变量获取)</summary>
        private static readonly byte[] DEFAULT_SALT = Encoding.UTF8.GetBytes("WSL_SECURE_2026");

        /// <summary>数据保护作用域</summary>
        private static readonly DataProtectionScope PROTECTION_SCOPE = DataProtectionScope.CurrentUser;

        #endregion

        #region 私有字段

        private readonly string _configPath;
        private readonly JavaScriptSerializer _serializer;
        private Dictionary<string, string> _secureCache;
        private readonly object _lock = new object();

        #endregion

        #region 事件

        public event Action<string> OnLog;

        #endregion

        #region 构造函数

        private SecureConfigService()
        {
            _serializer = new JavaScriptSerializer();
            _secureCache = new Dictionary<string, string>();

            // 配置文件路径
            var appDir = AppDomain.CurrentDomain.BaseDirectory;
            _configPath = Path.Combine(appDir, "zcg", CONFIG_FILE);

            // 加载配置
            Load();
        }

        #endregion

        #region 核心配置键 (替代硬编码)

        /// <summary>昵称加密密钥</summary>
        public const string KEY_NICKNAME_AES_KEY = "NICKNAME_AES_KEY";

        /// <summary>昵称加密IV</summary>
        public const string KEY_NICKNAME_AES_IV = "NICKNAME_AES_IV";

        /// <summary>NIM消息密钥</summary>
        public const string KEY_NIM_MSG_KEY = "NIM_MSG_KEY";

        /// <summary>旺商聊AES密钥字符串</summary>
        public const string KEY_WSL_AES_KEY_STRING = "WSL_AES_KEY_STRING";

        /// <summary>开奖API Token</summary>
        public const string KEY_LOTTERY_API_TOKEN = "LOTTERY_API_TOKEN";

        #endregion

        #region 公共方法

        /// <summary>
        /// 获取敏感配置 (优先从环境变量获取)
        /// </summary>
        /// <param name="key">配置键</param>
        /// <param name="defaultValue">默认值</param>
        public string Get(string key, string defaultValue = null)
        {
            if (string.IsNullOrEmpty(key))
                return defaultValue;

            // 1. 优先从环境变量获取
            var envValue = Environment.GetEnvironmentVariable($"WSL_{key}");
            if (!string.IsNullOrEmpty(envValue))
            {
                Log($"[安全配置] 从环境变量获取: {key}");
                return envValue;
            }

            // 2. 从加密存储获取
            lock (_lock)
            {
                if (_secureCache.TryGetValue(key, out var value))
                {
                    return value;
                }
            }

            // 3. 返回默认值
            return defaultValue;
        }

        /// <summary>
        /// 获取敏感配置 (字节数组)
        /// </summary>
        public byte[] GetBytes(string key, byte[] defaultValue = null)
        {
            var value = Get(key);
            if (string.IsNullOrEmpty(value))
                return defaultValue;

            try
            {
                return Convert.FromBase64String(value);
            }
            catch
            {
                return Encoding.UTF8.GetBytes(value);
            }
        }

        /// <summary>
        /// 设置敏感配置
        /// </summary>
        public void Set(string key, string value)
        {
            if (string.IsNullOrEmpty(key))
                return;

            lock (_lock)
            {
                _secureCache[key] = value;
            }

            Save();
            Log($"[安全配置] 已保存: {key}");
        }

        /// <summary>
        /// 设置敏感配置 (字节数组)
        /// </summary>
        public void SetBytes(string key, byte[] value)
        {
            Set(key, Convert.ToBase64String(value));
        }

        /// <summary>
        /// 删除配置
        /// </summary>
        public bool Remove(string key)
        {
            lock (_lock)
            {
                if (_secureCache.Remove(key))
                {
                    Save();
                    Log($"[安全配置] 已删除: {key}");
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 检查配置是否存在
        /// </summary>
        public bool Contains(string key)
        {
            // 检查环境变量
            if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable($"WSL_{key}")))
                return true;

            lock (_lock)
            {
                return _secureCache.ContainsKey(key);
            }
        }

        /// <summary>
        /// 初始化默认配置 (首次运行时调用)
        /// </summary>
        public void InitializeDefaults()
        {
            // 只有当配置不存在时才设置默认值
            if (!Contains(KEY_NICKNAME_AES_KEY))
            {
                // 从现有硬编码值迁移
                Set(KEY_NICKNAME_AES_KEY, "d6ba6647b7c43b79d0e42ceb2790e342");
                Log("[安全配置] 已迁移默认昵称密钥");
            }

            if (!Contains(KEY_NICKNAME_AES_IV))
            {
                Set(KEY_NICKNAME_AES_IV, "kgWRyiiODMjSCh0m");
                Log("[安全配置] 已迁移默认昵称IV");
            }

            if (!Contains(KEY_WSL_AES_KEY_STRING))
            {
                Set(KEY_WSL_AES_KEY_STRING, "49KdgB8_9=12+3hF");
                Log("[安全配置] 已迁移默认WSL密钥");
            }
        }

        #endregion

        #region 加密存储

        /// <summary>
        /// 加载配置
        /// </summary>
        private void Load()
        {
            try
            {
                if (!File.Exists(_configPath))
                {
                    Log("[安全配置] 配置文件不存在，使用空配置");
                    return;
                }

                var encryptedData = File.ReadAllBytes(_configPath);
                
                // 使用 DPAPI 解密
                var decryptedData = ProtectedData.Unprotect(
                    encryptedData, 
                    DEFAULT_SALT, 
                    PROTECTION_SCOPE);

                var json = Encoding.UTF8.GetString(decryptedData);
                _secureCache = _serializer.Deserialize<Dictionary<string, string>>(json) 
                    ?? new Dictionary<string, string>();

                Log($"[安全配置] 已加载 {_secureCache.Count} 项配置");
            }
            catch (Exception ex)
            {
                Log($"[安全配置] 加载失败: {ex.Message}");
                _secureCache = new Dictionary<string, string>();
            }
        }

        /// <summary>
        /// 保存配置
        /// </summary>
        private void Save()
        {
            try
            {
                var dir = Path.GetDirectoryName(_configPath);
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                string json;
                lock (_lock)
                {
                    json = _serializer.Serialize(_secureCache);
                }

                var plainData = Encoding.UTF8.GetBytes(json);
                
                // 使用 DPAPI 加密
                var encryptedData = ProtectedData.Protect(
                    plainData, 
                    DEFAULT_SALT, 
                    PROTECTION_SCOPE);

                File.WriteAllBytes(_configPath, encryptedData);
                Log("[安全配置] 已保存配置文件");
            }
            catch (Exception ex)
            {
                Log($"[安全配置] 保存失败: {ex.Message}");
            }
        }

        #endregion

        #region 辅助方法

        private void Log(string message)
        {
            Logger.Info(message);
            OnLog?.Invoke(message);
        }

        #endregion
    }

    /// <summary>
    /// WslCrypto 的安全版本扩展
    /// 从 SecureConfigService 获取密钥而非硬编码
    /// </summary>
    public static class SecureWslCrypto
    {
        private static byte[] _nicknameKey;
        private static byte[] _nicknameIV;

        /// <summary>
        /// 获取昵称加密密钥 (从安全配置获取)
        /// </summary>
        public static byte[] GetNicknameKey()
        {
            if (_nicknameKey == null)
            {
                var keyStr = SecureConfigService.Instance.Get(
                    SecureConfigService.KEY_NICKNAME_AES_KEY,
                    "d6ba6647b7c43b79d0e42ceb2790e342"); // 兼容默认值
                _nicknameKey = Encoding.UTF8.GetBytes(keyStr);
            }
            return _nicknameKey;
        }

        /// <summary>
        /// 获取昵称加密IV (从安全配置获取)
        /// </summary>
        public static byte[] GetNicknameIV()
        {
            if (_nicknameIV == null)
            {
                var ivStr = SecureConfigService.Instance.Get(
                    SecureConfigService.KEY_NICKNAME_AES_IV,
                    "kgWRyiiODMjSCh0m"); // 兼容默认值
                _nicknameIV = Encoding.UTF8.GetBytes(ivStr);
            }
            return _nicknameIV;
        }

        /// <summary>
        /// 解密昵称 (使用安全配置的密钥)
        /// </summary>
        public static string DecryptNickname(string ciphertextBase64)
        {
            if (string.IsNullOrWhiteSpace(ciphertextBase64))
                return null;

            try
            {
                var cipherBytes = Convert.FromBase64String(ciphertextBase64);

                using (var aes = System.Security.Cryptography.Aes.Create())
                {
                    aes.Key = GetNicknameKey();
                    aes.IV = GetNicknameIV();
                    aes.Mode = CipherMode.CBC;
                    aes.Padding = PaddingMode.PKCS7;

                    using (var decryptor = aes.CreateDecryptor())
                    using (var ms = new MemoryStream(cipherBytes))
                    using (var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
                    using (var sr = new StreamReader(cs, Encoding.UTF8))
                    {
                        return sr.ReadToEnd();
                    }
                }
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// 清除缓存的密钥 (密钥更新后调用)
        /// </summary>
        public static void ClearKeyCache()
        {
            _nicknameKey = null;
            _nicknameIV = null;
        }
    }
}
