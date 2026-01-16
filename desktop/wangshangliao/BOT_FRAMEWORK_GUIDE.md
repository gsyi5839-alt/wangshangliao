# 旺商聊机器人框架使用指南

本指南说明如何使用完整的机器人框架连接旺商聊并实现自动回复功能。

## 📦 框架文件结构

```
Services/Bot/
├── BotController.cs          # 机器人主控制器
├── MessageDispatcher.cs      # 消息调度器
└── Handlers/
    ├── BetHandler.cs         # 下注处理器
    ├── ScoreHandler.cs       # 上下分处理器
    └── AutoReplyHandler.cs   # 自动回复处理器
```

---

## 🚀 快速启动

### 1. 启动机器人

```csharp
// 获取机器人控制器实例
var bot = BotController.Instance;

// 监听日志
bot.OnLog += msg => Console.WriteLine(msg);

// 监听上下分审核请求
bot.OnDepositRequest += (playerId, amount, balance) => {
    // 显示审核UI或自动处理
};

// 启动机器人（传入群ID）
await bot.StartAsync("123456789");
```

### 2. 停止机器人

```csharp
BotController.Instance.Stop();
```

---

## 📋 消息处理流程

```
旺商聊消息 → ChatService → BotController → MessageDispatcher
                                              ↓
                              ┌───────────────┼───────────────┐
                              ↓               ↓               ↓
                         BetHandler     ScoreHandler    AutoReplyHandler
                         (下注处理)     (上下分处理)    (自动回复)
                              ↓               ↓               ↓
                              └───────────────┼───────────────┘
                                              ↓
                                        发送回复消息
```

---

## 🎯 处理器说明

### BetHandler (下注处理器)

**优先级:** 10 (最高)

**功能:**
- 解析下注消息 (大100, dad50, 操13/100 等)
- 验证余额和限额
- 扣除余额并记录下注
- 发送确认消息

**使用:**
```csharp
// 手动启用群
var handler = new BetHandler();
handler.EnableTeam("123456789");
handler.SetCurrentPeriod("20240110001");
```

---

### ScoreHandler (上下分处理器)

**优先级:** 20

**功能:**
- 处理上分请求 (上100, +100, 100到)
- 处理下分请求 (下100, 回100, 提现)
- 处理查分请求 (发1, 1, 查)
- 处理回水请求 (回水, 返点)

**配置:**
```csharp
var config = new ScoreHandlerConfig
{
    DepositEnabled = true,
    WithdrawEnabled = true,
    AutoDeposit = false,       // 是否自动上分
    AutoWithdraw = false,      // 是否自动下分
    MinBetCountForWithdraw = 1 // 下分最少下注次数
};
BotController.Instance.SetScoreConfig(config);
```

**关键词配置:**
| 功能 | 默认关键词 |
|------|------------|
| 上分 | 上分, 上芬, +, 到, 充值 |
| 下分 | 下分, 下芬, 回, 回芬, 提现 |
| 查分 | 发1, 1, 查, 查分, 余额 |
| 回水 | 回水, 返水, 返点 |

---

### AutoReplyHandler (自动回复处理器)

**优先级:** 50 (最低)

**功能:**
- 关键词自动回复
- 发送二维码图片
- 发送历史记录
- 发送个人数据

**内置规则:**
| 名称 | 触发关键词 | 回复 |
|------|------------|------|
| 财付通 | 财富, cf, 发财富... | 发送财付通二维码 |
| 支付宝 | 支付, zf, 发支付... | 发送支付宝二维码 |
| 微信 | 微信, 发微信... | 发送微信二维码 |
| 历史 | 历史, 2, 发历史... | 发送开奖历史 |
| 账单 | 账单, 1, 数据... | 发送个人数据 |

**添加自定义规则:**
```csharp
BotController.Instance.AddAutoReplyRule(new AutoReplyRule
{
    Name = "客服",
    Keywords = new[] { "客服", "联系客服", "找人" },
    ReplyType = AutoReplyType.Text,
    ReplyContent = "请联系管理员: @管理员"
});
```

---

## 🔧 管理操作

### 手动上分
```csharp
var newBalance = BotController.Instance.ManualDeposit("playerId", 1000m, "管理员上分");
```

### 手动下分
```csharp
var (success, balance, error) = BotController.Instance.ManualWithdraw("playerId", 500m);
```

### 查询余额
```csharp
var balance = BotController.Instance.GetPlayerBalance("playerId");
```

### 获取下注核对
```csharp
var checkMsg = BotController.Instance.GetBetCheckMessage();
// 输出:
// 第20240110001期 下注核对
// ----------------------
// 玩家A: 大100 小单50 = 150
// 玩家B: 对子100 = 100
// ----------------------
// 人数:2 总分:250
```

---

## 🎲 开奖处理

### 手动触发开奖
```csharp
// 当收到开奖结果时调用
await BotController.Instance.ProcessLotteryResultAsync(
    d1: 8, d2: 5, d3: 6, sum: 19);
```

### 自动开奖 (结合LotteryService)
```csharp
// 在LotteryService收到开奖结果时
LotteryService.Instance.OnLotteryResult += async (period, result) =>
{
    // 解析结果: "8+5+6=19"
    var parts = result.Split('+', '=');
    if (parts.Length == 4)
    {
        await BotController.Instance.ProcessLotteryResultAsync(
            int.Parse(parts[0]),
            int.Parse(parts[1]),
            int.Parse(parts[2].Split('=')[0]),
            int.Parse(parts[3]));
    }
};
```

---

## 📊 从招财狗提取的协议信息

### QX框架API (36个核心函数)

| 函数 | 功能 |
|------|------|
| `QX_Group_SendMsg` | 发送群消息 |
| `QX_Friend_SendMsg` | 发送私聊消息 |
| `QX_Group_GetUserList` | 获取群成员列表 |
| `QX_Group_UserSayState` | 设置群禁言状态 |
| `QX_Group_WithdrawMessage` | 撤回消息 |
| `QX_Group_UserSetCardName` | 设置群名片 |
| `QX_Transfer_Send` | 发送转账 |

### 通信端口
- 本地端口: **14745**
- 协议: HPSocket TCP

### 自动回复关键词
```
财付通: 财富|发财富|财付通|cf|CF|caif...
支付宝: 支付|支付宝|发支付|zf|ZF...
微信: 微信|发微信|微信多少...
历史: 历史|发历史|开奖历史|2
账单: 账单|数据|1
```

---

## 💡 使用示例

### 完整启动流程

```csharp
public async Task StartBot()
{
    var bot = BotController.Instance;

    // 1. 设置日志
    bot.OnLog += msg => {
        RunLogService.Instance.Log(msg);
        Console.WriteLine(msg);
    };

    // 2. 设置上下分审核回调
    bot.OnDepositRequest += (pid, amount, balance) => {
        // 显示审核对话框
        ShowDepositApprovalDialog(pid, amount);
    };

    // 3. 配置上下分
    bot.SetScoreConfig(new ScoreHandlerConfig
    {
        AutoDeposit = false,
        AutoWithdraw = false,
        MinBetCountForWithdraw = 3
    });

    // 4. 添加自定义自动回复
    bot.AddAutoReplyRule(new AutoReplyRule
    {
        Name = "欢迎",
        Keywords = new[] { "新人", "新手", "怎么玩" },
        ReplyType = AutoReplyType.Template,
        TemplateKey = "发送规矩内容"
    });

    // 5. 启动
    var success = await bot.StartAsync("目标群ID");
    if (success)
    {
        MessageBox.Show("机器人启动成功！");
    }
}
```

---

## 📝 注意事项

1. **先连接旺商聊**: 启动机器人前确保旺商聊已运行并连接
2. **期号自动计算**: 框架会根据彩种类型自动计算期号
3. **消息去重**: 框架内置消息去重机制，防止重复处理
4. **线程安全**: 所有服务都使用锁机制保证线程安全

---

*整合时间: 2026-01-10*
*基于招财狗(ZCG) v4.25版本逆向分析*
