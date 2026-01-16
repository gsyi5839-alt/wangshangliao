using System;
using System.Security.Cryptography;
using System.Text;

namespace WangShangLiaoBot.Services
{
    /// <summary>
    /// 敏感操作服务 - 管理敏感操作密码
    /// </summary>
    public sealed class SensitiveOperationService
    {
        private static SensitiveOperationService _instance;
        public static SensitiveOperationService Instance => 
            _instance ?? (_instance = new SensitiveOperationService());

        private SensitiveOperationService() { }

        private const string Prefix = "SensitiveOp:";

        /// <summary>敏感操作密码是否已启用</summary>
        public bool PasswordEnabled
        {
            get => GetBool(Prefix + "PasswordEnabled", false);
            set => SetBool(Prefix + "PasswordEnabled", value);
        }

        /// <summary>密码哈希值</summary>
        private string PasswordHash
        {
            get => GetString(Prefix + "PasswordHash", "");
            set => SetString(Prefix + "PasswordHash", value);
        }

        /// <summary>设置新密码</summary>
        public bool SetPassword(string oldPassword, string newPassword)
        {
            // If password is currently enabled, verify old password
            if (PasswordEnabled && !string.IsNullOrEmpty(PasswordHash))
            {
                if (!VerifyPassword(oldPassword))
                    return false;
            }

            // Set new password
            if (string.IsNullOrEmpty(newPassword))
            {
                PasswordHash = "";
                PasswordEnabled = false;
            }
            else
            {
                PasswordHash = HashPassword(newPassword);
                PasswordEnabled = true;
            }
            return true;
        }

        /// <summary>验证密码</summary>
        public bool VerifyPassword(string password)
        {
            if (!PasswordEnabled || string.IsNullOrEmpty(PasswordHash))
                return true;

            if (string.IsNullOrEmpty(password))
                return false;

            return PasswordHash == HashPassword(password);
        }

        /// <summary>关闭敏感密码</summary>
        public void DisablePassword()
        {
            PasswordEnabled = false;
            PasswordHash = "";
        }

        /// <summary>检查是否需要密码验证</summary>
        public bool RequiresPassword => PasswordEnabled && !string.IsNullOrEmpty(PasswordHash);

        /// <summary>密码哈希</summary>
        private string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                var bytes = Encoding.UTF8.GetBytes(password + "WangShangLiaoBot_Salt");
                var hash = sha256.ComputeHash(bytes);
                return Convert.ToBase64String(hash);
            }
        }

        // Setting helpers
        private bool GetBool(string key, bool defaultValue)
        {
            var s = DataService.Instance.GetSetting(key, defaultValue ? "1" : "0");
            return s == "1" || (bool.TryParse(s, out var b) && b);
        }

        private void SetBool(string key, bool value)
        {
            DataService.Instance.SaveSetting(key, value ? "1" : "0");
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

