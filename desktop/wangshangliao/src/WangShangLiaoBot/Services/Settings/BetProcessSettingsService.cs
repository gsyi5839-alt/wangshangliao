using System;
using System.IO;
using System.Text;

namespace WangShangLiaoBot.Services
{
    /// <summary>
    /// Bet process settings service - manages all bet handling configuration
    /// Settings persist to file: 下注处理设置.ini
    /// </summary>
    public sealed class BetProcessSettingsService
    {
        private static BetProcessSettingsService _instance;
        public static BetProcessSettingsService Instance => _instance ?? (_instance = new BetProcessSettingsService());

        private readonly string _settingsFile;

        private BetProcessSettingsService()
        {
            _settingsFile = Path.Combine(DataService.Instance.DatabaseDir, "下注处理设置.ini");
            LoadFromFile();
        }

        #region 基本设置 - Row 1-2

        /// <summary>允许改加注</summary>
        public bool AllowModifyBet { get; set; } = true;

        /// <summary>禁止取消</summary>
        public bool ProhibitCancel { get; set; } = false;

        /// <summary>下注显示</summary>
        public bool ShowBet { get; set; } = true;

        /// <summary>变量先注后分</summary>
        public bool VariableBetLater { get; set; } = true;

        /// <summary>上分后自动处理之前下注</summary>
        public bool AutoProcessBeforeScore { get; set; } = false;

        /// <summary>发送封盘后上分不用处理之前下注</summary>
        public bool SendSealBeforeProcess { get; set; } = false;

        #endregion

        #region 重复下注处理 - Row 3-4

        /// <summary>
        /// 重复下注处理模式: 
        /// 0=算成加注(不推荐), 1=同注不算 不同等于加注, 2=算最后一次下注(推荐), 3=算前第一次下注
        /// </summary>
        public int RepeatBetMode { get; set; } = 2; // Default: 算最后一次下注

        #endregion

        #region 模糊匹配 - Row 5-6

        /// <summary>模糊匹配下注开/关</summary>
        public bool FuzzyMatchEnabled { get; set; } = true;

        /// <summary>模糊匹配支持提醒</summary>
        public bool FuzzyMatchSupportRemind { get; set; } = true;

        /// <summary>无账单下注提醒</summary>
        public bool NoBillRemindEnabled { get; set; } = true;

        /// <summary>无账单提醒内容</summary>
        public string NoBillRemindContent { get; set; } = "@QQ 无账单提醒[下注内容]";

        #endregion

        #region 组合下注无效 - Row 7-10

        /// <summary>杀组合下注无效</summary>
        public bool CombinationInvalidEnabled { get; set; } = true;

        /// <summary>杀组合无效提示</summary>
        public string CombinationInvalidMsg { get; set; } = "本群不支持相反攻击,攻击无效 请重新攻击!";

        /// <summary>多组合下注无效</summary>
        public bool MultiCombinationInvalidEnabled { get; set; } = true;

        /// <summary>多组合无效提示</summary>
        public string MultiCombinationInvalidMsg { get; set; } = "本群不支持多组合攻击,攻击无效 请重新攻击!";

        /// <summary>单注反下注无效</summary>
        public bool SingleOppositeInvalidEnabled { get; set; } = true;

        /// <summary>单注反下注无效提示</summary>
        public string SingleOppositeInvalidMsg { get; set; } = "@qq 本群不支持对下,变相对下攻击无效 请重新";

        /// <summary>最多组合限制启用</summary>
        public bool MaxCombinationEnabled { get; set; } = true;

        /// <summary>最多组合数量</summary>
        public int MaxCombinationCount { get; set; } = 3;

        /// <summary>超过组合限制提示</summary>
        public string MaxCombinationMsg { get; set; } = "@qq 本群不支持对下,变相对下攻击无效 请重新";

        #endregion

        #region 群开关 - Row 11-14

        /// <summary>开启仅支持拼音下注</summary>
        public bool PinyinBetOnly { get; set; } = false;

        /// <summary>
        /// 中文下注处理模式: 0=有效并提醒, 1=无效并提醒
        /// </summary>
        public int ChineseBetMode { get; set; } = 0;

        /// <summary>拼音提示内容</summary>
        public string PinyinRemindContent { get; set; } = "请拼音下柱谢谢";

        /// <summary>接收群聊下注</summary>
        public bool ReceiveGroupBet { get; set; } = false;

        /// <summary>自动禁言解禁群</summary>
        public bool AutoMuteUnmute { get; set; } = false;

        #endregion

        #region 好友开关 - Row 11-16 (Right side)

        /// <summary>开启好友私聊下注</summary>
        public bool EnableFriendChat { get; set; } = false;

        /// <summary>自动同意好友添加</summary>
        public bool AutoAgreeFriend { get; set; } = false;

        /// <summary>只接群成员下注</summary>
        public bool OnlyMemberBet { get; set; } = false;

        /// <summary>私聊下注不在群内反馈</summary>
        public bool FriendBetNotInGroup { get; set; } = false;

        /// <summary>私聊上下分不在群内反馈</summary>
        public bool FriendScoreNotInGroup { get; set; } = false;

        /// <summary>私聊词库『在』群内反馈</summary>
        public bool FriendQueryInGroup { get; set; } = false;

        #endregion

        #region 其他设置 - Row 15-16

        /// <summary>全局图片发送启用</summary>
        public bool GlobalImageSendEnabled { get; set; } = false;

        /// <summary>图片文字大小(像素)</summary>
        public int FontSize { get; set; } = 19;

        /// <summary>历史显示启用</summary>
        public bool HistoryShowEnabled { get; set; } = true;

        /// <summary>历史显示期数</summary>
        public int HistoryPeriodCount { get; set; } = 11;

        /// <summary>全局数字小写</summary>
        public bool GlobalDigitLower { get; set; } = false;

        /// <summary>大写数字采用①②③</summary>
        public bool UpperDigitUseCircled { get; set; } = false;

        #endregion

        #region Load/Save

        /// <summary>
        /// Load settings from file
        /// </summary>
        public void LoadFromFile()
        {
            try
            {
                if (!File.Exists(_settingsFile)) return;

                var lines = File.ReadAllLines(_settingsFile, Encoding.UTF8);
                foreach (var line in lines)
                {
                    if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#")) continue;
                    var idx = line.IndexOf('=');
                    if (idx <= 0) continue;

                    var key = line.Substring(0, idx).Trim();
                    var value = line.Substring(idx + 1).Trim();

                    switch (key)
                    {
                        // 基本设置
                        case "AllowModifyBet": AllowModifyBet = value == "1"; break;
                        case "ProhibitCancel": ProhibitCancel = value == "1"; break;
                        case "ShowBet": ShowBet = value == "1"; break;
                        case "VariableBetLater": VariableBetLater = value == "1"; break;
                        case "AutoProcessBeforeScore": AutoProcessBeforeScore = value == "1"; break;
                        case "SendSealBeforeProcess": SendSealBeforeProcess = value == "1"; break;

                        // 重复下注处理
                        case "RepeatBetMode": 
                            if (int.TryParse(value, out var rbm) && rbm >= 0 && rbm <= 3) 
                                RepeatBetMode = rbm; 
                            break;

                        // 模糊匹配
                        case "FuzzyMatchEnabled": FuzzyMatchEnabled = value == "1"; break;
                        case "FuzzyMatchSupportRemind": FuzzyMatchSupportRemind = value == "1"; break;
                        case "NoBillRemindEnabled": NoBillRemindEnabled = value == "1"; break;
                        case "NoBillRemindContent": NoBillRemindContent = value; break;

                        // 组合下注无效
                        case "CombinationInvalidEnabled": CombinationInvalidEnabled = value == "1"; break;
                        case "CombinationInvalidMsg": CombinationInvalidMsg = value; break;
                        case "MultiCombinationInvalidEnabled": MultiCombinationInvalidEnabled = value == "1"; break;
                        case "MultiCombinationInvalidMsg": MultiCombinationInvalidMsg = value; break;
                        case "SingleOppositeInvalidEnabled": SingleOppositeInvalidEnabled = value == "1"; break;
                        case "SingleOppositeInvalidMsg": SingleOppositeInvalidMsg = value; break;
                        case "MaxCombinationEnabled": MaxCombinationEnabled = value == "1"; break;
                        case "MaxCombinationCount": 
                            if (int.TryParse(value, out var mcc) && mcc >= 1 && mcc <= 10) 
                                MaxCombinationCount = mcc; 
                            break;
                        case "MaxCombinationMsg": MaxCombinationMsg = value; break;

                        // 群开关
                        case "PinyinBetOnly": PinyinBetOnly = value == "1"; break;
                        case "ChineseBetMode": 
                            if (int.TryParse(value, out var cbm) && cbm >= 0 && cbm <= 1) 
                                ChineseBetMode = cbm; 
                            break;
                        case "PinyinRemindContent": PinyinRemindContent = value; break;
                        case "ReceiveGroupBet": ReceiveGroupBet = value == "1"; break;
                        case "AutoMuteUnmute": AutoMuteUnmute = value == "1"; break;

                        // 好友开关
                        case "EnableFriendChat": EnableFriendChat = value == "1"; break;
                        case "AutoAgreeFriend": AutoAgreeFriend = value == "1"; break;
                        case "OnlyMemberBet": OnlyMemberBet = value == "1"; break;
                        case "FriendBetNotInGroup": FriendBetNotInGroup = value == "1"; break;
                        case "FriendScoreNotInGroup": FriendScoreNotInGroup = value == "1"; break;
                        case "FriendQueryInGroup": FriendQueryInGroup = value == "1"; break;

                        // 其他设置
                        case "GlobalImageSendEnabled": GlobalImageSendEnabled = value == "1"; break;
                        case "FontSize": 
                            if (int.TryParse(value, out var fs) && fs >= 8 && fs <= 72) 
                                FontSize = fs; 
                            break;
                        case "HistoryShowEnabled": HistoryShowEnabled = value == "1"; break;
                        case "HistoryPeriodCount": 
                            if (int.TryParse(value, out var hpc) && hpc >= 1 && hpc <= 100) 
                                HistoryPeriodCount = hpc; 
                            break;
                        case "GlobalDigitLower": GlobalDigitLower = value == "1"; break;
                        case "UpperDigitUseCircled": UpperDigitUseCircled = value == "1"; break;
                    }
                }

                Logger.Info("[BetProcessSettings] Settings loaded from file");
            }
            catch (Exception ex)
            {
                Logger.Error($"[BetProcessSettings] Load error: {ex.Message}");
            }
        }

        /// <summary>
        /// Save settings to file
        /// </summary>
        public void SaveToFile()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(_settingsFile));

                var sb = new StringBuilder();
                sb.AppendLine("# 下注处理设置");
                sb.AppendLine($"# 更新时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                sb.AppendLine();

                // 基本设置
                sb.AppendLine("# === 基本设置 ===");
                sb.AppendLine($"AllowModifyBet={B(AllowModifyBet)}");
                sb.AppendLine($"ProhibitCancel={B(ProhibitCancel)}");
                sb.AppendLine($"ShowBet={B(ShowBet)}");
                sb.AppendLine($"VariableBetLater={B(VariableBetLater)}");
                sb.AppendLine($"AutoProcessBeforeScore={B(AutoProcessBeforeScore)}");
                sb.AppendLine($"SendSealBeforeProcess={B(SendSealBeforeProcess)}");
                sb.AppendLine();

                // 重复下注处理
                sb.AppendLine("# === 重复下注处理 (0=算成加注, 1=同注不算, 2=算最后一次, 3=算第一次) ===");
                sb.AppendLine($"RepeatBetMode={RepeatBetMode}");
                sb.AppendLine();

                // 模糊匹配
                sb.AppendLine("# === 模糊匹配 ===");
                sb.AppendLine($"FuzzyMatchEnabled={B(FuzzyMatchEnabled)}");
                sb.AppendLine($"FuzzyMatchSupportRemind={B(FuzzyMatchSupportRemind)}");
                sb.AppendLine($"NoBillRemindEnabled={B(NoBillRemindEnabled)}");
                sb.AppendLine($"NoBillRemindContent={NoBillRemindContent}");
                sb.AppendLine();

                // 组合下注无效
                sb.AppendLine("# === 组合下注无效 ===");
                sb.AppendLine($"CombinationInvalidEnabled={B(CombinationInvalidEnabled)}");
                sb.AppendLine($"CombinationInvalidMsg={CombinationInvalidMsg}");
                sb.AppendLine($"MultiCombinationInvalidEnabled={B(MultiCombinationInvalidEnabled)}");
                sb.AppendLine($"MultiCombinationInvalidMsg={MultiCombinationInvalidMsg}");
                sb.AppendLine($"SingleOppositeInvalidEnabled={B(SingleOppositeInvalidEnabled)}");
                sb.AppendLine($"SingleOppositeInvalidMsg={SingleOppositeInvalidMsg}");
                sb.AppendLine($"MaxCombinationEnabled={B(MaxCombinationEnabled)}");
                sb.AppendLine($"MaxCombinationCount={MaxCombinationCount}");
                sb.AppendLine($"MaxCombinationMsg={MaxCombinationMsg}");
                sb.AppendLine();

                // 群开关
                sb.AppendLine("# === 群开关 ===");
                sb.AppendLine($"PinyinBetOnly={B(PinyinBetOnly)}");
                sb.AppendLine($"ChineseBetMode={ChineseBetMode}");
                sb.AppendLine($"PinyinRemindContent={PinyinRemindContent}");
                sb.AppendLine($"ReceiveGroupBet={B(ReceiveGroupBet)}");
                sb.AppendLine($"AutoMuteUnmute={B(AutoMuteUnmute)}");
                sb.AppendLine();

                // 好友开关
                sb.AppendLine("# === 好友开关 ===");
                sb.AppendLine($"EnableFriendChat={B(EnableFriendChat)}");
                sb.AppendLine($"AutoAgreeFriend={B(AutoAgreeFriend)}");
                sb.AppendLine($"OnlyMemberBet={B(OnlyMemberBet)}");
                sb.AppendLine($"FriendBetNotInGroup={B(FriendBetNotInGroup)}");
                sb.AppendLine($"FriendScoreNotInGroup={B(FriendScoreNotInGroup)}");
                sb.AppendLine($"FriendQueryInGroup={B(FriendQueryInGroup)}");
                sb.AppendLine();

                // 其他设置
                sb.AppendLine("# === 其他设置 ===");
                sb.AppendLine($"GlobalImageSendEnabled={B(GlobalImageSendEnabled)}");
                sb.AppendLine($"FontSize={FontSize}");
                sb.AppendLine($"HistoryShowEnabled={B(HistoryShowEnabled)}");
                sb.AppendLine($"HistoryPeriodCount={HistoryPeriodCount}");
                sb.AppendLine($"GlobalDigitLower={B(GlobalDigitLower)}");
                sb.AppendLine($"UpperDigitUseCircled={B(UpperDigitUseCircled)}");

                File.WriteAllText(_settingsFile, sb.ToString(), Encoding.UTF8);
                Logger.Info("[BetProcessSettings] Settings saved to file");
            }
            catch (Exception ex)
            {
                Logger.Error($"[BetProcessSettings] Save error: {ex.Message}");
            }
        }

        private static string B(bool val) => val ? "1" : "0";

        #endregion
    }
}

