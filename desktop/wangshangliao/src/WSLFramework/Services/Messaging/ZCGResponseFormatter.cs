using System;
using System.Collections.Generic;
using System.Text;
using WSLFramework.Protocol;

namespace WSLFramework.Services
{
    /// <summary>
    /// ZCG响应格式化器 - 完全匹配招财狗的消息格式
    /// 基于深度逆向分析实现
    /// </summary>
    public static class ZCGResponseFormatter
    {
        #region 余额查询响应
        
        /// <summary>
        /// 格式化余额查询响应 - 匹配ZCG格式
        /// 格式: [LQ:@QQ号] (短ID)\n老板，您的账户还是零！\n当前余额:0
        /// </summary>
        public static string FormatBalanceQuery(string playerId, string nickname, int balance)
        {
            var shortId = ZCGMessage.GetShortId(playerId);
            var sb = new StringBuilder();
            
            sb.AppendLine($"[LQ:@{playerId}] ({shortId})");
            
            if (balance <= 0)
            {
                sb.AppendLine("老板，您的账户还是零！");
            }
            else
            {
                sb.AppendLine($"欢迎查询，{nickname}！");
            }
            
            sb.Append($"当前余额:{balance}");
            
            return sb.ToString();
        }
        
        /// <summary>
        /// 格式化余额为零的提示
        /// </summary>
        public static string FormatZeroBalance(string playerId)
        {
            var shortId = ZCGMessage.GetShortId(playerId);
            return $"[LQ:@{playerId}] ({shortId})\n老板，您的账户还是零！\n当前余额:0";
        }
        
        #endregion
        
        #region 上分响应
        
        /// <summary>
        /// 格式化上分成功响应
        /// </summary>
        public static string FormatUpSuccess(string playerId, int amount, int newBalance)
        {
            var shortId = ZCGMessage.GetShortId(playerId);
            var sb = new StringBuilder();
            
            sb.AppendLine($"[LQ:@{playerId}] ({shortId})");
            sb.AppendLine("亲，您的上分已到账，祝你天天好运~");
            sb.AppendLine($"上分金额: {amount}");
            sb.Append($"当前余额: {newBalance}");
            
            return sb.ToString();
        }
        
        /// <summary>
        /// 格式化上分请求（需要手动处理）
        /// </summary>
        public static string FormatUpRequest(string playerId, int amount)
        {
            var shortId = ZCGMessage.GetShortId(playerId);
            return $"[LQ:@{playerId}] ({shortId})\n亲，上分未自动上分功能已经关闭~";
        }
        
        /// <summary>
        /// 格式化上分请求审核中 - 完全匹配ZCG
        /// </summary>
        public static string FormatUpRequestPending(string playerId, int amount)
        {
            var shortId = ZCGMessage.GetShortId(playerId);
            return $"[LQ:@{playerId}] ({shortId})\n亲，您的上分请求我们正在火速审核~";
        }
        
        /// <summary>
        /// 格式化托上分成功（托管自动上分）
        /// </summary>
        public static string FormatTrusteeUpSuccess(string playerId, int amount, int newBalance)
        {
            var shortId = ZCGMessage.GetShortId(playerId);
            var sb = new StringBuilder();
            
            sb.AppendLine($"({shortId})");
            sb.AppendLine("亲，您的上分已到账祝你天天好运~");
            sb.AppendLine($"上分金额:{amount}");
            sb.Append($"当前余额:{newBalance}");
            
            return sb.ToString();
        }
        
        #endregion
        
        #region 下分响应
        
        /// <summary>
        /// 格式化下分成功响应
        /// </summary>
        public static string FormatDownSuccess(string playerId, int amount, int newBalance)
        {
            var shortId = ZCGMessage.GetShortId(playerId);
            var sb = new StringBuilder();
            
            sb.AppendLine($"[LQ:@{playerId}] ({shortId})");
            sb.AppendLine($"亲，您的下分{amount}已处理");
            sb.AppendLine($"请核实账户，如有错误请联系客服");
            sb.Append($"当前余额: {newBalance}");
            
            return sb.ToString();
        }
        
        /// <summary>
        /// 格式化余额不足提示
        /// </summary>
        public static string FormatInsufficientBalance(string playerId, int requested, int current)
        {
            var shortId = ZCGMessage.GetShortId(playerId);
            var sb = new StringBuilder();
            
            sb.AppendLine($"[LQ:@{playerId}] ({shortId})");
            sb.AppendLine("亲，您的余额不足无法下分");
            sb.AppendLine($"请求下分: {requested}");
            sb.Append($"当前余额: {current}");
            
            return sb.ToString();
        }
        
        /// <summary>
        /// 格式化下分正在处理
        /// </summary>
        public static string FormatDownPending(string playerId, int amount)
        {
            var shortId = ZCGMessage.GetShortId(playerId);
            return $"[LQ:@{playerId}] ({shortId})\n亲，您的下分{amount}正在处理中，请耐心等待";
        }
        
        /// <summary>
        /// 格式化下分请求（等待手动处理）
        /// </summary>
        public static string FormatDownRequest(string playerId, int amount, int currentBalance)
        {
            var shortId = ZCGMessage.GetShortId(playerId);
            var sb = new StringBuilder();
            sb.AppendLine($"[LQ:@{playerId}] ({shortId})");
            sb.AppendLine($"亲，您的下分申请{amount}已收到");
            sb.AppendLine($"当前余额: {currentBalance}");
            sb.Append("请等待管理员处理~");
            return sb.ToString();
        }
        
        #endregion
        
        #region 下注响应
        
        /// <summary>
        /// 格式化下注成功响应
        /// </summary>
        public static string FormatBetSuccess(string playerId, string betType, int amount, int newBalance)
        {
            var shortId = ZCGMessage.GetShortId(playerId);
            var betTypeDisplay = GetBetTypeDisplay(betType);
            
            var sb = new StringBuilder();
            sb.AppendLine($"[LQ:@{playerId}] ({shortId})");
            sb.AppendLine($"下注{betTypeDisplay}{amount}，成功录取");
            sb.Append($"余额: {newBalance}");
            
            return sb.ToString();
        }
        
        /// <summary>
        /// 格式化下注失败（余额不足）
        /// </summary>
        public static string FormatBetInsufficientBalance(string playerId, string betType, int amount, int current)
        {
            var shortId = ZCGMessage.GetShortId(playerId);
            var betTypeDisplay = GetBetTypeDisplay(betType);
            
            var sb = new StringBuilder();
            sb.AppendLine($"[LQ:@{playerId}] ({shortId})");
            sb.AppendLine($"余粮不足！当前余额: {current}");
            sb.Append($"上分后录取：{betTypeDisplay}{amount}");
            
            return sb.ToString();
        }
        
        /// <summary>
        /// 格式化余粮不足提示 - 完全匹配ZCG格式
        /// </summary>
        public static string FormatBetAfterRecharge(string playerId, string betContent)
        {
            var shortId = ZCGMessage.GetShortId(playerId);
            return $"[LQ:@{playerId}] ({shortId})\n余粮不足，上芬后录取，{betContent}";
        }
        
        /// <summary>
        /// 格式化上分后录取提示 (带当前余额)
        /// </summary>
        public static string FormatScoreInsufficientWithBet(string playerId, int currentScore, string betContent)
        {
            var shortId = ZCGMessage.GetShortId(playerId);
            var sb = new StringBuilder();
            sb.AppendLine($"[LQ:@{playerId}] ({shortId})");
            sb.AppendLine("老板，您的账户余额不足！");
            sb.AppendLine($"当前余粮:{currentScore}");
            sb.Append($"上分后录取：{betContent}");
            return sb.ToString();
        }
        
        /// <summary>
        /// 格式化封盘期间下注失败
        /// </summary>
        public static string FormatBetClosed(string playerId)
        {
            var shortId = ZCGMessage.GetShortId(playerId);
            return $"[LQ:@{playerId}] ({shortId})\n下注失败，目前已封盘，请等待下一期";
        }
        
        /// <summary>
        /// 格式化无效下注
        /// </summary>
        public static string FormatInvalidBet(string playerId, string content)
        {
            var shortId = ZCGMessage.GetShortId(playerId);
            return $"[LQ:@{playerId}] ({shortId})\n下注格式无效，请检查后重新下注";
        }
        
        /// <summary>
        /// 获取下注类型显示名称
        /// </summary>
        private static string GetBetTypeDisplay(string betType)
        {
            switch (betType)
            {
                case "BIG": return "大";
                case "SMALL": return "小";
                case "ODD": return "单";
                case "EVEN": return "双";
                case "BIG_ODD": return "大单";
                case "BIG_EVEN": return "大双";
                case "SMALL_ODD": return "小单";
                case "SMALL_EVEN": return "小双";
                case "LEOPARD": return "豹子";
                case "STRAIGHT": return "顺子";
                case "PAIR": return "对子";
                default:
                    if (betType.StartsWith("NUM_"))
                    {
                        return betType.Substring(4);
                    }
                    return betType;
            }
        }
        
        #endregion
        
        #region 开奖消息
        
        /// <summary>
        /// 格式化开奖消息 - ZCG格式
        /// 格式: 开:7+8+3=18 DAS 期3382926期
        /// </summary>
        public static string FormatOpenResult(int num1, int num2, int num3, string period)
        {
            var sum = num1 + num2 + num3;
            var resultCode = GetResultCode(sum, num1, num2, num3);
            return $"开:{num1}+{num2}+{num3}={sum:D2} {resultCode} 期{period}期";
        }
        
        /// <summary>
        /// 格式化开奖结果（详细版）
        /// </summary>
        public static string FormatOpenResultDetail(int num1, int num2, int num3, string period, List<string> history)
        {
            var sum = num1 + num2 + num3;
            var resultCode = GetResultCode(sum, num1, num2, num3);
            var shapeCode = GetShapeCode(num1, num2, num3);
            var bigSmallCode = GetBigSmallCode(sum);
            
            var sb = new StringBuilder();
            sb.AppendLine($"開:{num1} + {num2} + {num3} = {sum:D2} {resultCode} {shapeCode} {bigSmallCode}");
            sb.AppendLine("人數:0  总分:0");
            sb.AppendLine("----------------------");
            sb.AppendLine();
            sb.AppendLine("----------------------");
            
            // 历史记录
            if (history != null && history.Count > 0)
            {
                sb.AppendLine($"ls：{string.Join(" ", history)}");
            }
            
            return sb.ToString();
        }
        
        /// <summary>
        /// 获取结果代码 (如 DAS = 大单顺)
        /// </summary>
        private static string GetResultCode(int sum, int num1, int num2, int num3)
        {
            var sb = new StringBuilder();
            
            // D=大, X=小
            sb.Append(sum >= 14 ? "D" : "X");
            
            // A=单, S=双
            sb.Append(sum % 2 == 1 ? "A" : "S");
            
            // 特殊形态
            if (num1 == num2 && num2 == num3)
            {
                sb.Append("B"); // 豹子 Bao
            }
            else if (IsStraight(num1, num2, num3))
            {
                sb.Append("S"); // 顺子 Shun
            }
            else if (num1 == num2 || num2 == num3 || num1 == num3)
            {
                sb.Append("D"); // 对子 Dui
            }
            
            return sb.ToString();
        }
        
        /// <summary>
        /// 获取形态代码
        /// </summary>
        private static string GetShapeCode(int num1, int num2, int num3)
        {
            if (num1 == num2 && num2 == num3)
                return "BZ"; // 豹子
            if (IsStraight(num1, num2, num3))
                return "SZ"; // 顺子
            if (num1 == num2 || num2 == num3 || num1 == num3)
                return "DZ"; // 对子
            if (HasConsecutive(num1, num2, num3))
                return "BS"; // 半顺
            return "--"; // 杂
        }
        
        /// <summary>
        /// 获取大小代码
        /// </summary>
        private static string GetBigSmallCode(int sum)
        {
            if (sum >= 14)
                return sum > 20 ? "H" : "B"; // H=很大, B=大
            else
                return sum < 7 ? "L" : "B";  // L=很小, B=偏小
        }
        
        private static bool IsStraight(int a, int b, int c)
        {
            var nums = new[] { a, b, c };
            Array.Sort(nums);
            return nums[1] == nums[0] + 1 && nums[2] == nums[1] + 1;
        }
        
        private static bool HasConsecutive(int a, int b, int c)
        {
            return Math.Abs(a - b) == 1 || Math.Abs(b - c) == 1 || Math.Abs(a - c) == 1;
        }
        
        #endregion
        
        #region 账单格式
        
        /// <summary>
        /// 格式化账单头部
        /// </summary>
        public static string FormatBillHeader(int num1, int num2, int num3, string period, int playerCount, int totalScore)
        {
            var sum = num1 + num2 + num3;
            var resultCode = GetResultCode(sum, num1, num2, num3);
            var shapeCode = GetShapeCode(num1, num2, num3);
            var bigSmallCode = GetBigSmallCode(sum);
            
            var sb = new StringBuilder();
            sb.AppendLine($"開:{num1} + {num2} + {num3} = {sum:D2} {resultCode} {shapeCode} {bigSmallCode}");
            sb.AppendLine($"人數:{playerCount}  总分:{totalScore}");
            sb.AppendLine("----------------------");
            
            return sb.ToString();
        }
        
        /// <summary>
        /// 格式化账单行（单个玩家）
        /// </summary>
        public static string FormatBillLine(string nickname, string betType, int betAmount, bool isWin, int winAmount)
        {
            var betTypeDisplay = GetBetTypeDisplay(betType);
            var status = isWin ? "✓" : "✗";
            var result = isWin ? $"+{winAmount}" : $"-{betAmount}";
            
            return $"{nickname}: {betTypeDisplay}{betAmount} {status} {result}";
        }
        
        /// <summary>
        /// 格式化完整账单
        /// </summary>
        public static string FormatFullBill(int num1, int num2, int num3, string period, 
            List<BetRecord> bets, int totalWin, int totalLose)
        {
            var sb = new StringBuilder();
            
            // 头部
            sb.Append(FormatBillHeader(num1, num2, num3, period, bets.Count, totalWin - totalLose));
            
            // 下注列表
            foreach (var bet in bets)
            {
                sb.AppendLine(FormatBillLine(bet.Nickname, bet.BetType, bet.Amount, bet.IsWin, bet.WinAmount));
            }
            
            sb.AppendLine("----------------------");
            
            return sb.ToString();
        }
        
        #endregion
        
        #region 系统消息
        
        /// <summary>
        /// 格式化倒计时40秒提醒
        /// </summary>
        public static string FormatCountdown40()
        {
            return "--距开奖还剩时间还有40秒--\n下注下注格式 格式 格";
        }
        
        /// <summary>
        /// 格式化封盘消息
        /// </summary>
        public static string FormatBetClose()
        {
            return "==封盘结束==\n以下收钱的都不\n==庄家为准==";
        }
        
        /// <summary>
        /// 格式化账单前提示
        /// </summary>
        public static string FormatBillNotice()
        {
            return "账单\n-------------------\n";
        }
        
        /// <summary>
        /// 格式化群公告
        /// </summary>
        public static string FormatGroupNotice()
        {
            return "本群经营娱乐，每三十分钟分分钟过期没有人进入的有效会议已解散！！！！！";
        }
        
        /// <summary>
        /// 格式化群名片修改通知
        /// </summary>
        public static string FormatNameCardChange(string playerId, string oldName, string newName)
        {
            var shortId = ZCGMessage.GetShortId(playerId);
            return $"{newName}({shortId})群名片自动修改为：{newName}";
        }
        
        #endregion
        
        #region 托管相关
        
        /// <summary>
        /// 格式化托管成功消息
        /// </summary>
        public static string FormatTrusteeSuccess(string playerId, string trusteeName)
        {
            var shortId = ZCGMessage.GetShortId(playerId);
            return $"[LQ:@{playerId}] ({shortId})\n您已被设置为托管成员：{trusteeName}\n托管将自动为您处理上分和代付机要码托管";
        }
        
        /// <summary>
        /// 格式化取消托管消息
        /// </summary>
        public static string FormatTrusteeCancelled(string playerId)
        {
            var shortId = ZCGMessage.GetShortId(playerId);
            return $"[LQ:@{playerId}] ({shortId})\n已取消托管身份，请手动进行上下分操作";
        }
        
        #endregion
        
        #region 定时消息格式 - 完全匹配ZCG旧程序
        
        /// <summary>
        /// 格式化核对消息 - 完全匹配ZCG旧程序格式
        /// </summary>
        public static string FormatCheck()
        {
            return "核对\n-------------------\n";
        }
        
        /// <summary>
        /// 格式化卡奖提醒 - 完全匹配ZCG旧程序格式
        /// </summary>
        public static string FormatStuckNotice()
        {
            return "本群如遇卡奖情况，十分钟官网没开奖，本期无效，无需纠结！！！！\n";
        }
        
        /// <summary>
        /// 格式化封盘线消息 - 完全匹配ZCG旧程序格式
        /// </summary>
        public static string FormatCloseLine()
        {
            return "==加封盘线==\n以上有钱的都接\n==庄显为准==";
        }
        
        /// <summary>
        /// 格式化倒计时消息（可自定义秒数）- 完全匹配ZCG旧程序格式
        /// </summary>
        public static string FormatCountdown(int seconds)
        {
            if (seconds == 40)
            {
                return $"--距离封盘时间还有{seconds}秒--\n改注加注带改 或者 加";
            }
            return $"--距离封盘时间还有{seconds}秒--";
        }
        
        /// <summary>
        /// 格式化详细开奖结果（包含历史记录）- 完全匹配ZCG旧程序格式
        /// </summary>
        public static string FormatOpenResultFull(
            int num1, int num2, int num3, string period,
            int playerCount, int totalScore,
            List<int> historyResults, List<string> lhbHistory, List<int> tailHistory, List<string> bsdHistory)
        {
            var sum = num1 + num2 + num3;
            var resultCode = GetResultCode(sum, num1, num2, num3);
            var shapeCode = GetShapeCode(num1, num2, num3);
            var bigSmallCode = GetBigSmallCode(sum);
            
            var sb = new StringBuilder();
            
            // 开奖行
            sb.AppendLine($"開:{num1} + {num2} + {num3} = {sum:D2} {resultCode} {shapeCode} {bigSmallCode}");
            sb.AppendLine($"人數:{playerCount}  總分:{totalScore}");
            sb.AppendLine("----------------------");
            sb.AppendLine();
            sb.AppendLine("----------------------");
            
            // 历史记录
            if (historyResults != null && historyResults.Count > 0)
            {
                sb.AppendLine($"ls：{string.Join(" ", historyResults.ConvertAll(n => n.ToString("D2")))}");
            }
            
            // 龙虎豹历史
            if (lhbHistory != null && lhbHistory.Count > 0)
            {
                sb.AppendLine($"龙虎豹ls：{string.Join(" ", lhbHistory)}");
            }
            
            // 尾球历史
            if (tailHistory != null && tailHistory.Count > 0)
            {
                sb.AppendLine($"尾球ls：{string.Join(" ", tailHistory)}");
            }
            
            // 豹顺对历史
            if (bsdHistory != null && bsdHistory.Count > 0)
            {
                sb.Append($"豹顺对历史：{string.Join(" ", bsdHistory)}");
            }
            
            return sb.ToString();
        }
        
        #endregion
    }
}
