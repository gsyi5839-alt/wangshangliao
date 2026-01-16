using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace WangShangLiaoBot.Services.PureClient
{
    /// <summary>
    /// 旺商聊加密/解密服务
    /// 基于逆向分析的AES-256-GCM实现
    /// </summary>
    public static class WslCrypto
    {
        // AES-GCM参数
        private const int KEY_SIZE = 32;      // 256 bits
        private const int NONCE_SIZE = 12;    // 96 bits (GCM推荐)
        private const int TAG_SIZE = 16;      // 128 bits

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

            // 解析结构
            // Part1: 8字节版本 + 96字节AppKey数据
            // Part2: 8字节版本 + 96字节Token数据
            // Part3: 加密密钥和签名数据

            var info = new WslKeyInfo
            {
                Version1 = BitConverter.ToInt64(part1, 0),
                Version2 = BitConverter.ToInt64(part2, 0),
                
                // AppKey数据 (从Part1提取)
                AppKeyData = new byte[part1.Length - 8],
                
                // Token数据 (从Part2提取)
                TokenData = new byte[part2.Length - 8],
                
                // 加密密钥可能在Part3的前32字节
                EncryptionKey = new byte[KEY_SIZE],
                
                // IV/Nonce可能在Part3的32-44字节
                BaseNonce = new byte[NONCE_SIZE],
                
                // 原始Part3数据 (用于进一步分析)
                SignatureData = part3
            };

            // 复制数据
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
        /// AES-256-GCM 加密
        /// </summary>
        public static byte[] Encrypt(byte[] plaintext, byte[] key, byte[] nonce, byte[] associatedData = null)
        {
            if (key.Length != KEY_SIZE)
                throw new ArgumentException($"Key must be {KEY_SIZE} bytes");
            if (nonce.Length != NONCE_SIZE)
                throw new ArgumentException($"Nonce must be {NONCE_SIZE} bytes");

            using (var aesGcm = new AesGcm(key))
            {
                var ciphertext = new byte[plaintext.Length];
                var tag = new byte[TAG_SIZE];

                aesGcm.Encrypt(nonce, plaintext, ciphertext, tag, associatedData);

                // 输出格式: nonce + tag + ciphertext
                var result = new byte[NONCE_SIZE + TAG_SIZE + ciphertext.Length];
                Array.Copy(nonce, 0, result, 0, NONCE_SIZE);
                Array.Copy(tag, 0, result, NONCE_SIZE, TAG_SIZE);
                Array.Copy(ciphertext, 0, result, NONCE_SIZE + TAG_SIZE, ciphertext.Length);

                return result;
            }
        }

        /// <summary>
        /// AES-256-GCM 解密
        /// </summary>
        public static byte[] Decrypt(byte[] encryptedData, byte[] key, byte[] associatedData = null)
        {
            if (key.Length != KEY_SIZE)
                throw new ArgumentException($"Key must be {KEY_SIZE} bytes");

            // 解析输入格式: nonce + tag + ciphertext
            if (encryptedData.Length < NONCE_SIZE + TAG_SIZE)
                throw new ArgumentException("Encrypted data too short");

            var nonce = new byte[NONCE_SIZE];
            var tag = new byte[TAG_SIZE];
            var ciphertext = new byte[encryptedData.Length - NONCE_SIZE - TAG_SIZE];

            Array.Copy(encryptedData, 0, nonce, 0, NONCE_SIZE);
            Array.Copy(encryptedData, NONCE_SIZE, tag, 0, TAG_SIZE);
            Array.Copy(encryptedData, NONCE_SIZE + TAG_SIZE, ciphertext, 0, ciphertext.Length);

            using (var aesGcm = new AesGcm(key))
            {
                var plaintext = new byte[ciphertext.Length];
                aesGcm.Decrypt(nonce, ciphertext, tag, plaintext, associatedData);
                return plaintext;
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
        /// 派生密钥 (HKDF)
        /// </summary>
        public static byte[] DeriveKey(byte[] inputKey, byte[] salt, byte[] info, int outputLength = KEY_SIZE)
        {
            // 简化的HKDF实现
            using (var hmac = new HMACSHA256(salt ?? new byte[32]))
            {
                // Extract
                var prk = hmac.ComputeHash(inputKey);
                
                // Expand
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
        /// <summary>Part1的版本号</summary>
        public long Version1 { get; set; }
        
        /// <summary>Part2的版本号</summary>
        public long Version2 { get; set; }
        
        /// <summary>AppKey数据</summary>
        public byte[] AppKeyData { get; set; }
        
        /// <summary>Token数据</summary>
        public byte[] TokenData { get; set; }
        
        /// <summary>加密密钥 (32字节)</summary>
        public byte[] EncryptionKey { get; set; }
        
        /// <summary>基础Nonce (12字节)</summary>
        public byte[] BaseNonce { get; set; }
        
        /// <summary>签名/认证数据</summary>
        public byte[] SignatureData { get; set; }

        /// <summary>获取AppKey的十六进制表示</summary>
        public string GetAppKeyHex()
        {
            return BitConverter.ToString(AppKeyData).Replace("-", "");
        }

        /// <summary>获取加密密钥的十六进制表示</summary>
        public string GetEncryptionKeyHex()
        {
            return BitConverter.ToString(EncryptionKey).Replace("-", "");
        }
    }
}
