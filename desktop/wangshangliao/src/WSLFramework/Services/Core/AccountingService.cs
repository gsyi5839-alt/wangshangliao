using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using WSLFramework.Utils;

namespace WSLFramework.Services
{
    /// <summary>
    /// 算账服务 - 根据最底层消息协议文档第十节实现
    /// 负责完整的算账流程管理：开盘→封盘→开奖→结算
    /// </summary>
    public class AccountingService
    {
        #region 单例模式

        private static readonly Lazy<AccountingService> _instance =
            new Lazy<AccountingService>(() => new AccountingService());

        public static AccountingService Instance => _instance.Value;

        #endregion

        #region 常量

        /// <summary>默认封盘提醒时间(秒)</summary>
        public const int DEFAULT_SEAL_WARN_TIME = 40;

        /// <summary>默认封盘时间(秒)</summary>
        public const int DEFAULT_SEAL_TIME = 2;

        /// <summary>默认规矩发送时间(秒)</summary>
        public const int DEFAULT_RULE_TIME = 10;

        #endregion

        #region 私有字段

        private CDPBridge _cdpBridge;
        private XPluginClient _xpluginClient;
        private string _robotId;
        private string _groupId;
        private CancellationTokenSource _cycleCts;
        private Task _cycleTask;
        private volatile bool _isRunning;

        #endregion

        #region 配置属性

        /// <summary>封盘提醒时间(秒)</summary>
        public int SealWarnTime { get; set; } = DEFAULT_SEAL_WARN_TIME;

        /// <summary>封盘时间(秒)</summary>
        public int SealTime { get; set; } = DEFAULT_SEAL_TIME;

        /// <summary>规矩发送时间(秒)</summary>
        public int RuleTime { get; set; } = DEFAULT_RULE_TIME;

        /// <summary>封盘提醒内容</summary>
        public string SealWarnContent { get; set; } = "--距离封盘时间还有{0}秒--";

        /// <summary>封盘内容</summary>
        public string SealContent { get; set; } = "==加封盘线==\n以上有钱的都接\n==庄显为准==";

        /// <summary>核对内容</summary>
        public string CheckContent { get; set; } = "核对\n-------------------";

        /// <summary>规矩内容</summary>
        public string RuleContent { get; set; } = "本群如遇卡奖情况，十分钟官网没开奖，本期无效！！！！";

        /// <summary>是否正在运行</summary>
        public bool IsRunning => _isRunning;

        /// <summary>当前期数</summary>
        public int CurrentPeriod { get; private set; }

        /// <summary>当前阶段</summary>
        public AccountingPhase CurrentPhase { get; private set; } = AccountingPhase.Idle;

        #endregion

        #region 事件

        public event Action<string> OnLog;
        public event Action<AccountingPhase> OnPhaseChanged;
        public event Action<LotteryResultData> OnLotteryResult;
        public event Action<string> OnMessageSent;

        #endregion

        #region 构造函数

        private AccountingService()
        {
        }

        #endregion

        #region 初始化

        /// <summary>
        /// 初始化服务
        /// </summary>
        public void Initialize(CDPBridge cdpBridge, XPluginClient xpluginClient, 
            string robotId, string groupId)
        {
            _cdpBridge = cdpBridge;
            _xpluginClient = xpluginClient;
            _robotId = robotId;
            _groupId = groupId;
            
            Log($"算账服务初始化: robotId={robotId}, groupId={groupId}");
        }

        /// <summary>
        /// 从配置加载设置
        /// </summary>
        public void LoadConfig(ConfigService config)
        {
            if (config == null) return;

            // 尝试从配置读取
            // SealWarnTime = config.GetInt("封盘设置", "PC封盘_提醒时间", DEFAULT_SEAL_WARN_TIME);
            // SealTime = config.GetInt("封盘设置", "PC封盘_封盘时间", DEFAULT_SEAL_TIME);
            // ...
        }

        #endregion

        #region 算账周期控制

        /// <summary>
        /// 开始算账循环
        /// </summary>
        public async Task StartCycleAsync()
        {
            if (_isRunning)
            {
                Log("算账周期已在运行中");
                return;
            }

            _isRunning = true;
            _cycleCts = new CancellationTokenSource();

            Log("开始算账循环");
            SetPhase(AccountingPhase.Initializing);

            _cycleTask = Task.Run(() => RunCycleLoopAsync(_cycleCts.Token));
        }

        /// <summary>
        /// 停止算账循环
        /// </summary>
        public async Task StopCycleAsync()
        {
            if (!_isRunning)
                return;

            Log("停止算账循环");
            _cycleCts?.Cancel();

            if (_cycleTask != null)
            {
                await Task.WhenAny(_cycleTask, Task.Delay(5000));
            }

            _isRunning = false;
            SetPhase(AccountingPhase.Idle);
        }

        /// <summary>
        /// 运行算账循环
        /// </summary>
        private async Task RunCycleLoopAsync(CancellationToken token)
        {
            // 算账周期: 210秒 (3分30秒)
            const int CYCLE_SECONDS = 210;

            while (!token.IsCancellationRequested)
            {
                try
                {
                    // 计算距离下一期开奖的时间
                    var now = DateTime.Now;
                    var secondsInCycle = (now.Minute * 60 + now.Second) % CYCLE_SECONDS;
                    var secondsToNextDraw = CYCLE_SECONDS - secondsInCycle;

                    // 如果距离开奖时间大于封盘提醒时间，先等待
                    if (secondsToNextDraw > SealWarnTime)
                    {
                        // 1. 开盘阶段 - 解除禁言
                        await RunOpenPhaseAsync(token);

                        // 等待到封盘提醒时间
                        var waitSeconds = secondsToNextDraw - SealWarnTime;
                        if (waitSeconds > 0)
                        {
                            await Task.Delay(waitSeconds * 1000, token);
                        }
                    }

                    // 2. 发送封盘提醒
                    await SendSealWarnAsync(token);

                    // 3. 等待到封盘时间 (开奖前2秒)
                    now = DateTime.Now;
                    secondsInCycle = (now.Minute * 60 + now.Second) % CYCLE_SECONDS;
                    secondsToNextDraw = CYCLE_SECONDS - secondsInCycle;
                    
                    var waitToSeal = secondsToNextDraw - SealTime;
                    if (waitToSeal > 0)
                    {
                        await Task.Delay(waitToSeal * 1000, token);
                    }

                    // 4. 封盘阶段 - 开启禁言
                    await RunSealPhaseAsync(token);

                    // 5. 等待开奖
                    await Task.Delay(SealTime * 1000, token);

                    // 6. 开奖阶段
                    await RunDrawPhaseAsync(token);

                    // 7. 短暂延迟后继续下一周期
                    await Task.Delay(1000, token);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    Log($"算账循环异常: {ex.Message}");
                    await Task.Delay(5000, token);
                }
            }
        }

        #endregion

        #region 各阶段实现

        /// <summary>
        /// 开盘阶段
        /// </summary>
        private async Task RunOpenPhaseAsync(CancellationToken token)
        {
            SetPhase(AccountingPhase.Opening);
            Log("开盘阶段 - 解除禁言");

            // 解除禁言
            await SetGroupMuteAsync(false);

            // 发送开盘提醒
            var content = string.Format(SealWarnContent, SealWarnTime);
            await SendGroupMessageAsync(content);
        }

        /// <summary>
        /// 发送封盘提醒
        /// </summary>
        private async Task SendSealWarnAsync(CancellationToken token)
        {
            SetPhase(AccountingPhase.Warning);
            Log("发送封盘提醒");

            var content = string.Format(SealWarnContent, SealWarnTime);
            await SendGroupMessageAsync(content);
        }

        /// <summary>
        /// 封盘阶段
        /// </summary>
        private async Task RunSealPhaseAsync(CancellationToken token)
        {
            SetPhase(AccountingPhase.Sealing);
            Log("封盘阶段 - 开启禁言");

            // 开启禁言
            await SetGroupMuteAsync(true);

            // 发送封盘消息
            await SendGroupMessageAsync(SealContent);

            // 发送核对
            await SendGroupMessageAsync(CheckContent);
        }

        /// <summary>
        /// 开奖阶段
        /// </summary>
        private async Task RunDrawPhaseAsync(CancellationToken token)
        {
            SetPhase(AccountingPhase.Drawing);
            Log("开奖阶段");

            // 获取开奖结果
            var lottery = LotteryService.Instance;
            var result = await lottery.FetchLatestResultAsync();

            if (result != null)
            {
                // 解析期数
                int.TryParse(result.Period, out var periodNum);
                CurrentPeriod = periodNum;
                
                // 构建开奖消息
                var resultData = CalculateResult(
                    result.Num1, result.Num2, result.Num3, periodNum);

                // 发送开奖结果
                await SendLotteryResultAsync(resultData);

                // 触发开奖事件
                OnLotteryResult?.Invoke(resultData);

                // 进入结算阶段
                SetPhase(AccountingPhase.Settling);

                // 结算玩家
                await SettlePlayersAsync(resultData);
            }
            else
            {
                Log("获取开奖结果失败");
            }
        }

        /// <summary>
        /// 结算玩家
        /// </summary>
        private async Task SettlePlayersAsync(LotteryResultData result)
        {
            Log("开始结算玩家");

            // 获取下注记录并结算
            var settlement = SettlementService.Instance;
            // await settlement.SettleAsync(result);

            Log("玩家结算完成");
        }

        #endregion

        #region 消息发送

        /// <summary>
        /// 发送群消息
        /// </summary>
        public async Task<bool> SendGroupMessageAsync(string content)
        {
            // BUG FIX: 检查必要参数
            if (string.IsNullOrEmpty(_groupId))
            {
                Log("发送群消息失败: 群号未设置");
                return false;
            }

            if (string.IsNullOrEmpty(content))
            {
                Log("发送群消息失败: 消息内容为空");
                return false;
            }

            try
            {
                bool success = false;

                // 优先使用 xplugin TCP
                if (_xpluginClient != null && _xpluginClient.IsConnected && !string.IsNullOrEmpty(_robotId))
                {
                    var response = await _xpluginClient.SendGroupMessageAsync(
                        _robotId, content, _groupId);
                    success = XPluginClient.IsSendMessageSuccess(
                        ExtractBase64Result(response));
                }
                // 备用 CDP
                else if (_cdpBridge != null && _cdpBridge.IsConnected)
                {
                    success = await _cdpBridge.SendGroupMessageAsync(_groupId, content);
                }
                else
                {
                    Log("发送群消息失败: 无可用的连接 (xplugin 和 CDP 均未连接)");
                    return false;
                }

                var displayContent = content.Length > 30 ? content.Substring(0, 30) + "..." : content;
                if (success)
                {
                    OnMessageSent?.Invoke(content);
                    Log($"发送群消息: {displayContent}");
                }
                else
                {
                    Log($"发送群消息失败: {displayContent}");
                }

                return success;
            }
            catch (Exception ex)
            {
                Log($"发送群消息异常: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 设置群禁言
        /// </summary>
        public async Task<bool> SetGroupMuteAsync(bool mute)
        {
            // BUG FIX: 检查必要参数
            if (string.IsNullOrEmpty(_groupId))
            {
                Log("设置群禁言失败: 群号未设置");
                return false;
            }

            try
            {
                bool success = false;

                // 优先使用 xplugin TCP
                if (_xpluginClient != null && _xpluginClient.IsConnected && !string.IsNullOrEmpty(_robotId))
                {
                    var response = await _xpluginClient.SetGroupMuteAsync(
                        _robotId, _groupId, mute);
                    success = XPluginClient.IsMuteSuccess(
                        ExtractBase64Result(response));
                }
                // 备用 CDP
                else if (_cdpBridge != null && _cdpBridge.IsConnected)
                {
                    success = await _cdpBridge.MuteAllAsync(_groupId, mute);
                }
                else
                {
                    Log("设置群禁言失败: 无可用的连接");
                    return false;
                }

                Log($"设置群禁言: {(mute ? "开启" : "解除")} - {(success ? "成功" : "失败")}");
                return success;
            }
            catch (Exception ex)
            {
                Log($"设置群禁言异常: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 发送@消息
        /// </summary>
        public async Task<bool> SendAtMessageAsync(string userId, string suffix = "")
        {
            var content = $"[@{userId}] {suffix}".Trim();
            return await SendGroupMessageAsync(content);
        }

        /// <summary>
        /// 发送开奖结果消息
        /// </summary>
        private async Task SendLotteryResultAsync(LotteryResultData result)
        {
            // 发送开奖号码
            var resultMsg = $"开:{result.Num1}+{result.Num2}+{result.Num3}={result.Total} " +
                           $"{result.SizeShort}{result.ParityShort} 第{result.Period}期";
            await SendGroupMessageAsync(resultMsg);

            // 发送详细统计
            var statsMsg = $"開:{result.Num1} + {result.Num2} + {result.Num3} = {result.Total} " +
                          $"{result.SizeShort}{result.ParityShort} -- {result.DragonShort}\n" +
                          $"人數:{result.PlayerCount}  總分:{result.TotalScore}\n" +
                          $"----------------------";
            await SendGroupMessageAsync(statsMsg);
        }

        #endregion

        #region 开奖计算

        /// <summary>
        /// 计算开奖结果
        /// 根据最底层消息协议文档第十节
        /// </summary>
        public LotteryResultData CalculateResult(int num1, int num2, int num3, int period)
        {
            var total = num1 + num2 + num3;

            // 大小判断 (11为界)
            var size = total >= 11 ? "大" : "小";
            var sizeShort = total >= 11 ? "D" : "X";

            // 单双判断
            var parity = total % 2 == 1 ? "单" : "双";
            var parityShort = total % 2 == 1 ? "D" : "S";

            // 龙虎判断 (比较第一位和第三位)
            string dragon, dragonShort;
            if (num1 > num3)
            {
                dragon = "龙";
                dragonShort = "L";
            }
            else if (num1 < num3)
            {
                dragon = "虎";
                dragonShort = "H";
            }
            else
            {
                dragon = "豹";
                dragonShort = "B";
            }

            return new LotteryResultData
            {
                Period = period,
                Num1 = num1,
                Num2 = num2,
                Num3 = num3,
                Total = total,
                Size = size,
                SizeShort = sizeShort,
                Parity = parity,
                ParityShort = parityShort,
                Dragon = dragon,
                DragonShort = dragonShort
            };
        }

        #endregion

        #region 辅助方法

        /// <summary>
        /// 设置当前阶段
        /// </summary>
        private void SetPhase(AccountingPhase phase)
        {
            if (CurrentPhase != phase)
            {
                CurrentPhase = phase;
                OnPhaseChanged?.Invoke(phase);
                Log($"阶段变更: {phase}");
            }
        }

        /// <summary>
        /// 从响应中提取 Base64 结果
        /// </summary>
        private string ExtractBase64Result(string response)
        {
            if (string.IsNullOrEmpty(response))
                return "";

            var idx = response.IndexOf("返回结果:");
            if (idx >= 0)
            {
                var result = response.Substring(idx + 5).Trim();
                var endIdx = result.IndexOf('\n');
                if (endIdx > 0)
                    result = result.Substring(0, endIdx);
                return result;
            }
            return "";
        }

        private void Log(string message)
        {
            Logger.Info($"[Accounting] {message}");
            OnLog?.Invoke(message);
        }

        #endregion
    }

    #region 算账阶段枚举

    /// <summary>
    /// 算账阶段
    /// </summary>
    public enum AccountingPhase
    {
        /// <summary>空闲</summary>
        Idle = 0,
        /// <summary>初始化中</summary>
        Initializing = 1,
        /// <summary>开盘阶段 (等待下注)</summary>
        Opening = 2,
        /// <summary>提醒阶段 (封盘提醒)</summary>
        Warning = 3,
        /// <summary>封盘阶段 (停止下注)</summary>
        Sealing = 4,
        /// <summary>开奖阶段 (等待结果)</summary>
        Drawing = 5,
        /// <summary>结算阶段 (计算盈亏)</summary>
        Settling = 6
    }

    #endregion

    #region 开奖结果数据

    /// <summary>
    /// 开奖结果数据 (根据最底层消息协议文档)
    /// </summary>
    public class LotteryResultData
    {
        /// <summary>期数</summary>
        public int Period { get; set; }

        /// <summary>第一个数字</summary>
        public int Num1 { get; set; }

        /// <summary>第二个数字</summary>
        public int Num2 { get; set; }

        /// <summary>第三个数字</summary>
        public int Num3 { get; set; }

        /// <summary>总和</summary>
        public int Total { get; set; }

        /// <summary>大小 (大/小)</summary>
        public string Size { get; set; }

        /// <summary>大小简写 (D/X)</summary>
        public string SizeShort { get; set; }

        /// <summary>单双 (单/双)</summary>
        public string Parity { get; set; }

        /// <summary>单双简写 (D/S)</summary>
        public string ParityShort { get; set; }

        /// <summary>龙虎 (龙/虎/豹)</summary>
        public string Dragon { get; set; }

        /// <summary>龙虎简写 (L/H/B)</summary>
        public string DragonShort { get; set; }

        /// <summary>玩家人数</summary>
        public int PlayerCount { get; set; }

        /// <summary>总积分</summary>
        public int TotalScore { get; set; }

        /// <summary>
        /// 获取显示字符串
        /// </summary>
        public string GetDisplayString()
        {
            return $"開:{Num1} + {Num2} + {Num3} = {Total} {SizeShort}{ParityShort} -- {DragonShort}";
        }

        /// <summary>
        /// 获取简短结果
        /// </summary>
        public string GetShortResult()
        {
            return $"{Num1}+{Num2}+{Num3}={Total} {SizeShort}{ParityShort}";
        }
    }

    #endregion
}
