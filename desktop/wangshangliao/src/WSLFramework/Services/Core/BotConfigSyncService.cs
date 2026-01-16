using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Web.Script.Serialization;

namespace WSLFramework.Services
{
    /// <summary>
    /// 机器人配置同步服务 - 从主框架接收并应用所有功能配置
    /// 包括：赔率设置、封盘设置、托管设置、回复模板等
    /// </summary>
    public sealed class BotConfigSyncService
    {
        private static BotConfigSyncService _instance;
        public static BotConfigSyncService Instance => _instance ?? (_instance = new BotConfigSyncService());

        private readonly JavaScriptSerializer _serializer = new JavaScriptSerializer();
        private SyncedBotConfig _config;
        private readonly object _lock = new object();

        public event Action<string> OnLog;
        public event Action<SyncedBotConfig> OnConfigUpdated;

        private BotConfigSyncService()
        {
            _config = new SyncedBotConfig();
            _serializer.MaxJsonLength = int.MaxValue;
        }

        private string ConfigPath => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "WSLFramework", "synced-bot-config.json");

        #region 配置访问

        /// <summary>
        /// 获取当前配置
        /// </summary>
        public SyncedBotConfig GetConfig()
        {
            lock (_lock)
            {
                return _config ?? new SyncedBotConfig();
            }
        }

        /// <summary>
        /// 获取赔率配置
        /// </summary>
        public ClassicOddsConfig GetOddsConfig()
        {
            return GetConfig().OddsConfig ?? new ClassicOddsConfig();
        }

        /// <summary>
        /// 获取封盘配置
        /// </summary>
        public SealingConfig GetSealingConfig()
        {
            return GetConfig().SealingConfig ?? new SealingConfig();
        }

        /// <summary>
        /// 获取托管配置
        /// </summary>
        public TrusteeConfig GetTrusteeConfig()
        {
            return GetConfig().TrusteeConfig ?? new TrusteeConfig();
        }

        #endregion

        #region 配置同步

        /// <summary>
        /// 从主框架同步配置 (JSON格式)
        /// </summary>
        public bool SyncFromJson(string configJson)
        {
            try
            {
                if (string.IsNullOrEmpty(configJson))
                {
                    Log("配置JSON为空");
                    return false;
                }

                var newConfig = _serializer.Deserialize<SyncedBotConfig>(configJson);
                if (newConfig == null)
                {
                    Log("配置解析失败");
                    return false;
                }

                lock (_lock)
                {
                    _config = newConfig;
                }

                // 应用配置到各个子服务
                ApplyConfigToServices();

                // 保存到本地
                SaveConfig();

                Log($"配置同步成功 - 群号:{_config.GroupId}, 彩种:{_config.SealingConfig?.LotteryType}");
                OnConfigUpdated?.Invoke(_config);
                return true;
            }
            catch (Exception ex)
            {
                Log($"配置同步异常: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 同步赔率配置
        /// </summary>
        public bool SyncOddsConfig(string oddsJson)
        {
            try
            {
                var odds = _serializer.Deserialize<ClassicOddsConfig>(oddsJson);
                if (odds != null)
                {
                    lock (_lock)
                    {
                        _config.OddsConfig = odds;
                    }
                    Log($"赔率配置同步成功 - 大小单双:{odds.DxdsOdds}, 数字:{odds.DigitOdds}");
                    SaveConfig();
                    return true;
                }
            }
            catch (Exception ex)
            {
                Log($"赔率配置同步异常: {ex.Message}");
            }
            return false;
        }

        /// <summary>
        /// 同步封盘配置
        /// </summary>
        public bool SyncSealingConfig(string sealingJson)
        {
            try
            {
                var sealing = _serializer.Deserialize<SealingConfig>(sealingJson);
                if (sealing != null)
                {
                    lock (_lock)
                    {
                        _config.SealingConfig = sealing;
                    }
                    Log($"封盘配置同步成功 - 彩种:{sealing.LotteryType}, 封盘秒:{sealing.SealingSeconds}");
                    SaveConfig();
                    
                    // 应用到封盘服务
                    ApplySealingConfig();
                    return true;
                }
            }
            catch (Exception ex)
            {
                Log($"封盘配置同步异常: {ex.Message}");
            }
            return false;
        }

        /// <summary>
        /// 同步托管配置
        /// </summary>
        public bool SyncTrusteeConfig(string trusteeJson)
        {
            try
            {
                var trustee = _serializer.Deserialize<TrusteeConfig>(trusteeJson);
                if (trustee != null)
                {
                    lock (_lock)
                    {
                        _config.TrusteeConfig = trustee;
                    }
                    Log($"托管配置同步成功 - 启用:{trustee.Enabled}, 策略数:{trustee.Strategies?.Count}");
                    SaveConfig();
                    return true;
                }
            }
            catch (Exception ex)
            {
                Log($"托管配置同步异常: {ex.Message}");
            }
            return false;
        }

        /// <summary>
        /// 同步基本设置
        /// </summary>
        public bool SyncBasicConfig(string groupId, string adminId, string myWangShangId, int debugPort)
        {
            lock (_lock)
            {
                _config.GroupId = groupId;
                _config.AdminWangWangId = adminId;
                _config.MyWangShangId = myWangShangId;
                _config.DebugPort = debugPort;
            }
            Log($"基本配置同步成功 - 群号:{groupId}, 管理员:{adminId}");
            SaveConfig();
            return true;
        }

        /// <summary>
        /// 同步自动回复设置
        /// </summary>
        public bool SyncAutoReplyConfig(bool enabled, List<KeywordReplyRule> rules)
        {
            lock (_lock)
            {
                _config.EnableAutoReply = enabled;
                _config.KeywordRules = rules ?? new List<KeywordReplyRule>();
            }
            Log($"自动回复配置同步成功 - 启用:{enabled}, 规则数:{rules?.Count ?? 0}");
            SaveConfig();
            
            // 应用到自动回复服务
            ApplyAutoReplyConfig();
            return true;
        }

        /// <summary>
        /// 同步自动回复设置 (JSON版本)
        /// </summary>
        public bool SyncAutoReplyConfigJson(bool enabled, string rulesJson)
        {
            try
            {
                var rules = string.IsNullOrEmpty(rulesJson) 
                    ? new List<KeywordReplyRule>() 
                    : _serializer.Deserialize<List<KeywordReplyRule>>(rulesJson) ?? new List<KeywordReplyRule>();
                return SyncAutoReplyConfig(enabled, rules);
            }
            catch (Exception ex)
            {
                Log($"自动回复配置解析异常: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 同步话术模板
        /// </summary>
        public bool SyncTemplateConfig(ReplyTemplateConfig template)
        {
            if (template == null) return false;
            
            lock (_lock)
            {
                _config.TemplateConfig = template;
            }
            Log($"话术模板配置同步成功");
            SaveConfig();
            return true;
        }

        /// <summary>
        /// 同步话术模板 (JSON版本)
        /// </summary>
        public bool SyncTemplateConfigJson(string templateJson)
        {
            try
            {
                if (string.IsNullOrEmpty(templateJson)) return false;
                var template = _serializer.Deserialize<ReplyTemplateConfig>(templateJson);
                return SyncTemplateConfig(template);
            }
            catch (Exception ex)
            {
                Log($"话术模板配置解析异常: {ex.Message}");
                return false;
            }
        }

        #endregion

        #region 配置应用

        /// <summary>
        /// 将配置应用到各个子服务
        /// </summary>
        private void ApplyConfigToServices()
        {
            ApplySealingConfig();
            ApplyAutoReplyConfig();
            ApplyScoreConfig();
        }

        /// <summary>
        /// 应用封盘配置 - 存储配置后通知更新事件
        /// </summary>
        private void ApplySealingConfig()
        {
            try
            {
                var sealing = _config?.SealingConfig;
                if (sealing == null) return;
                
                // 配置已保存，触发更新事件供外部服务订阅
                OnConfigUpdated?.Invoke(_config);
                Log($"封盘配置已保存 (彩种:{sealing.LotteryType}, 间隔:{sealing.DrawIntervalSeconds}秒)");
            }
            catch (Exception ex)
            {
                Log($"应用封盘配置异常: {ex.Message}");
            }
        }

        /// <summary>
        /// 应用自动回复配置 - 存储配置后通知更新事件
        /// </summary>
        private void ApplyAutoReplyConfig()
        {
            try
            {
                // 配置已保存，触发更新事件供外部服务订阅
                OnConfigUpdated?.Invoke(_config);
                Log($"自动回复配置已保存 (启用:{_config.EnableAutoReply}, 规则数:{_config.KeywordRules?.Count ?? 0})");
            }
            catch (Exception ex)
            {
                Log($"应用自动回复配置异常: {ex.Message}");
            }
        }

        /// <summary>
        /// 应用上下分配置 - 存储配置后通知更新事件
        /// </summary>
        private void ApplyScoreConfig()
        {
            try
            {
                // 配置已保存，触发更新事件供外部服务订阅
                OnConfigUpdated?.Invoke(_config);
                Log($"上下分配置已保存 (上分词:{_config.UpScoreKeywords}, 下分词:{_config.DownScoreKeywords})");
            }
            catch (Exception ex)
            {
                Log($"应用上下分配置异常: {ex.Message}");
            }
        }

        #endregion

        #region 持久化

        /// <summary>
        /// 加载本地配置
        /// </summary>
        public void LoadConfig()
        {
            try
            {
                if (!File.Exists(ConfigPath))
                {
                    Log("本地配置不存在，使用默认配置");
                    return;
                }

                var json = File.ReadAllText(ConfigPath, Encoding.UTF8);
                var config = _serializer.Deserialize<SyncedBotConfig>(json);
                
                if (config != null)
                {
                    lock (_lock)
                    {
                        _config = config;
                    }
                    ApplyConfigToServices();
                    Log($"本地配置加载成功 - 群号:{_config.GroupId}");
                }
            }
            catch (Exception ex)
            {
                Log($"加载本地配置异常: {ex.Message}");
            }
        }

        /// <summary>
        /// 保存配置到本地
        /// </summary>
        private void SaveConfig()
        {
            try
            {
                var dir = Path.GetDirectoryName(ConfigPath);
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                var json = _serializer.Serialize(_config);
                File.WriteAllText(ConfigPath, json, Encoding.UTF8);
            }
            catch (Exception ex)
            {
                Log($"保存配置异常: {ex.Message}");
            }
        }

        #endregion

        private void Log(string message)
        {
            OnLog?.Invoke($"[ConfigSync] {message}");
        }
    }

    #region 配置数据模型

    /// <summary>
    /// 同步的机器人配置 - 从主框架同步
    /// </summary>
    [Serializable]
    public class SyncedBotConfig
    {
        // ===== 基本设置 =====
        public string GroupId { get; set; } = "";
        public string GroupName { get; set; } = "";
        public string MyWangShangId { get; set; } = "";
        public string AdminWangWangId { get; set; } = "";
        public int DebugPort { get; set; } = 9222;
        
        // ===== 自动回复 =====
        public bool EnableAutoReply { get; set; } = false;
        public List<KeywordReplyRule> KeywordRules { get; set; } = new List<KeywordReplyRule>();
        
        // ===== 上下分设置 =====
        public string UpScoreKeywords { get; set; } = "查|c|。";
        public string DownScoreKeywords { get; set; } = "回";
        public int MinRoundsBeforeDownScore { get; set; } = 0;
        public int MinScoreForSingleDown { get; set; } = 30;
        
        // ===== 子配置 =====
        public ClassicOddsConfig OddsConfig { get; set; }
        public SealingConfig SealingConfig { get; set; }
        public TrusteeConfig TrusteeConfig { get; set; }
        public ReplyTemplateConfig TemplateConfig { get; set; }
        
        // ===== 同步时间戳 =====
        public long LastSyncTime { get; set; }
    }

    /// <summary>
    /// 经典玩法赔率配置
    /// </summary>
    [Serializable]
    public class ClassicOddsConfig
    {
        // 大小单双
        public decimal DxdsOdds { get; set; } = 1.8m;
        public decimal BigOddSmallEvenOdds { get; set; } = 5.0m;
        public decimal BigEvenSmallOddOdds { get; set; } = 5.0m;
        
        // 特殊玩法
        public decimal ExtremeOdds { get; set; } = 0m;
        public decimal DigitOdds { get; set; } = 9.0m;
        public decimal PairOdds { get; set; } = 2.0m;
        public decimal StraightOdds { get; set; } = 0m;
        public decimal LeopardOdds { get; set; } = 49.0m;
        public decimal HalfStraightOdds { get; set; } = 0m;
        public decimal MixedOdds { get; set; } = 0m;
        public decimal EdgeOdds { get; set; } = 5.0m;
        public decimal SumOdds { get; set; } = 49.0m;
        public decimal CombinationOdds { get; set; } = 1.2m;
        public decimal MiddleOdds { get; set; } = 0m;
        
        // 龙虎玩法
        public bool DragonTigerEnabled { get; set; } = false;
        public decimal DragonTigerOdds { get; set; } = 0.6m;
        public decimal DragonTigerDrawOdds { get; set; } = 0m;
        
        // 尾球玩法
        public bool TailBallEnabled { get; set; } = false;
        public decimal TailOdds1314BigSmall { get; set; } = 1.4m;
        public decimal TailOdds1314Combo { get; set; } = 3.8m;
        
        // 特殊规则
        public bool PairReturn { get; set; } = true;
        public bool SequenceReturn { get; set; } = true;
        public bool LeopardReturn { get; set; } = false;
        public bool LeopardKillAll { get; set; } = false;
        
        // 极数范围
        public int ExtremeMax { get; set; } = 22;
        public int ExtremeMaxEnd { get; set; } = 27;
        public int ExtremeMin { get; set; } = 0;
        public int ExtremeMinEnd { get; set; } = 5;
    }

    /// <summary>
    /// 封盘配置
    /// </summary>
    [Serializable]
    public class SealingConfig
    {
        public int LotteryType { get; set; } = 1; // 1=加拿大28, 2=比特28, 3=北京28
        public int DrawIntervalSeconds { get; set; } = 210;
        public string TeamId { get; set; }
        
        public bool ReminderEnabled { get; set; } = true;
        public int ReminderSeconds { get; set; } = 60;
        public string ReminderContent { get; set; } = "--距离封盘时间还有[封盘倒计时]秒--";
        
        public int SealingSeconds { get; set; } = 20;
        public string SealingContent { get; set; } = "========封盘线=======";
        
        public bool RuleEnabled { get; set; } = true;
        public int RuleSeconds { get; set; } = 1;
        public string RuleContent { get; set; } = "";
        
        public bool AutoMute { get; set; } = true;
        public int MuteBeforeSeconds { get; set; } = 5;
    }

    /// <summary>
    /// 托管配置
    /// </summary>
    [Serializable]
    public class TrusteeConfig
    {
        public bool Enabled { get; set; } = false;
        public int DelayAfterDraw { get; set; } = 10;
        public int DelayBeforeSeal { get; set; } = 5;
        public bool AutoDeposit { get; set; } = true;
        public bool AutoWithdraw { get; set; } = true;
        public List<TrusteeStrategy> Strategies { get; set; } = new List<TrusteeStrategy>();
    }

    /// <summary>
    /// 托管策略
    /// </summary>
    [Serializable]
    public class TrusteeStrategy
    {
        public decimal MinBalance { get; set; }
        public decimal MaxBalance { get; set; }
        public List<string> BetContents { get; set; } = new List<string>();
    }

    /// <summary>
    /// 关键词回复规则
    /// </summary>
    [Serializable]
    public class KeywordReplyRule
    {
        public string Keyword { get; set; }
        public string Reply { get; set; }
        public bool Enabled { get; set; } = true;
    }

    /// <summary>
    /// 回复话术模板配置
    /// </summary>
    [Serializable]
    public class ReplyTemplateConfig
    {
        // 上下分话术
        public string NotArrivedText { get; set; } = "[艾特] 没到，请您联系接单核实原因";
        public string ZeroArrivedText { get; set; } = "[艾特][分数]到[换行]操库:[余粮]";
        public string HasScoreArrivedText { get; set; } = "[艾特][分数]到[换行]操库:[余粮]";
        public string CheckScoreText { get; set; } = "[艾特][分数]查[换行]操库:[留分]";
        public string ClientDownReplyContent { get; set; } = "已收到[昵称][分数]请求，请稍等";
        
        // 下注话术
        public string BetDisplay { get; set; } = "[昵称]";
        public string CancelBet { get; set; } = "[昵称] 取消";
        public string FuzzyRemind { get; set; } = "[昵称] 余额不足，请上分后继续";
        public string SealedUnprocessed { get; set; } = "[昵称] 慢作业结束攻击要快！";
        
        // 查询话术
        public string DataNoAttack { get; set; } = "[昵称] $:[余粮]";
        public string DataHasAttack { get; set; } = "";
        
        // 群规话术
        public string GroupRulesKeyword { get; set; } = "群规|规则|新人|福利|玩法";
        public string GroupRules { get; set; } = "请遵守群规，谢谢！";
        
        // 尾巴话术
        public string TailUnsealed { get; set; } = "离考试结束还有[封盘倒计时]秒";
        public string TailSealed { get; set; } = "已封盘";
    }

    #endregion
}
