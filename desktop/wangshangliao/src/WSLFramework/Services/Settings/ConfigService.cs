using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using WSLFramework.Protocol;
using WSLFramework.Utils;

namespace WSLFramework.Services
{
    /// <summary>
    /// 配置管理服务 - 兼容招财狗配置格式
    /// </summary>
    public class ConfigService
    {
        private static ConfigService _instance;
        public static ConfigService Instance => _instance ?? (_instance = new ConfigService());
        
        private readonly Dictionary<string, Dictionary<string, string>> _config;
        private string _configPath;
        
        public event Action<string> OnLog;
        
        // ========== 基础配置 ==========
        public string SoftwareName { get; set; } = "旺商聊框架";
        public ushort HpSocketPort { get; set; } = 14746;
        public int CdpPort { get; set; } = 9222;
        
        // ========== 账号配置 ==========
        public string Account { get; set; }
        public string Password { get; set; }
        public string GroupId { get; set; }
        public string Wwid { get; set; }
        public bool AutoLogin { get; set; }
        public bool RememberPassword { get; set; }
        public string RobotQQ { get; set; }        // 机器人QQ号
        public string AdminQQ { get; set; }         // 管理员QQ号
        public string BindGroupId { get; set; }    // 绑定群号
        
        // ========== 彩种配置 ==========
        public int LotteryType { get; set; } = 1;   // 彩种: 1=加拿大28
        public int Channel { get; set; } = 0;       // 通道
        
        // ========== 功能开关 (选择框) ==========
        public bool AutoUpScore { get; set; } = true;           // 托自动上分
        public bool AutoDownScore { get; set; } = true;         // 托自动下分
        public bool AutoReply { get; set; } = true;
        public bool GroupBetEnabled { get; set; } = true;       // 接收群聊下注开关
        public bool PrivateBetEnabled { get; set; } = true;     // 好友私聊下注开关
        public bool ShowImages { get; set; } = true;
        public bool SendLotteryImage { get; set; } = false;     // 开奖图片发送
        public bool SendBillImage { get; set; } = false;        // 账单2图片发送
        public bool ViolationRecall { get; set; } = true;       // 发言检测_违规撤回
        public bool KickBlacklist { get; set; } = true;         // 被群管理踢出自动加黑名单
        public bool RobotKickBlacklist { get; set; } = true;    // 被机器人踢出自动加黑名单
        public bool FuzzyMatchEnabled { get; set; } = true;     // 模糊匹配开关
        public bool FuzzyMatchAtRemind { get; set; } = false;   // 模糊匹配艾特提醒
        public bool NoBillBetRemind { get; set; } = true;       // 无账单下注提醒
        public bool Straight890910 { get; set; } = true;        // 顺子算890910
        public bool LockCardEnabled { get; set; } = true;       // 锁名片开关
        public bool LockCardRemindToGroup { get; set; } = true; // 锁名片_提醒发送到群
        public bool UpScoreSound { get; set; } = true;          // 上分提示音开关
        public bool DownScoreSound { get; set; } = true;        // 下分提示音开关
        public bool CustomerDownReply { get; set; } = true;     // 客户下分回复
        public bool DownScoreNoUrge { get; set; } = true;       // 下分勿催
        public bool LotterySound { get; set; } = true;          // 开奖提示音开关
        public bool SealSound { get; set; } = true;             // 封盘提示音开关
        public bool BetBeforeScore { get; set; } = true;        // 先注后分
        public bool AllowModifyBet { get; set; } = true;        // 允许改加注
        public bool DownScoreFeedback { get; set; } = false;    // 下分消息反馈
        public bool UpScoreFeedback { get; set; } = false;      // 上分消息反馈
        public bool PlayerQueryEnabled { get; set; } = true;    // 玩家查询今天数据_开关
        public bool AtScoreNoReason { get; set; } = true;       // 艾特分_私聊加分允许无理由
        public bool AutoAcceptFriend { get; set; } = true;      // 自动同意好友添加
        public bool PrivateReplyInGroup { get; set; } = true;   // 私聊词库在群内反馈
        public bool NumberUppercase { get; set; } = false;      // 数字大写类型
        public bool NumberLowercase { get; set; } = true;       // 全局数字小写
        public bool LockCardKick { get; set; } = true;          // 锁名片_超次数踢人
        public bool PasswordDisabled { get; set; } = true;      // 敏感操作_关闭密码
        public bool ScoreRepeatFilter { get; set; } = true;     // 上下分重复过滤
        public bool AtChangeNickname { get; set; } = false;     // 艾特变昵称
        public bool AutoProcessPendingBet { get; set; } = true; // 上分后自动处理之前下注
        public bool PinyinBetOnly { get; set; } = false;        // 仅支持拼音下注开关
        public bool TailBallEnabled { get; set; } = false;      // 尾球玩法开关
        public bool TailBallNo09 { get; set; } = true;          // 尾球禁止下09
        public bool LongHuBaoEnabled { get; set; } = true;      // 龙虎豹玩法开关
        public bool ImageMute { get; set; } = true;             // 发言检测_图片禁言
        public bool BSDSpecialRule { get; set; } = true;        // 豹顺对特殊规则_开关
        public bool BSDPairReturn { get; set; } = true;         // 豹顺对特殊规则_对子回本
        public bool BSDLeopardReturn { get; set; } = true;      // 豹顺对特殊规则_豹子回本
        public bool BSDStraightReturn { get; set; } = true;     // 豹顺对特殊规则_顺子回本
        public bool SingleDigitOdds { get; set; } = true;       // 单独数字赔率
        public bool ZeroScoreKeepBill { get; set; } = true;     // 零分不删除账单
        public bool GroupMemberOnly { get; set; } = false;      // 只接群里成员下注
        public bool TrusteeEnabled { get; set; } = true;        // 玩家托管开关
        public bool GuessNumberEnabled { get; set; } = true;    // 猜数字开关
        public bool BillAutoAcceptJoin { get; set; } = true;    // 账单玩家进群自动同意
        public bool BillHideLostPlayer { get; set; } = true;    // 账单不显示输光玩家
        public bool TwoSevenEnabled { get; set; } = true;       // 二七玩法_开关
        public bool TrusteeAutoJoin { get; set; } = true;       // 托自动同意进群
        
        // ========== 算账控制 ==========
        public bool AutoMuteOnSeal { get; set; } = false;       // 启停禁言群（开奖时自动禁言/解禁）
        public bool StopAfterCurrentPeriod { get; set; } = false; // 开完本期停
        public bool SupportNicknameChange { get; set; } = true; // 支持变昵称
        
        // ========== 封盘设置 ==========
        public int SealSeconds { get; set; } = 30;              // 封盘提前时间
        public int RemindSeconds { get; set; } = 40;            // 提醒时间
        public int RuleSeconds { get; set; } = 10;              // 发送规矩时间
        public int CheckSeconds { get; set; } = 20;             // 发送核对时间
        public bool SealRemindEnabled { get; set; } = true;     // 封盘提醒开关
        public bool SealRuleEnabled { get; set; } = true;       // 封盘发送规矩开关
        public bool BetDataSendEnabled { get; set; } = true;    // 下注数据发送开关
        public bool SendAfterLottery { get; set; } = true;      // 开奖后发送
        public string SealRemindContent { get; set; } = "--距离封盘时间还有40秒--\n改注加注带改 或者 加";
        public string SealContent { get; set; } = "==加封盘线==\n以上有钱的都接\n==庄显为准==";
        public string RuleContent { get; set; } = "本群如遇卡奖情况，十分钟官网没开奖，本期无效，无需纠结！！！！\n";
        public string BetDataContent { get; set; } = "核对\n-------------------\n";
        
        // ========== 下注限额 ==========
        public decimal MinBet { get; set; } = 10;               // 单注下限
        public decimal MaxBet { get; set; } = 50000;            // 单注上限
        public decimal MinComboBet { get; set; } = 10;          // 组合下限
        public decimal MaxComboBet { get; set; } = 30000;       // 组合上限
        public decimal MinDigitBet { get; set; } = 10;          // 数字下限
        public decimal MaxDigitBet { get; set; } = 20000;       // 数字上限
        public decimal MinLHBBet { get; set; } = 10;            // 龙虎下限
        public decimal MaxLHBBet { get; set; } = 10000;         // 龙虎上限
        public decimal MinPairBet { get; set; } = 10;           // 对子下限
        public decimal MaxPairBet { get; set; } = 10000;        // 对子上限
        public decimal MinStraightBet { get; set; } = 10;       // 顺子下限
        public decimal MaxStraightBet { get; set; } = 10000;    // 顺子上限
        public decimal MinLeopardBet { get; set; } = 10;        // 豹子下限
        public decimal MaxLeopardBet { get; set; } = 200;       // 豹子上限
        public decimal MaxTotalBet { get; set; } = 600000;      // 总额上限
        
        // ========== 赔率设置 ==========
        public decimal OddsSingle { get; set; } = 1.98m;        // 单注赔率
        public decimal OddsCombo { get; set; } = 1.98m;         // 组合赔率
        public decimal OddsDigit { get; set; } = 10m;           // 数字赔率
        public decimal OddsExtreme { get; set; } = 11m;         // 极数赔率
        public decimal OddsBigOdd { get; set; } = 5m;           // 大单赔率
        public decimal OddsSmallEven { get; set; } = 5m;        // 小双赔率
        public decimal OddsBigEven { get; set; } = 5m;          // 大双赔率
        public decimal OddsSmallOdd { get; set; } = 5m;         // 小单赔率
        public decimal OddsPair { get; set; } = 2m;             // 对子赔率
        public decimal OddsStraight { get; set; } = 11m;        // 顺子赔率
        public decimal OddsHalfStraight { get; set; } = 1.97m;  // 半顺赔率
        public decimal OddsLeopard { get; set; } = 59m;         // 豹子赔率
        public decimal OddsMixed { get; set; } = 2.2m;          // 杂赔率
        public decimal OddsLHB { get; set; } = 1.92m;           // 龙虎豹赔率
        
        // ========== 发言检测 ==========
        public int MuteWordCount { get; set; } = 1000;          // 字数禁言
        public int KickWordCount { get; set; } = 2000;          // 字数踢出
        public int MuteLineCount { get; set; } = 4;             // 行数禁言
        public int MuteTime { get; set; } = 100;                // 禁言时间
        public int MaxCardChange { get; set; } = 5;             // 最大改名片次数
        public int ImageKickCount { get; set; } = 3;            // 图片次数踢出
        
        // ========== 回水设置 ==========
        public int RebateMode { get; set; } = 0;                // 回水方式: 0=关闭, 1=组合比例, 2=下注次数, 3=流水, 4=输分
        public decimal RebatePercent { get; set; } = 1m;        // 默认返点百分比
        public int RebateBetCount { get; set; } = 1;            // 默认把数
        
        // ========== 托管设置 ==========
        public int TrusteeUpDelay1 { get; set; } = 5;           // 托上分延迟1
        public int TrusteeUpDelay2 { get; set; } = 100;         // 托上分延迟2
        public int TrusteeDownDelay1 { get; set; } = 100;       // 托下分延迟1
        public int TrusteeDownDelay2 { get; set; } = 200;       // 托下分延迟2
        
        // ========== 内部回复模板 ==========
        public string ReplyBetShow { get; set; } = "@[昵称] ([短ID])\n已录取{下注内容}，余粮:{余粮}";
        public string ReplyTrusteeSuccess { get; set; } = "@[昵称] ([短ID])\n已为您设置为托管成员";
        public string ReplyTrusteeCancelled { get; set; } = "@[昵称] ([短ID])\n已取消托管身份";
        public string ReplyBetCancelled { get; set; } = "@[昵称] ([短ID])\n取消下注成功！！！";
        public string ReplyDownProcessing { get; set; } = "@[昵称] ([短ID])\n下分正在处理中，请稍等！";
        public string ReplyBetNoDown { get; set; } = "@[昵称] ([短ID])\n正在下注，无法下分！";
        public string ReplyDownNoBet { get; set; } = "@[昵称] ([短ID])\n正在下分，暂无法下注！";
        public string ReplyAttackValid { get; set; } = "@[昵称] ([短ID])\n余额不足，上分后录取，{下注内容}";
        public string ReplyFuzzyMatch { get; set; } = "@[昵称] ([短ID])\n已为您模糊匹配下注:{匹配内容}，请检查后进行下注";
        public string ReplyBetClosed { get; set; } = "@[昵称] ([短ID])\n下注无效，下注已封，请等待下期\n剩余:{剩余时间}秒";
        public string ReplyUpNoArrived { get; set; } = "@[昵称] 老板还没到账请稍等片刻或联系管理！";
        public string ReplyUpArrived { get; set; } = "@[昵称] {额度}已到账，余粮:{余粮}，祝您好运发大财！";
        public string ReplyDownQuery { get; set; } = "@[昵称] 您的余粮:{余粮}，请在下期开奖后申请下分！";
        public string ReplyDownNoUrge { get; set; } = "@[昵称] 正在处理中请勿催促，三天内下分到账无需纠结！";
        public string ReplyCustomerDown { get; set; } = "@[昵称] 下分:{下分金额}正在处理中请等待确认！nn";
        public string ReplyQuery0Score { get; set; } = "@[昵称] ([短ID])\n查询：老板您的账户余额不足！\n当前余粮:0";
        public string ReplyQueryHasScore { get; set; } = "@[昵称] ([短ID])\n老板余粮还有{余粮}，余粮:{余粮}";
        public string ReplyQueryHasAttack { get; set; } = "@[昵称] ([短ID])\n已录取{下注内容}，余粮:{余粮}";
        public string ReplyJoinPrivate { get; set; } = "恭喜发财，私聊都是骗子，请认准管理。";
        public string ReplyPrivateTailNotSealed { get; set; } = "离作业做题结束还有[封盘倒计时]秒";
        public string ReplyPrivateTailSealed { get; set; } = "作业做题已结束";
        public string ReplyNo09 { get; set; } = "禁止点09";
        
        // ========== 扩展回复模板 (ZCG完整格式) ==========
        public string Reply0Score { get; set; } = "[艾特]([旺旺])\n老板，您的账户余额不足！\n当前余粮:[余粮]";
        public string ReplyHasScoreNoBet { get; set; } = "[艾特]([旺旺])\n您本次暂无攻擊,余粮:[余粮]";
        public string ReplyHasScoreHasBet { get; set; } = "[艾特]([旺旺])\n本次攻擊:[下注],余粮:[余粮]";
        public string ReplyDownMinBet { get; set; } = "[艾特]([旺旺])\n补给回粮后至少攻擊[目标次数]把起才可回粮,当前攻擊了[下注次数]把";
        public string ReplyDownMinAmount { get; set; } = "[艾特]([旺旺])\n低于[最低下分],请一次性回芬！";
        public string ReplyUpRequest { get; set; } = "[艾特]([旺旺])\n亲，您的上分请求我们正在火速审核~";
        public string ReplyDownReject { get; set; } = "[艾特] 拒绝吓芬，请您联系接单核实原因！";
        public string ReplyUpArrived0 { get; set; } = "[艾特][分数]到[换行]粮库:[余粮][换行]祝您大吉大利，旗开得胜！！";
        public string ReplyUpArrivedSecond { get; set; } = "恭喜老板上分成功,祝您多多爆庄";
        public string ReplyDownSecond { get; set; } = "欢迎老板再次提款~~";
        
        // ========== 账单发送模板 ==========
        public string BillFormat { get; set; } = "開:[一区]+[二区]+[三区]=[开奖号码] [大小单双] [豹顺对子] [龙虎豹]\n人數:[客户人数] 總分:[总分数]\n----------------------\n[账单]\n----------------------\nls：[开奖历史]\n龙虎豹ls：[龙虎历史]\n尾球ls：[尾球历史]\n豹顺对历史：[豹顺对历史]";
        public string LotteryFormat { get; set; } = "开:[一区]+[二区]+[三区]=[开奖号码] [大小单双] 第[期数]期";
        public string BetCheckFormat { get; set; } = "核对\n-------------------\n[下注核对]";
        
        // ========== 历史显示期数 ==========
        public int HistoryDisplayCount { get; set; } = 11;
        public int PlayerQueryInterval { get; set; } = 10;  // 玩家查询间隔(秒)
        
        // ========== 单独数字赔率 ==========
        public Dictionary<int, decimal> DigitOdds { get; set; } = new Dictionary<int, decimal>
        {
            { 0, 665m }, { 1, 99m }, { 2, 49m }, { 3, 39m }, { 4, 29m },
            { 5, 19m }, { 6, 16m }, { 7, 15m }, { 8, 14m }, { 9, 14m },
            { 10, 13m }, { 11, 12m }, { 12, 11m }, { 13, 10m }, { 14, 10m },
            { 15, 11m }, { 16, 12m }, { 17, 13m }, { 18, 14m }, { 19, 14m },
            { 20, 15m }, { 21, 16m }, { 22, 19m }, { 23, 29m }, { 24, 39m },
            { 25, 49m }, { 26, 99m }, { 27, 665m }
        };
        
        // ========== 自动回复关键词 ==========
        public string HistoryKeywords { get; set; } = "历史|发历史|开奖历史|历史发|发下历史|2";
        public string PersonalDataKeywords { get; set; } = "账单|数据|我有下注吗|下注情况|1";
        public string WeChatKeywords { get; set; } = "微信|发微信|微信多少|微信号";
        public string AlipayKeywords { get; set; } = "支付|支付宝|发支付宝|发支付|支付多少";
        public string CaifutongKeywords { get; set; } = "财富|发财富|财付通|发财付通|cf|CF";
        
        private ConfigService()
        {
            _config = new Dictionary<string, Dictionary<string, string>>();
            _configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.ini");
        }
        
        /// <summary>
        /// 获取数据目录路径 (zcg目录)
        /// </summary>
        public string DataDir => Path.Combine(Path.GetDirectoryName(_configPath) ?? AppDomain.CurrentDomain.BaseDirectory, "zcg");
        
        /// <summary>
        /// 获取设置.ini路径 (ZCG加密格式)
        /// </summary>
        public string SettingsIniPath => Path.Combine(DataDir, "设置.ini");
        
        /// <summary>
        /// 获取登录配置.ini路径
        /// </summary>
        public string LoginConfigIniPath => Path.Combine(DataDir, "登录配置.ini");
        
        /// <summary>
        /// 加载配置
        /// </summary>
        public void Load(string path = null)
        {
            if (!string.IsNullOrEmpty(path))
                _configPath = path;
                
            if (!File.Exists(_configPath))
            {
                Log($"配置文件不存在: {_configPath}，创建默认配置");
                CreateDefaultConfig();
            }
            
            try
            {
                _config.Clear();
                var currentSection = "";
                
                // 使用 GBK 编码读取配置文件 (兼容ZCG)
                var lines = File.ReadAllLines(_configPath, Encoding.GetEncoding("GBK"));
                
                foreach (var line in lines)
                {
                    var trimmed = line.Trim();
                    if (string.IsNullOrEmpty(trimmed)) continue;
                    
                    // 节名
                    if (trimmed.StartsWith("[") && trimmed.EndsWith("]"))
                    {
                        currentSection = trimmed.Substring(1, trimmed.Length - 2);
                        if (!_config.ContainsKey(currentSection))
                            _config[currentSection] = new Dictionary<string, string>();
                        continue;
                    }
                    
                    // 键值对
                    var eqIndex = trimmed.IndexOf('=');
                    if (eqIndex > 0 && !string.IsNullOrEmpty(currentSection))
                    {
                        var key = trimmed.Substring(0, eqIndex);
                        var value = trimmed.Substring(eqIndex + 1);
                        _config[currentSection][key] = value;
                    }
                }
                
                // 解析到属性
                ParseConfigToProperties();
                Log($"配置已加载: {_configPath}");
                
                // 尝试加载ZCG设置文件
                if (File.Exists(SettingsIniPath))
                {
                    LoadZCGSettings(SettingsIniPath);
                }
            }
            catch (Exception ex)
            {
                Log($"加载配置失败: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 创建默认配置文件
        /// </summary>
        private void CreateDefaultConfig()
        {
            try
            {
                var sb = new StringBuilder();
                sb.AppendLine("[登录设置]");
                sb.AppendLine("软件名=旺商聊框架");
                sb.AppendLine("协议勾选=1");
                sb.AppendLine("账号=");
                sb.AppendLine("密码=");
                sb.AppendLine("选择框=1");
                sb.AppendLine("群号=");
                sb.AppendLine("记住密码=0");
                sb.AppendLine("wwid=");
                sb.AppendLine("自动登录=0");
                sb.AppendLine();
                sb.AppendLine("[nim]");
                sb.AppendLine("版本=1");
                sb.AppendLine("尼称=0");
                sb.AppendLine();
                sb.AppendLine("[端口]");
                sb.AppendLine("端口=14746");
                
                File.WriteAllText(_configPath, sb.ToString(), Encoding.GetEncoding("GBK"));
                Log($"已创建默认配置文件: {_configPath}");
            }
            catch (Exception ex)
            {
                Log($"创建默认配置失败: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 保存配置
        /// </summary>
        public void Save(string path = null)
        {
            if (!string.IsNullOrEmpty(path))
                _configPath = path;
                
            try
            {
                // 更新配置字典
                PropertiesToConfig();
                
                var sb = new StringBuilder();
                
                foreach (var section in _config)
                {
                    sb.AppendLine($"[{section.Key}]");
                    foreach (var kv in section.Value)
                    {
                        sb.AppendLine($"{kv.Key}={kv.Value}");
                    }
                    sb.AppendLine();
                }
                
                // 使用 GBK 编码保存 (兼容ZCG)
                File.WriteAllText(_configPath, sb.ToString(), Encoding.GetEncoding("GBK"));
                Log($"配置已保存: {_configPath}");
            }
            catch (Exception ex)
            {
                Log($"保存配置失败: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 保存ZCG设置文件 (加密格式)
        /// </summary>
        public void SaveZCGSettings()
        {
            try
            {
                if (!Directory.Exists(DataDir))
                    Directory.CreateDirectory(DataDir);
                    
                var sb = new StringBuilder();
                
                // 选项区
                sb.AppendLine("[选项]");
                sb.AppendLine($"选项_是否自动上分={ZCGCrypto.EncryptBool(AutoUpScore)}");
                sb.AppendLine($"选项_是否自动下分={ZCGCrypto.EncryptBool(AutoDownScore)}");
                sb.AppendLine($"选项_发言检测_违规撤回={ZCGCrypto.EncryptBool(ViolationRecall)}");
                sb.AppendLine($"选项_模糊匹配开关={ZCGCrypto.EncryptBool(FuzzyMatchEnabled)}");
                sb.AppendLine($"选项_无账单下注提醒={ZCGCrypto.EncryptBool(NoBillBetRemind)}");
                sb.AppendLine($"选项_锁名片开关={ZCGCrypto.EncryptBool(LockCardEnabled)}");
                sb.AppendLine($"选项_龙虎豹玩法开关={ZCGCrypto.EncryptBool(LongHuBaoEnabled)}");
                sb.AppendLine($"选项_尾球玩法开关={ZCGCrypto.EncryptBool(TailBallEnabled)}");
                sb.AppendLine();
                
                // 编辑框区
                sb.AppendLine("[编辑框]");
                sb.AppendLine($"编辑框_机器人QQ={ZCGCrypto.Encrypt(RobotQQ ?? "")}");
                sb.AppendLine($"编辑框_管理QQ号={ZCGCrypto.Encrypt(AdminQQ ?? "")}");
                sb.AppendLine($"编辑框_接群号={ZCGCrypto.Encrypt(BindGroupId ?? "")}");
                sb.AppendLine($"编辑框_单注下限={ZCGCrypto.Encrypt(MinBet.ToString())}");
                sb.AppendLine($"编辑框_单注上限={ZCGCrypto.Encrypt(MaxBet.ToString())}");
                sb.AppendLine();
                
                // 封盘设置区
                sb.AppendLine("[封盘设置]");
                sb.AppendLine($"封盘提前时间={ZCGCrypto.Encrypt(SealSeconds.ToString())}");
                sb.AppendLine($"提醒时间={ZCGCrypto.Encrypt(RemindSeconds.ToString())}");
                sb.AppendLine($"发送规矩时间={ZCGCrypto.Encrypt(RuleSeconds.ToString())}");
                sb.AppendLine($"核对时间={ZCGCrypto.Encrypt(CheckSeconds.ToString())}");
                sb.AppendLine($"封盘提醒开关={ZCGCrypto.EncryptBool(SealRemindEnabled)}");
                sb.AppendLine();
                
                // 赔率区
                sb.AppendLine("[赔率]");
                sb.AppendLine($"下注={ZCGCrypto.Encrypt(((int)(OddsSingle * 100)).ToString())}");
                sb.AppendLine($"组合={ZCGCrypto.Encrypt(((int)(OddsCombo * 100)).ToString())}");
                sb.AppendLine($"数字={ZCGCrypto.Encrypt(((int)(OddsDigit * 100)).ToString())}");
                sb.AppendLine($"极数={ZCGCrypto.Encrypt(((int)(OddsExtreme * 100)).ToString())}");
                sb.AppendLine($"对子={ZCGCrypto.Encrypt(((int)(OddsPair * 100)).ToString())}");
                sb.AppendLine($"顺子={ZCGCrypto.Encrypt(((int)(OddsStraight * 100)).ToString())}");
                sb.AppendLine($"豹子={ZCGCrypto.Encrypt(((int)(OddsLeopard * 100)).ToString())}");
                sb.AppendLine($"龙虎={ZCGCrypto.Encrypt(((int)(OddsLHB * 100)).ToString())}");
                sb.AppendLine();
                
                // 内部回复区
                sb.AppendLine("[内部回复]");
                sb.AppendLine($"下注提示={ZCGCrypto.Encrypt(ReplyBetShow)}");
                sb.AppendLine($"托管成功={ZCGCrypto.Encrypt(ReplyTrusteeSuccess)}");
                sb.AppendLine($"取消托管={ZCGCrypto.Encrypt(ReplyTrusteeCancelled)}");
                sb.AppendLine($"取消下注={ZCGCrypto.Encrypt(ReplyBetCancelled)}");
                sb.AppendLine($"下分处理中={ZCGCrypto.Encrypt(ReplyDownProcessing)}");
                sb.AppendLine($"查余额_0分={ZCGCrypto.Encrypt(ReplyQuery0Score)}");
                sb.AppendLine($"查余额_有分={ZCGCrypto.Encrypt(ReplyQueryHasScore)}");
                
                File.WriteAllText(SettingsIniPath, sb.ToString(), Encoding.GetEncoding("GBK"));
                Log($"ZCG设置已保存: {SettingsIniPath}");
            }
            catch (Exception ex)
            {
                Log($"保存ZCG设置失败: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 解析配置到属性
        /// </summary>
        private void ParseConfigToProperties()
        {
            // 基础设置
            if (TryGetValue("基础设置", "软件名", out var name))
                SoftwareName = ZCGCrypto.Decrypt(name);
                
            // 端口
            if (TryGetValue("端口", "端口", out var port) && int.TryParse(ZCGCrypto.Decrypt(port), out var p))
                HpSocketPort = (ushort)p;
                
            // 账号
            if (TryGetValue("登录", "账号", out var account))
                Account = ZCGCrypto.Decrypt(account);
            if (TryGetValue("登录", "密码", out var pwd))
                Password = ZCGCrypto.Decrypt(pwd);
            if (TryGetValue("登录", "自动登录", out var autoLogin))
                AutoLogin = ZCGCrypto.DecryptBool(autoLogin);
                
            // 选项
            if (TryGetValue("选项", "选项_是否自动上分", out var autoUp))
                AutoUpScore = ZCGCrypto.DecryptBool(autoUp);
            if (TryGetValue("选项", "选项_是否自动下分", out var autoDown))
                AutoDownScore = ZCGCrypto.DecryptBool(autoDown);
        }
        
        /// <summary>
        /// 属性转配置
        /// </summary>
        private void PropertiesToConfig()
        {
            SetValue("基础设置", "软件名", ZCGCrypto.Encrypt(SoftwareName));
            SetValue("端口", "端口", ZCGCrypto.Encrypt(HpSocketPort.ToString()));
            SetValue("登录", "账号", ZCGCrypto.Encrypt(Account ?? ""));
            SetValue("登录", "自动登录", ZCGCrypto.EncryptBool(AutoLogin));
            SetValue("选项", "选项_是否自动上分", ZCGCrypto.EncryptBool(AutoUpScore));
            SetValue("选项", "选项_是否自动下分", ZCGCrypto.EncryptBool(AutoDownScore));
        }
        
        /// <summary>
        /// 获取配置值
        /// </summary>
        public bool TryGetValue(string section, string key, out string value)
        {
            value = null;
            if (_config.TryGetValue(section, out var sectionDict))
            {
                return sectionDict.TryGetValue(key, out value);
            }
            return false;
        }
        
        /// <summary>
        /// 获取配置值 (解密)
        /// </summary>
        public string GetDecryptedValue(string section, string key, string defaultValue = "")
        {
            if (TryGetValue(section, key, out var value))
            {
                return ZCGCrypto.Decrypt(value);
            }
            return defaultValue;
        }
        
        /// <summary>
        /// 获取布尔配置 (解密)
        /// </summary>
        public bool GetBoolValue(string section, string key, bool defaultValue = false)
        {
            if (TryGetValue(section, key, out var value))
            {
                return ZCGCrypto.DecryptBool(value);
            }
            return defaultValue;
        }
        
        /// <summary>
        /// 设置配置值
        /// </summary>
        public void SetValue(string section, string key, string value)
        {
            if (!_config.ContainsKey(section))
                _config[section] = new Dictionary<string, string>();
            _config[section][key] = value;
        }
        
        /// <summary>
        /// 设置加密值
        /// </summary>
        public void SetEncryptedValue(string section, string key, string plainValue)
        {
            SetValue(section, key, ZCGCrypto.Encrypt(plainValue));
        }
        
        /// <summary>
        /// 设置布尔值
        /// </summary>
        public void SetBoolValue(string section, string key, bool value)
        {
            SetValue(section, key, ZCGCrypto.EncryptBool(value));
        }
        
        /// <summary>
        /// 加载招财狗设置文件
        /// </summary>
        public void LoadZCGSettings(string path)
        {
            if (!File.Exists(path))
            {
                Log($"ZCG设置文件不存在: {path}");
                return;
            }
            
            try
            {
                var lines = File.ReadAllLines(path, Encoding.GetEncoding("GBK"));
                var section = "";
                
                foreach (var line in lines)
                {
                    var trimmed = line.Trim();
                    if (string.IsNullOrEmpty(trimmed)) continue;
                    
                    if (trimmed.StartsWith("[") && trimmed.EndsWith("]"))
                    {
                        section = trimmed.Substring(1, trimmed.Length - 2);
                        continue;
                    }
                    
                    var eq = trimmed.IndexOf('=');
                    if (eq > 0)
                    {
                        var key = trimmed.Substring(0, eq);
                        var value = trimmed.Substring(eq + 1);
                        SetValue(section, key, value);
                        
                        // 解析到属性
                        ParseZCGValue(section, key, value);
                    }
                }
                
                Log($"已加载ZCG设置: {path}");
            }
            catch (Exception ex)
            {
                Log($"加载ZCG设置失败: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 解析ZCG配置值到属性
        /// </summary>
        private void ParseZCGValue(string section, string key, string value)
        {
            // ZCG使用 "真/假" 表示布尔值
            bool ParseBool(string v) => v == "真" || v == "1" || v.ToLower() == "true";
            decimal ParseDecimal(string v) => decimal.TryParse(v, out var d) ? d : 0;
            int ParseInt(string v) => int.TryParse(v, out var i) ? i : 0;
            
            switch (section)
            {
                case "彩种":
                    if (key == "彩种") LotteryType = ParseInt(value);
                    else if (key == "通道") Channel = ParseInt(value);
                    break;
                    
                case "选择框":
                    if (key == "选择框_开奖图片发送") SendLotteryImage = ParseBool(value);
                    else if (key == "选择框_账单2图片发送") SendBillImage = ParseBool(value);
                    else if (key == "选择框_托自动上分") AutoUpScore = ParseBool(value);
                    else if (key == "选择框_托自动下分") AutoDownScore = ParseBool(value);
                    else if (key == "选择框_发言检测_违规撤回") ViolationRecall = ParseBool(value);
                    else if (key == "选择框_被群管理踢出自动加黑名单") KickBlacklist = ParseBool(value);
                    else if (key == "选择框_被机器人踢出自动加黑名单") RobotKickBlacklist = ParseBool(value);
                    else if (key == "选择框_接收群聊下注开关") GroupBetEnabled = ParseBool(value);
                    else if (key == "选择框_模糊匹配开关") FuzzyMatchEnabled = ParseBool(value);
                    else if (key == "选择框_模糊匹配艾特提醒") FuzzyMatchAtRemind = ParseBool(value);
                    else if (key == "选择框_无账单下注提醒") NoBillBetRemind = ParseBool(value);
                    else if (key == "选择框_顺子算890910") Straight890910 = ParseBool(value);
                    else if (key == "选择框_锁名片开关") LockCardEnabled = ParseBool(value);
                    else if (key == "选择框_锁名片_提醒发送到群") LockCardRemindToGroup = ParseBool(value);
                    else if (key == "选择框_上分提示音开关") UpScoreSound = ParseBool(value);
                    else if (key == "选择框_下分提示音开关") DownScoreSound = ParseBool(value);
                    else if (key == "选择框_客户下分回复") CustomerDownReply = ParseBool(value);
                    else if (key == "选择框_下分勿催") DownScoreNoUrge = ParseBool(value);
                    else if (key == "选择框_开奖提示音开关") LotterySound = ParseBool(value);
                    else if (key == "选择框_封盘提示音开关") SealSound = ParseBool(value);
                    else if (key == "选择框_基本设置_变量先注后分") BetBeforeScore = ParseBool(value);
                    else if (key == "选择框_基本设置_允许改加注") AllowModifyBet = ParseBool(value);
                    else if (key == "选择框_下分消息反馈") DownScoreFeedback = ParseBool(value);
                    else if (key == "选择框_上分消息反馈") UpScoreFeedback = ParseBool(value);
                    else if (key == "选择框_玩家查询今天数据_开关") PlayerQueryEnabled = ParseBool(value);
                    else if (key == "选择框_艾特分_私聊加分允许无理由") AtScoreNoReason = ParseBool(value);
                    else if (key == "选择框_好友私聊下注开关") PrivateBetEnabled = ParseBool(value);
                    else if (key == "选择框_自动同意好友添加") AutoAcceptFriend = ParseBool(value);
                    else if (key == "选择框_私聊词库在群内反馈") PrivateReplyInGroup = ParseBool(value);
                    else if (key == "选择框_基本设置_数字大写类型") NumberUppercase = ParseBool(value);
                    else if (key == "选择框_基本设置_全局数字小写") NumberLowercase = ParseBool(value);
                    else if (key == "选择框_锁名片_超次数踢人") LockCardKick = ParseBool(value);
                    else if (key == "选择框_敏感操作_关闭密码") PasswordDisabled = ParseBool(value);
                    else if (key == "选择框_上下分重复过滤") ScoreRepeatFilter = ParseBool(value);
                    else if (key == "选择框_仅支持拼音下注开关") PinyinBetOnly = ParseBool(value);
                    else if (key == "选择框_尾球玩法开关") TailBallEnabled = ParseBool(value);
                    else if (key == "选择框_尾球禁止下09") TailBallNo09 = ParseBool(value);
                    else if (key == "选择框_龙虎豹玩法开关") LongHuBaoEnabled = ParseBool(value);
                    else if (key == "选择框_发言检测_图片禁言") ImageMute = ParseBool(value);
                    else if (key == "选择框_豹顺对特殊规则_开关") BSDSpecialRule = ParseBool(value);
                    else if (key == "选择框_豹顺对特殊规则_对子回本") BSDPairReturn = ParseBool(value);
                    else if (key == "选择框_豹顺对特殊规则_豹子回本") BSDLeopardReturn = ParseBool(value);
                    else if (key == "选择框_豹顺对特殊规则_顺子回本") BSDStraightReturn = ParseBool(value);
                    else if (key == "选择框_单独数字赔率") SingleDigitOdds = ParseBool(value);
                    else if (key == "选择框_零分不删除账单") ZeroScoreKeepBill = ParseBool(value);
                    else if (key == "选择框_只接群里成员下注") GroupMemberOnly = ParseBool(value);
                    else if (key == "选择框_玩家托管开关") TrusteeEnabled = ParseBool(value);
                    else if (key == "选择框_猜数字开关") GuessNumberEnabled = ParseBool(value);
                    else if (key == "选择框_账单玩家进群自动同意") BillAutoAcceptJoin = ParseBool(value);
                    else if (key == "选择框_账单不显示输光玩家") BillHideLostPlayer = ParseBool(value);
                    else if (key == "选择框_二七玩法_开关") TwoSevenEnabled = ParseBool(value);
                    else if (key == "选择框_托自动同意进群") TrusteeAutoJoin = ParseBool(value);
                    break;
                    
                case "编辑框":
                    if (key == "编辑框_机器人QQ") RobotQQ = value;
                    else if (key == "编辑框_管理QQ号码") AdminQQ = value;
                    else if (key == "编辑框_绑定群号") BindGroupId = value;
                    else if (key == "编辑框_发言检测_字数禁言") MuteWordCount = ParseInt(value);
                    else if (key == "编辑框_发言检测_字数踢出") KickWordCount = ParseInt(value);
                    else if (key == "编辑框_发言检测_行数禁言") MuteLineCount = ParseInt(value);
                    else if (key == "编辑框_发言检测_禁言时间") MuteTime = ParseInt(value);
                    else if (key == "编辑框_发言检测_图片次数踢出") ImageKickCount = ParseInt(value);
                    else if (key == "编辑框_锁名片_最大改名片次数") MaxCardChange = ParseInt(value);
                    else if (key == "编辑框_托上分延迟1") TrusteeUpDelay1 = ParseInt(value);
                    else if (key == "编辑框_托上分延迟2") TrusteeUpDelay2 = ParseInt(value);
                    else if (key == "编辑框_托下分延迟1") TrusteeDownDelay1 = ParseInt(value);
                    else if (key == "编辑框_托下分延迟2") TrusteeDownDelay2 = ParseInt(value);
                    else if (key == "编辑框_普通赔率_单注") OddsSingle = ParseDecimal(value);
                    else if (key == "编辑框_普通赔率_大双小单") OddsBigEven = ParseDecimal(value);
                    else if (key == "编辑框_普通赔率_大单小双") OddsBigOdd = ParseDecimal(value);
                    else if (key == "编辑框_普通赔率_极大极小") OddsExtreme = ParseDecimal(value);
                    else if (key == "编辑框_普通赔率_数字") OddsDigit = ParseDecimal(value);
                    else if (key == "编辑框_普通赔率_对子") OddsPair = ParseDecimal(value);
                    else if (key == "编辑框_普通赔率_顺子") OddsStraight = ParseDecimal(value);
                    else if (key == "编辑框_普通赔率_半顺") OddsHalfStraight = ParseDecimal(value);
                    else if (key == "编辑框_普通赔率_豹子") OddsLeopard = ParseDecimal(value);
                    else if (key == "编辑框_普通赔率_杂") OddsMixed = ParseDecimal(value);
                    else if (key == "编辑框_龙虎豹赔率") OddsLHB = ParseDecimal(value);
                    else if (key == "编辑框_单点最高赔付") MaxTotalBet = ParseDecimal(value);
                    else if (key == "编辑框_玩家查询今天数据_时间间隔") PlayerQueryInterval = ParseInt(value);
                    else if (key == "编辑框_基本设置_历史显示期数") HistoryDisplayCount = ParseInt(value);
                    else if (key == "编辑框_自动回复_历史关键词") HistoryKeywords = value;
                    else if (key == "编辑框_自动回复_个人数据关键词") PersonalDataKeywords = value;
                    else if (key == "编辑框_自动回复_微信关键词") WeChatKeywords = value;
                    else if (key == "编辑框_自动回复_支付宝关键词") AlipayKeywords = value;
                    else if (key == "编辑框_自动回复_财付通关键词") CaifutongKeywords = value;
                    else if (key == "编辑框_上分没到词") ReplyUpNoArrived = value;
                    else if (key == "编辑框_上分到词") ReplyUpArrived = value;
                    else if (key == "编辑框_上分到词_0分") ReplyUpArrived0 = value;
                    else if (key == "编辑框_上分到词_第二条") ReplyUpArrivedSecond = value;
                    else if (key == "编辑框_下分查分词") ReplyDownQuery = value;
                    else if (key == "编辑框_下分查分词_第二条") ReplyDownSecond = value;
                    else if (key == "编辑框_下分勿催词") ReplyDownNoUrge = value;
                    else if (key == "编辑框_客户下分回复内容") ReplyCustomerDown = value;
                    else if (key == "编辑框_下分拒绝词") ReplyDownReject = value;
                    break;
                    
                case "封盘设置":
                    if (key == "开奖发送图片") SendLotteryImage = ParseBool(value);
                    else if (key == "下注数据发送开关") BetDataSendEnabled = ParseBool(value);
                    else if (key == "开奖后发送") SendAfterLottery = ParseBool(value);
                    else if (key == "禁言提前时间") SealSeconds = ParseInt(value);
                    else if (key == "下注数据时间") CheckSeconds = ParseInt(value);
                    else if (key == "PC封盘_提醒时间") RemindSeconds = ParseInt(value);
                    else if (key == "PC封盘_提醒开关") SealRemindEnabled = ParseBool(value);
                    else if (key == "PC封盘_提醒内容") SealRemindContent = value.Replace("[换行]", "\n");
                    else if (key == "PC封盘_封盘时间") SealSeconds = ParseInt(value);
                    else if (key.StartsWith("PC封盘_封盘内容")) SealContent = value.Replace("[换行]", "\n");
                    else if (key == "PC封盘_发送规矩时间") RuleSeconds = ParseInt(value);
                    else if (key == "PC封盘_发送规矩开关") SealRuleEnabled = ParseBool(value);
                    else if (key == "PC封盘_发送规矩内容") RuleContent = value.Replace("[换行]", "\n");
                    else if (key == "账单发送") BillFormat = value.Replace("[换行]", "\n");
                    else if (key == "开奖发送") LotteryFormat = value.Replace("[换行]", "\n");
                    else if (key == "下注数据发送") BetDataContent = value.Replace("[换行]", "\n");
                    break;
                    
                case "下限":
                    if (key == "单注") MinBet = ParseDecimal(value);
                    else if (key == "组合") MinComboBet = ParseDecimal(value);
                    else if (key == "数字") MinDigitBet = ParseDecimal(value);
                    else if (key == "龙虎") MinLHBBet = ParseDecimal(value);
                    else if (key == "对子") MinPairBet = ParseDecimal(value);
                    else if (key == "顺子") MinStraightBet = ParseDecimal(value);
                    else if (key == "豹子") MinLeopardBet = ParseDecimal(value);
                    break;
                    
                case "上限":
                    if (key == "单注") MaxBet = ParseDecimal(value);
                    else if (key == "组合") MaxComboBet = ParseDecimal(value);
                    else if (key == "数字") MaxDigitBet = ParseDecimal(value);
                    else if (key == "龙虎") MaxLHBBet = ParseDecimal(value);
                    else if (key == "对子") MaxPairBet = ParseDecimal(value);
                    else if (key == "顺子") MaxStraightBet = ParseDecimal(value);
                    else if (key == "豹子") MaxLeopardBet = ParseDecimal(value);
                    else if (key == "总额") MaxTotalBet = ParseDecimal(value);
                    break;
                    
                case "回水设置":
                    if (key == "回水方式") RebateMode = ParseInt(value);
                    break;
                    
                case "流水返点":
                    if (key == "默认百分比") RebatePercent = ParseDecimal(value);
                    else if (key == "默认把数") RebateBetCount = ParseInt(value);
                    break;
                    
                case "内部回复":
                    if (key == "下注显示") ReplyBetShow = value.Replace("[换行]", "\n");
                    else if (key == "托管成功") ReplyTrusteeSuccess = value.Replace("[换行]", "\n");
                    else if (key == "取消托管") ReplyTrusteeCancelled = value.Replace("[换行]", "\n");
                    else if (key == "取消下注") ReplyBetCancelled = value.Replace("[换行]", "\n");
                    else if (key == "下分正在处理") ReplyDownProcessing = value.Replace("[换行]", "\n");
                    else if (key == "下注不能下分") ReplyBetNoDown = value.Replace("[换行]", "\n");
                    else if (key == "下分不能下注") ReplyDownNoBet = value.Replace("[换行]", "\n");
                    else if (key == "攻击上分有效") ReplyAttackValid = value.Replace("[换行]", "\n");
                    else if (key == "模糊匹配提醒") ReplyFuzzyMatch = value.Replace("[换行]", "\n");
                    else if (key == "已封盘下注无效") ReplyBetClosed = value.Replace("[换行]", "\n");
                    else if (key == "下分最少下注次数") ReplyDownMinBet = value.Replace("[换行]", "\n");
                    else if (key == "下分一次性回") ReplyDownMinAmount = value.Replace("[换行]", "\n");
                    else if (key == "客户上分回复") ReplyUpRequest = value.Replace("[换行]", "\n");
                    else if (key == "发1_0分") Reply0Score = value.Replace("[换行]", "\n");
                    else if (key == "发1_有分无攻击") ReplyHasScoreNoBet = value.Replace("[换行]", "\n");
                    else if (key == "发1_有分有攻击") ReplyHasScoreHasBet = value.Replace("[换行]", "\n");
                    else if (key == "进群私聊玩家") ReplyJoinPrivate = value.Replace("[换行]", "\n");
                    else if (key == "私聊尾巴_未封盘") ReplyPrivateTailNotSealed = value.Replace("[换行]", "\n");
                    else if (key == "私聊尾巴_已封盘") ReplyPrivateTailSealed = value.Replace("[换行]", "\n");
                    else if (key == "禁止点09") ReplyNo09 = value;
                    break;
                    
                case "单独数字赔率":
                    if (int.TryParse(key, out var digit) && digit >= 0 && digit <= 27)
                    {
                        DigitOdds[digit] = ParseDecimal(value);
                    }
                    break;
            }
        }
        
        /// <summary>
        /// 导出为招财狗格式
        /// </summary>
        public void ExportToZCGFormat(string path)
        {
            try
            {
                var sb = new StringBuilder();
                
                foreach (var section in _config)
                {
                    sb.AppendLine($"[{section.Key}]");
                    foreach (var kv in section.Value)
                    {
                        sb.AppendLine($"{kv.Key}={kv.Value}");
                    }
                }
                
                File.WriteAllText(path, sb.ToString(), Encoding.GetEncoding("GB2312"));
                Log($"已导出ZCG格式: {path}");
            }
            catch (Exception ex)
            {
                Log($"导出失败: {ex.Message}");
            }
        }
        
        private void Log(string message)
        {
            Logger.Info($"[Config] {message}");
            OnLog?.Invoke(message);
        }
    }
}
