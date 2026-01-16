using System;
using System.Threading.Tasks;
using WSLFramework.Services.EventDriven.Network;

namespace WSLFramework.Services.EventDriven
{
    /// <summary>
    /// 旺商聊消息发送器 - 直接发送软件预设内容
    /// 不使用消息构建器，直接集成 ZCGResponseFormatter 和 ConfigService
    /// </summary>
    public class WSLMessageSender
    {
        private readonly WSLConnectionManager _connection;
        private readonly Action<string> _log;
        
        /// <summary>发送成功事件</summary>
        public event Action<string, string> OnMessageSent;
        
        /// <summary>发送失败事件</summary>
        public event Action<string, string, string> OnSendFailed;

        public WSLMessageSender(WSLConnectionManager connection, Action<string> logAction = null)
        {
            _connection = connection ?? throw new ArgumentNullException(nameof(connection));
            _log = logAction;
        }

        #region 基础发送

        /// <summary>
        /// 发送群消息 (纯文本)
        /// </summary>
        public async Task<bool> SendGroupAsync(string groupId, string content)
        {
            if (string.IsNullOrEmpty(groupId) || string.IsNullOrEmpty(content))
                return false;

            if (_connection == null || !_connection.IsConnected)
            {
                Log($"未连接，无法发送到群 {groupId}");
                OnSendFailed?.Invoke(groupId, content, "未连接");
                return false;
            }

            try
            {
                // BUG修复: 安全的日志预览
                string preview = content.Length > 50 ? content.Substring(0, 50) + "..." : content;
                Log($"发送群消息: {groupId} -> {preview}");
                bool result = await _connection.SendGroupMessageAsync(groupId, content);
                
                if (result)
                {
                    OnMessageSent?.Invoke(groupId, content);
                }
                else
                {
                    OnSendFailed?.Invoke(groupId, content, "发送失败");
                }
                
                return result;
            }
            catch (Exception ex)
            {
                Log($"发送异常: {ex.Message}");
                OnSendFailed?.Invoke(groupId, content, ex.Message);
                return false;
            }
        }

        /// <summary>
        /// 发送私聊消息
        /// </summary>
        public async Task<bool> SendPrivateAsync(string userId, string content)
        {
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(content))
                return false;

            if (_connection == null || !_connection.IsConnected)
            {
                Log($"未连接，无法发送私聊 {userId}");
                return false;
            }

            try
            {
                // BUG修复: 安全的日志预览
                string preview = content.Length > 50 ? content.Substring(0, 50) + "..." : content;
                Log($"发送私聊: {userId} -> {preview}");
                return await _connection.SendPrivateMessageAsync(userId, content);
            }
            catch (Exception ex)
            {
                Log($"发送私聊异常: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 向多个群发送同一消息
        /// </summary>
        public async Task SendToGroupsAsync(string[] groupIds, string content, int delayMs = 100)
        {
            if (groupIds == null || groupIds.Length == 0) return;

            foreach (var groupId in groupIds)
            {
                await SendGroupAsync(groupId, content);
                if (delayMs > 0)
                    await Task.Delay(delayMs);
            }
        }

        #endregion

        #region 预设消息发送 - 直接使用软件配置

        /// <summary>
        /// 发送余额查询回复 - 使用 ZCGResponseFormatter
        /// </summary>
        public async Task<bool> SendBalanceReplyAsync(string groupId, string playerId, string nickname, int balance)
        {
            var content = ZCGResponseFormatter.FormatBalanceQuery(playerId, nickname, balance);
            return await SendGroupAsync(groupId, content);
        }

        /// <summary>
        /// 发送上分成功回复
        /// </summary>
        public async Task<bool> SendUpSuccessAsync(string groupId, string playerId, int amount, int newBalance)
        {
            var content = ZCGResponseFormatter.FormatUpSuccess(playerId, amount, newBalance);
            return await SendGroupAsync(groupId, content);
        }

        /// <summary>
        /// 发送下分成功回复
        /// </summary>
        public async Task<bool> SendDownSuccessAsync(string groupId, string playerId, int amount, int newBalance)
        {
            var content = ZCGResponseFormatter.FormatDownSuccess(playerId, amount, newBalance);
            return await SendGroupAsync(groupId, content);
        }

        /// <summary>
        /// 发送下注成功回复
        /// </summary>
        public async Task<bool> SendBetSuccessAsync(string groupId, string playerId, string betType, int amount, int newBalance)
        {
            var content = ZCGResponseFormatter.FormatBetSuccess(playerId, betType, amount, newBalance);
            return await SendGroupAsync(groupId, content);
        }

        /// <summary>
        /// 发送余额不足回复
        /// </summary>
        public async Task<bool> SendInsufficientBalanceAsync(string groupId, string playerId, string betContent, int currentScore)
        {
            var content = ZCGResponseFormatter.FormatScoreInsufficientWithBet(playerId, currentScore, betContent);
            return await SendGroupAsync(groupId, content);
        }

        /// <summary>
        /// 发送封盘期间下注失败
        /// </summary>
        public async Task<bool> SendBetClosedAsync(string groupId, string playerId)
        {
            var content = ZCGResponseFormatter.FormatBetClosed(playerId);
            return await SendGroupAsync(groupId, content);
        }

        /// <summary>
        /// 发送开奖结果
        /// </summary>
        public async Task<bool> SendOpenResultAsync(string groupId, int num1, int num2, int num3, string period)
        {
            var content = ZCGResponseFormatter.FormatOpenResult(num1, num2, num3, period);
            return await SendGroupAsync(groupId, content);
        }

        /// <summary>
        /// 发送托管成功消息
        /// </summary>
        public async Task<bool> SendTrusteeSuccessAsync(string groupId, string playerId, string trusteeName)
        {
            var content = ZCGResponseFormatter.FormatTrusteeSuccess(playerId, trusteeName);
            return await SendGroupAsync(groupId, content);
        }

        /// <summary>
        /// 发送取消托管消息
        /// </summary>
        public async Task<bool> SendTrusteeCancelledAsync(string groupId, string playerId)
        {
            var content = ZCGResponseFormatter.FormatTrusteeCancelled(playerId);
            return await SendGroupAsync(groupId, content);
        }

        #endregion

        #region 定时消息 - 封盘/倒计时

        /// <summary>
        /// 发送倒计时提醒 - 使用配置内容
        /// </summary>
        public async Task<bool> SendCountdownAsync(string groupId, int seconds)
        {
            // 40秒使用配置的提醒内容
            if (seconds == 40)
            {
                var config = ConfigService.Instance;
                return await SendGroupAsync(groupId, config.SealRemindContent);
            }
            
            var content = ZCGResponseFormatter.FormatCountdown(seconds);
            return await SendGroupAsync(groupId, content);
        }

        /// <summary>
        /// 发送封盘线消息 - 使用配置内容
        /// </summary>
        public async Task<bool> SendSealLineAsync(string groupId)
        {
            var config = ConfigService.Instance;
            return await SendGroupAsync(groupId, config.SealContent);
        }

        /// <summary>
        /// 发送卡奖提醒 - 使用配置内容
        /// </summary>
        public async Task<bool> SendStuckNoticeAsync(string groupId)
        {
            var config = ConfigService.Instance;
            return await SendGroupAsync(groupId, config.RuleContent);
        }

        /// <summary>
        /// 发送核对消息 - 使用配置内容
        /// </summary>
        public async Task<bool> SendCheckAsync(string groupId)
        {
            var config = ConfigService.Instance;
            return await SendGroupAsync(groupId, config.BetDataContent);
        }

        /// <summary>
        /// 发送封盘流程 (完整)
        /// </summary>
        public async Task SendSealingSequenceAsync(string groupId)
        {
            // 1. 封盘线
            await SendSealLineAsync(groupId);
            await Task.Delay(1000);
            
            // 2. 卡奖提醒
            await SendStuckNoticeAsync(groupId);
        }

        #endregion

        #region 使用 ConfigService 模板发送

        /// <summary>
        /// 发送下注显示回复 - 使用配置模板
        /// </summary>
        public async Task<bool> SendBetShowReplyAsync(string groupId, string nickname, string shortId, string betContent, int balance)
        {
            var config = ConfigService.Instance;
            var content = config.ReplyBetShow
                .Replace("[昵称]", nickname)
                .Replace("[短ID]", shortId)
                .Replace("{下注内容}", betContent)
                .Replace("{余粮}", balance.ToString());
            
            return await SendGroupAsync(groupId, content);
        }

        /// <summary>
        /// 发送余额查询回复 - 使用配置模板
        /// </summary>
        public async Task<bool> SendQueryReplyAsync(string groupId, string nickname, string shortId, int balance, string betContent = null)
        {
            var config = ConfigService.Instance;
            string content;
            
            if (balance <= 0)
            {
                content = config.ReplyQuery0Score
                    .Replace("[昵称]", nickname)
                    .Replace("[短ID]", shortId);
            }
            else if (!string.IsNullOrEmpty(betContent))
            {
                content = config.ReplyQueryHasAttack
                    .Replace("[昵称]", nickname)
                    .Replace("[短ID]", shortId)
                    .Replace("{下注内容}", betContent)
                    .Replace("{余粮}", balance.ToString());
            }
            else
            {
                content = config.ReplyQueryHasScore
                    .Replace("[昵称]", nickname)
                    .Replace("[短ID]", shortId)
                    .Replace("{余粮}", balance.ToString());
            }
            
            return await SendGroupAsync(groupId, content);
        }

        /// <summary>
        /// 发送封盘下注无效回复 - 使用配置模板
        /// </summary>
        public async Task<bool> SendBetClosedReplyAsync(string groupId, string nickname, string shortId, int remainSeconds)
        {
            var config = ConfigService.Instance;
            var content = config.ReplyBetClosed
                .Replace("[昵称]", nickname)
                .Replace("[短ID]", shortId)
                .Replace("{剩余时间}", remainSeconds.ToString());
            
            return await SendGroupAsync(groupId, content);
        }

        /// <summary>
        /// 发送上分到账回复 - 使用配置模板
        /// </summary>
        public async Task<bool> SendUpArrivedReplyAsync(string groupId, string nickname, int amount, int balance)
        {
            var config = ConfigService.Instance;
            var content = config.ReplyUpArrived
                .Replace("[昵称]", nickname)
                .Replace("{额度}", amount.ToString())
                .Replace("{余粮}", balance.ToString());
            
            return await SendGroupAsync(groupId, content);
        }

        /// <summary>
        /// 发送取消下注回复 - 使用配置模板
        /// </summary>
        public async Task<bool> SendBetCancelledReplyAsync(string groupId, string nickname, string shortId)
        {
            var config = ConfigService.Instance;
            var content = config.ReplyBetCancelled
                .Replace("[昵称]", nickname)
                .Replace("[短ID]", shortId);
            
            return await SendGroupAsync(groupId, content);
        }

        /// <summary>
        /// 发送余额不足回复 - 使用配置模板
        /// </summary>
        public async Task<bool> SendAttackValidReplyAsync(string groupId, string nickname, string shortId, string betContent)
        {
            var config = ConfigService.Instance;
            var content = config.ReplyAttackValid
                .Replace("[昵称]", nickname)
                .Replace("[短ID]", shortId)
                .Replace("{下注内容}", betContent);
            
            return await SendGroupAsync(groupId, content);
        }

        /// <summary>
        /// 发送进群欢迎私聊 - 使用配置模板
        /// </summary>
        public async Task<bool> SendJoinWelcomeAsync(string userId)
        {
            var config = ConfigService.Instance;
            return await SendPrivateAsync(userId, config.ReplyJoinPrivate);
        }

        #endregion

        #region 使用 AutoReplyService

        /// <summary>
        /// 根据消息内容获取自动回复
        /// </summary>
        public string GetAutoReply(string content)
        {
            return AutoReplyService.Instance.GetReply(content);
        }

        /// <summary>
        /// 发送自动回复 (如果有匹配)
        /// </summary>
        public async Task<bool> SendAutoReplyIfMatchedAsync(string groupId, string content)
        {
            var reply = GetAutoReply(content);
            if (!string.IsNullOrEmpty(reply))
            {
                return await SendGroupAsync(groupId, reply);
            }
            return false;
        }

        #endregion

        #region 私聊回复 - 数字1/2查询

        /// <summary>
        /// 处理私聊消息并回复
        /// 数字1 = 余额查询
        /// 数字2 = 历史记录查询
        /// </summary>
        public async Task<bool> HandlePrivateQueryAsync(string userId, string nickname, string content, int countdown)
        {
            if (string.IsNullOrWhiteSpace(content))
                return false;

            content = content.Trim();
            string response = null;

            // 数字1 - 余额查询
            if (content == "1" || content == "查" || content == "余额")
            {
                var balance = ScoreService.Instance.GetBalance(userId);
                var shortId = GetShortId(userId);
                response = FormatPrivateBalanceReply(nickname, shortId, (int)balance, countdown);
            }
            // 数字2 - 历史记录查询
            else if (content == "2" || content == "历史" || content == "记录")
            {
                response = await FormatPrivateHistoryReplyAsync(countdown);
            }

            if (!string.IsNullOrEmpty(response))
            {
                Log($"私聊回复: {userId} -> {response.Substring(0, Math.Min(50, response.Length))}...");
                return await SendPrivateAsync(userId, response);
            }

            return false;
        }

        /// <summary>
        /// 格式化私聊余额回复
        /// 格式: 拉(7813)
        ///       老板，您的账户余额不足！
        ///       当前余粮:0
        ///       离作业做题结束还有99秒
        /// </summary>
        private string FormatPrivateBalanceReply(string nickname, string shortId, int balance, int countdown)
        {
            var sb = new System.Text.StringBuilder();
            
            // 昵称和短ID
            if (!string.IsNullOrEmpty(nickname))
            {
                sb.AppendLine($"{nickname}({shortId})");
            }
            else
            {
                sb.AppendLine($"({shortId})");
            }
            
            // 余额信息
            if (balance <= 0)
            {
                sb.AppendLine("老板，您的账户余额不足！");
                sb.AppendLine("当前余粮:0");
            }
            else
            {
                sb.AppendLine($"老板，您的账户有余额！");
                sb.AppendLine($"当前余粮:{balance}");
            }
            
            // 倒计时
            sb.Append($"离作业做题结束还有{countdown}秒");
            
            return sb.ToString();
        }

        /// <summary>
        /// 格式化私聊历史记录回复
        /// 格式: ls：09 17 13 10 13 05 08 17 22 11
        ///       龙虎豹ls：L B H H H B B B H B
        ///       尾球ls：9 4 6 2 7 2 3 9 8 2
        ///       豹顺对历史：对 -- -- -- -- -- -- -- -- --
        ///       离作业做题结束还有119秒
        /// </summary>
        private async Task<string> FormatPrivateHistoryReplyAsync(int countdown)
        {
            var sb = new System.Text.StringBuilder();
            
            try
            {
                // 获取最近10期开奖结果
                var lottery = LotteryService.Instance;
                var results = await lottery.GetRecentResultsAsync(10);
                
                if (results == null || results.Count == 0)
                {
                    sb.AppendLine("暂无历史记录");
                    sb.Append($"离作业做题结束还有{countdown}秒");
                    return sb.ToString();
                }
                
                // 和值历史 ls：09 17 13...
                sb.Append("ls：");
                foreach (var r in results)
                {
                    sb.Append($"{r.Sum:D2} ");
                }
                sb.AppendLine();
                
                // 龙虎豹历史 (L=龙/大, H=虎/小, B=豹/相等)
                // 根据第一个数字和第三个数字比较
                sb.Append("龙虎豹ls：");
                foreach (var r in results)
                {
                    sb.Append($"{GetLongHuBaoChar(r.Num1, r.Num3)} ");
                }
                sb.AppendLine();
                
                // 尾球历史 (和值的个位数)
                sb.Append("尾球ls：");
                foreach (var r in results)
                {
                    sb.Append($"{r.Sum % 10} ");
                }
                sb.AppendLine();
                
                // 豹顺对历史
                sb.Append("豹顺对历史：");
                foreach (var r in results)
                {
                    sb.Append($"{GetSpecialTypeChar(r)} ");
                }
                sb.AppendLine();
            }
            catch (Exception ex)
            {
                Log($"获取历史记录异常: {ex.Message}");
                sb.AppendLine("获取历史记录失败");
            }
            
            // 倒计时
            sb.Append($"离作业做题结束还有{countdown}秒");
            
            return sb.ToString();
        }

        /// <summary>
        /// 获取龙虎豹字符
        /// L=龙(第一个数>第三个数), H=虎(第一个数<第三个数), B=豹(相等)
        /// </summary>
        private string GetLongHuBaoChar(int num1, int num3)
        {
            if (num1 > num3) return "L";      // 龙
            if (num1 < num3) return "H";      // 虎
            return "B";                        // 豹/和
        }

        /// <summary>
        /// 获取特殊形态字符 (豹子/顺子/对子/杂)
        /// </summary>
        private string GetSpecialTypeChar(LotteryResult r)
        {
            if (r.IsLeopard) return "豹";
            if (r.IsStraight) return "顺";
            if (r.IsPair) return "对";
            return "--";
        }

        /// <summary>
        /// 获取短ID (取后4位)
        /// </summary>
        private string GetShortId(string playerId)
        {
            if (string.IsNullOrEmpty(playerId))
                return "0000";
            return playerId.Length > 4 ? playerId.Substring(playerId.Length - 4) : playerId;
        }

        #endregion

        private void Log(string message)
        {
            _log?.Invoke($"[MessageSender] {message}");
        }
    }
}
