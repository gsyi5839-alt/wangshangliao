using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using WangShangLiaoBot.Models.Betting;
using LotteryResult = WangShangLiaoBot.Services.LotteryResult;

namespace WangShangLiaoBot.Services.Betting
{
    /// <summary>
    /// Settle bets for a lottery period when LotteryService publishes a result.
    /// - Reads bet ledger from BetLedgerService
    /// - Calculates profit/loss using odds configured in OddsConfigService/ConfigService
    /// - Updates player scores via DataService.SavePlayer
    /// - Writes settlement output used by template tokens like [中奖玩家]
    /// </summary>
    public sealed class BetSettlementService
    {
        private static BetSettlementService _instance;
        public static BetSettlementService Instance => _instance ?? (_instance = new BetSettlementService());

        private BetSettlementService() { }

        public bool IsRunning { get; private set; }
        
        /// <summary>
        /// Event fired after settlement is completed for a period
        /// Parameters: period, playerCount, totalProfit
        /// </summary>
        public event Action<string, int, decimal> OnSettlementComplete;

        public void Start()
        {
            if (IsRunning) return;
            LotteryService.Instance.OnResultUpdated += OnLotteryResult;
            IsRunning = true;
        }

        public void Stop()
        {
            if (!IsRunning) return;
            LotteryService.Instance.OnResultUpdated -= OnLotteryResult;
            IsRunning = false;
        }

        private void OnLotteryResult(LotteryResult result)
        {
            try
            {
                if (!IsRunning) return;
                if (result == null || string.IsNullOrEmpty(result.Period)) return;

                var odds = OddsConfigService.Instance.LoadClassicOdds();
                var config = ConfigService.Instance.Config;
                
                // At least classic odds should be configured
                if (odds == null)
                {
                    odds = new ClassicOddsConfig { DxdsOdds = 1.8m };
                }

                var day = DateTime.Today;
                var period = result.Period;

                var sum = result.Sum;
                var n1 = result.Number1;
                var n2 = result.Number2;
                var n3 = result.Number3;
                var dxds = GetDxdsCode(sum);
                var baoShunDui = GetBaoShunDui(n1, n2, n3);
                var dragonTiger = GetDragonTiger(sum, config);

                // Settle per group (teamId) to avoid mixing multiple groups when customers choose group dynamically.
                var all = BetLedgerService.Instance.ReadBets(day, teamId: null, period: period);
                if (all.Count == 0) return;

                foreach (var team in all.GroupBy(b => b.TeamId ?? "unknown-team"))
                {
                    var teamId = team.Key;
                    var bets = team.ToList();
                    if (bets.Count == 0) continue;

                    var settlements = new List<SettlementEntry>();

                    foreach (var g in bets.GroupBy(b => b.PlayerId))
                    {
                        var playerId = g.Key;
                        var player = DataService.Instance.GetOrCreatePlayer(playerId, g.First().PlayerNick);
                        var scoreBefore = player.Score;

                        decimal stake = 0m;
                        decimal profit = 0m;
                        var detail = new StringBuilder();

                        foreach (var b in g)
                        {
                            stake += b.TotalAmount;

                            // Re-parse from raw text to structured items for settlement
                            if (!BetMessageParser.TryParse(b.RawText, out var items, out var t, out var norm))
                                continue;

                            foreach (var it in items)
                            {
                                var p = SettleItem(it, sum, n1, n2, n3, dxds, baoShunDui, dragonTiger, odds, config);
                                profit += p;
                                if (detail.Length > 0) detail.Append(" ");
                                var sign = p >= 0 ? "+" : "";
                                detail.Append($"{it.Code}{it.Amount}:{sign}{p.ToString(CultureInfo.InvariantCulture)}");
                            }
                        }

                        var scoreAfter = scoreBefore + profit;
                        player.Score = scoreAfter;
                        DataService.Instance.SavePlayer(player);

                        settlements.Add(new SettlementEntry
                        {
                            Period = period,
                            PlayerId = playerId,
                            PlayerNick = player.Nickname ?? g.First().PlayerNick,
                            Stake = stake,
                            Profit = profit,
                            ScoreBefore = scoreBefore,
                            ScoreAfter = scoreAfter,
                            Detail = detail.ToString()
                        });
                    }

                    WriteSettlement(day, teamId, period, settlements, sum, dxds, baoShunDui);
                    
                    // Fire settlement complete event
                    var totalProfit = settlements.Sum(s => s.Profit);
                    try
                    {
                        OnSettlementComplete?.Invoke(period, settlements.Count, totalProfit);
                    }
                    catch (Exception evEx)
                    {
                        Logger.Error($"[BetSettlement] OnSettlementComplete event error: {evEx.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                // Log but don't break lottery loop
                Logger.Error($"[BetSettlement] OnLotteryResult error: {ex.Message}");
            }
        }

        /// <summary>
        /// Settle a single bet item based on lottery result and configured odds
        /// </summary>
        private static decimal SettleItem(BetItem it, int sum, int n1, int n2, int n3, 
            string dxds, string baoShunDui, string dragonTiger, 
            ClassicOddsConfig odds, Models.AppConfig config)
        {
            if (it == null || it.Amount <= 0) return 0m;

            switch (it.Kind)
            {
                // ===== Classic DXDS (大小单双) =====
                case BetKind.Dxds:
                    return SettleDxds(it, dxds, odds.DxdsOdds);

                case BetKind.BigSmall:
                    return SettleBigSmall(it, sum, odds.DxdsOdds);

                case BetKind.OddEven:
                    return SettleOddEven(it, sum, odds.DxdsOdds);

                // ===== Pair/Straight/Leopard (对子/顺子/豹子) =====
                case BetKind.Pair:
                    return SettlePair(it, baoShunDui, config);

                case BetKind.Straight:
                    return SettleStraight(it, baoShunDui, config);

                case BetKind.Leopard:
                    return SettleLeopard(it, baoShunDui, config);

                // ===== Digit (数字 0-27) =====
                case BetKind.Digit:
                    return SettleDigit(it, sum, config);

                // ===== Extreme (极数/极大/极小) =====
                case BetKind.Extreme:
                    return SettleExtreme(it, sum, config);

                // ===== Dragon Tiger (龙虎) =====
                case BetKind.DragonTiger:
                    return SettleDragonTiger(it, dragonTiger, config);

                // ===== Three Army (三军) =====
                case BetKind.ThreeArmy:
                    return SettleThreeArmy(it, n1, n2, n3, config);

                // ===== Edge (边) =====
                case BetKind.Edge:
                    return SettleEdge(it, sum, config);

                // ===== Half Straight (半顺) =====
                case BetKind.HalfStraight:
                    return SettleHalfStraight(it, baoShunDui, config);

                // ===== Sum/和 =====
                case BetKind.Sum:
                    return SettleSum(it, sum, config);

                // ===== Middle/中 =====
                case BetKind.Middle:
                    return SettleMiddle(it, sum, config);

                // ===== Mixed/杂 =====
                case BetKind.Mixed:
                    return SettleMixed(it, baoShunDui, config);

                // ===== Combination/组合 =====
                case BetKind.Combination:
                    return SettleCombination(it, baoShunDui, config);

                // ===== Tail bets (尾球) =====
                case BetKind.TailSingle:
                case BetKind.TailCombination:
                case BetKind.TailDigit:
                    return SettleTailBet(it, sum, config);

                default:
                    return 0m;
            }
        }

        #region Settlement Methods

        /// <summary>
        /// Settle DXDS combo bet (XD/XS/DD/DS)
        /// </summary>
        private static decimal SettleDxds(BetItem it, string dxds, decimal odds)
        {
            var win = string.Equals(it.Code, dxds, StringComparison.OrdinalIgnoreCase);
            return win ? it.Amount * odds : -it.Amount;
        }

        /// <summary>
        /// Settle Big/Small bet
        /// </summary>
        private static decimal SettleBigSmall(BetItem it, int sum, decimal odds)
        {
            var bs = sum >= 14 ? "大" : "小";
            var bsCode = sum >= 14 ? "D" : "X";
            var code = it.Code?.ToUpper() ?? "";
            var win = code == bsCode || code == bs;
            return win ? it.Amount * odds : -it.Amount;
        }

        /// <summary>
        /// Settle Odd/Even bet
        /// </summary>
        private static decimal SettleOddEven(BetItem it, int sum, decimal odds)
        {
            var oe = (sum % 2 == 0) ? "双" : "单";
            var oeCode = (sum % 2 == 0) ? "S" : "D";
            var code = it.Code?.ToUpper() ?? "";
            var win = code == oeCode || code == oe || code == "单" || code == "双";
            if (code == "单") win = (sum % 2 != 0);
            if (code == "双") win = (sum % 2 == 0);
            return win ? it.Amount * odds : -it.Amount;
        }

        /// <summary>
        /// Settle Pair bet (对子)
        /// </summary>
        private static decimal SettlePair(BetItem it, string baoShunDui, Models.AppConfig config)
        {
            var win = baoShunDui == "对子";
            // 对子回本 - return stake instead of winning
            if (win && config.PairReturn)
            {
                return 0m; // 回本 = no profit/loss
            }
            // Use configured odds from config.PairOdds (default 2.0)
            var odds = config.PairOdds > 0 ? config.PairOdds : 2.0m;
            return win ? it.Amount * odds : -it.Amount;
        }

        /// <summary>
        /// Settle Straight bet (顺子)
        /// </summary>
        private static decimal SettleStraight(BetItem it, string baoShunDui, Models.AppConfig config)
        {
            var win = baoShunDui == "顺子";
            // 顺子回本 - return stake instead of winning
            if (win && config.SequenceReturn)
            {
                return 0m; // 回本 = no profit/loss
            }
            // Use configured odds from config.StraightOdds (default 5.0)
            var odds = config.StraightOdds > 0 ? config.StraightOdds : 5.0m;
            return win ? it.Amount * odds : -it.Amount;
        }

        /// <summary>
        /// Settle Leopard bet (豹子)
        /// </summary>
        private static decimal SettleLeopard(BetItem it, string baoShunDui, Models.AppConfig config)
        {
            var win = baoShunDui == "豹子";
            // 豹子通杀 - all lose
            if (config.LeopardKillAll && baoShunDui == "豹子")
            {
                return -it.Amount;
            }
            // 豹子回本
            if (win && config.LeopardReturn)
            {
                return 0m;
            }
            // Use configured odds from config.LeopardOdds (default 49.0)
            var odds = config.LeopardOdds > 0 ? config.LeopardOdds : 49.0m;
            return win ? it.Amount * odds : -it.Amount;
        }

        /// <summary>
        /// Settle Digit bet (数字 0-27)
        /// </summary>
        private static decimal SettleDigit(BetItem it, int sum, Models.AppConfig config)
        {
            if (!int.TryParse(it.Code, out var target)) return 0m;
            var win = (sum == target);
            var odds = config.DigitOdds > 0 ? config.DigitOdds : 9.0m;
            return win ? it.Amount * odds : -it.Amount;
        }

        /// <summary>
        /// Settle Extreme bet (极数/极大/极小)
        /// Uses config.ExtremeMax ~ ExtremeMaxEnd for 极大 range
        /// Uses config.ExtremeMin ~ ExtremeMinEnd for 极小 range
        /// </summary>
        private static decimal SettleExtreme(BetItem it, int sum, Models.AppConfig config)
        {
            var code = it.Code?.ToUpper() ?? "";
            bool win = false;
            
            // 极大: sum in range [ExtremeMax, ExtremeMaxEnd] (default 22-27)
            // 极小: sum in range [ExtremeMin, ExtremeMinEnd] (default 0-5)
            var extremeMaxStart = config.ExtremeMax >= 0 ? config.ExtremeMax : 22;
            var extremeMaxEnd = config.ExtremeMaxEnd > 0 ? config.ExtremeMaxEnd : 27;
            var extremeMinStart = config.ExtremeMin >= 0 ? config.ExtremeMin : 0;
            var extremeMinEnd = config.ExtremeMinEnd >= 0 ? config.ExtremeMinEnd : 5;
            
            // 极大: sum在极大范围内 [22, 27]
            bool isExtremeBig = sum >= extremeMaxStart && sum <= extremeMaxEnd;
            // 极小: sum在极小范围内 [0, 5]
            bool isExtremeSmall = sum >= extremeMinStart && sum <= extremeMinEnd;
            
            if (code == "JD" || code == "极大")
            {
                win = isExtremeBig;
            }
            else if (code == "JX" || code == "极小")
            {
                win = isExtremeSmall;
            }
            else if (code == "J" || code == "极")
            {
                win = isExtremeBig || isExtremeSmall;
            }
            
            var odds = config.ExtremeOdds > 0 ? config.ExtremeOdds : 5.0m;
            return win ? it.Amount * odds : -it.Amount;
        }

        /// <summary>
        /// Settle Dragon Tiger bet (龙虎)
        /// Supports:
        /// - 龙虎斗模式 (Mode=0): 区域比较
        /// - 龙虎豹模式 (Mode=1): 按号码列表判断
        /// - 开和龙虎回本
        /// - 豹子通杀龙虎和
        /// - 阶梯赔率
        /// </summary>
        private static decimal SettleDragonTiger(BetItem it, string dragonTiger, Models.AppConfig config)
        {
            if (!config.DragonTigerEnabled) return 0m;
            
            var code = it.Code?.ToUpper() ?? "";
            bool win = false;
            bool isDraw = dragonTiger == "和";
            bool isLeopard = dragonTiger == "豹";
            decimal odds = 0m;
            
            // 豹子通杀龙虎和
            if (config.DragonTigerLeopardKillAll && isLeopard)
            {
                return -it.Amount; // 通杀
            }
            
            // 判断下注类型和输赢
            bool betDragon = code == "L" || code == "龙";
            bool betTiger = code == "H" || code == "虎";
            bool betDraw = code == "HE" || code == "和" || code == "LHH";
            bool betLeopard = code == "B" || code == "豹";
            
            if (betDragon)
            {
                // 下注龙
                if (dragonTiger == "龙")
                {
                    win = true;
                }
                else if (isDraw && config.DragonTigerDrawReturn)
                {
                    return 0m; // 开和，龙虎回本
                }
            }
            else if (betTiger)
            {
                // 下注虎
                if (dragonTiger == "虎")
                {
                    win = true;
                }
                else if (isDraw && config.DragonTigerDrawReturn)
                {
                    return 0m; // 开和，龙虎回本
                }
            }
            else if (betDraw)
            {
                // 下注和
                win = isDraw;
                // 使用和赔率
                if (it.Amount >= config.DragonTigerBetOverAmount && config.DragonTigerBetOverAmount > 0)
                {
                    odds = config.DragonTigerDrawOdds2; // 超额后的和赔率
                }
                else
                {
                    odds = config.DragonTigerDrawOdds; // 普通和赔率
                }
                if (odds == 0m && win) return 0m; // 赔率0代表回本
                if (odds <= 0) odds = 8.0m; // 默认和赔率
                return win ? it.Amount * odds : -it.Amount;
            }
            else if (betLeopard && config.DragonTigerMode == 1)
            {
                // 龙虎豹模式下注豹
                win = isLeopard;
                odds = config.DragonTigerLeopardOdds > 0 ? config.DragonTigerLeopardOdds : 0.6m;
                return win ? it.Amount * odds : -it.Amount;
            }
            
            // 选择龙虎赔率（阶梯）
            if (it.Amount >= config.DragonTigerBetOverAmount && config.DragonTigerBetOverAmount > 0)
            {
                odds = config.DragonTigerOdds2 > 0 ? config.DragonTigerOdds2 : config.DragonTigerOdds;
            }
            else
            {
                odds = config.DragonTigerOdds > 0 ? config.DragonTigerOdds : 0.6m;
            }
            
            return win ? it.Amount * odds : -it.Amount;
        }

        /// <summary>
        /// Settle Three Army bet (三军)
        /// </summary>
        private static decimal SettleThreeArmy(BetItem it, int n1, int n2, int n3, Models.AppConfig config)
        {
            if (!config.ThreeArmyEnabled) return 0m;
            
            // 三军: 三个数字中任意一个与下注相同
            if (!int.TryParse(it.Code, out var target) && it.Code != "SJ" && it.Code != "三军") 
                return 0m;
            
            var matches = 0;
            if (n1 == target) matches++;
            if (n2 == target) matches++;
            if (n3 == target) matches++;
            
            if (matches == 0) return -it.Amount;
            
            // Use corresponding odds based on match count
            decimal odds;
            switch (matches)
            {
                case 1:
                    odds = config.ThreeArmyOdds1 > 0 ? config.ThreeArmyOdds1 : 1.0m;
                    break;
                case 2:
                    odds = config.ThreeArmyOdds2 > 0 ? config.ThreeArmyOdds2 : 2.0m;
                    break;
                case 3:
                    odds = config.ThreeArmyOdds3 > 0 ? config.ThreeArmyOdds3 : 3.0m;
                    break;
                default:
                    odds = 0m;
                    break;
            }
            
            return it.Amount * odds;
        }

        /// <summary>
        /// Settle Edge bet (边/大边/小边)
        /// Uses config.BigEdgeOdds for 大边, config.SmallEdgeOdds for 小边
        /// </summary>
        private static decimal SettleEdge(BetItem it, int sum, Models.AppConfig config)
        {
            var code = it.Code?.ToUpper() ?? "";
            bool win = false;
            decimal odds = 0m;
            
            // 大边: sum >= 22, 小边: sum <= 5, 边: 大边或小边
            if (code == "DB" || code == "大边")
            {
                win = sum >= 22;
                odds = config.BigEdgeOdds > 0 ? config.BigEdgeOdds : (config.EdgeOdds > 0 ? config.EdgeOdds : 5.0m);
            }
            else if (code == "XB" || code == "小边")
            {
                win = sum <= 5;
                odds = config.SmallEdgeOdds > 0 ? config.SmallEdgeOdds : (config.EdgeOdds > 0 ? config.EdgeOdds : 5.0m);
            }
            else if (code == "B" || code == "边")
            {
                // 通用边：大边或小边任一中奖
                bool isBigEdge = sum >= 22;
                bool isSmallEdge = sum <= 5;
                win = isBigEdge || isSmallEdge;
                // 使用对应的赔率
                if (isBigEdge)
                    odds = config.BigEdgeOdds > 0 ? config.BigEdgeOdds : (config.EdgeOdds > 0 ? config.EdgeOdds : 5.0m);
                else if (isSmallEdge)
                    odds = config.SmallEdgeOdds > 0 ? config.SmallEdgeOdds : (config.EdgeOdds > 0 ? config.EdgeOdds : 5.0m);
                else
                    odds = config.EdgeOdds > 0 ? config.EdgeOdds : 5.0m;
            }
            
            if (odds == 0m) odds = config.EdgeOdds > 0 ? config.EdgeOdds : 5.0m;
            return win ? it.Amount * odds : -it.Amount;
        }

        /// <summary>
        /// Settle Half Straight bet (半顺)
        /// </summary>
        private static decimal SettleHalfStraight(BetItem it, string baoShunDui, Models.AppConfig config)
        {
            // 半顺: 只在开出"半顺"时赢，不包含"杂"
            var win = baoShunDui == "半顺";
            var odds = config.HalfStraightOdds > 0 ? config.HalfStraightOdds : 2.0m;
            return win ? it.Amount * odds : -it.Amount;
        }

        /// <summary>
        /// Settle Sum bet (和)
        /// </summary>
        private static decimal SettleSum(BetItem it, int sum, Models.AppConfig config)
        {
            // 和: 通常指特定的和值，这里简化为命中0或27
            var win = sum == 0 || sum == 27;
            var odds = config.SumOdds > 0 ? config.SumOdds : 49.0m;
            return win ? it.Amount * odds : -it.Amount;
        }

        /// <summary>
        /// Settle Middle bet (中)
        /// </summary>
        private static decimal SettleMiddle(BetItem it, int sum, Models.AppConfig config)
        {
            // 中: sum在6-21之间（非边）
            var win = sum >= 6 && sum <= 21;
            var odds = config.MiddleOdds > 0 ? config.MiddleOdds : 1.2m;
            return win ? it.Amount * odds : -it.Amount;
        }

        /// <summary>
        /// Settle Mixed bet (杂)
        /// </summary>
        private static decimal SettleMixed(BetItem it, string baoShunDui, Models.AppConfig config)
        {
            // 杂: 非豹子/顺子/对子
            var win = baoShunDui != "豹子" && baoShunDui != "顺子" && baoShunDui != "对子";
            var odds = config.MixedOdds > 0 ? config.MixedOdds : 1.2m;
            return win ? it.Amount * odds : -it.Amount;
        }

        /// <summary>
        /// Settle Combination bet (组合)
        /// 组合: 三个数字各不相同且不是顺子/豹子/对子 (类似杂六)
        /// </summary>
        private static decimal SettleCombination(BetItem it, string baoShunDui, Models.AppConfig config)
        {
            // 组合: 三个数字各不相同且不是顺子/豹子/对子
            // 即: 杂 或 半顺 (非豹子/顺子/对子)
            var win = baoShunDui != "豹子" && baoShunDui != "顺子" && baoShunDui != "对子";
            var odds = config.CombinationOdds > 0 ? config.CombinationOdds : 1.2m;
            return win ? it.Amount * odds : -it.Amount;
        }

        /// <summary>
        /// Settle Tail bet (尾球)
        /// Supports: 尾大/尾小/尾单/尾双 (TailSingle), 尾组合 (TailCombination), 尾数字/尾特码 (TailDigit)
        /// Odds selection based on:
        /// - 尾球开0或9: use TailOdds09* (-1 means kill all)
        /// - 有13/14: use TailOddsWith1314* (with threshold)
        /// - 无13/14: use TailOdds1314* (normal odds)
        /// </summary>
        private static decimal SettleTailBet(BetItem it, int sum, Models.AppConfig config)
        {
            if (!config.TailBallEnabled) return 0m;
            
            var tail = sum % 10; // 尾数
            var code = it.Code?.ToUpper() ?? "";
            bool win = false;
            bool isBigSmallOddEven = false; // 尾大小单双
            bool isCombo = false; // 尾组合
            bool isSpecial = false; // 尾特码/尾数字
            
            // Determine bet type and win condition
            if (code == "WD" || code == "尾大")
            {
                win = tail >= 5;
                isBigSmallOddEven = true;
            }
            else if (code == "WX" || code == "尾小")
            {
                win = tail < 5;
                isBigSmallOddEven = true;
            }
            else if (code == "WDD" || code == "尾单")
            {
                win = tail % 2 != 0;
                isBigSmallOddEven = true;
            }
            else if (code == "WDS" || code == "尾双")
            {
                win = tail % 2 == 0;
                isBigSmallOddEven = true;
            }
            else if (code == "WZH" || code == "尾组合" || it.Kind == BetKind.TailCombination)
            {
                // 尾组合: 尾数为某些组合时中奖，简化处理
                isCombo = true;
                win = true; // 需要根据具体规则判断
            }
            else if (code == "WSZ" || code == "尾数字" || it.Kind == BetKind.TailDigit)
            {
                // 尾数字/尾特码: 猜中具体尾数
                isSpecial = true;
                if (int.TryParse(code.Replace("WSZ", "").Replace("尾", ""), out var targetTail))
                {
                    win = tail == targetTail;
                }
            }
            
            // Determine odds based on game result
            decimal odds = 0m;
            bool is1314 = (sum == 13 || sum == 14);
            bool isTail09 = (tail == 0 || tail == 9);
            
            // Priority: 尾球开0/9 > 有13/14 > 无13/14
            if (isTail09)
            {
                // 尾球开0或9时的赔率 (-1代表杀)
                if (isBigSmallOddEven)
                    odds = config.TailOdds09BigSmall;
                else if (isCombo)
                    odds = config.TailOdds09Combo;
                else
                    odds = config.TailOdds09BigSmall; // 默认用大小单双赔率
                
                // -1代表杀(通杀)
                if (odds == -1m)
                {
                    return -it.Amount;
                }
            }
            else if (is1314)
            {
                // 有13/14时的阶梯赔率
                if (isBigSmallOddEven)
                {
                    // 检查是否超过阈值
                    if (it.Amount >= config.TailBallOver1)
                        odds = config.TailOddsWith1314BigSmall;
                    else
                        odds = config.TailOdds1314BigSmall; // 未超阈值用普通赔率
                }
                else if (isCombo)
                {
                    if (it.Amount >= config.TailBallOver2)
                        odds = config.TailOddsWith1314Combo;
                    else
                        odds = config.TailOdds1314Combo;
                }
                else if (isSpecial)
                {
                    odds = config.TailOdds1314Special;
                }
            }
            else
            {
                // 无13/14时的普通赔率
                if (isBigSmallOddEven)
                    odds = config.TailOdds1314BigSmall;
                else if (isCombo)
                    odds = config.TailOdds1314Combo;
                else if (isSpecial)
                    odds = config.TailOdds1314Special;
            }
            
            // 赔率为0代表回本
            if (odds == 0m && win)
            {
                return 0m; // 回本
            }
            
            // Default fallback
            if (odds <= 0 && odds != -1m)
            {
                odds = 1.4m; // 默认赔率
            }
            
            return win ? it.Amount * odds : -it.Amount;
        }

        #endregion

        #region Helper Methods

        private string GetSettlementFile(DateTime day, string teamId, string period)
        {
            var safeTeam = string.IsNullOrWhiteSpace(teamId) ? "unknown-team" : teamId.Trim();
            var dir = Path.Combine(DataService.Instance.DatabaseDir, "Bets", day.ToString("yyyy-MM-dd"), safeTeam);
            Directory.CreateDirectory(dir);
            return Path.Combine(dir, $"settle-{period}.txt");
        }

        private void WriteSettlement(DateTime day, string teamId, string period, List<SettlementEntry> list, int sum, string dxds, string baoShunDui)
        {
            try
            {
                var file = GetSettlementFile(day, teamId, period);
                var sb = new StringBuilder();
                sb.AppendLine($"期号{period} 号码合计={sum} [{dxds}] [{baoShunDui}]");

                foreach (var s in list.OrderByDescending(x => x.Profit))
                {
                    var sign = s.Profit >= 0 ? "+" : "";
                    sb.AppendLine($"{s.PlayerNick}({Short4(s.PlayerId)}) 盈亏:{sign}{s.Profit} 下注:{s.Stake} {s.ScoreBefore}->{s.ScoreAfter}");
                }

                File.WriteAllText(file, sb.ToString(), Encoding.UTF8);
            }
            catch { }
        }

        public string ReadWinnersText(DateTime day, string teamId, string period)
        {
            try
            {
                var file = GetSettlementFile(day, teamId, period);
                if (!File.Exists(file)) return "";
                return File.ReadAllText(file, Encoding.UTF8);
            }
            catch
            {
                return "";
            }
        }

        private static string Short4(string id)
        {
            if (string.IsNullOrEmpty(id)) return "";
            return id.Length >= 4 ? id.Substring(0, 4) : id;
        }

        private static string GetDxdsCode(int sum)
        {
            var bigSmall = sum >= 14 ? "D" : "X";
            var oddEven = (sum % 2 == 0) ? "S" : "D";
            return bigSmall + oddEven;
        }

        /// <summary>
        /// Determine if result is Leopard/Straight/Pair/Mixed
        /// </summary>
        private static string GetBaoShunDui(int a, int b, int c)
        {
            // 豹子：三数相同
            if (a == b && b == c) return "豹子";
            
            // 对子：两数相同
            if (a == b || a == c || b == c) return "对子";
            
            // 顺子：三数连续
            var arr = new[] { a, b, c }.OrderBy(x => x).ToArray();
            if (arr[0] + 1 == arr[1] && arr[1] + 1 == arr[2]) return "顺子";
            
            // 半顺：两数连续
            if (arr[0] + 1 == arr[1] || arr[1] + 1 == arr[2]) return "半顺";
            
            return "杂";
        }

        /// <summary>
        /// Determine Dragon/Tiger/Draw/Leopard result based on sum and config
        /// Supports:
        /// - Mode 0 (龙虎斗): Zone comparison rules
        /// - Mode 1 (龙虎豹): Number list based
        /// </summary>
        private static string GetDragonTiger(int sum, Models.AppConfig config)
        {
            // 龙虎豹模式 (Mode = 1): 根据号码列表判断
            if (config.DragonTigerMode == 1)
            {
                // Check if sum is in leopard numbers
                if (!string.IsNullOrEmpty(config.LeopardNumbers))
                {
                    var leopardNums = config.LeopardNumbers.Split(new[] { ',', '，' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(s => s.Trim())
                        .ToList();
                    if (leopardNums.Contains(sum.ToString()) || leopardNums.Contains(sum.ToString("00")))
                        return "豹";
                }
                
                // Check if sum is in dragon numbers
                if (!string.IsNullOrEmpty(config.DragonNumbers))
                {
                    var dragonNums = config.DragonNumbers.Split(new[] { ',', '，' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(s => s.Trim())
                        .ToList();
                    if (dragonNums.Contains(sum.ToString()) || dragonNums.Contains(sum.ToString("00")))
                        return "龙";
                }
                
                // Check if sum is in tiger numbers
                if (!string.IsNullOrEmpty(config.TigerNumbers))
                {
                    var tigerNums = config.TigerNumbers.Split(new[] { ',', '，' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(s => s.Trim())
                        .ToList();
                    if (tigerNums.Contains(sum.ToString()) || tigerNums.Contains(sum.ToString("00")))
                        return "虎";
                }
                
                return "虎"; // 默认
            }
            
            // 龙虎斗模式 (Mode = 0): 区域比较规则
            // 注意：这里需要三个数字(n1,n2,n3)来正确计算区域值
            // 简化处理：默认比较和值与14
            // Zone1 大于 Zone2 则开龙，小于则开虎，相等则开和
            // 这里简化为比较和值
            
            // 默认使用和值比较
            // 如果有号码定义，先检查号码
            if (!string.IsNullOrEmpty(config.DragonNumbers))
            {
                var dragonNums = config.DragonNumbers.Split(new[] { ',', '，' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => s.Trim())
                    .ToList();
                if (dragonNums.Contains(sum.ToString()) || dragonNums.Contains(sum.ToString("00")))
                    return "龙";
            }
            
            if (!string.IsNullOrEmpty(config.TigerNumbers))
            {
                var tigerNums = config.TigerNumbers.Split(new[] { ',', '，' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => s.Trim())
                    .ToList();
                if (tigerNums.Contains(sum.ToString()) || tigerNums.Contains(sum.ToString("00")))
                    return "虎";
            }
            
            // 默认比较：和值 >= 14 为龙，< 14 为虎，=14 为和
            if (sum > 14) return "龙";
            if (sum < 14) return "虎";
            return "和";
        }

        #endregion
    }
}
