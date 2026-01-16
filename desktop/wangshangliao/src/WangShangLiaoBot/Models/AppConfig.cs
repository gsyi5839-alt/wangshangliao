using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;

namespace WangShangLiaoBot.Models
{
    /// <summary>
    /// 应用程序配置模型
    /// </summary>
    [Serializable]
    public class AppConfig
    {
        private static AppConfig _instance;
        private static readonly object _lock = new object();
        private static string _configPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "WangShangLiaoBot", "config.xml");

        /// <summary>
        /// 获取单例实例
        /// </summary>
        public static AppConfig Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            _instance = Load();
                        }
                    }
                }
                return _instance;
            }
        }

        /// <summary>
        /// 从文件加载配置
        /// </summary>
        public static AppConfig Load()
        {
            try
            {
                if (File.Exists(_configPath))
                {
                    var serializer = new XmlSerializer(typeof(AppConfig));
                    using (var reader = new StreamReader(_configPath))
                    {
                        return (AppConfig)serializer.Deserialize(reader) ?? new AppConfig();
                    }
                }
            }
            catch { }
            return new AppConfig();
        }

        /// <summary>
        /// 保存配置到文件
        /// </summary>
        public void Save()
        {
            try
            {
                var dir = Path.GetDirectoryName(_configPath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }
                var serializer = new XmlSerializer(typeof(AppConfig));
                using (var writer = new StreamWriter(_configPath))
                {
                    serializer.Serialize(writer, this);
                }
            }
            catch { }
        }

        // 兼容属性
        /// <summary>旺商号（兼容别名）</summary>
        public string WangWangId => MyWangShangId;
        /// <summary>昵称（兼容别名）</summary>
        public string Nickname { get; set; } = "";
        /// <summary>机器人旺商号（兼容别名）</summary>
        public string BotWangWangId => MyWangShangId;

        // ===== 登录设置 =====
        /// <summary>账号</summary>
        public string Username { get; set; } = "";
        /// <summary>是否记住用户</summary>
        public bool RememberUser { get; set; } = false;
        /// <summary>跳过登录验证（直接启动主界面）</summary>
        public bool SkipLogin { get; set; } = false;

        // ===== Server API Settings =====
        /// <summary>Client API base URL, e.g. https://bocail.com/api</summary>
        public string ApiBaseUrl { get; set; } = "https://bocail.com/api";
        /// <summary>JWT token for the logged-in client user</summary>
        public string ClientToken { get; set; } = "";
        /// <summary>Lottery API URL override (optional)</summary>
        public string LotteryApiUrl { get; set; } = "";
        /// <summary>Lottery API token (optional, pulled from server after login)</summary>
        public string LotteryApiToken { get; set; } = "";
        
        // ===== 基本设置 =====
        /// <summary>我的旺商号</summary>
        public string MyWangShangId { get; set; } = "";
        /// <summary>管理旺旺号</summary>
        public string AdminWangWangId { get; set; } = "";
        /// <summary>绑定群号</summary>
        public string GroupId { get; set; } = "";
        /// <summary>绑定群名称</summary>
        public string GroupName { get; set; } = "";
        /// <summary>旺商聊安装路径</summary>
        public string WangShangLiaoPath { get; set; } = @"C:\旺商聊";
        /// <summary>CDP调试端口</summary>
        public int DebugPort { get; set; } = 9222;
        
        // ===== ZCG连接配置 (基于逆向分析) =====
        /// <summary>ZCG安装目录</summary>
        public string ZcgPath { get; set; } = "";
        /// <summary>云信AppKey (ZCG使用: b03cfcd909dbf05c25163cc8c7e7b6cf)</summary>
        public string NimAppKey { get; set; } = "b03cfcd909dbf05c25163cc8c7e7b6cf";
        /// <summary>云信Accid (数字ID)</summary>
        public string NimAccid { get; set; } = "";
        /// <summary>云信Token (登录凭证)</summary>
        public string NimToken { get; set; } = "";
        /// <summary>签名Token (S_TOKEN)</summary>
        public string SignToken { get; set; } = "";
        /// <summary>内部UID</summary>
        public string NimUid { get; set; } = "";
        /// <summary>XPlugin端口 (默认14745)</summary>
        public int XPluginPort { get; set; } = 14745;
        /// <summary>是否使用ZCG连接方式</summary>
        public bool UseZcgConnection { get; set; } = false;
        
        // ===== 自动回复设置 =====
        /// <summary>是否启用自动回复</summary>
        public bool EnableAutoReply { get; set; } = false;
        /// <summary>自动回复内容</summary>
        public string AutoReplyContent { get; set; } = "您好，已收到您的消息，稍后回复~";
        /// <summary>关键词回复规则</summary>
        public List<KeywordReplyRule> KeywordRules { get; set; } = new List<KeywordReplyRule>();
        
        // ===== 回复模板 - 财付通 =====
        /// <summary>财付通发送文本</summary>
        public string CftSendText { get; set; } = "私聊前排接单";
        /// <summary>财付通文本</summary>
        public string CftText { get; set; } = "";
        /// <summary>财付通回复词库</summary>
        public string CftReplyKeywords { get; set; } = "财富|发财富|财付通|发财付通|财富多少|财付通给|cft|接下财富|接下财付通|财量多少|财富哪个|财富几个|财富帐户";
        
        // ===== 回复模板 - 支付宝 =====
        /// <summary>支付宝发送文本</summary>
        public string ZfbSendText { get; set; } = "私聊前排客服";
        /// <summary>支付宝文本</summary>
        public string ZfbText { get; set; } = "";
        /// <summary>支付宝回复词库</summary>
        public string ZfbReplyKeywords { get; set; } = "支付|支付宝|发支付宝|发支付|支付多少|支付宝一下|接下支付|接支付宝多少|zfb|支付宝发下|发下支付宝|支付发来|支付宝?";
        
        // ===== 回复模板 - 微信 =====
        /// <summary>微信发送文本</summary>
        public string WxSendText { get; set; } = "私聊前排客服";
        /// <summary>微信文本</summary>
        public string WxText { get; set; } = "";
        /// <summary>微信回复词库</summary>
        public string WxReplyKeywords { get; set; } = "微信|发微信|微信多少|微信号";
        
        // ===== 上下分设置 =====
        /// <summary>上分关键字</summary>
        public string UpScoreKeywords { get; set; } = "查|c|。";
        /// <summary>下分关键字</summary>
        public string DownScoreKeywords { get; set; } = "回";
        /// <summary>上分后X把起才可下分</summary>
        public int MinRoundsBeforeDownScore { get; set; } = 0;
        /// <summary>X分以下下分只能一次回</summary>
        public int MinScoreForSingleDown { get; set; } = 30;
        /// <summary>客户下分回复内容</summary>
        public string ClientDownReplyContent { get; set; } = "已收到[昵称][分数]请求，请稍等";
        
        // ===== 消息反馈设置 =====
        /// <summary>上分消息反馈至群</summary>
        public bool UpScoreFeedbackToGroup { get; set; } = false;
        /// <summary>下分消息反馈至群</summary>
        public bool DownScoreFeedbackToGroup { get; set; } = false;
        
        // ===== 提示音设置 =====
        /// <summary>启用上分提示音</summary>
        public bool EnableUpScoreSound { get; set; } = true;
        /// <summary>启用下分提示音</summary>
        public bool EnableDownScoreSound { get; set; } = true;
        
        // ===== 话术配置 =====
        /// <summary>没到词</summary>
        public string NotArrivedText { get; set; } = "[艾特] 没到，请您联系接单核实原因";
        /// <summary>0分到词</summary>
        public string ZeroArrivedText { get; set; } = "[艾特][分数]到[换行]操库:[余粮]";
        /// <summary>有分到词</summary>
        public string HasScoreArrivedText { get; set; } = "[艾特][分数]到[换行]操库:[余粮]";
        /// <summary>查分词</summary>
        public string CheckScoreText { get; set; } = "[艾特][分数]查[换行]操库:[留分]";
        /// <summary>勿催词</summary>
        public string DontRushText { get; set; } = "[艾特] 您的提款请求我们正在火速审核~~";
        /// <summary>拒绝词</summary>
        public string RejectText { get; set; } = "[艾特] 拒绝，请您联系接单核实";
        
        // ===== 经典玩法设置 - 大小单双超设置 =====
        /// <summary>大小单双超设置数字 (如 "13|14")</summary>
        public string SizeOddEvenNumbers { get; set; } = "13|14";
        /// <summary>大小单双超-算总注(true)/算单注(false)</summary>
        public bool SizeOddEvenSumBet { get; set; } = true;
        /// <summary>大小单双超-行1最小</summary>
        public decimal SizeRow1Min { get; set; } = 2;
        /// <summary>大小单双超-行1最大</summary>
        public decimal SizeRow1Max { get; set; } = 10000;
        /// <summary>大小单双超-行1赔率</summary>
        public decimal SizeRow1Odds { get; set; } = 0;
        /// <summary>大小单双超-行2最小</summary>
        public decimal SizeRow2Min { get; set; } = 1000;
        /// <summary>大小单双超-行2最大</summary>
        public decimal SizeRow2Max { get; set; } = 60000;
        /// <summary>大小单双超-行2赔率</summary>
        public decimal SizeRow2Odds { get; set; } = 0;
        /// <summary>大小单双超-行3最小</summary>
        public decimal SizeRow3Min { get; set; } = 6000;
        /// <summary>大小单双超-行3最大</summary>
        public decimal SizeRow3Max { get; set; } = 0;
        /// <summary>大小单双超-行3赔率</summary>
        public decimal SizeRow3Odds { get; set; } = 0;
        /// <summary>大小单双超-行4最小</summary>
        public decimal SizeRow4Min { get; set; } = 6000;
        /// <summary>大小单双超-行4最大</summary>
        public decimal SizeRow4Max { get; set; } = 0;
        /// <summary>大小单双超-行4赔率</summary>
        public decimal SizeRow4Odds { get; set; } = 0;
        
        // ===== 经典玩法设置 - 大双小单超设置 =====
        /// <summary>大双小单超-总注超1</summary>
        public decimal BigDoubleSmallSingle1Amount { get; set; } = 0;
        /// <summary>大双小单超-赔率1</summary>
        public decimal BigDoubleSmallSingle1Odds { get; set; } = 0;
        /// <summary>大双小单超-总注超2</summary>
        public decimal BigDoubleSmallSingle2Amount { get; set; } = 1000;
        /// <summary>大双小单超-赔率2</summary>
        public decimal BigDoubleSmallSingle2Odds { get; set; } = 0;
        
        // ===== 经典玩法设置 - 大单小双超设置 =====
        /// <summary>大单小双超-总注超1</summary>
        public decimal BigSingleSmallDouble1Amount { get; set; } = 50000;
        /// <summary>大单小双超-赔率1</summary>
        public decimal BigSingleSmallDouble1Odds { get; set; } = 0;
        /// <summary>大单小双超-总注超2</summary>
        public decimal BigSingleSmallDouble2Amount { get; set; } = 10000;
        /// <summary>大单小双超-赔率2</summary>
        public decimal BigSingleSmallDouble2Odds { get; set; } = 0;
        
        // ===== 经典玩法设置 - 豹/顺/对子 =====
        /// <summary>豹顺对开关</summary>
        public bool LeopardSequencePairEnabled { get; set; } = true;
        /// <summary>对子回本</summary>
        public bool PairReturn { get; set; } = true;
        /// <summary>顺子回本</summary>
        public bool SequenceReturn { get; set; } = true;
        /// <summary>豹子回本</summary>
        public bool LeopardReturn { get; set; } = false;
        /// <summary>豹子通杀</summary>
        public bool LeopardKillAll { get; set; } = false;
        /// <summary>半顺杂开关</summary>
        public bool HalfMixedEnabled { get; set; } = false;
        /// <summary>0,9回本</summary>
        public bool Digit09Return { get; set; } = false;
        /// <summary>1314对/顺/豹子回本</summary>
        public bool Digit1314Return { get; set; } = false;
        /// <summary>数字开对/顺/豹子回本</summary>
        public bool NumLSPReturn { get; set; } = false;
        /// <summary>数字开1314对/顺/豹子回本</summary>
        public bool Num1314LSPReturn { get; set; } = false;
        /// <summary>极数开对/顺/豹子回本</summary>
        public bool ExtremeLSPReturn { get; set; } = false;
        /// <summary>开1314，中对子回本</summary>
        public bool Open1314PairReturn { get; set; } = false;
        /// <summary>890,910算顺子</summary>
        public bool Seq890910AsSequence { get; set; } = true;
        
        // ===== 经典玩法设置 - 超无视 =====
        /// <summary>超无视-算总注(true)/算单注(false)</summary>
        public bool IgnoreOverSumBet { get; set; } = true;
        /// <summary>超无视-金额超</summary>
        public decimal IgnoreOverAmount { get; set; } = 0;
        /// <summary>超无视-记超无视</summary>
        public bool IgnoreOverEnabled { get; set; } = false;
        /// <summary>杀双多组对压不记超无视</summary>
        public bool KillDoubleIgnore { get; set; } = false;
        /// <summary>无13 14赔率</summary>
        public bool No1314Odds { get; set; } = false;
        
        // ===== 经典玩法设置 - 赔率 =====
        /// <summary>大小单双赔率</summary>
        public decimal DxdsOdds { get; set; } = 1.8m;
        /// <summary>大单小双赔率</summary>
        public decimal BigOddSmallEvenOdds { get; set; } = 5.0m;
        /// <summary>大双小单赔率</summary>
        public decimal BigEvenSmallOddOdds { get; set; } = 5.0m;
        /// <summary>极大极小赔率</summary>
        public decimal ExtremeOdds { get; set; } = 0m;
        /// <summary>特码数字赔率 (0-27)</summary>
        public decimal DigitOdds { get; set; } = 9.0m;
        /// <summary>对子赔率</summary>
        public decimal PairOdds { get; set; } = 2.0m;
        /// <summary>顺子赔率</summary>
        public decimal StraightOdds { get; set; } = 0m;
        /// <summary>半顺赔率</summary>
        public decimal HalfStraightOdds { get; set; } = 0m;
        /// <summary>豹子赔率</summary>
        public decimal LeopardOdds { get; set; } = 49.0m;
        /// <summary>杂赔率</summary>
        public decimal MixedOdds { get; set; } = 0m;
        /// <summary>大边赔率</summary>
        public decimal BigEdgeOdds { get; set; } = 0m;
        /// <summary>小边赔率</summary>
        public decimal SmallEdgeOdds { get; set; } = 0m;
        /// <summary>中赔率</summary>
        public decimal MiddleOdds { get; set; } = 0m;
        /// <summary>边历史赔率</summary>
        public decimal EdgeHistoryOdds { get; set; } = 0m;
        /// <summary>边赔率 (通用)</summary>
        public decimal EdgeOdds { get; set; } = 5.0m;
        /// <summary>和赔率</summary>
        public decimal SumOdds { get; set; } = 49.0m;
        /// <summary>组合赔率 (经典玩法)</summary>
        public decimal CombinationOdds { get; set; } = 1.2m;
        
        // ===== 经典玩法设置 - 极数 =====
        /// <summary>极大阈值起始 (默认22)</summary>
        public int ExtremeMax { get; set; } = 22;
        /// <summary>极大阈值结束 (默认27)</summary>
        public int ExtremeMaxEnd { get; set; } = 27;
        /// <summary>极小阈值起始 (默认0)</summary>
        public int ExtremeMin { get; set; } = 0;
        /// <summary>极小阈值结束 (默认5)</summary>
        public int ExtremeMinEnd { get; set; } = 5;
        
        // ===== 经典玩法设置 - 单独数字赔率 =====
        /// <summary>启用单独数字赔率</summary>
        public bool SingleDigitOddsEnabled { get; set; } = false;
        /// <summary>单独数字赔率 (格式: "0:9,1:9,2:9...")</summary>
        public string SingleDigitOddsList { get; set; } = "0:9,1:9,2:9,3:9,4:9,5:9,6:9,7:9,8:9,9:9";
        
        // ===== 经典玩法设置 - 特码格式 =====
        /// <summary>特码下注字眼</summary>
        public string SpecialCodeChars { get; set; } = "操|草|点|+|*|'|T";
        /// <summary>特码格式 - true=特码T金额, false=金额T特码</summary>
        public bool SpecialCodeFirst { get; set; } = false;
        /// <summary>以小的金额为特码</summary>
        public bool SmallAmountAsSpecial { get; set; } = true;
        /// <summary>单点最高赔付</summary>
        public decimal MaxSinglePayout { get; set; } = 5000;
        /// <summary>最大数字个数</summary>
        public int MaxDigitCount { get; set; } = 3;
        
        // ===== 尾球玩法设置 =====
        /// <summary>尾球玩法开启</summary>
        public bool TailBallEnabled { get; set; } = false;
        /// <summary>尾球玩法不算进经典玩法总注</summary>
        public bool TailBallNotCountClassic { get; set; } = false;
        /// <summary>无13 14 - 尾大小单双赔率</summary>
        public decimal TailOdds1314BigSmall { get; set; } = 1.4m;
        /// <summary>无13 14 - 尾组合赔率</summary>
        public decimal TailOdds1314Combo { get; set; } = 3.8m;
        /// <summary>无13 14 - 尾特码赔率</summary>
        public decimal TailOdds1314Special { get; set; } = 5m;
        /// <summary>尾球开0 9 - 尾大小单双赔率</summary>
        public decimal TailOdds09BigSmall { get; set; } = -1m;
        /// <summary>尾球开0 9 - 尾组合赔率</summary>
        public decimal TailOdds09Combo { get; set; } = -1m;
        /// <summary>有13 14 - 算单注(true)/算总注(false)</summary>
        public bool TailBetTypeSingle { get; set; } = true;
        /// <summary>有13 14 - 尾球超1</summary>
        public int TailBallOver1 { get; set; } = 1000;
        /// <summary>有13 14 - 尾大小单双赔率</summary>
        public decimal TailOddsWith1314BigSmall { get; set; } = 0m;
        /// <summary>有13 14 - 尾球超2</summary>
        public int TailBallOver2 { get; set; } = 1000;
        /// <summary>有13 14 - 尾组合赔率</summary>
        public decimal TailOddsWith1314Combo { get; set; } = 0m;
        /// <summary>其他玩法算进总注</summary>
        public bool OtherGameCountTotal { get; set; } = false;
        /// <summary>禁止点0 9</summary>
        public bool TailForbid09 { get; set; } = false;
        /// <summary>前球玩法开启</summary>
        public bool FrontBallEnabled { get; set; } = false;
        /// <summary>中球玩法开启</summary>
        public bool MiddleBallEnabled { get; set; } = false;
        
        // ===== 龙虎玩法设置 =====
        /// <summary>龙虎玩法开启</summary>
        public bool DragonTigerEnabled { get; set; } = false;
        /// <summary>龙虎模式 (0=龙虎斗, 1=龙虎豹)</summary>
        public int DragonTigerMode { get; set; } = 0;
        /// <summary>龙虎区域1 (0=一区, 1=二区, 2=三区)</summary>
        public int DragonTigerZone1 { get; set; } = 0;
        /// <summary>龙虎比较方式 (0=大于, 1=小于, 2=等于)</summary>
        public int DragonTigerCompare { get; set; } = 0;
        /// <summary>龙虎区域2 (0=一区, 1=二区, 2=三区)</summary>
        public int DragonTigerZone2 { get; set; } = 0;
        /// <summary>开和龙虎回本</summary>
        public bool DragonTigerDrawReturn { get; set; } = false;
        /// <summary>豹子通杀龙虎和</summary>
        public bool DragonTigerLeopardKillAll { get; set; } = false;
        /// <summary>龙虎赔率</summary>
        public decimal DragonTigerOdds { get; set; } = 0.6m;
        /// <summary>和赔率</summary>
        public decimal DragonTigerDrawOdds { get; set; } = 0m;
        /// <summary>龙虎和下注总额超</summary>
        public int DragonTigerBetOverAmount { get; set; } = 0;
        /// <summary>龙虎赔率2 (超额后)</summary>
        public decimal DragonTigerOdds2 { get; set; } = 0m;
        /// <summary>和赔率2 (超额后)</summary>
        public decimal DragonTigerDrawOdds2 { get; set; } = 0m;
        /// <summary>龙虎豹赔率</summary>
        public decimal DragonTigerLeopardOdds { get; set; } = 0.6m;
        /// <summary>龙号码</summary>
        public string DragonNumbers { get; set; } = "00, 03, 06, 09, 12, 15, 18, 21, 24, 27";
        /// <summary>虎号码</summary>
        public string TigerNumbers { get; set; } = "01, 04, 07, 10, 13, 16, 19, 22, 25";
        /// <summary>豹号码</summary>
        public string LeopardNumbers { get; set; } = "02, 05, 08, 11, 14, 17, 20, 23, 26";
        
        // ===== 三军玩法设置 =====
        /// <summary>三军玩法开启</summary>
        public bool ThreeArmyEnabled { get; set; } = false;
        /// <summary>三军赔率1</summary>
        public decimal ThreeArmyOdds1 { get; set; } = 0m;
        /// <summary>三军赔率2</summary>
        public decimal ThreeArmyOdds2 { get; set; } = 0m;
        /// <summary>三军赔率3</summary>
        public decimal ThreeArmyOdds3 { get; set; } = 0m;
        
        // ===== 定位球玩法设置 =====
        /// <summary>定位球玩法开启</summary>
        public bool PositionBallEnabled { get; set; } = false;
        /// <summary>单注赔率</summary>
        public decimal PositionBallSingleOdds { get; set; } = 0m;
        /// <summary>单注下注范围最小值</summary>
        public int PositionBallSingleRangeMin { get; set; } = 0;
        /// <summary>单注下注范围最大值</summary>
        public int PositionBallSingleRangeMax { get; set; } = 0;
        /// <summary>组合赔率</summary>
        public decimal PositionBallComboOdds { get; set; } = 0m;
        /// <summary>组合下注范围最小值</summary>
        public int PositionBallComboRangeMin { get; set; } = 0;
        /// <summary>组合下注范围最大值</summary>
        public int PositionBallComboRangeMax { get; set; } = 0;
        /// <summary>特码赔率</summary>
        public decimal PositionBallSpecialOdds { get; set; } = 0m;
        /// <summary>特码下注范围最小值</summary>
        public int PositionBallSpecialRangeMin { get; set; } = 0;
        /// <summary>特码下注范围最大值</summary>
        public int PositionBallSpecialRangeMax { get; set; } = 0;
        
        // ===== 其他玩法 - 二七玩法设置 =====
        /// <summary>二七玩法开启</summary>
        public bool TwoSevenEnabled { get; set; } = false;
        /// <summary>单注总额超阈值</summary>
        public decimal TwoSevenSingleExceed { get; set; } = 0m;
        /// <summary>单注超阈值赔率</summary>
        public decimal TwoSevenSingleOdds { get; set; } = 0m;
        /// <summary>组合总额超阈值</summary>
        public decimal TwoSevenComboExceed { get; set; } = 0m;
        /// <summary>组合超阈值赔率</summary>
        public decimal TwoSevenComboOdds { get; set; } = 0m;
        
        // ===== 其他玩法 - 反向开奖玩法设置 =====
        /// <summary>反向开奖玩法开启</summary>
        public bool ReverseLotteryEnabled { get; set; } = false;
        /// <summary>下注总额最多为总分的百分比</summary>
        public decimal ReverseLotteryBetMaxRatio { get; set; } = 0m;
        /// <summary>每次结算扣除盈利的百分比</summary>
        public decimal ReverseLotteryProfitDeduct { get; set; } = 0m;
        
        // ===== 其他玩法 - 长龙玩法设置 =====
        /// <summary>长龙玩法开启</summary>
        public bool DragonStreakEnabled { get; set; } = false;
        /// <summary>连续出现次数阈值1</summary>
        public int DragonStreak1Times { get; set; } = 0;
        /// <summary>连续出现次数1减少的赔率</summary>
        public decimal DragonStreak1Reduce { get; set; } = 0m;
        /// <summary>连续出现次数阈值2</summary>
        public int DragonStreak2Times { get; set; } = 0;
        /// <summary>连续出现次数2减少的赔率</summary>
        public decimal DragonStreak2Reduce { get; set; } = 0m;
        
        // ===== 黑名单 =====
        /// <summary>黑名单列表</summary>
        public List<string> Blacklist { get; set; } = new List<string>();
        
        // ===== 窗口设置 =====
        /// <summary>主窗口位置X</summary>
        public int MainFormX { get; set; } = 100;
        /// <summary>主窗口位置Y</summary>
        public int MainFormY { get; set; } = 100;
        
        // ===== 账单设置 - 开奖发送 =====
        /// <summary>启用开奖通知</summary>
        public bool EnableLotteryNotify { get; set; } = true;
        /// <summary>开奖带8</summary>
        public bool LotteryWith8 { get; set; } = false;
        /// <summary>开奖发送内容模板 - 支持变量替换</summary>
        public string LotterySendContent { get; set; } = "";
        /// <summary>开奖图片发送</summary>
        public bool LotteryImageSend { get; set; } = false;
        /// <summary>期数</summary>
        public int PeriodCount { get; set; } = 21;
        
        // ===== 账单设置 - 账单格式 =====
        /// <summary>账单列数</summary>
        public int BillColumns { get; set; } = 4;
        /// <summary>账单图片发送</summary>
        public bool BillImageSend { get; set; } = false;
        /// <summary>账单秒顺回复</summary>
        public bool BillSecondReply { get; set; } = false;
        
        // ===== 账单设置 - 群作业 =====
        /// <summary>群作业账单发送</summary>
        public bool GroupTaskSend { get; set; } = false;
        /// <summary>隐藏输光玩家</summary>
        public bool HideLostPlayers { get; set; } = false;
        /// <summary>保留零分账单</summary>
        public bool KeepZeroScoreBill { get; set; } = false;
        /// <summary>只保留近10期群作业</summary>
        public bool KeepRecent10Tasks { get; set; } = true;
        /// <summary>自动同意玩家进群</summary>
        public bool AutoApprovePlayer { get; set; } = false;
        /// <summary>账单最小位数</summary>
        public int BillMinDigits { get; set; } = 4;
        /// <summary>账单隐藏阈值</summary>
        public int BillHideThreshold { get; set; } = 0;
        
        // ===== 账单设置 - 禁言核对 =====
        /// <summary>提前禁言秒数</summary>
        public int MuteBeforeSeconds { get; set; } = 2;
        /// <summary>下注数据延迟秒数</summary>
        public int BetDataDelaySeconds { get; set; } = 10;
        /// <summary>下注数据图片发送</summary>
        public bool BetDataImageSend { get; set; } = false;
        /// <summary>群作业通知</summary>
        public bool GroupTaskNotify { get; set; } = false;
        
        // ===== 账单设置 - 消息反馈 =====
        /// <summary>反馈旺旺号</summary>
        public string FeedbackWangWangId { get; set; } = "";
        /// <summary>反馈群号</summary>
        public string FeedbackGroupId { get; set; } = "";
        /// <summary>反馈到旺旺</summary>
        public bool FeedbackToWangWang { get; set; } = false;
        /// <summary>反馈到群</summary>
        public bool FeedbackToGroup { get; set; } = false;
        /// <summary>下注核对反馈</summary>
        public bool BetCheckFeedback { get; set; } = false;
        /// <summary>下注汇总反馈</summary>
        public bool BetSummaryFeedback { get; set; } = false;
        /// <summary>盈利反馈</summary>
        public bool ProfitFeedback { get; set; } = false;
        /// <summary>账单发送反馈</summary>
        public bool BillSendFeedback { get; set; } = false;
        
        // ===== 内部回复设置 - 左侧面板 =====
        /// <summary>下注显示</summary>
        public string InternalBetDisplay { get; set; } = "[昵称]";
        /// <summary>取消下注</summary>
        public string InternalCancelBet { get; set; } = "[昵称] Qu Xiao";
        /// <summary>模糊提醒</summary>
        public string InternalFuzzyRemind { get; set; } = "[昵称] Yu Bu Zu Shang Fen Hou Lu Qu";
        /// <summary>攻击上分有效</summary>
        public string InternalAttackValid { get; set; } = "";
        /// <summary>下注不能下分</summary>
        public string InternalBetNoDown { get; set; } = "";
        /// <summary>下注不能下分2</summary>
        public string InternalBetNoDown2 { get; set; } = "";
        /// <summary>下分正在处理</summary>
        public string InternalDownProcessing { get; set; } = "[昵称] Shao Deng";
        /// <summary>已封盘未处理</summary>
        public string InternalSealedUnprocessed { get; set; } = "[昵称]慢作业结束攻击要快！姿势要帅！";
        /// <summary>取消托管成功</summary>
        public string InternalCancelTrustee { get; set; } = "";
        /// <summary>禁止点09</summary>
        public string InternalForbid09 { get; set; } = "";
        
        // ===== 内部回复设置 - 上下分 =====
        /// <summary>下分最少次数</summary>
        public string InternalUpDownMin { get; set; } = "[昵称] 6把回首冲后10把回横50以上起回";
        /// <summary>下分最少次数2</summary>
        public string InternalUpDownMin2 { get; set; } = "[昵称] Di Yu最低吓呀!";
        /// <summary>一次回</summary>
        public string InternalUpDownMax { get; set; } = "";
        /// <summary>客户上分</summary>
        public string InternalUpDownPlayer { get; set; } = "";
        
        // ===== 内部回复设置 - 个人数据反馈 =====
        /// <summary>数据反馈关键词</summary>
        public string InternalDataKeyword { get; set; } = "账单|数据|我有下注吗|下注情况|1";
        /// <summary>账单0分</summary>
        public string InternalDataBill { get; set; } = "[昵称] $:0.00";
        /// <summary>有分无攻击</summary>
        public string InternalDataNoAttack { get; set; } = "[昵称] $:[余粮]";
        /// <summary>有分有攻击</summary>
        public string InternalDataHasAttack { get; set; } = "";
        
        // ===== 内部回复设置 - 进群/群规 =====
        /// <summary>进群/群规触发关键词（用|分隔多个关键词）</summary>
        public string InternalGroupRulesKeyword { get; set; } = "群规|规则|新人|福利|玩法";
        /// <summary>进群/群规说明</summary>
        public string InternalGroupRules { get; set; } = "认准管理员请加客服，群内有规则/福利说明，请遵守群规，谢谢！";
        
        // ===== 内部回复设置 - 私聊尾巴 =====
        /// <summary>未封盘尾巴</summary>
        public string InternalTailUnsealed { get; set; } = "离考试结束还有[封盘倒计时]秒";
        /// <summary>已封盘尾巴</summary>
        public string InternalTailSealed { get; set; } = "一封盘线";
    }
    
    /// <summary>
    /// 关键词回复规则
    /// </summary>
    [Serializable]
    public class KeywordReplyRule
    {
        /// <summary>关键词</summary>
        public string Keyword { get; set; }
        /// <summary>回复内容</summary>
        public string Reply { get; set; }
        /// <summary>是否启用</summary>
        public bool Enabled { get; set; } = true;
    }
}

