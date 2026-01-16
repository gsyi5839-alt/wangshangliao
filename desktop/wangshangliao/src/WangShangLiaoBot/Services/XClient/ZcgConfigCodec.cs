using System;
using System.Collections.Generic;
using System.Text;

namespace WangShangLiaoBot.Services.XClient
{
    /// <summary>
    /// ZCG配置值编解码器
    /// 基于逆向分析获取的编码规则
    /// 
    /// 编码规则:
    /// - 后缀: "20CB5D79B"
    /// - 布尔值: 真="ACC920CB5D79B", 假="C5F620CB5D79B"
    /// - 数字: hex(value * 10 + 20) + "20CB5D79B"
    /// - 字符串: 特殊编码 + "20CB5D79B"
    /// </summary>
    public static class ZcgConfigCodec
    {
        #region 常量

        /// <summary>编码后缀</summary>
        public const string SUFFIX = "20CB5D79B";

        /// <summary>布尔值 - 真/开启</summary>
        public const string BOOL_TRUE = "ACC920CB5D79B";

        /// <summary>布尔值 - 假/关闭</summary>
        public const string BOOL_FALSE = "C5F620CB5D79B";

        #endregion

        #region 解码方法

        /// <summary>
        /// 解码布尔值
        /// </summary>
        public static bool DecodeBool(string encoded)
        {
            if (string.IsNullOrEmpty(encoded)) return false;
            return encoded == BOOL_TRUE;
        }

        /// <summary>
        /// 解码数字
        /// 规则: 去除后缀，十六进制转十进制，(value - 20) / 10
        /// </summary>
        public static decimal DecodeNumber(string encoded)
        {
            if (string.IsNullOrEmpty(encoded)) return 0;

            try
            {
                // 去除后缀
                var hex = encoded.Replace(SUFFIX, "");
                if (string.IsNullOrEmpty(hex)) return 0;

                // 十六进制转十进制
                var decValue = Convert.ToInt64(hex, 16);

                // 应用公式: (value - 20) / 10
                return (decValue - 20) / 10m;
            }
            catch
            {
                return 0;
            }
        }

        /// <summary>
        /// 解码整数
        /// </summary>
        public static int DecodeInt(string encoded)
        {
            return (int)Math.Round(DecodeNumber(encoded));
        }

        /// <summary>
        /// 解码字符串 (GBK编码的十六进制)
        /// </summary>
        public static string DecodeString(string encoded)
        {
            if (string.IsNullOrEmpty(encoded)) return "";

            try
            {
                // 去除后缀
                var hex = encoded.Replace(SUFFIX, "");
                if (string.IsNullOrEmpty(hex)) return "";

                // 每两个字符为一个字节
                var bytes = new List<byte>();
                for (int i = 0; i < hex.Length - 1; i += 2)
                {
                    var byteStr = hex.Substring(i, 2);
                    bytes.Add(Convert.ToByte(byteStr, 16));
                }

                // GBK解码
                var gbk = Encoding.GetEncoding("GBK");
                return gbk.GetString(bytes.ToArray());
            }
            catch
            {
                return "";
            }
        }

        #endregion

        #region 编码方法

        /// <summary>
        /// 编码布尔值
        /// </summary>
        public static string EncodeBool(bool value)
        {
            return value ? BOOL_TRUE : BOOL_FALSE;
        }

        /// <summary>
        /// 编码数字
        /// 规则: (value * 10 + 20) 转十六进制 + 后缀
        /// </summary>
        public static string EncodeNumber(decimal value)
        {
            var intValue = (int)Math.Round(value * 10 + 20);
            return intValue.ToString("X") + SUFFIX;
        }

        /// <summary>
        /// 编码整数
        /// </summary>
        public static string EncodeInt(int value)
        {
            return EncodeNumber(value);
        }

        /// <summary>
        /// 编码字符串 (转换为GBK十六进制)
        /// </summary>
        public static string EncodeString(string value)
        {
            if (string.IsNullOrEmpty(value)) return SUFFIX;

            try
            {
                var gbk = Encoding.GetEncoding("GBK");
                var bytes = gbk.GetBytes(value);
                var hex = BitConverter.ToString(bytes).Replace("-", "");
                return hex + SUFFIX;
            }
            catch
            {
                return SUFFIX;
            }
        }

        #endregion

        #region 配置项解析

        /// <summary>
        /// 解析ZCG设置.ini中的赔率配置
        /// </summary>
        public static Dictionary<string, decimal> ParseOddsConfig(Dictionary<string, string> rawConfig)
        {
            var odds = new Dictionary<string, decimal>();

            // 赔率配置映射
            var oddsKeys = new Dictionary<string, string>
            {
                { "大注", "[赔率]大注" },
                { "小注", "[赔率]小注" },
                { "单注", "[赔率]单注" },
                { "双注", "[赔率]双注" },
                { "大单", "[赔率]大单" },
                { "大双", "[赔率]大双" },
                { "小单", "[赔率]小单" },
                { "小双", "[赔率]小双" },
                { "顺子", "[赔率]顺子" },
                { "半顺", "[赔率]半顺" },
                { "豹子", "[赔率]豹子" },
                { "对子", "[赔率]对子" },
                { "杂六", "[赔率]杂六" }
            };

            foreach (var kv in oddsKeys)
            {
                if (rawConfig.TryGetValue(kv.Value, out var encoded))
                {
                    odds[kv.Key] = DecodeNumber(encoded);
                }
            }

            return odds;
        }

        /// <summary>
        /// 解析ZCG设置.ini中的功能开关配置
        /// </summary>
        public static Dictionary<string, bool> ParseSwitchConfig(Dictionary<string, string> rawConfig)
        {
            var switches = new Dictionary<string, bool>();

            // 功能开关映射
            var switchKeys = new[]
            {
                "选项框_发封盘图片开关",
                "选项框_私聊下注开关",
                "选项框_模糊匹配开关",
                "选项框_自动同步群资料",
                "选项框_限注开关",
                "选项框_杀数玩法开关",
                "选项框_尾数玩法开关"
            };

            foreach (var key in switchKeys)
            {
                var fullKey = "[选项框]" + key.Replace("选项框_", "");
                if (rawConfig.TryGetValue(fullKey, out var encoded))
                {
                    var shortKey = key.Replace("选项框_", "").Replace("开关", "");
                    switches[shortKey] = DecodeBool(encoded);
                }
            }

            return switches;
        }

        #endregion

        #region 数值范围解码

        /// <summary>
        /// 解码赔率范围字符串
        /// 格式: "min>max" 编码形式
        /// </summary>
        public static (decimal min, decimal max) DecodeOddsRange(string encoded)
        {
            if (string.IsNullOrEmpty(encoded)) return (0, 0);

            try
            {
                var decoded = DecodeString(encoded);
                var parts = decoded.Split('>');
                if (parts.Length == 2)
                {
                    decimal.TryParse(parts[0], out var min);
                    decimal.TryParse(parts[1], out var max);
                    return (min, max);
                }
            }
            catch { }

            return (0, 0);
        }

        /// <summary>
        /// 解码限注配置
        /// 格式: "单注最小|单注最大,单项最小|单项最大"
        /// </summary>
        public static (int singleMin, int singleMax, int itemMin, int itemMax) DecodeBetLimits(string encoded)
        {
            if (string.IsNullOrEmpty(encoded)) return (0, 0, 0, 0);

            try
            {
                var decoded = DecodeString(encoded);
                var parts = decoded.Split(',');

                int singleMin = 0, singleMax = 0, itemMin = 0, itemMax = 0;

                if (parts.Length >= 1)
                {
                    var single = parts[0].Split('|');
                    if (single.Length >= 2)
                    {
                        int.TryParse(single[0], out singleMin);
                        int.TryParse(single[1], out singleMax);
                    }
                }

                if (parts.Length >= 2)
                {
                    var item = parts[1].Split('|');
                    if (item.Length >= 2)
                    {
                        int.TryParse(item[0], out itemMin);
                        int.TryParse(item[1], out itemMax);
                    }
                }

                return (singleMin, singleMax, itemMin, itemMax);
            }
            catch { }

            return (0, 0, 0, 0);
        }

        #endregion
    }
}
