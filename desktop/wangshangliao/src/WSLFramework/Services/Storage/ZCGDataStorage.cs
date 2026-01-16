using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Web.Script.Serialization;
using WSLFramework.Utils;

namespace WSLFramework.Services
{
    /// <summary>
    /// ZCG数据存储服务 - 按照旧程序 C:\zcg25.12.11\zcg\ 的结构存放数据
    /// </summary>
    public class ZCGDataStorage
    {
        private static readonly Lazy<ZCGDataStorage> _lazy = 
            new Lazy<ZCGDataStorage>(() => new ZCGDataStorage());
        public static ZCGDataStorage Instance => _lazy.Value;

        private readonly JavaScriptSerializer _serializer;
        private readonly object _lock = new object();

        // 数据根目录 (模仿旧程序 C:\zcg25.12.11\zcg\)
        public string DataRoot { get; private set; }

        // 子目录
        public string BackupDir => Path.Combine(DataRoot, "备份");
        public string ImageDir => Path.Combine(DataRoot, "图片");
        public string DatabaseDir => Path.Combine(DataRoot, "数据库");
        public string PlayerDataDir => Path.Combine(DataRoot, "玩家资料");
        public string GroupCacheDir => Path.Combine(DataRoot, "群成员缓存");
        public string LogDir { get; private set; }

        // 数据库文件路径
        public string SettingsDbPath => Path.Combine(DataRoot, "设置.db");
        public string ScoreDbPath => Path.Combine(DataRoot, "上下分.db");
        public string BillDbPath => Path.Combine(DataRoot, "账单.db");
        public string BetDbPath => Path.Combine(DataRoot, "攻击.db");
        public string PlayerNameDbPath => Path.Combine(DataRoot, "玩家姓名.db");
        public string InviteDbPath => Path.Combine(DataRoot, "邀请记录.db");

        // 配置文件路径
        public string SettingsIniPath => Path.Combine(DataRoot, "设置.ini");
        public string LoginConfigPath => Path.Combine(DataRoot, "登录配置.ini");
        
        // 根目录配置文件
        public string RootConfigPath => Path.Combine(Path.GetDirectoryName(DataRoot) ?? DataRoot, "config.ini");
        public string PluginConfigPath => Path.Combine(Path.GetDirectoryName(DataRoot) ?? DataRoot, "Plugin.ini");
        public string PortConfigPath => Path.Combine(Path.GetDirectoryName(DataRoot) ?? DataRoot, "zcg端口.ini");

        // bin目录 (运行时依赖)
        public string BinDir => Path.Combine(DataRoot, "bin");

        private ZCGDataStorage()
        {
            _serializer = new JavaScriptSerializer { MaxJsonLength = int.MaxValue };
            
            // 默认使用程序目录下的 zcg 子目录
            var exeDir = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            DataRoot = Path.Combine(exeDir, "zcg");
            LogDir = Path.Combine(DataRoot, "收发日志");  // 收发日志目录在zcg目录下

            EnsureDirectories();
        }

        /// <summary>
        /// 设置数据根目录
        /// </summary>
        public void SetDataRoot(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                Logger.Error("[ZCGDataStorage] 数据目录路径不能为空");
                return;
            }
            
            DataRoot = path;
            
            // Path.GetDirectoryName 可能返回 null，需要处理
            // 收发日志目录在DataRoot(zcg目录)下
            LogDir = Path.Combine(DataRoot, "收发日志");
                
            EnsureDirectories();
            Logger.Info($"[ZCGDataStorage] 数据目录设置为: {DataRoot}");
        }

        /// <summary>
        /// 确保所有必要的目录存在
        /// </summary>
        private void EnsureDirectories()
        {
            try
            {
                Directory.CreateDirectory(DataRoot);
                Directory.CreateDirectory(BackupDir);
                Directory.CreateDirectory(ImageDir);
                Directory.CreateDirectory(DatabaseDir);
                Directory.CreateDirectory(PlayerDataDir);
                Directory.CreateDirectory(GroupCacheDir);
                Directory.CreateDirectory(LogDir);
                Directory.CreateDirectory(BinDir);

                // 图片子目录
                Directory.CreateDirectory(Path.Combine(ImageDir, "封盘图片"));
                Directory.CreateDirectory(Path.Combine(ImageDir, "微信二维码"));
                Directory.CreateDirectory(Path.Combine(ImageDir, "支付宝二维码"));
                Directory.CreateDirectory(Path.Combine(ImageDir, "财付通二维码"));
                Directory.CreateDirectory(Path.Combine(ImageDir, "文字到图片"));

                // 初始化数据库文件 (如果不存在)
                InitializeDatabaseFiles();
                
                // 初始化配置文件 (如果不存在)
                InitializeConfigFiles();

                Logger.Info($"[ZCGDataStorage] 数据目录已初始化: {DataRoot}");
            }
            catch (Exception ex)
            {
                Logger.Error($"[ZCGDataStorage] 创建目录失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 初始化数据库文件
        /// </summary>
        private void InitializeDatabaseFiles()
        {
            // 确保所有.db文件存在
            if (!File.Exists(SettingsDbPath))
                SaveJsonData(SettingsDbPath, new Dictionary<string, object>());
            if (!File.Exists(ScoreDbPath))
                SaveJsonData(ScoreDbPath, new List<ZCGScoreRecord>());
            if (!File.Exists(BillDbPath))
                SaveJsonData(BillDbPath, new List<BillRecord>());
            if (!File.Exists(BetDbPath))
                SaveJsonData(BetDbPath, new List<BetRecord>());
            if (!File.Exists(PlayerNameDbPath))
                SaveJsonData(PlayerNameDbPath, new Dictionary<string, string>());
            if (!File.Exists(InviteDbPath))
                SaveJsonData(InviteDbPath, new List<InviteRecord>());
        }

        /// <summary>
        /// 初始化配置文件
        /// </summary>
        private void InitializeConfigFiles()
        {
            // 初始化设置.ini (ZCG加密格式)
            if (!File.Exists(SettingsIniPath))
            {
                CreateDefaultSettingsIni();
            }
            
            // 初始化登录配置.ini
            if (!File.Exists(LoginConfigPath))
            {
                CreateDefaultLoginConfigIni();
            }
            
            // 初始化端口配置
            if (!File.Exists(PortConfigPath))
            {
                CreateDefaultPortConfig();
            }
        }

        /// <summary>
        /// 创建默认设置.ini文件 (完整ZCG格式)
        /// </summary>
        private void CreateDefaultSettingsIni()
        {
            try
            {
                var sb = new StringBuilder();
                
                // [运行标志]
                sb.AppendLine("[运行标志]");
                sb.AppendLine("运行标志=0");
                sb.AppendLine();
                
                // [彩种]
                sb.AppendLine("[彩种]");
                sb.AppendLine("彩种=1");
                sb.AppendLine("通道=0");
                sb.AppendLine();
                
                // [选择框] - 功能开关
                sb.AppendLine("[选择框]");
                sb.AppendLine("选择框_开奖图片发送=假");
                sb.AppendLine("选择框_账单2图片发送=假");
                sb.AppendLine("选择框_托自动上分=真");
                sb.AppendLine("选择框_托自动下分=真");
                sb.AppendLine("选择框_发言检测_违规撤回=真");
                sb.AppendLine("选择框_被群管理踢出自动加黑名单=真");
                sb.AppendLine("选择框_被机器人踢出自动加黑名单=真");
                sb.AppendLine("选择框_接收群聊下注开关=真");
                sb.AppendLine("选择框_模糊匹配开关=真");
                sb.AppendLine("选择框_模糊匹配艾特提醒=假");
                sb.AppendLine("选择框_无账单下注提醒=真");
                sb.AppendLine("选择框_顺子算890910=真");
                sb.AppendLine("选择框_锁名片开关=真");
                sb.AppendLine("选择框_锁名片_提醒发送到群=真");
                sb.AppendLine("选择框_上分提示音开关=真");
                sb.AppendLine("选择框_下分提示音开关=真");
                sb.AppendLine("选择框_客户下分回复=真");
                sb.AppendLine("选择框_下分勿催=真");
                sb.AppendLine("选择框_开奖提示音开关=真");
                sb.AppendLine("选择框_封盘提示音开关=真");
                sb.AppendLine("选择框_基本设置_变量先注后分=真");
                sb.AppendLine("选择框_基本设置_允许改加注=真");
                sb.AppendLine("选择框_下分消息反馈=假");
                sb.AppendLine("选择框_上分消息反馈=假");
                sb.AppendLine("选择框_玩家查询今天数据_开关=真");
                sb.AppendLine("选择框_艾特分_私聊加分允许无理由=真");
                sb.AppendLine("选择框_好友私聊下注开关=真");
                sb.AppendLine("选择框_自动同意好友添加=真");
                sb.AppendLine("选择框_私聊词库在群内反馈=真");
                sb.AppendLine("选择框_基本设置_数字大写类型=假");
                sb.AppendLine("选择框_基本设置_全局数字小写=真");
                sb.AppendLine("选择框_锁名片_超次数踢人=真");
                sb.AppendLine("选择框_敏感操作_关闭密码=真");
                sb.AppendLine("选择框_上下分重复过滤=真");
                sb.AppendLine("选择框_基本设置_艾特变昵称=假");
                sb.AppendLine("选择框_基本设置_上分后自动处理之前下注=真");
                sb.AppendLine("选择框_仅支持拼音下注开关=假");
                sb.AppendLine("选择框_尾球玩法开关=假");
                sb.AppendLine("选择框_尾球禁止下09=真");
                sb.AppendLine("选择框_龙虎豹玩法开关=真");
                sb.AppendLine("选择框_发言检测_图片禁言=真");
                sb.AppendLine("选择框_豹顺对特殊规则_开关=真");
                sb.AppendLine("选择框_豹顺对特殊规则_对子回本=真");
                sb.AppendLine("选择框_豹顺对特殊规则_豹子回本=真");
                sb.AppendLine("选择框_豹顺对特殊规则_顺子回本=真");
                sb.AppendLine("选择框_单独数字赔率=真");
                sb.AppendLine("选择框_零分不删除账单=真");
                sb.AppendLine("选择框_只接群里成员下注=假");
                sb.AppendLine("选择框_玩家托管开关=真");
                sb.AppendLine("选择框_猜数字开关=真");
                sb.AppendLine("选择框_账单玩家进群自动同意=真");
                sb.AppendLine("选择框_账单不显示输光玩家=真");
                sb.AppendLine("选择框_二七玩法_开关=真");
                sb.AppendLine("选择框_托自动同意进群=真");
                sb.AppendLine();
                
                // [编辑框] - 各种参数设置
                sb.AppendLine("[编辑框]");
                sb.AppendLine("编辑框_机器人QQ=");
                sb.AppendLine("编辑框_管理QQ号码=10010");
                sb.AppendLine("编辑框_绑定群号=");
                sb.AppendLine("编辑框_发言检测_字数禁言=100");
                sb.AppendLine("编辑框_发言检测_字数踢出=200");
                sb.AppendLine("编辑框_发言检测_行数禁言=4");
                sb.AppendLine("编辑框_发言检测_禁言时间=10");
                sb.AppendLine("编辑框_发言检测_图片次数踢出=3");
                sb.AppendLine("编辑框_锁名片_最大改名片次数=5");
                sb.AppendLine("编辑框_极大1=22");
                sb.AppendLine("编辑框_极大2=27");
                sb.AppendLine("编辑框_极小1=0");
                sb.AppendLine("编辑框_极小2=5");
                sb.AppendLine("编辑框_托上分延迟1=5");
                sb.AppendLine("编辑框_托上分延迟2=10");
                sb.AppendLine("编辑框_托下分延迟1=10");
                sb.AppendLine("编辑框_托下分延迟2=20");
                sb.AppendLine("编辑框_普通赔率_单注=1.98");
                sb.AppendLine("编辑框_普通赔率_大双小单=5");
                sb.AppendLine("编辑框_普通赔率_大单小双=5");
                sb.AppendLine("编辑框_普通赔率_极大极小=11");
                sb.AppendLine("编辑框_普通赔率_数字=10");
                sb.AppendLine("编辑框_普通赔率_对子=2");
                sb.AppendLine("编辑框_普通赔率_顺子=11");
                sb.AppendLine("编辑框_普通赔率_半顺=1.97");
                sb.AppendLine("编辑框_普通赔率_豹子=59");
                sb.AppendLine("编辑框_普通赔率_杂=2.2");
                sb.AppendLine("编辑框_龙虎豹赔率=1.92");
                sb.AppendLine("编辑框_单点最高赔付=20000");
                sb.AppendLine("编辑框_玩家查询今天数据_时间间隔=10");
                sb.AppendLine("编辑框_基本设置_历史显示期数=11");
                sb.AppendLine("编辑框_自动回复_历史关键词=历史|发历史|开奖历史|历史发|发下历史|2");
                sb.AppendLine("编辑框_自动回复_个人数据关键词=账单|数据|我有下注吗|下注情况|1");
                sb.AppendLine("编辑框_上分没到词=[艾特] 没到，请您联系接单核实原因！");
                sb.AppendLine("编辑框_上分到词=[艾特] [分数]到[换行]粮库:[余粮][换行]祝您大吉大利，旗开得胜！！");
                sb.AppendLine("编辑框_上分到词_0分=[艾特][分数]到[换行]粮库:[余粮][换行]祝您大吉大利，旗开得胜！！");
                sb.AppendLine("编辑框_下分查分词=[艾特] [分数]查[换行]粮库:[留分][换行]感谢老板支持，祝您一路发！！！");
                sb.AppendLine("编辑框_下分勿催词=[艾特] 您的提款请求正在审核，请耐心稍等！");
                sb.AppendLine("编辑框_客户下分回复内容=[艾特] 已收到回芬[分数]请求");
                sb.AppendLine("编辑框_下分拒绝词=[艾特] 拒绝吓芬，请您联系接单核实原因！");
                sb.AppendLine();
                
                // [封盘设置]
                sb.AppendLine("[封盘设置]");
                sb.AppendLine("开奖发送图片=假");
                sb.AppendLine("下注数据发送开关=真");
                sb.AppendLine("开奖后发送=真");
                sb.AppendLine("下注数据不显示QQ=真");
                sb.AppendLine("禁言提前时间=5");
                sb.AppendLine("下注数据时间=10");
                sb.AppendLine("PC封盘_提醒时间=60");
                sb.AppendLine("PC封盘_提醒开关=真");
                sb.AppendLine("PC封盘_提醒内容=--距离封盘时间还有40秒--[换行]改注加注带改 或者 加");
                sb.AppendLine("PC封盘_封盘时间=20");
                sb.AppendLine("PC封盘_封盘内容========封盘线=======[换行]以上有钱的都接[换行]=====庄显为准=======");
                sb.AppendLine("PC封盘_发送规矩时间=1");
                sb.AppendLine("PC封盘_发送规矩内容=本群如遇卡奖情况，十分钟官网没开奖，本期无效，无需纠结！");
                sb.AppendLine("PC封盘_发送规矩开关=真");
                sb.AppendLine("下注数据发送=核对[换行]-------------------[换行][下注核对]");
                sb.AppendLine("账单发送=開:[一区]+[二区]+[三区]=[开奖号码] [大小单双] [豹顺对子] [龙虎豹][换行]人數:[客户人数] 總分:[总分数][换行]----------------------[换行][账单][换行]----------------------[换行]ls：[开奖历史][换行]龙虎豹ls：[龙虎历史][换行]尾球ls：[尾球历史][换行]豹顺对历史：[豹顺对历史]");
                sb.AppendLine("开奖发送=开:[一区]+[二区]+[三区]=[开奖号码] [大小单双] 第[期数]期");
                sb.AppendLine();
                
                // [下限]
                sb.AppendLine("[下限]");
                sb.AppendLine("单注=10");
                sb.AppendLine("组合=10");
                sb.AppendLine("数字=10");
                sb.AppendLine("极数=0");
                sb.AppendLine("龙虎=10");
                sb.AppendLine("对子=10");
                sb.AppendLine("顺子=10");
                sb.AppendLine("半顺=0");
                sb.AppendLine("杂=0");
                sb.AppendLine("豹子=10");
                sb.AppendLine();
                
                // [上限]
                sb.AppendLine("[上限]");
                sb.AppendLine("单注=50000");
                sb.AppendLine("组合=30000");
                sb.AppendLine("数字=20000");
                sb.AppendLine("极数=0");
                sb.AppendLine("龙虎=10000");
                sb.AppendLine("对子=10000");
                sb.AppendLine("顺子=10000");
                sb.AppendLine("半顺=0");
                sb.AppendLine("杂=0");
                sb.AppendLine("豹子=2000");
                sb.AppendLine("总额=600000");
                sb.AppendLine();
                
                // [内部回复]
                sb.AppendLine("[内部回复]");
                sb.AppendLine("下注显示=[艾特]([旺旺])[换行]本次攻擊:[玩家攻击],余粮:[余粮]");
                sb.AppendLine("托管成功=[艾特]([旺旺])[换行]已为您托管：[托管内容][换行]下局自动为您攻击");
                sb.AppendLine("取消托管=[艾特]([旺旺])[换行]已为您取消托管成功，请重新发起下注！");
                sb.AppendLine("取消下注=[艾特]([旺旺])[换行]取消了下注！！！");
                sb.AppendLine("下分正在处理=[艾特]([旺旺])[换行]下分正在处理中，请稍等！");
                sb.AppendLine("下注不能下分=[艾特]([旺旺])[换行]正在攻擊，不能下芬。");
                sb.AppendLine("下分不能下注=[艾特]([旺旺])[换行]正在下芬，禁止攻擊。");
                sb.AppendLine("攻击上分有效=[艾特]([旺旺])[换行]余粮不足，上芬后录取，[下注内容]");
                sb.AppendLine("模糊匹配提醒=[艾特]([旺旺])[换行]已为您模糊匹配攻擊:[模糊攻击],如果不对请重新发起攻擊");
                sb.AppendLine("已封盘下注无效=[艾特]([旺旺])[换行]攻擊慢了！[换行]未处理：[未处理内容]");
                sb.AppendLine("发1_0分=[艾特]([旺旺])[换行]老板，您的账户余额不足！[换行]当前余粮:[余粮]");
                sb.AppendLine("发1_有分无攻击=[艾特]([旺旺])[换行]您本次暂无攻擊,余粮:[余粮]");
                sb.AppendLine("发1_有分有攻击=[艾特]([旺旺])[换行]本次攻擊:[下注],余粮:[余粮]");
                sb.AppendLine("进群私聊玩家=恭喜发财，私聊都是骗子，请认准管理。");
                sb.AppendLine("私聊尾巴_未封盘=离作业做题结束还有[封盘倒计时]秒");
                sb.AppendLine("私聊尾巴_已封盘=作业做题已结束");
                sb.AppendLine("禁止点09=禁止点09");
                sb.AppendLine();
                
                // [单独数字赔率]
                sb.AppendLine("[单独数字赔率]");
                sb.AppendLine("0=665");
                sb.AppendLine("1=99");
                sb.AppendLine("2=49");
                sb.AppendLine("3=39");
                sb.AppendLine("4=29");
                sb.AppendLine("5=19");
                sb.AppendLine("6=16");
                sb.AppendLine("7=15");
                sb.AppendLine("8=14");
                sb.AppendLine("9=14");
                sb.AppendLine("10=13");
                sb.AppendLine("11=12");
                sb.AppendLine("12=11");
                sb.AppendLine("13=10");
                sb.AppendLine("14=10");
                sb.AppendLine("15=11");
                sb.AppendLine("16=12");
                sb.AppendLine("17=13");
                sb.AppendLine("18=14");
                sb.AppendLine("19=14");
                sb.AppendLine("20=15");
                sb.AppendLine("21=16");
                sb.AppendLine("22=19");
                sb.AppendLine("23=29");
                sb.AppendLine("24=39");
                sb.AppendLine("25=49");
                sb.AppendLine("26=99");
                sb.AppendLine("27=665");
                sb.AppendLine();
                
                // [回水设置]
                sb.AppendLine("[回水设置]");
                sb.AppendLine("回水方式=0");
                sb.AppendLine();
                
                // [流水返点]
                sb.AppendLine("[流水返点]");
                sb.AppendLine("默认百分比=1");
                sb.AppendLine("默认把数=1");
                sb.AppendLine("艾特分理由=反水");
                sb.AppendLine();
                
                // [配置]
                sb.AppendLine("[配置]");
                sb.AppendLine("重复下注_处理方式=2");
                sb.AppendLine("特码格式=0");
                sb.AppendLine("中文下注处理方式=0");
                sb.AppendLine("龙虎玩法_模式=0");
                sb.AppendLine();
                
                // [备用号]
                sb.AppendLine("[备用号]");
                sb.AppendLine("备用群号=");
                
                File.WriteAllText(SettingsIniPath, sb.ToString(), Encoding.GetEncoding("GBK"));
                Logger.Info($"[ZCGDataStorage] 已创建完整设置.ini");
            }
            catch (Exception ex)
            {
                Logger.Error($"[ZCGDataStorage] 创建设置.ini失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 创建默认登录配置.ini
        /// </summary>
        private void CreateDefaultLoginConfigIni()
        {
            try
            {
                var sb = new StringBuilder();
                sb.AppendLine("[登录设置]");
                sb.AppendLine("版本=4.29");
                
                File.WriteAllText(LoginConfigPath, sb.ToString(), Encoding.GetEncoding("GBK"));
                Logger.Info($"[ZCGDataStorage] 已创建默认登录配置.ini");
            }
            catch (Exception ex)
            {
                Logger.Error($"[ZCGDataStorage] 创建登录配置.ini失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 创建默认端口配置
        /// </summary>
        private void CreateDefaultPortConfig()
        {
            try
            {
                var sb = new StringBuilder();
                sb.AppendLine("[端口]");
                sb.AppendLine("端口=14746");
                
                File.WriteAllText(PortConfigPath, sb.ToString(), Encoding.GetEncoding("GBK"));
                Logger.Info($"[ZCGDataStorage] 已创建默认zcg端口.ini");
            }
            catch (Exception ex)
            {
                Logger.Error($"[ZCGDataStorage] 创建端口配置失败: {ex.Message}");
            }
        }

        #region 收发日志记录 (zcg收发日志 - 记录发送到群聊的消息)

        /// <summary>
        /// 记录收发日志 (ZCG原版格式 - 收集发送到群聊的消息)
        /// </summary>
        public void LogMessage(string rqq, string group, string fromQQ, string beingQQ, string remark, string data, string content)
        {
            try
            {
                var now = DateTime.Now;
                var logFile = Path.Combine(LogDir, $"{now:yyyy年MM月dd日HH时}.log");

                var sb = new StringBuilder();
                sb.AppendLine($"{now:yyyy/M/d H:m:s}    ");
                sb.AppendLine($"RQQ:{rqq}");
                sb.AppendLine($"群:{group}");
                sb.AppendLine($"fromQQ:{fromQQ}");
                sb.AppendLine($"beingQQ:{beingQQ}");
                if (!string.IsNullOrEmpty(remark))
                    sb.AppendLine($"备注：{remark}");
                sb.AppendLine($"数据：");
                if (!string.IsNullOrEmpty(data))
                    sb.AppendLine(data);
                if (!string.IsNullOrEmpty(content))
                    sb.AppendLine($"内容：{content}");
                sb.AppendLine();

                lock (_lock)
                {
                    File.AppendAllText(logFile, sb.ToString(), Encoding.GetEncoding("GBK"));
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"[ZCGDataStorage] 记录日志失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 记录系统日志
        /// </summary>
        public void LogSystem(string rqq, string message)
        {
            LogMessage(rqq, "", "", "", "系统日志", "", message);
        }

        /// <summary>
        /// 记录收到群消息 (从群聊收到的消息)
        /// </summary>
        public void LogGroupMessage(string rqq, string groupId, string fromQQ, string content)
        {
            LogMessage(rqq, groupId, fromQQ, "", "收到群消息", "", content);
        }

        /// <summary>
        /// 记录发送群消息 (发送到群聊的消息)
        /// </summary>
        public void LogSendGroupMessage(string rqq, string groupId, string content)
        {
            LogMessage(rqq, groupId, "", "", "发送群消息", "", content);
        }

        /// <summary>
        /// 记录收到私聊消息
        /// </summary>
        public void LogPrivateMessage(string rqq, string fromQQ, string content)
        {
            LogMessage(rqq, "", fromQQ, "", "收到私聊", "", content);
        }

        /// <summary>
        /// 记录发送私聊消息
        /// </summary>
        public void LogSendPrivateMessage(string rqq, string toQQ, string content)
        {
            LogMessage(rqq, "", "", toQQ, "发送私聊", "", content);
        }

        /// <summary>
        /// 记录下注处理日志
        /// </summary>
        public void LogBetProcess(string rqq, string groupId, string fromQQ, string betContent, string result)
        {
            LogMessage(rqq, groupId, fromQQ, "", "下注处理", $"下注内容:{betContent}", $"处理结果:{result}");
        }

        /// <summary>
        /// 记录上分处理日志
        /// </summary>
        public void LogScoreUp(string rqq, string groupId, string playerId, int amount, int balance)
        {
            LogMessage(rqq, groupId, playerId, "", "上分处理", $"上分:{amount}", $"余粮:{balance}");
        }

        /// <summary>
        /// 记录下分处理日志
        /// </summary>
        public void LogScoreDown(string rqq, string groupId, string playerId, int amount, int balance)
        {
            LogMessage(rqq, groupId, playerId, "", "下分处理", $"下分:{amount}", $"余粮:{balance}");
        }

        /// <summary>
        /// 记录开奖结果日志
        /// </summary>
        public void LogLotteryResult(string rqq, string groupId, string period, string result, string resultType)
        {
            LogMessage(rqq, groupId, "", "", "开奖结果", $"期号:{period}|结果:{result}", $"类型:{resultType}");
        }

        /// <summary>
        /// 记录封盘通知日志
        /// </summary>
        public void LogSealNotice(string rqq, string groupId, int countdown)
        {
            LogMessage(rqq, groupId, "", "", "封盘通知", $"倒计时:{countdown}秒", "");
        }

        /// <summary>
        /// 记录账单发送日志
        /// </summary>
        public void LogBillSend(string rqq, string groupId, string period, int playerCount, int totalBet)
        {
            LogMessage(rqq, groupId, "", "", "账单发送", $"期号:{period}|人数:{playerCount}|总分:{totalBet}", "");
        }

        /// <summary>
        /// 记录API调用日志 (XPlugin API调用)
        /// </summary>
        public void LogApiCall(string rqq, string apiCall, string result)
        {
            try
            {
                var now = DateTime.Now;
                var logFile = Path.Combine(LogDir, $"API调用日志_{now:yyyy年MM月dd日}.log");
                
                var sb = new StringBuilder();
                sb.AppendLine($"{now:yyyy年MM月dd日HH时mm分ss秒}   {apiCall}|返回结果:{result}");
                
                lock (_lock)
                {
                    File.AppendAllText(logFile, sb.ToString(), Encoding.GetEncoding("GBK"));
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"记录API日志失败: {ex.Message}");
            }
        }

        #endregion

        #region 数据库操作 (JSON格式，模仿.db文件)

        /// <summary>
        /// 保存设置数据
        /// </summary>
        public void SaveSettings(Dictionary<string, object> settings)
        {
            SaveJsonData(SettingsDbPath, settings);
        }

        /// <summary>
        /// 加载设置数据
        /// </summary>
        public Dictionary<string, object> LoadSettings()
        {
            return LoadJsonData<Dictionary<string, object>>(SettingsDbPath) ?? new Dictionary<string, object>();
        }

        /// <summary>
        /// 保存上下分记录
        /// </summary>
        public void SaveScoreRecords(List<ZCGScoreRecord> records)
        {
            SaveJsonData(ScoreDbPath, records);
        }

        /// <summary>
        /// 加载上下分记录
        /// </summary>
        public List<ZCGScoreRecord> LoadScoreRecords()
        {
            return LoadJsonData<List<ZCGScoreRecord>>(ScoreDbPath) ?? new List<ZCGScoreRecord>();
        }

        /// <summary>
        /// 保存账单数据
        /// </summary>
        public void SaveBills(List<BillRecord> bills)
        {
            SaveJsonData(BillDbPath, bills);
        }

        /// <summary>
        /// 加载账单数据
        /// </summary>
        public List<BillRecord> LoadBills()
        {
            return LoadJsonData<List<BillRecord>>(BillDbPath) ?? new List<BillRecord>();
        }

        /// <summary>
        /// 保存下注/攻击记录
        /// </summary>
        public void SaveBets(List<BetRecord> bets)
        {
            SaveJsonData(BetDbPath, bets);
        }

        /// <summary>
        /// 加载下注/攻击记录
        /// </summary>
        public List<BetRecord> LoadBets()
        {
            return LoadJsonData<List<BetRecord>>(BetDbPath) ?? new List<BetRecord>();
        }

        /// <summary>
        /// 保存玩家姓名
        /// </summary>
        public void SavePlayerNames(Dictionary<string, string> names)
        {
            SaveJsonData(PlayerNameDbPath, names);
        }

        /// <summary>
        /// 加载玩家姓名
        /// </summary>
        public Dictionary<string, string> LoadPlayerNames()
        {
            return LoadJsonData<Dictionary<string, string>>(PlayerNameDbPath) ?? new Dictionary<string, string>();
        }

        /// <summary>
        /// 保存邀请记录
        /// </summary>
        public void SaveInvites(List<InviteRecord> invites)
        {
            SaveJsonData(InviteDbPath, invites);
        }

        /// <summary>
        /// 加载邀请记录
        /// </summary>
        public List<InviteRecord> LoadInvites()
        {
            return LoadJsonData<List<InviteRecord>>(InviteDbPath) ?? new List<InviteRecord>();
        }

        #endregion

        #region 账号列表存储

        /// <summary>
        /// 账号列表文件路径
        /// </summary>
        public string AccountsFilePath => Path.Combine(DataRoot, "accounts.json");

        /// <summary>
        /// 保存账号列表
        /// </summary>
        public void SaveAccounts(List<AccountData> accounts)
        {
            SaveJsonData(AccountsFilePath, accounts);
            Logger.Info($"[ZCGDataStorage] 账号列表已保存，共 {accounts.Count} 个账号");
        }

        /// <summary>
        /// 加载账号列表
        /// </summary>
        public List<AccountData> LoadAccounts()
        {
            var accounts = LoadJsonData<List<AccountData>>(AccountsFilePath) ?? new List<AccountData>();
            Logger.Info($"[ZCGDataStorage] 账号列表已加载，共 {accounts.Count} 个账号");
            return accounts;
        }

        #endregion

        #region 备份功能 (按期号备份)

        /// <summary>
        /// 备份当前期数据
        /// </summary>
        public void BackupPeriod(string period)
        {
            try
            {
                var backupPath = Path.Combine(BackupDir, period);
                Directory.CreateDirectory(backupPath);

                // 复制所有数据库文件到备份目录
                CopyFileIfExists(SettingsDbPath, Path.Combine(backupPath, "设置.db"));
                CopyFileIfExists(ScoreDbPath, Path.Combine(backupPath, "上下分.db"));
                CopyFileIfExists(BillDbPath, Path.Combine(backupPath, "账单.db"));
                CopyFileIfExists(BetDbPath, Path.Combine(backupPath, "攻击.db"));
                CopyFileIfExists(PlayerNameDbPath, Path.Combine(backupPath, "玩家姓名.db"));
                CopyFileIfExists(InviteDbPath, Path.Combine(backupPath, "邀请记录.db"));

                // 保存日期数据库
                var dateDbPath = Path.Combine(backupPath, $"{DateTime.Now:yyyy年M月d日}.db");
                SaveJsonData(dateDbPath, new
                {
                    Period = period,
                    BackupTime = DateTime.Now,
                    Date = DateTime.Now.ToString("yyyy年M月d日")
                });

                Logger.Info($"[ZCGDataStorage] 期数 {period} 数据已备份到: {backupPath}");
            }
            catch (Exception ex)
            {
                Logger.Error($"[ZCGDataStorage] 备份失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 从备份恢复期数据
        /// </summary>
        public bool RestoreFromBackup(string period)
        {
            try
            {
                var backupPath = Path.Combine(BackupDir, period);
                if (!Directory.Exists(backupPath))
                {
                    Logger.Error($"[ZCGDataStorage] 备份不存在: {period}");
                    return false;
                }

                // 从备份目录恢复所有数据库文件
                CopyFileIfExists(Path.Combine(backupPath, "设置.db"), SettingsDbPath);
                CopyFileIfExists(Path.Combine(backupPath, "上下分.db"), ScoreDbPath);
                CopyFileIfExists(Path.Combine(backupPath, "账单.db"), BillDbPath);
                CopyFileIfExists(Path.Combine(backupPath, "攻击.db"), BetDbPath);
                CopyFileIfExists(Path.Combine(backupPath, "玩家姓名.db"), PlayerNameDbPath);
                CopyFileIfExists(Path.Combine(backupPath, "邀请记录.db"), InviteDbPath);

                Logger.Info($"[ZCGDataStorage] 已从备份 {period} 恢复数据");
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error($"[ZCGDataStorage] 恢复失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 获取所有备份期号列表
        /// </summary>
        public List<string> GetBackupPeriods()
        {
            var periods = new List<string>();
            try
            {
                if (Directory.Exists(BackupDir))
                {
                    foreach (var dir in Directory.GetDirectories(BackupDir))
                    {
                        periods.Add(Path.GetFileName(dir));
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"[ZCGDataStorage] 获取备份列表失败: {ex.Message}");
            }
            return periods;
        }

        #endregion

        #region 玩家资料

        /// <summary>
        /// 保存玩家资料
        /// </summary>
        public void SavePlayerProfile(string playerId, PlayerProfile profile)
        {
            var filePath = Path.Combine(PlayerDataDir, $"{playerId}.json");
            SaveJsonData(filePath, profile);
        }

        /// <summary>
        /// 加载玩家资料
        /// </summary>
        public PlayerProfile LoadPlayerProfile(string playerId)
        {
            var filePath = Path.Combine(PlayerDataDir, $"{playerId}.json");
            return LoadJsonData<PlayerProfile>(filePath);
        }

        #endregion

        #region 群成员缓存

        /// <summary>
        /// 保存群成员缓存
        /// </summary>
        public void SaveGroupMembers(string groupId, List<ZCGGroupMemberInfo> members)
        {
            var filePath = Path.Combine(GroupCacheDir, $"{groupId}.json");
            SaveJsonData(filePath, members);
        }

        /// <summary>
        /// 加载群成员缓存
        /// </summary>
        public List<ZCGGroupMemberInfo> LoadGroupMembers(string groupId)
        {
            var filePath = Path.Combine(GroupCacheDir, $"{groupId}.json");
            return LoadJsonData<List<ZCGGroupMemberInfo>>(filePath) ?? new List<ZCGGroupMemberInfo>();
        }

        #endregion

        #region 日期数据库

        /// <summary>
        /// 保存今日数据
        /// </summary>
        public void SaveTodayData(object data)
        {
            var filePath = Path.Combine(DatabaseDir, $"{DateTime.Now:yyyy年M月d日}.db");
            SaveJsonData(filePath, data);
        }

        /// <summary>
        /// 加载指定日期数据
        /// </summary>
        public T LoadDateData<T>(DateTime date) where T : class
        {
            var filePath = Path.Combine(DatabaseDir, $"{date:yyyy年M月d日}.db");
            return LoadJsonData<T>(filePath);
        }

        #endregion

        #region 私有方法

        private void SaveJsonData<T>(string filePath, T data)
        {
            try
            {
                lock (_lock)
                {
                    var json = _serializer.Serialize(data);
                    File.WriteAllText(filePath, json, Encoding.UTF8);
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"[ZCGDataStorage] 保存数据失败 ({filePath}): {ex.Message}");
            }
        }

        private T LoadJsonData<T>(string filePath) where T : class
        {
            try
            {
                if (File.Exists(filePath))
                {
                    var json = File.ReadAllText(filePath, Encoding.UTF8);
                    return _serializer.Deserialize<T>(json);
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"[ZCGDataStorage] 加载数据失败 ({filePath}): {ex.Message}");
            }
            return null;
        }

        private void CopyFileIfExists(string source, string dest)
        {
            if (File.Exists(source))
            {
                File.Copy(source, dest, true);
            }
        }

        #endregion
    }

    #region 数据模型

    /// <summary>
    /// ZCG上下分记录（用于数据存储）
    /// </summary>
    public class ZCGScoreRecord
    {
        public string Id { get; set; }
        public string PlayerId { get; set; }
        public string PlayerName { get; set; }
        public string Type { get; set; } // "上分" or "下分"
        public int Amount { get; set; }
        public int BalanceBefore { get; set; }
        public int BalanceAfter { get; set; }
        public string Period { get; set; }
        public DateTime Time { get; set; }
        public string Operator { get; set; }
        public string Remark { get; set; }
    }

    /// <summary>
    /// 账单记录
    /// </summary>
    public class BillRecord
    {
        public string Id { get; set; }
        public string Period { get; set; }
        public DateTime Time { get; set; }
        public int PlayerCount { get; set; }
        public int TotalBet { get; set; }
        public int TotalWin { get; set; }
        public int Profit { get; set; }
        public string Result { get; set; } // 开奖结果 "1+2+3=6"
        public string ResultType { get; set; } // "大单" "小双" 等
        public List<PlayerBillItem> Players { get; set; }
    }

    /// <summary>
    /// 玩家账单项
    /// </summary>
    public class PlayerBillItem
    {
        public string PlayerId { get; set; }
        public string PlayerName { get; set; }
        public int BetAmount { get; set; }
        public int WinAmount { get; set; }
        public int Profit { get; set; }
        public List<string> BetDetails { get; set; }
    }

    /// <summary>
    /// 邀请记录
    /// </summary>
    public class InviteRecord
    {
        public string Id { get; set; }
        public string InviterId { get; set; }
        public string InviterName { get; set; }
        public string InviteeId { get; set; }
        public string InviteeName { get; set; }
        public DateTime Time { get; set; }
        public int Reward { get; set; }
    }

    // BetRecord 类定义在 PlayerService.cs 中

    /// <summary>
    /// 玩家资料
    /// </summary>
    public class PlayerProfile
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Nickname { get; set; }
        public int Balance { get; set; }
        public int TotalBet { get; set; }
        public int TotalWin { get; set; }
        public int TotalProfit { get; set; }
        public DateTime FirstSeen { get; set; }
        public DateTime LastSeen { get; set; }
        public int BetCount { get; set; }
        public int WinCount { get; set; }
        public string Level { get; set; } // 会员等级
        public string Remark { get; set; }
    }

    /// <summary>
    /// 账号数据（用于持久化存储）
    /// </summary>
    public class AccountData
    {
        public string Id { get; set; } // 连接ID
        public string Account { get; set; } // 旺商聊账号
        public string Password { get; set; } // 加密后的密码
        public string Nickname { get; set; } // 昵称
        public string Wwid { get; set; } // 旺旺ID
        public string GroupId { get; set; } // 绑定群号
        public string Status { get; set; } // 状态
        public bool AutoMode { get; set; } // 自动模式
        public DateTime CreateTime { get; set; } // 创建时间
        public DateTime LastLoginTime { get; set; } // 最后登录时间
    }

    /// <summary>
    /// ZCG群成员信息（用于数据存储）
    /// </summary>
    public class ZCGGroupMemberInfo
    {
        public string MemberId { get; set; }
        public string Nickname { get; set; }
        public string Card { get; set; } // 群名片
        public string Role { get; set; } // owner/admin/member
        public DateTime JoinTime { get; set; }
        public DateTime LastActiveTime { get; set; }
    }

    #endregion
}
