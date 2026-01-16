using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace WSLFramework.Services.Storage
{
    /// <summary>
    /// ZCG配置服务 - 兼容ZCG设置.ini的编码格式
    /// 编码规则: 每个字符 XOR 0x10，后缀 20CB5D79B
    /// </summary>
    public class ZCGConfigService
    {
        #region 单例模式
        private static readonly Lazy<ZCGConfigService> _instance = 
            new Lazy<ZCGConfigService>(() => new ZCGConfigService());
        public static ZCGConfigService Instance => _instance.Value;
        #endregion

        #region 常量
        private const byte XOR_KEY = 0x10;
        private const string VALUE_SUFFIX = "20CB5D79B";
        private const string TRUE_VALUE = "C5F620CB5D79B";
        private const string FALSE_VALUE = "ACC920CB5D79B";
        #endregion

        #region 配置路径
        public string BaseDir { get; set; }
        public string ZcgDir => Path.Combine(BaseDir, "zcg");
        public string SettingIniPath => Path.Combine(ZcgDir, "设置.ini");
        public string LoginConfigPath => Path.Combine(ZcgDir, "登录配置.ini");
        public string ConfigIniPath => Path.Combine(BaseDir, "config.ini");

        public ZCGConfigService()
        {
            BaseDir = AppDomain.CurrentDomain.BaseDirectory;
        }
        #endregion

        #region 编解码方法

        /// <summary>
        /// 编码字符串 (用于写入设置.ini)
        /// </summary>
        public string Encode(string value)
        {
            if (string.IsNullOrEmpty(value)) return VALUE_SUFFIX;
            
            var sb = new StringBuilder();
            var bytes = Encoding.GetEncoding("GB2312").GetBytes(value);
            
            foreach (var b in bytes)
            {
                sb.Append((b ^ XOR_KEY).ToString("X2"));
            }
            
            sb.Append(VALUE_SUFFIX);
            return sb.ToString();
        }

        /// <summary>
        /// 解码字符串 (用于读取设置.ini)
        /// </summary>
        public string Decode(string encoded)
        {
            if (string.IsNullOrEmpty(encoded)) return "";
            
            // 移除后缀
            if (encoded.EndsWith(VALUE_SUFFIX))
            {
                encoded = encoded.Substring(0, encoded.Length - VALUE_SUFFIX.Length);
            }
            
            if (encoded.Length == 0) return "";
            
            // 解码
            var bytes = new List<byte>();
            for (int i = 0; i < encoded.Length - 1; i += 2)
            {
                var hex = encoded.Substring(i, 2);
                try
                {
                    var b = Convert.ToByte(hex, 16);
                    bytes.Add((byte)(b ^ XOR_KEY));
                }
                catch
                {
                    // 忽略无效的十六进制
                }
            }
            
            return Encoding.GetEncoding("GB2312").GetString(bytes.ToArray());
        }

        /// <summary>
        /// 编码布尔值
        /// </summary>
        public string EncodeBool(bool value)
        {
            return value ? TRUE_VALUE : FALSE_VALUE;
        }

        /// <summary>
        /// 解码布尔值
        /// </summary>
        public bool DecodeBool(string encoded)
        {
            return encoded == TRUE_VALUE || encoded.StartsWith("C5F6");
        }

        #endregion

        #region 配置读取

        /// <summary>
        /// 读取设置.ini
        /// </summary>
        public Dictionary<string, string> ReadSettingIni()
        {
            var settings = new Dictionary<string, string>();
            
            if (!File.Exists(SettingIniPath))
            {
                return settings;
            }
            
            try
            {
                var lines = File.ReadAllLines(SettingIniPath, Encoding.GetEncoding("GB2312"));
                string currentSection = "";
                
                foreach (var line in lines)
                {
                    var trimmed = line.Trim();
                    
                    if (trimmed.StartsWith("[") && trimmed.EndsWith("]"))
                    {
                        currentSection = trimmed.Substring(1, trimmed.Length - 2);
                        continue;
                    }
                    
                    var eqIndex = trimmed.IndexOf('=');
                    if (eqIndex > 0)
                    {
                        var key = trimmed.Substring(0, eqIndex);
                        var value = trimmed.Substring(eqIndex + 1);
                        var fullKey = string.IsNullOrEmpty(currentSection) ? key : $"{currentSection}.{key}";
                        
                        // 解码值
                        settings[fullKey] = Decode(value);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"读取设置.ini失败: {ex.Message}");
            }
            
            return settings;
        }

        /// <summary>
        /// 获取设置值
        /// </summary>
        public string GetSetting(string key, string defaultValue = "")
        {
            var settings = ReadSettingIni();
            return settings.TryGetValue(key, out var value) ? value : defaultValue;
        }

        /// <summary>
        /// 获取布尔设置
        /// </summary>
        public bool GetBoolSetting(string key, bool defaultValue = false)
        {
            var value = GetSetting(key, "");
            if (string.IsNullOrEmpty(value)) return defaultValue;
            return value == "真" || value == "1" || value.ToLower() == "true";
        }

        /// <summary>
        /// 获取整数设置
        /// </summary>
        public int GetIntSetting(string key, int defaultValue = 0)
        {
            var value = GetSetting(key, "");
            return int.TryParse(value, out var result) ? result : defaultValue;
        }

        /// <summary>
        /// 获取小数设置
        /// </summary>
        public double GetDoubleSetting(string key, double defaultValue = 0)
        {
            var value = GetSetting(key, "");
            return double.TryParse(value, out var result) ? result : defaultValue;
        }

        #endregion

        #region 配置写入

        /// <summary>
        /// 写入设置
        /// </summary>
        public void WriteSetting(string section, string key, string value)
        {
            // 确保目录存在
            Directory.CreateDirectory(ZcgDir);
            
            // 读取现有配置
            var lines = new List<string>();
            var sectionFound = false;
            var keyFound = false;
            var currentSection = "";
            
            if (File.Exists(SettingIniPath))
            {
                lines.AddRange(File.ReadAllLines(SettingIniPath, Encoding.GetEncoding("GB2312")));
            }
            
            // 查找并更新
            for (int i = 0; i < lines.Count; i++)
            {
                var line = lines[i].Trim();
                
                if (line.StartsWith("[") && line.EndsWith("]"))
                {
                    currentSection = line.Substring(1, line.Length - 2);
                    if (currentSection == section) sectionFound = true;
                    continue;
                }
                
                if (currentSection == section && line.StartsWith(key + "="))
                {
                    lines[i] = $"{key}={Encode(value)}";
                    keyFound = true;
                    break;
                }
            }
            
            // 如果没找到，添加新的
            if (!sectionFound)
            {
                lines.Add($"[{section}]");
                lines.Add($"{key}={Encode(value)}");
            }
            else if (!keyFound)
            {
                // 找到节的位置，在后面添加
                for (int i = 0; i < lines.Count; i++)
                {
                    if (lines[i].Trim() == $"[{section}]")
                    {
                        lines.Insert(i + 1, $"{key}={Encode(value)}");
                        break;
                    }
                }
            }
            
            File.WriteAllLines(SettingIniPath, lines, Encoding.GetEncoding("GB2312"));
        }

        #endregion

        #region 特定配置

        /// <summary>
        /// 获取机器人QQ
        /// </summary>
        public string GetRobotQQ()
        {
            return GetSetting("编辑框.编辑框_机器人QQ", "");
        }

        /// <summary>
        /// 获取管理QQ列表
        /// </summary>
        public string[] GetAdminQQs()
        {
            var value = GetSetting("编辑框.编辑框_管理QQ号码", "");
            if (string.IsNullOrEmpty(value)) return new string[0];
            return value.Split(new[] { '@', ',', '|' }, StringSplitOptions.RemoveEmptyEntries);
        }

        /// <summary>
        /// 获取绑定群号
        /// </summary>
        public string GetBindGroup()
        {
            return GetSetting("编辑框.编辑框_绑定群号", "");
        }

        /// <summary>
        /// 获取赔率设置
        /// </summary>
        public double GetOdds(string type)
        {
            return GetDoubleSetting($"编辑框.编辑框_普通赔率_{type}", 1.0);
        }

        #endregion
    }
}
