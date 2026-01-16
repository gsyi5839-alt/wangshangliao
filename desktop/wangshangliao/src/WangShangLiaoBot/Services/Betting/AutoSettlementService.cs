using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WangShangLiaoBot.Models.Betting;
using LotteryResult = WangShangLiaoBot.Services.LotteryResult;

namespace WangShangLiaoBot.Services.Betting
{
    /// <summary>
    /// 自动开奖结算服务 - 基于招财狗(ZCG)的结算系统
    /// 负责获取开奖结果、计算盈亏、结算账单
    /// </summary>
    public sealed class AutoSettlementService
    {
        private static AutoSettlementService _instance;
        public static AutoSettlementService Instance => _instance ?? (_instance = new AutoSettlementService());

        private readonly object _lock = new object();
        private Dictionary<string, List<BetRecord>> _periodBets = new Dictionary<string, List<BetRecord>>();
        
        /// <summary>最大保留的期号数量（防止内存泄漏）</summary>
        private const int MAX_PERIOD_CACHE = 100;

        // 事件
        public event Action<string, string> OnSendMessage;           // teamId, message
        public event Action<string, LotteryResult> OnLotteryResult;  // period, result
        public event Action<string, BillSummary> OnBillGenerated;    // period, bill
        #pragma warning disable CS0067 // 预留接口，BotController 订阅
        public event Action<string, int, decimal> OnSettlementComplete; // period, playerCount, totalProfit
        #pragma warning restore CS0067

        private AutoSettlementService() { }

        #region 下注管理

        /// <summary>
        /// 添加下注记录
        /// </summary>
        public void AddBetRecord(BetRecord record)
        {
            lock (_lock)
            {
                if (!_periodBets.TryGetValue(record.Period, out var list))
                {
                    list = new List<BetRecord>();
                    _periodBets[record.Period] = list;
                }
                list.Add(record);
                
                // 清理过期期号，防止内存泄漏
                CleanupOldPeriods();
            }
        }
        
        /// <summary>
        /// 清理旧期号的下注记录（保留最近的MAX_PERIOD_CACHE个）
        /// </summary>
        private void CleanupOldPeriods()
        {
            if (_periodBets.Count <= MAX_PERIOD_CACHE) return;
            
            // 按期号排序，移除最旧的
            var periods = _periodBets.Keys.OrderBy(p => p).ToList();
            var toRemove = periods.Take(periods.Count - MAX_PERIOD_CACHE).ToList();
            foreach (var period in toRemove)
            {
                _periodBets.Remove(period);
            }
            
            if (toRemove.Count > 0)
            {
                Logger.Info($"[AutoSettlementService] 清理过期期号数据: {toRemove.Count}个");
            }
        }

        /// <summary>
        /// 获取期号的所有下注
        /// </summary>
        public List<BetRecord> GetPeriodBets(string period)
        {
            lock (_lock)
            {
                if (_periodBets.TryGetValue(period, out var list))
                    return new List<BetRecord>(list);
                return new List<BetRecord>();
            }
        }

        /// <summary>
        /// 清除期号的下注记录
        /// </summary>
        public void ClearPeriodBets(string period)
        {
            lock (_lock)
            {
                _periodBets.Remove(period);
            }
        }

        #endregion

        #region 开奖结算

        /// <summary>
        /// 处理开奖结果并结算
        /// </summary>
        public async Task<BillSummary> ProcessLotteryResultAsync(string period, LotteryResult result, string teamId)
        {
            Logger.Info($"[结算服务] 开始结算 - 期号:{period}, 开奖:{result.Dice1}+{result.Dice2}+{result.Dice3}={result.Sum}");

            // 1. 获取该期所有下注
            var bets = GetPeriodBets(period);
            if (bets.Count == 0)
            {
                Logger.Info($"[结算服务] 期号{period}无下注记录");
                return null;
            }

            // 2. 发送开奖消息
            SendLotteryMessage(teamId, period, result);

            // 3. 计算每个玩家的盈亏
            var playerSettlements = new List<PlayerSettlement>();
            decimal totalBet = 0;
            decimal totalPayout = 0;

            foreach (var bet in bets)
            {
                var settlement = CalculatePlayerSettlement(bet, result);
                playerSettlements.Add(settlement);
                totalBet += settlement.TotalBet;
                totalPayout += settlement.TotalPayout;

                // 结算到玩家账户
                await SettlePlayerAsync(settlement);
            }

            // 4. 生成账单
            var bill = new BillSummary
            {
                Period = period,
                TeamId = teamId,
                LotteryResult = result,
                PlayerCount = playerSettlements.Select(p => p.PlayerId).Distinct().Count(),
                TotalBet = totalBet,
                TotalPayout = totalPayout,
                HouseProfit = totalBet - totalPayout,
                PlayerSettlements = playerSettlements,
                SettledAt = DateTime.Now
            };

            // 5. 发送账单消息
            SendBillMessage(teamId, bill);

            // 6. 触发事件
            OnLotteryResult?.Invoke(period, result);
            OnBillGenerated?.Invoke(period, bill);

            // 7. 清理下注记录
            ClearPeriodBets(period);

            Logger.Info($"[结算服务] 结算完成 - 期号:{period}, 人数:{bill.PlayerCount}, 总下注:{totalBet:F2}, 庄家盈利:{bill.HouseProfit:F2}");

            return bill;
        }

        /// <summary>
        /// 计算单个玩家的结算
        /// </summary>
        private PlayerSettlement CalculatePlayerSettlement(BetRecord bet, LotteryResult result)
        {
            var settlement = new PlayerSettlement
            {
                PlayerId = bet.PlayerId,
                PlayerNick = bet.PlayerNick,
                Period = bet.Period,
                TotalBet = bet.TotalAmount,
                BalanceBefore = ScoreService.Instance.GetBalance(bet.PlayerId),
                ItemResults = new List<BetItemResult>()
            };

            decimal totalPayout = 0;
            var winDetails = new List<string>();
            var loseDetails = new List<string>();

            foreach (var item in bet.Items)
            {
                var itemResult = CalculateBetItemResult(item, result);
                settlement.ItemResults.Add(itemResult);

                if (itemResult.IsWin)
                {
                    totalPayout += itemResult.Payout;
                    winDetails.Add($"{CodeToChinese(item.Code, item.Kind)}{item.Amount}中{itemResult.Payout:F0}");
                }
                else
                {
                    loseDetails.Add($"{CodeToChinese(item.Code, item.Kind)}{item.Amount}");
                }
            }

            settlement.TotalPayout = totalPayout;
            settlement.NetProfit = totalPayout - bet.TotalAmount;
            settlement.WinDetail = string.Join(" ", winDetails);
            settlement.LoseDetail = string.Join(" ", loseDetails);

            return settlement;
        }

        /// <summary>
        /// 计算单个下注项结果
        /// </summary>
        private BetItemResult CalculateBetItemResult(BetItem item, LotteryResult result)
        {
            var odds = OddsService.Instance.GetOdds(item.Kind, item.Code);
            var isWin = CheckIsWin(item, result);

            return new BetItemResult
            {
                Kind = item.Kind,
                Code = item.Code,
                Amount = item.Amount,
                Odds = odds,
                IsWin = isWin,
                Payout = isWin ? item.Amount * odds : 0
            };
        }

        /// <summary>
        /// 判断是否中奖
        /// </summary>
        private bool CheckIsWin(BetItem item, LotteryResult result)
        {
            var config = OddsService.Instance.GetConfig();

            switch (item.Kind)
            {
                case BetKind.BigSmall:
                    if (item.Code == "D" || item.Code == "大") return result.IsBig;
                    if (item.Code == "X" || item.Code == "小") return !result.IsBig;
                    return false;

                case BetKind.OddEven:
                    if (item.Code == "单" || item.Code == "D") return result.IsOdd;
                    if (item.Code == "双" || item.Code == "S") return !result.IsOdd;
                    return false;

                case BetKind.Dxds:
                    switch (item.Code)
                    {
                        case "DD": return result.IsBig && result.IsOdd;   // 大单
                        case "DS": return result.IsBig && !result.IsOdd;  // 大双
                        case "XD": return !result.IsBig && result.IsOdd;  // 小单
                        case "XS": return !result.IsBig && !result.IsOdd; // 小双
                    }
                    return false;

                case BetKind.Digit:
                    if (int.TryParse(item.Code, out var targetDigit))
                        return result.Sum == targetDigit;
                    return false;

                case BetKind.Extreme:
                    if (item.Code == "JD" || item.Code == "极大")
                        return result.Sum >= config.ExtremeHighStart && result.Sum <= config.ExtremeHighEnd;
                    if (item.Code == "JX" || item.Code == "极小")
                        return result.Sum >= config.ExtremeLowStart && result.Sum <= config.ExtremeLowEnd;
                    return false;

                case BetKind.Pair:
                    return result.IsPair;

                case BetKind.Straight:
                    return result.IsStraight;

                case BetKind.Leopard:
                    return result.IsLeopard;

                case BetKind.HalfStraight:
                    return result.IsHalfStraight;

                case BetKind.Mixed:
                    return result.IsMixed;

                case BetKind.DragonTiger:
                    if (item.Code == "L" || item.Code == "龙") return result.DragonTiger == "龙";
                    if (item.Code == "H" || item.Code == "虎") return result.DragonTiger == "虎";
                    if (item.Code == "豹") return result.DragonTiger == "豹";
                    return false;

                default:
                    return false;
            }
        }

        /// <summary>
        /// 结算到玩家账户
        /// </summary>
        private async Task SettlePlayerAsync(PlayerSettlement settlement)
        {
            try
            {
                if (settlement.NetProfit > 0)
                {
                    // 中奖：返还本金+奖金
                    ScoreService.Instance.AddWinnings(settlement.PlayerId, settlement.TotalPayout, settlement.Period);
                }
                else if (settlement.NetProfit == 0)
                {
                    // 打平：返还本金
                    ScoreService.Instance.AddScore(settlement.PlayerId, settlement.TotalBet, $"第{settlement.Period}期打平返还");
                }
                // 输了不需要处理，下注时已扣除

                settlement.BalanceAfter = ScoreService.Instance.GetBalance(settlement.PlayerId);
            }
            catch (Exception ex)
            {
                Logger.Error($"[结算服务] 玩家{settlement.PlayerId}结算失败: {ex.Message}");
            }
        }

        #endregion

        #region 消息生成

        /// <summary>
        /// 发送开奖消息
        /// </summary>
        private void SendLotteryMessage(string teamId, string period, LotteryResult result)
        {
            var variables = MessageTemplateService.Instance.CreateLotteryVariables(
                period,
                result.Dice1, result.Dice2, result.Dice3, result.Sum,
                0, 0
            );

            var message = MessageTemplateService.Instance.Render("开奖发送", variables);
            if (!string.IsNullOrEmpty(message))
            {
                OnSendMessage?.Invoke(teamId, message);
            }
        }

        /// <summary>
        /// 发送账单消息
        /// </summary>
        private void SendBillMessage(string teamId, BillSummary bill)
        {
            var variables = MessageTemplateService.Instance.CreateLotteryVariables(
                bill.Period,
                bill.LotteryResult.Dice1, bill.LotteryResult.Dice2, bill.LotteryResult.Dice3, bill.LotteryResult.Sum,
                bill.PlayerCount,
                bill.TotalBet
            );

            var message = MessageTemplateService.Instance.Render("账单发送", variables);

            // 添加玩家明细
            var sb = new StringBuilder();
            sb.AppendLine(message);
            sb.AppendLine("----------------------");

            foreach (var ps in bill.PlayerSettlements)
            {
                var profitStr = ps.NetProfit >= 0 ? $"+{ps.NetProfit:F0}" : $"{ps.NetProfit:F0}";
                sb.AppendLine($"{ps.PlayerNick}: {ps.TotalBet:F0} -> {profitStr}");
            }

            OnSendMessage?.Invoke(teamId, sb.ToString());
        }

        /// <summary>
        /// 生成下注核对消息
        /// </summary>
        public string GenerateBetCheckMessage(string period, string teamId)
        {
            var bets = GetPeriodBets(period);
            if (bets.Count == 0) return "本期暂无下注";

            var sb = new StringBuilder();
            sb.AppendLine($"第{period}期 下注核对");
            sb.AppendLine("----------------------");

            var playerBets = bets.GroupBy(b => b.PlayerId);
            foreach (var group in playerBets)
            {
                var player = group.First();
                var totalAmount = group.Sum(b => b.TotalAmount);
                var betContent = string.Join(" ", group.Select(b => b.NormalizedText));
                sb.AppendLine($"{player.PlayerNick}: {betContent} = {totalAmount:F0}");
            }

            sb.AppendLine("----------------------");
            sb.AppendLine($"人数:{playerBets.Count()} 总分:{bets.Sum(b => b.TotalAmount):F0}");

            return sb.ToString();
        }

        private string CodeToChinese(string code, BetKind kind)
        {
            switch (code)
            {
                case "D": return "大";
                case "X": return "小";
                case "单": return "单";
                case "双": return "双";
                case "DD": return "大单";
                case "DS": return "大双";
                case "XD": return "小单";
                case "XS": return "小双";
                case "DZ": return "对子";
                case "SZ": return "顺子";
                case "BZ": return "豹子";
                case "BS": return "半顺";
                case "JD": return "极大";
                case "JX": return "极小";
                case "L": return "龙";
                case "H": return "虎";
                default:
                    if (kind == BetKind.Digit) return code;
                    return code;
            }
        }

        #endregion
    }

    #region 数据模型

    // LotteryResult 现在使用 WangShangLiaoBot.Services.LotteryResult（通过 using alias）
    // 以保持项目中类型一致性

    /// <summary>
    /// 玩家结算结果
    /// </summary>
    public class PlayerSettlement
    {
        public string PlayerId { get; set; }
        public string PlayerNick { get; set; }
        public string Period { get; set; }
        public decimal TotalBet { get; set; }
        public decimal TotalPayout { get; set; }
        public decimal NetProfit { get; set; }
        public decimal BalanceBefore { get; set; }
        public decimal BalanceAfter { get; set; }
        public string WinDetail { get; set; }
        public string LoseDetail { get; set; }
        public List<BetItemResult> ItemResults { get; set; }
    }

    /// <summary>
    /// 下注项结果
    /// </summary>
    public class BetItemResult
    {
        public BetKind Kind { get; set; }
        public string Code { get; set; }
        public decimal Amount { get; set; }
        public decimal Odds { get; set; }
        public bool IsWin { get; set; }
        public decimal Payout { get; set; }
    }

    /// <summary>
    /// 账单汇总
    /// </summary>
    public class BillSummary
    {
        public string Period { get; set; }
        public string TeamId { get; set; }
        public LotteryResult LotteryResult { get; set; }
        public int PlayerCount { get; set; }
        public decimal TotalBet { get; set; }
        public decimal TotalPayout { get; set; }
        public decimal HouseProfit { get; set; }
        public List<PlayerSettlement> PlayerSettlements { get; set; }
        public DateTime SettledAt { get; set; }
    }

    #endregion
}
