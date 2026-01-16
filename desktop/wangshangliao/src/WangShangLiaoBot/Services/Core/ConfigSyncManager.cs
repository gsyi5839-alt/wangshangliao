using System;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using WangShangLiaoBot.Models;
using WangShangLiaoBot.Services.HPSocket;

namespace WangShangLiaoBot.Services
{
    /// <summary>
    /// 配置同步管理器 - 将主框架的所有配置同步到副框架
    /// 在配置变更时自动同步，确保副框架拥有完整的功能设定
    /// </summary>
    public sealed class ConfigSyncManager
    {
        private static ConfigSyncManager _instance;
        public static ConfigSyncManager Instance => _instance ?? (_instance = new ConfigSyncManager());

        private readonly JavaScriptSerializer _serializer = new JavaScriptSerializer();
        private bool _autoSyncEnabled = true;

        public event Action<string> OnLog;

        private ConfigSyncManager()
        {
            _serializer.MaxJsonLength = int.MaxValue;
        }

        #region 自动同步控制

        /// <summary>
        /// 启用自动同步
        /// </summary>
        public void EnableAutoSync()
        {
            _autoSyncEnabled = true;
            Log("自动配置同步已启用");
        }

        /// <summary>
        /// 禁用自动同步
        /// </summary>
        public void DisableAutoSync()
        {
            _autoSyncEnabled = false;
            Log("自动配置同步已禁用");
        }

        #endregion

        #region 全量同步

        /// <summary>
        /// 同步全部配置到副框架
        /// </summary>
        public async Task<bool> SyncAllConfigAsync()
        {
            try
            {
                var client = FrameworkClient.Instance;
                if (!client.IsConnected)
                {
                    Log("副框架未连接，无法同步配置");
                    return false;
                }

                Log("开始同步全部配置...");

                var config = ConfigService.Instance.Config;
                if (config == null)
                {
                    Log("配置为空");
                    return false;
                }

                // 构建完整的同步配置
                var syncConfig = BuildSyncConfig(config);
                var configJson = _serializer.Serialize(syncConfig);

                var result = await client.SyncFullConfigAsync(configJson);
                
                if (result)
                {
                    Log($"全部配置同步成功 ({configJson.Length} 字节)");
                }
                else
                {
                    Log("全部配置同步失败");
                }

                return result;
            }
            catch (Exception ex)
            {
                Log($"同步全部配置异常: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 构建同步配置对象
        /// </summary>
        private SyncFullConfig BuildSyncConfig(AppConfig config)
        {
            return new SyncFullConfig
            {
                // 基本设置
                GroupId = config.GroupId,
                GroupName = config.GroupName,
                MyWangShangId = config.MyWangShangId,
                AdminWangWangId = config.AdminWangWangId,
                DebugPort = config.DebugPort,

                // 自动回复
                EnableAutoReply = config.EnableAutoReply,
                KeywordRules = config.KeywordRules,

                // 上下分
                UpScoreKeywords = config.UpScoreKeywords,
                DownScoreKeywords = config.DownScoreKeywords,
                MinRoundsBeforeDownScore = config.MinRoundsBeforeDownScore,
                MinScoreForSingleDown = config.MinScoreForSingleDown,

                // 赔率配置
                OddsConfig = new SyncOddsConfig
                {
                    DxdsOdds = config.DxdsOdds,
                    BigOddSmallEvenOdds = config.BigOddSmallEvenOdds,
                    BigEvenSmallOddOdds = config.BigEvenSmallOddOdds,
                    ExtremeOdds = config.ExtremeOdds,
                    DigitOdds = config.DigitOdds,
                    PairOdds = config.PairOdds,
                    StraightOdds = config.StraightOdds,
                    LeopardOdds = config.LeopardOdds,
                    HalfStraightOdds = config.HalfStraightOdds,
                    MixedOdds = config.MixedOdds,
                    EdgeOdds = config.EdgeOdds,
                    SumOdds = config.SumOdds,
                    CombinationOdds = config.CombinationOdds,
                    MiddleOdds = config.MiddleOdds,

                    // 龙虎
                    DragonTigerEnabled = config.DragonTigerEnabled,
                    DragonTigerOdds = config.DragonTigerOdds,
                    DragonTigerDrawOdds = config.DragonTigerDrawOdds,

                    // 尾球
                    TailBallEnabled = config.TailBallEnabled,
                    TailOdds1314BigSmall = config.TailOdds1314BigSmall,
                    TailOdds1314Combo = config.TailOdds1314Combo,

                    // 特殊规则
                    PairReturn = config.PairReturn,
                    SequenceReturn = config.SequenceReturn,
                    LeopardReturn = config.LeopardReturn,
                    LeopardKillAll = config.LeopardKillAll,

                    // 极数范围
                    ExtremeMax = config.ExtremeMax,
                    ExtremeMaxEnd = config.ExtremeMaxEnd,
                    ExtremeMin = config.ExtremeMin,
                    ExtremeMinEnd = config.ExtremeMinEnd
                },

                // 话术模板
                TemplateConfig = new SyncTemplateConfig
                {
                    NotArrivedText = config.NotArrivedText,
                    ZeroArrivedText = config.ZeroArrivedText,
                    HasScoreArrivedText = config.HasScoreArrivedText,
                    CheckScoreText = config.CheckScoreText,
                    ClientDownReplyContent = config.ClientDownReplyContent,
                    BetDisplay = config.InternalBetDisplay,
                    CancelBet = config.InternalCancelBet,
                    FuzzyRemind = config.InternalFuzzyRemind,
                    SealedUnprocessed = config.InternalSealedUnprocessed,
                    DataNoAttack = config.InternalDataNoAttack,
                    DataHasAttack = config.InternalDataHasAttack,
                    GroupRulesKeyword = config.InternalGroupRulesKeyword,
                    GroupRules = config.InternalGroupRules,
                    TailUnsealed = config.InternalTailUnsealed,
                    TailSealed = config.InternalTailSealed
                },

                LastSyncTime = DateTimeOffset.Now.ToUnixTimeMilliseconds()
            };
        }

        #endregion

        #region 分项同步

        /// <summary>
        /// 同步基本配置
        /// </summary>
        public async Task<bool> SyncBasicConfigAsync()
        {
            if (!_autoSyncEnabled) return false;

            try
            {
                var client = FrameworkClient.Instance;
                if (!client.IsConnected) return false;

                var config = ConfigService.Instance.Config;
                return await client.SyncBasicConfigAsync(
                    config.GroupId,
                    config.AdminWangWangId,
                    config.MyWangShangId,
                    config.DebugPort);
            }
            catch (Exception ex)
            {
                Log($"同步基本配置异常: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 同步赔率配置
        /// </summary>
        public async Task<bool> SyncOddsConfigAsync()
        {
            if (!_autoSyncEnabled) return false;

            try
            {
                var client = FrameworkClient.Instance;
                if (!client.IsConnected) return false;

                var config = ConfigService.Instance.Config;
                var oddsConfig = new SyncOddsConfig
                {
                    DxdsOdds = config.DxdsOdds,
                    BigOddSmallEvenOdds = config.BigOddSmallEvenOdds,
                    BigEvenSmallOddOdds = config.BigEvenSmallOddOdds,
                    ExtremeOdds = config.ExtremeOdds,
                    DigitOdds = config.DigitOdds,
                    PairOdds = config.PairOdds,
                    StraightOdds = config.StraightOdds,
                    LeopardOdds = config.LeopardOdds,
                    HalfStraightOdds = config.HalfStraightOdds,
                    MixedOdds = config.MixedOdds,
                    EdgeOdds = config.EdgeOdds,
                    SumOdds = config.SumOdds,
                    CombinationOdds = config.CombinationOdds,
                    MiddleOdds = config.MiddleOdds,
                    DragonTigerEnabled = config.DragonTigerEnabled,
                    DragonTigerOdds = config.DragonTigerOdds,
                    DragonTigerDrawOdds = config.DragonTigerDrawOdds,
                    TailBallEnabled = config.TailBallEnabled,
                    TailOdds1314BigSmall = config.TailOdds1314BigSmall,
                    TailOdds1314Combo = config.TailOdds1314Combo,
                    PairReturn = config.PairReturn,
                    SequenceReturn = config.SequenceReturn,
                    LeopardReturn = config.LeopardReturn,
                    LeopardKillAll = config.LeopardKillAll,
                    ExtremeMax = config.ExtremeMax,
                    ExtremeMaxEnd = config.ExtremeMaxEnd,
                    ExtremeMin = config.ExtremeMin,
                    ExtremeMinEnd = config.ExtremeMinEnd
                };

                var json = _serializer.Serialize(oddsConfig);
                return await client.SyncOddsConfigAsync(json);
            }
            catch (Exception ex)
            {
                Log($"同步赔率配置异常: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 同步自动回复配置
        /// </summary>
        public async Task<bool> SyncAutoReplyConfigAsync()
        {
            if (!_autoSyncEnabled) return false;

            try
            {
                var client = FrameworkClient.Instance;
                if (!client.IsConnected) return false;

                var config = ConfigService.Instance.Config;
                var rulesJson = _serializer.Serialize(config.KeywordRules ?? new System.Collections.Generic.List<KeywordReplyRule>());
                
                return await client.SyncAutoReplyConfigAsync(config.EnableAutoReply, rulesJson);
            }
            catch (Exception ex)
            {
                Log($"同步自动回复配置异常: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 同步话术模板
        /// </summary>
        public async Task<bool> SyncTemplateConfigAsync()
        {
            if (!_autoSyncEnabled) return false;

            try
            {
                var client = FrameworkClient.Instance;
                if (!client.IsConnected) return false;

                var config = ConfigService.Instance.Config;
                var template = new SyncTemplateConfig
                {
                    NotArrivedText = config.NotArrivedText,
                    ZeroArrivedText = config.ZeroArrivedText,
                    HasScoreArrivedText = config.HasScoreArrivedText,
                    CheckScoreText = config.CheckScoreText,
                    ClientDownReplyContent = config.ClientDownReplyContent,
                    BetDisplay = config.InternalBetDisplay,
                    CancelBet = config.InternalCancelBet,
                    FuzzyRemind = config.InternalFuzzyRemind,
                    SealedUnprocessed = config.InternalSealedUnprocessed,
                    DataNoAttack = config.InternalDataNoAttack,
                    DataHasAttack = config.InternalDataHasAttack,
                    GroupRulesKeyword = config.InternalGroupRulesKeyword,
                    GroupRules = config.InternalGroupRules,
                    TailUnsealed = config.InternalTailUnsealed,
                    TailSealed = config.InternalTailSealed
                };

                var json = _serializer.Serialize(template);
                return await client.SyncTemplateConfigAsync(json);
            }
            catch (Exception ex)
            {
                Log($"同步话术模板异常: {ex.Message}");
                return false;
            }
        }

        #endregion

        #region 开奖数据同步

        /// <summary>
        /// 发送开奖结果到副框架
        /// </summary>
        public async Task<bool> SendLotteryResultAsync(string period, int num1, int num2, int num3, int sum, int countdown)
        {
            try
            {
                var client = FrameworkClient.Instance;
                if (!client.IsConnected) return false;

                return await client.SendLotteryResultAsync(period, num1, num2, num3, sum, countdown);
            }
            catch (Exception ex)
            {
                Log($"发送开奖结果异常: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 发送封盘通知到副框架
        /// </summary>
        public async Task<bool> SendSealingNotifyAsync(string period, string content)
        {
            try
            {
                var client = FrameworkClient.Instance;
                if (!client.IsConnected) return false;

                return await client.SendSealingNotifyAsync(period, content);
            }
            catch (Exception ex)
            {
                Log($"发送封盘通知异常: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 发送封盘提醒到副框架
        /// </summary>
        public async Task<bool> SendReminderNotifyAsync(string period, int secondsToSeal, string content)
        {
            try
            {
                var client = FrameworkClient.Instance;
                if (!client.IsConnected) return false;

                return await client.SendReminderNotifyAsync(period, secondsToSeal, content);
            }
            catch (Exception ex)
            {
                Log($"发送封盘提醒异常: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 发送期号更新到副框架
        /// </summary>
        public async Task<bool> SendPeriodUpdateAsync(string currentPeriod, string nextPeriod, int countdown)
        {
            try
            {
                var client = FrameworkClient.Instance;
                if (!client.IsConnected) return false;

                return await client.SendPeriodUpdateAsync(currentPeriod, nextPeriod, countdown);
            }
            catch (Exception ex)
            {
                Log($"发送期号更新异常: {ex.Message}");
                return false;
            }
        }

        #endregion

        private void Log(string message)
        {
            var logMsg = $"[ConfigSync] {message}";
            Logger.Info(logMsg);
            OnLog?.Invoke(logMsg);
        }
    }

    #region 同步配置模型

    /// <summary>
    /// 全量同步配置
    /// </summary>
    [Serializable]
    public class SyncFullConfig
    {
        // 基本设置
        public string GroupId { get; set; }
        public string GroupName { get; set; }
        public string MyWangShangId { get; set; }
        public string AdminWangWangId { get; set; }
        public int DebugPort { get; set; }

        // 自动回复
        public bool EnableAutoReply { get; set; }
        public System.Collections.Generic.List<KeywordReplyRule> KeywordRules { get; set; }

        // 上下分
        public string UpScoreKeywords { get; set; }
        public string DownScoreKeywords { get; set; }
        public int MinRoundsBeforeDownScore { get; set; }
        public int MinScoreForSingleDown { get; set; }

        // 子配置
        public SyncOddsConfig OddsConfig { get; set; }
        public SyncSealingConfig SealingConfig { get; set; }
        public SyncTrusteeConfig TrusteeConfig { get; set; }
        public SyncTemplateConfig TemplateConfig { get; set; }

        public long LastSyncTime { get; set; }
    }

    /// <summary>
    /// 赔率同步配置
    /// </summary>
    [Serializable]
    public class SyncOddsConfig
    {
        public decimal DxdsOdds { get; set; }
        public decimal BigOddSmallEvenOdds { get; set; }
        public decimal BigEvenSmallOddOdds { get; set; }
        public decimal ExtremeOdds { get; set; }
        public decimal DigitOdds { get; set; }
        public decimal PairOdds { get; set; }
        public decimal StraightOdds { get; set; }
        public decimal LeopardOdds { get; set; }
        public decimal HalfStraightOdds { get; set; }
        public decimal MixedOdds { get; set; }
        public decimal EdgeOdds { get; set; }
        public decimal SumOdds { get; set; }
        public decimal CombinationOdds { get; set; }
        public decimal MiddleOdds { get; set; }

        public bool DragonTigerEnabled { get; set; }
        public decimal DragonTigerOdds { get; set; }
        public decimal DragonTigerDrawOdds { get; set; }

        public bool TailBallEnabled { get; set; }
        public decimal TailOdds1314BigSmall { get; set; }
        public decimal TailOdds1314Combo { get; set; }

        public bool PairReturn { get; set; }
        public bool SequenceReturn { get; set; }
        public bool LeopardReturn { get; set; }
        public bool LeopardKillAll { get; set; }

        public int ExtremeMax { get; set; }
        public int ExtremeMaxEnd { get; set; }
        public int ExtremeMin { get; set; }
        public int ExtremeMinEnd { get; set; }
    }

    /// <summary>
    /// 封盘同步配置
    /// </summary>
    [Serializable]
    public class SyncSealingConfig
    {
        public int LotteryType { get; set; }
        public int DrawIntervalSeconds { get; set; }
        public bool ReminderEnabled { get; set; }
        public int ReminderSeconds { get; set; }
        public string ReminderContent { get; set; }
        public int SealingSeconds { get; set; }
        public string SealingContent { get; set; }
        public bool AutoMute { get; set; }
        public int MuteBeforeSeconds { get; set; }
    }

    /// <summary>
    /// 托管同步配置
    /// </summary>
    [Serializable]
    public class SyncTrusteeConfig
    {
        public bool Enabled { get; set; }
        public int DelayAfterDraw { get; set; }
        public int DelayBeforeSeal { get; set; }
    }

    /// <summary>
    /// 话术模板同步配置
    /// </summary>
    [Serializable]
    public class SyncTemplateConfig
    {
        public string NotArrivedText { get; set; }
        public string ZeroArrivedText { get; set; }
        public string HasScoreArrivedText { get; set; }
        public string CheckScoreText { get; set; }
        public string ClientDownReplyContent { get; set; }
        public string BetDisplay { get; set; }
        public string CancelBet { get; set; }
        public string FuzzyRemind { get; set; }
        public string SealedUnprocessed { get; set; }
        public string DataNoAttack { get; set; }
        public string DataHasAttack { get; set; }
        public string GroupRulesKeyword { get; set; }
        public string GroupRules { get; set; }
        public string TailUnsealed { get; set; }
        public string TailSealed { get; set; }
    }

    #endregion
}
