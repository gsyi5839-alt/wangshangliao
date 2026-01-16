using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace WSLFramework.Services
{
    /// <summary>
    /// 旺商聊加密/解密服务
    /// 基于逆向分析的AES-256-CBC实现
    /// 支持完整的旺商聊NIM通信加密
    /// </summary>
    public static class WslCrypto
    {
        // AES参数
        private const int KEY_SIZE = 32;      // 256 bits
        private const int NONCE_SIZE = 12;    // 96 bits (GCM推荐)
        private const int TAG_SIZE = 16;      // 128 bits

        // ========== 旺商聊官方AES加密配置 (根据逆向分析文档) ==========
        // AES密钥原文: "49KdgB8_9=12+3hF" -> SHA256后使用
        private const string WSL_AES_KEY_STRING = "49KdgB8_9=12+3hF";
        // AES IV: 32个0 (16字节)
        private static readonly byte[] WSL_AES_IV = new byte[16]; // 全0
        
        // 预计算的SHA256密钥
        private static byte[] _wslAesKey;
        private static byte[] WslAesKey
        {
            get
            {
                if (_wslAesKey == null)
                {
                    using (var sha256 = SHA256.Create())
                    {
                        _wslAesKey = sha256.ComputeHash(Encoding.UTF8.GetBytes(WSL_AES_KEY_STRING));
                    }
                }
                return _wslAesKey;
            }
        }
        
        // 旺商聊昵称解密密钥 (从主框架移植)
        private static readonly byte[] NICKNAME_AES_KEY = Encoding.UTF8.GetBytes("d6ba6647b7c43b79d0e42ceb2790e342");
        private static readonly byte[] NICKNAME_AES_IV = Encoding.UTF8.GetBytes("kgWRyiiODMjSCh0m");

        // 旺商聊NIM消息加密密钥 (云信SDK默认)
        private static readonly byte[] NIM_MSG_KEY = Encoding.UTF8.GetBytes("45c6af3c98409b18a84451215d0bdd6e");
        
        /// <summary>
        /// 解密旺商聊昵称 (AES-256-CBC)
        /// </summary>
        public static string DecryptNickname(string ciphertextBase64)
        {
            if (string.IsNullOrWhiteSpace(ciphertextBase64))
                return null;
            
            try
            {
                byte[] cipherBytes;
                try
                {
                    cipherBytes = Convert.FromBase64String(ciphertextBase64);
                }
                catch
                {
                    return null;
                }
                
                using (var aes = Aes.Create())
                {
                    aes.Key = NICKNAME_AES_KEY;
                    aes.IV = NICKNAME_AES_IV;
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
        /// 加密旺商聊昵称 (AES-256-CBC)
        /// </summary>
        public static string EncryptNickname(string plaintext)
        {
            if (string.IsNullOrWhiteSpace(plaintext))
                return null;
            
            try
            {
                using (var aes = Aes.Create())
                {
                    aes.Key = NICKNAME_AES_KEY;
                    aes.IV = NICKNAME_AES_IV;
                    aes.Mode = CipherMode.CBC;
                    aes.Padding = PaddingMode.PKCS7;
                    
                    using (var encryptor = aes.CreateEncryptor())
                    {
                        var plaintextBytes = Encoding.UTF8.GetBytes(plaintext);
                        var cipherBytes = encryptor.TransformFinalBlock(plaintextBytes, 0, plaintextBytes.Length);
                        return Convert.ToBase64String(cipherBytes);
                    }
                }
            }
            catch
            {
                return null;
            }
        }
        
        /// <summary>
        /// 获取NIM消息加密密钥
        /// </summary>
        public static byte[] GetNimMessageKey()
        {
            return NIM_MSG_KEY;
        }
        
        /// <summary>
        /// 获取昵称加密密钥 (用于调试)
        /// </summary>
        public static (byte[] Key, byte[] IV) GetNicknameKeyPair()
        {
            return (NICKNAME_AES_KEY, NICKNAME_AES_IV);
        }
        
        /// <summary>
        /// 旺商聊官方加密 (AES-256-CBC，使用SHA256密钥)
        /// 根据逆向分析文档实现
        /// </summary>
        public static string WslEncrypt(string plaintext)
        {
            if (string.IsNullOrEmpty(plaintext))
                return null;
            
            try
            {
                using (var aes = Aes.Create())
                {
                    aes.Key = WslAesKey;
                    aes.IV = WSL_AES_IV;
                    aes.Mode = CipherMode.CBC;
                    aes.Padding = PaddingMode.PKCS7;
                    
                    using (var encryptor = aes.CreateEncryptor())
                    {
                        var plaintextBytes = Encoding.UTF8.GetBytes(plaintext);
                        var cipherBytes = encryptor.TransformFinalBlock(plaintextBytes, 0, plaintextBytes.Length);
                        return Convert.ToBase64String(cipherBytes);
                    }
                }
            }
            catch
            {
                return null;
            }
        }
        
        /// <summary>
        /// 旺商聊官方解密 (AES-256-CBC，使用SHA256密钥)
        /// 根据逆向分析文档实现
        /// </summary>
        public static string WslDecrypt(string ciphertextBase64)
        {
            if (string.IsNullOrEmpty(ciphertextBase64))
                return null;
            
            try
            {
                var cipherBytes = Convert.FromBase64String(ciphertextBase64);
                
                using (var aes = Aes.Create())
                {
                    aes.Key = WslAesKey;
                    aes.IV = WSL_AES_IV;
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
        /// 获取旺商聊AES密钥 (SHA256后的密钥)
        /// </summary>
        public static byte[] GetWslAesKey()
        {
            return WslAesKey;
        }
        
        /// <summary>
        /// 获取旺商聊AES IV
        /// </summary>
        public static byte[] GetWslAesIV()
        {
            return WSL_AES_IV;
        }

        /// <summary>
        /// 解析旺商聊环境密钥
        /// 格式: Part1.Part2.Part3 (Base64URL编码)
        /// </summary>
        public static WslKeyInfo ParseKey(string environmentKey)
        {
            var parts = environmentKey.Split('.');
            if (parts.Length != 3)
            {
                throw new ArgumentException("Invalid key format: expected 3 parts separated by '.'");
            }

            var part1 = DecodeBase64Url(parts[0]);
            var part2 = DecodeBase64Url(parts[1]);
            var part3 = DecodeBase64Url(parts[2]);

            var info = new WslKeyInfo
            {
                Version1 = BitConverter.ToInt64(part1, 0),
                Version2 = BitConverter.ToInt64(part2, 0),
                AppKeyData = new byte[part1.Length - 8],
                TokenData = new byte[part2.Length - 8],
                EncryptionKey = new byte[KEY_SIZE],
                BaseNonce = new byte[NONCE_SIZE],
                SignatureData = part3
            };

            Array.Copy(part1, 8, info.AppKeyData, 0, info.AppKeyData.Length);
            Array.Copy(part2, 8, info.TokenData, 0, info.TokenData.Length);
            
            if (part3.Length >= KEY_SIZE + NONCE_SIZE)
            {
                Array.Copy(part3, 0, info.EncryptionKey, 0, KEY_SIZE);
                Array.Copy(part3, KEY_SIZE, info.BaseNonce, 0, NONCE_SIZE);
            }

            return info;
        }

        /// <summary>
        /// AES-256-CBC 加密 (兼容旧系统)
        /// </summary>
        public static byte[] EncryptCBC(byte[] plaintext, byte[] key, byte[] iv)
        {
            using (var aes = Aes.Create())
            {
                aes.Key = key;
                aes.IV = iv;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                using (var encryptor = aes.CreateEncryptor())
                {
                    return encryptor.TransformFinalBlock(plaintext, 0, plaintext.Length);
                }
            }
        }

        /// <summary>
        /// AES-256-CBC 解密 (兼容旧系统)
        /// </summary>
        public static byte[] DecryptCBC(byte[] ciphertext, byte[] key, byte[] iv)
        {
            using (var aes = Aes.Create())
            {
                aes.Key = key;
                aes.IV = iv;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                using (var decryptor = aes.CreateDecryptor())
                {
                    return decryptor.TransformFinalBlock(ciphertext, 0, ciphertext.Length);
                }
            }
        }

        /// <summary>
        /// 生成随机Nonce
        /// </summary>
        public static byte[] GenerateNonce()
        {
            var nonce = new byte[NONCE_SIZE];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(nonce);
            }
            return nonce;
        }

        /// <summary>
        /// 生成随机IV (16字节)
        /// </summary>
        public static byte[] GenerateIV()
        {
            var iv = new byte[16];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(iv);
            }
            return iv;
        }

        /// <summary>
        /// 派生密钥 (HKDF)
        /// </summary>
        public static byte[] DeriveKey(byte[] inputKey, byte[] salt, byte[] info, int outputLength = KEY_SIZE)
        {
            using (var hmac = new HMACSHA256(salt ?? new byte[32]))
            {
                var prk = hmac.ComputeHash(inputKey);
                
                hmac.Key = prk;
                var output = new byte[outputLength];
                var t = new byte[0];
                var offset = 0;
                var counter = 1;

                while (offset < outputLength)
                {
                    var input = new byte[t.Length + (info?.Length ?? 0) + 1];
                    Array.Copy(t, 0, input, 0, t.Length);
                    if (info != null)
                        Array.Copy(info, 0, input, t.Length, info.Length);
                    input[input.Length - 1] = (byte)counter++;

                    t = hmac.ComputeHash(input);
                    var copyLen = Math.Min(t.Length, outputLength - offset);
                    Array.Copy(t, 0, output, offset, copyLen);
                    offset += copyLen;
                }

                return output;
            }
        }

        /// <summary>
        /// Base64URL解码
        /// </summary>
        public static byte[] DecodeBase64Url(string input)
        {
            var base64 = input.Replace('-', '+').Replace('_', '/');
            var padding = 4 - (base64.Length % 4);
            if (padding < 4)
            {
                base64 += new string('=', padding);
            }
            return Convert.FromBase64String(base64);
        }

        /// <summary>
        /// Base64URL编码
        /// </summary>
        public static string EncodeBase64Url(byte[] input)
        {
            return Convert.ToBase64String(input)
                .TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_');
        }

        /// <summary>
        /// 计算MD5
        /// </summary>
        public static string ComputeMD5(string input)
        {
            using (var md5 = MD5.Create())
            {
                var hash = md5.ComputeHash(Encoding.UTF8.GetBytes(input));
                return BitConverter.ToString(hash).Replace("-", "").ToLower();
            }
        }

        /// <summary>
        /// 计算SHA256
        /// </summary>
        public static byte[] ComputeSHA256(byte[] input)
        {
            using (var sha256 = SHA256.Create())
            {
                return sha256.ComputeHash(input);
            }
        }
    }

    /// <summary>
    /// 旺商聊密钥信息
    /// </summary>
    public class WslKeyInfo
    {
        public long Version1 { get; set; }
        public long Version2 { get; set; }
        public byte[] AppKeyData { get; set; }
        public byte[] TokenData { get; set; }
        public byte[] EncryptionKey { get; set; }
        public byte[] BaseNonce { get; set; }
        public byte[] SignatureData { get; set; }

        public string GetAppKeyHex()
        {
            return BitConverter.ToString(AppKeyData).Replace("-", "");
        }

        public string GetEncryptionKeyHex()
        {
            return BitConverter.ToString(EncryptionKey).Replace("-", "");
        }
    }
}
