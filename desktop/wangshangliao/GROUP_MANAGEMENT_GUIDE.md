# 群管理功能使用指南

本文档介绍旺商聊机器人系统的群管理功能，全部来自招财狗(ZCG)软件的逆向解析。

---

## 📋 目录

1. [发言检测/禁言/踢人](#发言检测禁言踢人)
2. [锁名片功能](#锁名片功能)
3. [进群欢迎私聊](#进群欢迎私聊)
4. [二七玩法](#二七玩法)

---

## 发言检测/禁言/踢人

### 功能说明
自动检测群内违规发言，包括字数过长、行数过多、发送图片、敏感词等，自动执行禁言或踢人处罚。

### 检测规则

| 规则 | 默认值 | 处罚 |
|------|--------|------|
| 字数超过100 | 100字 | 禁言 |
| 字数超过200 | 200字 | 踢出 |
| 行数超过4行 | 4行 | 禁言 |
| 发送图片 | 第3次 | 踢出 |
| 敏感词 | 自定义 | 禁言 |

### 配置参数

```csharp
var config = SpeechDetectionService.Instance.GetConfig();

config.Enabled = true;              // 启用发言检测
config.MuteCharLimit = 100;         // 禁言字数限制
config.KickCharLimit = 200;         // 踢出字数限制
config.MuteLineLimit = 4;           // 禁言行数限制
config.ImageMuteEnabled = true;     // 图片禁言开关
config.ImageKickCount = 3;          // 图片踢出次数
config.MuteDuration = 10;           // 禁言时长(分钟)
config.WithdrawViolation = true;    // 违规撤回
config.ZeroBalanceMuteIfNotDeposit = false;  // 0分玩家只能上分
config.AutoBlacklistOnKick = true;  // 被踢出加黑名单
config.ForbiddenWords = new List<string> { "敏感词1", "敏感词2" };
```

### 黑名单管理

```csharp
// 添加黑名单
SpeechDetectionService.Instance.AddToBlacklist(playerId);

// 移除黑名单
SpeechDetectionService.Instance.RemoveFromBlacklist(playerId);

// 检查是否在黑名单
bool isBlacklisted = SpeechDetectionService.Instance.IsBlacklisted(playerId);

// 获取黑名单列表
var blacklist = SpeechDetectionService.Instance.GetBlacklist();
```

---

## 锁名片功能

### 功能说明
防止玩家频繁修改群名片，超过限制次数自动踢出并加入黑名单。

### 默认配置

| 配置 | 值 | 说明 |
|------|-----|------|
| 最大修改次数 | 5次 | 超过踢出 |
| 超次数踢人 | ✓ | 自动踢出 |
| 群内通知 | ✓ | 发送警告 |
| 自动重置名片 | ✗ | 可选开启 |

### 代码示例

```csharp
// 注册玩家名片 (进群时调用)
CardLockService.Instance.RegisterCard(playerId, cardName);

// 检测名片变化
var result = CardLockService.Instance.OnCardChange(teamId, playerId, newCard);
if (!result.Allowed)
{
    // 玩家被踢出
}

// 重置修改次数
CardLockService.Instance.ResetChangeCount(playerId);

// 每日重置所有玩家
CardLockService.Instance.ResetAllChangeCounts();
```

### 消息模板变量

| 变量 | 说明 |
|------|------|
| `[旺旺]` | 玩家昵称 |
| `[次数]` | 已修改次数 |
| `[剩余]` | 剩余次数 |
| `[限制]` | 最大次数 |

---

## 进群欢迎私聊

### 功能说明
新成员进群时自动发送欢迎消息（私聊或群内），支持自动同意好友/入群申请。

### 默认配置

| 配置 | 值 |
|------|-----|
| 私聊欢迎 | ✓ |
| 群内欢迎 | ✗ |
| 自动同意好友 | ✓ |
| 账单玩家入群自动同意 | ✓ |
| 托管玩家入群自动同意 | ✓ |
| 欢迎延迟 | 1000ms |

### 欢迎消息

```
私聊欢迎: 恭喜发财，私聊都是骗子，请认准管理。
群内欢迎: 欢迎加入！

未封盘后缀: 当前可下注
已封盘后缀: 当前已封盘，请等待下期
```

### 代码示例

```csharp
// 处理成员进群
await WelcomeService.Instance.OnMemberJoined(teamId, playerId, playerNick, isSealed);

// 处理好友申请
WelcomeService.Instance.OnFriendRequest(requestId, playerId, playerNick, message);

// 处理入群申请
WelcomeService.Instance.OnJoinRequest(requestId, teamId, playerId, playerNick, inviterId);

// 处理成员离开
WelcomeService.Instance.OnMemberLeft(teamId, playerId, playerNick, isKicked, operatorId);
```

---

## 二七玩法

### 功能说明
当开奖号码为2或7时，使用特殊赔率进行结算。

### 赔率配置

| 玩法 | 限额 | 赔率 |
|------|------|------|
| 单注(大/小/单/双) | 49999 | 1.7 |
| 组合(大单/大双/小单/小双) | 29999 | 4.9 |

### 下注格式

```
27大100       → 二七玩法，大 100分
27小单50      → 二七玩法，小单 50分
二七大双200   → 二七玩法，大双 200分
```

### 代码示例

```csharp
// 检查是否为二七号码
bool isTwoSeven = TwoSevenService.Instance.IsTwoSevenNumber(winningNumber);

// 获取二七赔率
decimal odds = TwoSevenService.Instance.GetTwoSevenOdds(BetKind.BigSmall);

// 计算二七中奖
var settlement = TwoSevenService.Instance.CalculateWinnings(winningNumber, bets);

// 解析二七下注
var bets = TwoSevenService.Instance.ParseTwoSevenBets(message);
```

### 自定义二七号码

默认二七号码为 `2` 和 `7`，可以自定义：

```csharp
var config = TwoSevenService.Instance.GetConfig();
config.CustomNumbers = new List<int> { 2, 7, 12, 17 }; // 自定义号码
TwoSevenService.Instance.SaveConfig(config);
```

---

## UI控件使用

### 群管理设置控件

```csharp
// 在窗体中添加群管理设置控件
var groupControl = new GroupManagementControl();
groupControl.Dock = DockStyle.Fill;
this.Controls.Add(groupControl);
```

该控件包含三个Tab页：
- **发言检测**: 配置字数/行数限制、图片检测、敏感词、黑名单
- **锁名片**: 配置修改次数限制、警告/踢出模板
- **进群欢迎**: 配置欢迎消息、自动同意设置

---

## BotController集成

所有群管理功能已集成到 `BotController`：

```csharp
// 启动机器人 (自动启用所有群管理功能)
var bot = BotController.Instance;
await bot.StartAsync("群ID");

// 处理成员进群
await bot.OnMemberJoinedAsync(teamId, playerId, playerNick);

// 处理成员离开
bot.OnMemberLeft(teamId, playerId, playerNick, isKicked, operatorId);

// 处理名片修改
bot.OnCardModified(teamId, playerId, newCard);
```

### 处理器优先级

```
SpeechDetectionHandler  → 优先级 1000 (最高，先检测违规)
BetHandler              → 优先级 100
ScoreHandler            → 优先级 90
TrusteeHandler          → 优先级 60
GuessNumberHandler      → 优先级 55
BonusHandler            → 优先级 45
AutoReplyHandler        → 优先级 10 (最低)
```

---

## 文件结构

```
Services/
├── SpeechDetectionService.cs    # 发言检测服务
├── CardLockService.cs           # 锁名片服务
├── WelcomeService.cs            # 进群欢迎服务
└── Betting/
    └── TwoSevenService.cs       # 二七玩法服务

Bot/Handlers/
└── SpeechDetectionHandler.cs    # 发言检测处理器

Controls/
└── GroupManagementControl.cs    # 群管理设置UI
```

---

## 来源说明

所有功能均来自招财狗(ZCG) v4.25软件的完整逆向解析：

| 配置项 | 原始值 |
|--------|--------|
| 发言检测_字数禁言 | 100 |
| 发言检测_字数踢出 | 200 |
| 发言检测_行数禁言 | 4 |
| 发言检测_图片次数踢出 | 3 |
| 发言检测_禁言时间 | 10 |
| 发言检测_违规撤回 | 真 |
| 锁名片开关 | 真 |
| 锁名片_最大改名片次数 | 5 |
| 锁名片_超次数踢人 | 真 |
| 进群私聊玩家 | 恭喜发财，私聊都是骗子... |
| 自动同意好友添加 | 真 |
| 二七玩法_开关 | 真 |
| 二七玩法_单注总额 | 49999 |
| 二七玩法_单注赔率 | 1.7 |
| 二七玩法_组合总额 | 29999 |
| 二七玩法_组合赔率 | 4.9 |

---

*文档更新时间: 2026-01-10*
