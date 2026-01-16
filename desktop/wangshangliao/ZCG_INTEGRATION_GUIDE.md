# 招财狗(ZCG)功能整合指南

本指南说明如何使用从招财狗软件逆向提取并整合到旺商聊机器人系统的功能。

## 📦 新增文件

### Models (数据模型)
- `Models/Betting/FullOddsConfig.cs` - 完整赔率配置模型

### Services (服务)
- `Services/Betting/OddsService.cs` - 赔率计算服务
- `Services/MessageTemplateService.cs` - 消息模板服务
- `Services/ScoreService.cs` - 上下分服务
- `Services/RebateService.cs` - 回水计算服务

### Database (数据库)
- `backend/src/db/migrations/006_betting_system.sql` - 下注系统数据库迁移

---

## 🎯 核心功能

### 1. 赔率系统 (OddsService)

```csharp
// 获取赔率配置
var config = OddsService.Instance.GetConfig();

// 获取指定类型的赔率
var odds = OddsService.Instance.GetOdds(BetKind.Dxds, "DD"); // 大单赔率

// 获取数字赔率 (0-27)
var digitOdds = OddsService.Instance.GetOdds(BetKind.Digit, "13"); // 数字13赔率

// 验证下注金额
var (isValid, error) = OddsService.Instance.ValidateBetAmount(BetKind.Digit, 100m);

// 计算盈亏
var profit = OddsService.Instance.CalculateProfit(betItem, "8+5+6=19");
```

**默认赔率表:**
| 玩法 | 赔率 | 下限 | 上限 |
|------|------|------|------|
| 大小单双 | 1.8 | 20 | 50000 |
| 组合(大单等) | 3.8 | 20 | 30000 |
| 数字 | 10-665 | 20 | 20000 |
| 对子 | 2 | 20 | 10000 |
| 顺子 | 11 | 20 | 10000 |
| 豹子 | 59 | 20 | 2000 |
| 龙虎 | 1.92 | 20 | 10000 |

---

### 2. 下注解析 (BetMessageParser)

支持的下注格式：

**招财狗拼音格式：**
- `da100` - 大100
- `x50` - 小50
- `dad200` - 大单200
- `das100` - 大双100
- `xd50` - 小单50
- `xs30` - 小双30
- `dz100` - 对子100
- `sz50` - 顺子50
- `bz30` - 豹子30
- `long100` - 龙100
- `hu50` - 虎50
- `jd100` - 极大100
- `jx50` - 极小50

**特码格式：**
- `操13/100` - 数字13下100
- `草13 100` - 数字13下100
- `点13/50` - 数字13下50

**中文格式：**
- `大100` - 大100
- `小单50` - 小单50
- `对子100` - 对子100
- `13/100` - 数字13下100

```csharp
// 解析下注消息
if (BetMessageParser.TryParse("da100 x50 dad200", out var items, out var total, out var normalized))
{
    // items: 下注项列表
    // total: 总下注金额
    // normalized: 标准化显示 "大100 小50 大单200"
}
```

---

### 3. 消息模板 (MessageTemplateService)

**支持的变量：**
| 变量 | 说明 |
|------|------|
| `[艾特]` | @玩家 |
| `[旺旺]` / `[昵称]` | 玩家昵称 |
| `[余粮]` / `[余额]` | 账户余额 |
| `[玩家攻击]` / `[下注内容]` | 下注详情 |
| `[分数]` / `[金额]` | 金额 |
| `[期数]` | 当前期号 |
| `[开奖号码]` | 开奖结果(和值) |
| `[一区]` / `[二区]` / `[三区]` | 三个骰子值 |
| `[大小单双]` | 开奖类型 |
| `[豹顺对子]` | 特殊类型 |
| `[龙虎豹]` | 龙虎结果 |
| `[客户人数]` | 下注人数 |
| `[总分数]` | 总下注额 |
| `[换行]` | 换行符 |

```csharp
// 渲染下注成功模板
var variables = MessageTemplateService.Instance.CreateBetVariables(
    playerNick: "玩家A",
    playerId: "123456",
    betContent: "大100 小50",
    balance: 1000m,
    betAmount: 150m
);
var message = MessageTemplateService.Instance.Render("下注显示", variables);
// 输出: @玩家A(玩家A)
//       本次攻擊:大100 小50,余粮:1000.00

// 渲染开奖模板
var lotteryVars = MessageTemplateService.Instance.CreateLotteryVariables(
    period: "20240110001",
    d1: 8, d2: 5, d3: 6, sum: 19,
    playerCount: 10,
    totalBet: 5000m
);
var lotteryMsg = MessageTemplateService.Instance.Render("开奖发送", lotteryVars);
```

---

### 4. 上下分系统 (ScoreService)

```csharp
// 获取玩家余额
var balance = ScoreService.Instance.GetBalance("player123");

// 上分
var newBalance = ScoreService.Instance.AddScore(
    playerId: "player123",
    amount: 1000m,
    reason: "充值",
    playerNick: "玩家A"
);

// 下分
var (success, balance, error) = ScoreService.Instance.DeductScore(
    playerId: "player123",
    amount: 500m,
    reason: "提现"
);

// 扣除下注金额
var (betSuccess, betBalance, betError) = ScoreService.Instance.DeductBet("player123", 100m, "20240110001");

// 添加中奖金额
ScoreService.Instance.AddWinnings("player123", 180m, "20240110001");

// 获取今日统计
var stats = ScoreService.Instance.GetTodayStats("player123");
// stats.TotalBet, stats.TotalWin, stats.BetCount, stats.NetProfit
```

---

### 5. 回水系统 (RebateService)

**回水方式：**
1. 按组合比例
2. 按下注次数
3. 按下注流水 ✓ (默认)
4. 按输分

```csharp
// 计算回水
var (rebate, error) = RebateService.Instance.CalculateRebate(
    playerId: "player123",
    totalBet: 10000m,
    betCount: 50,
    totalLoss: 2000m
);

// 处理回水并发放
var (success, rebateAmount, message) = RebateService.Instance.ProcessRebate(
    playerId: "player123",
    playerNick: "玩家A",
    totalBet: 10000m,
    betCount: 50,
    totalLoss: 2000m
);
// 自动添加到玩家余额，返回消息模板
```

**默认阶梯配置 (按流水):**
| 流水范围 | 返点比例 |
|----------|----------|
| 100 - 10,000 | 6% |
| 10,001 - 30,000 | 8% |
| 30,001 - 2,000,000 | 10% |

---

## 📁 配置文件位置

所有配置文件存储在 `Data/` 目录：

```
Data/
├── odds-full.ini       # 完整赔率配置
├── message-templates.ini # 消息模板配置
├── player-scores.ini   # 玩家余额数据
├── score-transactions.log # 交易记录
└── rebate-config.ini   # 回水配置
```

---

## 🔧 使用示例

### 完整下注处理流程

```csharp
// 1. 解析下注消息
if (!BetMessageParser.TryParse(message, out var items, out var total, out var normalized))
{
    return; // 不是下注消息
}

// 2. 验证余额
var balance = ScoreService.Instance.GetBalance(playerId);
if (balance < total)
{
    var vars = MessageTemplateService.Instance.CreateBetVariables(nick, playerId, normalized, balance, total);
    var reply = MessageTemplateService.Instance.Render("余粮不足", vars);
    SendMessage(teamId, reply);
    return;
}

// 3. 验证限额
foreach (var item in items)
{
    var (valid, error) = OddsService.Instance.ValidateBetAmount(item.Kind, item.Amount);
    if (!valid)
    {
        SendMessage(teamId, $"@{nick} {error}");
        return;
    }
}

// 4. 扣除余额
var (success, newBalance, deductError) = ScoreService.Instance.DeductBet(playerId, total, period);
if (!success)
{
    SendMessage(teamId, $"@{nick} {deductError}");
    return;
}

// 5. 发送确认消息
var betVars = MessageTemplateService.Instance.CreateBetVariables(nick, playerId, normalized, newBalance, total);
var confirmMsg = MessageTemplateService.Instance.Render("下注显示", betVars);
SendMessage(teamId, confirmMsg);

// 6. 保存下注记录 (略)
```

### 开奖结算流程

```csharp
// 1. 获取开奖结果
var result = "8+5+6=19";

// 2. 遍历所有下注记录计算盈亏
foreach (var bet in periodBets)
{
    decimal totalProfit = 0;
    foreach (var item in bet.Items)
    {
        var profit = OddsService.Instance.CalculateProfit(item, result);
        totalProfit += profit;
    }

    // 3. 结算到玩家账户
    if (totalProfit > 0)
    {
        ScoreService.Instance.AddWinnings(bet.PlayerId, totalProfit + bet.TotalAmount, period);
    }
}

// 4. 发送开奖消息
var lotteryVars = MessageTemplateService.Instance.CreateLotteryVariables(period, 8, 5, 6, 19, playerCount, totalBet);
var lotteryMsg = MessageTemplateService.Instance.Render("开奖发送", lotteryVars);
SendMessage(teamId, lotteryMsg);
```

---

## 📝 注意事项

1. **配置持久化**: 所有服务都会自动保存配置到文件，重启后自动加载
2. **线程安全**: 所有服务都使用锁机制保证线程安全
3. **数据库迁移**: 运行 `006_betting_system.sql` 创建必要的数据库表
4. **模板自定义**: 可通过 `MessageTemplateService.SetTemplate()` 修改任何模板

---

## 🔗 相关文档

- `逆向分析结果/完整逆向分析报告.md` - 招财狗逆向分析报告
- `逆向分析结果/QX框架接口文档.md` - QX框架API文档
- `逆向分析结果/整合方案.md` - 整合方案详情

---

## 🕐 封盘定时任务 (SealingService)

### 功能特点

- **多彩种支持**: 加拿大28(3.5分/期)、比特28(1分/期)、北京28(5分/期)
- **状态机管理**: 接受下注→已提醒→已封盘→已发规则→等待开奖
- **自动禁言**: 封盘前自动禁言群聊
- **可配置提醒**: 封盘提醒、封盘线、规则发送

### 使用示例

```csharp
// 启动封盘服务
var now = DateTime.Now;
var nextDrawTime = now.AddSeconds(210); // 3.5分钟后
SealingService.Instance.Start("20240110001", nextDrawTime);

// 监听事件
SealingService.Instance.OnSendMessage += (teamId, msg) => SendMessage(teamId, msg);
SealingService.Instance.OnMuteGroup += (teamId) => MuteGroup(teamId);

// 查询状态
bool isSealed = SealingService.Instance.IsSealed();
int secondsLeft = SealingService.Instance.GetSecondsToNext();
```

### 配置项

| 配置 | 默认值 | 说明 |
|------|--------|------|
| ReminderSeconds | 60 | 提前60秒发送提醒 |
| SealingSeconds | 20 | 提前20秒封盘 |
| RuleSeconds | 1 | 开奖前1秒发送规则 |
| MuteBeforeSeconds | 5 | 提前5秒禁言 |

---

## 💰 自动开奖结算 (AutoSettlementService)

### 功能特点

- **自动结算**: 收到开奖结果后自动计算所有玩家盈亏
- **多玩法支持**: 支持所有下注类型(大小单双、数字、对子、顺子等)
- **账单生成**: 自动生成开奖账单和玩家明细
- **余额结算**: 自动更新玩家余额

### 使用示例

```csharp
// 添加下注记录
AutoSettlementService.Instance.AddBetRecord(new BetRecord {
    Period = "20240110001",
    PlayerId = "player123",
    PlayerNick = "玩家A",
    Items = items,
    TotalAmount = 150m
});

// 处理开奖结果
var result = new LotteryResult {
    Period = "20240110001",
    Dice1 = 8, Dice2 = 5, Dice3 = 6, Sum = 19
};
var bill = await AutoSettlementService.Instance.ProcessLotteryResultAsync(
    "20240110001", result, teamId);

// 监听事件
AutoSettlementService.Instance.OnBillGenerated += (period, bill) => {
    // 保存账单到数据库
};
```

### 开奖结果模型

```csharp
var result = new LotteryResult { Dice1 = 8, Dice2 = 5, Dice3 = 6, Sum = 19 };

result.IsBig;         // true (>=14)
result.IsOdd;         // true (单)
result.DXDS;          // "大单"
result.IsPair;        // false
result.IsStraight;    // false
result.IsLeopard;     // false
result.DragonTiger;   // "虎"
result.SpecialType;   // ""
```

---

## 🖥️ 管理界面控件

### 新增控件

| 控件 | 功能 |
|------|------|
| `SealingSettingsControl` | 封盘设置管理 |
| `FullOddsSettingsControl` | 完整赔率配置 |
| `MessageTemplateControl` | 消息模板编辑 |

### 使用方法

```csharp
// 在主窗体中添加控件
var sealingControl = new SealingSettingsControl();
tabPageSealing.Controls.Add(sealingControl);

var oddsControl = new FullOddsSettingsControl();
tabPageOdds.Controls.Add(oddsControl);

var templateControl = new MessageTemplateControl();
tabPageTemplate.Controls.Add(templateControl);
```

---

## 📊 招财狗功能配置提取

### 已提取的配置

| 类别 | 配置项 |
|------|--------|
| **封盘设置** | PC/加拿大/比特封盘时间、提醒内容、规则内容 |
| **彩种配置** | PC蛋蛋=1, 比特28=2, 北京28=3 |
| **龙虎玩法** | 龙虎赔率1.92, 自定义龙虎豹号码 |
| **二七玩法** | 单注赔率1.7, 组合赔率4.9 |
| **长龙减赔** | 连开3次减0.1, 连开6次减0.2 |
| **特殊规则** | 豹顺对回本、1314对子回本 |
| **猜数字** | 送分规则、禁猜号码 |
| **私聊托管** | 开奖后/封盘前不下注时间、托管下注内容 |
| **发言检测** | 字数禁言、行数禁言、图片禁言 |

---

*整合时间: 2026-01-10*
*基于招财狗(ZCG) v4.25版本逆向分析*
