using System;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using WangShangLiaoBot.Models;

namespace WangShangLiaoBot.Services
{
    /// <summary>
    /// 配置服务 - 管理应用程序配置的加载和保存
    /// </summary>
    public class ConfigService
    {
        private static ConfigService _instance;
        private static readonly object _lock = new object();
        
        /// <summary>单例实例</summary>
        public static ConfigService Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                            _instance = new ConfigService();
                    }
                }
                return _instance;
            }
        }
        
        /// <summary>当前配置</summary>
        public AppConfig Config { get; private set; }
        
        /// <summary>配置文件路径</summary>
        private string ConfigPath => Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory, "config.xml");
        
        private ConfigService()
        {
            Config = new AppConfig();
        }
        
        /// <summary>
        /// 加载配置
        /// </summary>
        public void LoadConfig()
        {
            try
            {
                if (File.Exists(ConfigPath))
                {
                    var serializer = new XmlSerializer(typeof(AppConfig));
                    using (var reader = new StreamReader(ConfigPath))
                    {
                        Config = (AppConfig)serializer.Deserialize(reader);
                    }
                    EnsureDefaultKeywordRules();
                    Logger.Info("配置加载成功");
                }
                else
                {
                    Logger.Info("配置文件不存在，使用默认配置");
                    Config = new AppConfig();
                    EnsureDefaultKeywordRules();
                    SaveConfig();
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"加载配置失败: {ex.Message}");
                Config = new AppConfig();
                EnsureDefaultKeywordRules();
            }
        }

        /// <summary>
        /// Ensure default keyword rules exist (only when config is empty).
        /// </summary>
        private void EnsureDefaultKeywordRules()
        {
            if (Config == null) return;
            if (Config.KeywordRules == null)
                Config.KeywordRules = new System.Collections.Generic.List<KeywordReplyRule>();

            // Seed missing defaults based on the design draft screenshot.
            // Never overwrite existing user rules; only add the ones that are absent.
            AddDefaultKeywordRuleIfMissing("历史2|1s..", "历史：[开奖历史]");
            AddDefaultKeywordRuleIfMissing("kjl开奖I.", "开奖网址：http:..");
            AddDefaultKeywordRuleIfMissing("31今天数据", "[今天统计]");
            AddDefaultKeywordRuleIfMissing("1", "[下注]");
        }

        /// <summary>
        /// Add a default keyword rule if it does not exist yet.
        /// </summary>
        private void AddDefaultKeywordRuleIfMissing(string keyword, string reply)
        {
            if (string.IsNullOrWhiteSpace(keyword)) return;
            if (Config == null) return;
            if (Config.KeywordRules == null)
                Config.KeywordRules = new System.Collections.Generic.List<KeywordReplyRule>();

            var exists = Config.KeywordRules.Any(r => string.Equals(r.Keyword, keyword, StringComparison.OrdinalIgnoreCase));
            if (exists) return;

            Config.KeywordRules.Add(new KeywordReplyRule
            {
                Keyword = keyword,
                Reply = reply ?? "",
                Enabled = true
            });
        }
        
        /// <summary>
        /// 保存配置
        /// </summary>
        public void SaveConfig()
        {
            try
            {
                var serializer = new XmlSerializer(typeof(AppConfig));
                using (var writer = new StreamWriter(ConfigPath))
                {
                    serializer.Serialize(writer, Config);
                }
                Logger.Info("配置保存成功");
            }
            catch (Exception ex)
            {
                Logger.Error($"保存配置失败: {ex.Message}");
            }
        }
    }
}

