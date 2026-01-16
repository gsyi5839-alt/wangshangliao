# 旺商聊群成员获取 - 深度逆向分析

## 主框架客户端功能 (v4.29)

### 界面布局

| 区域 | 功能 |
|------|------|
| **开奖信息** | 本期号码、倒计时、下期预告 |
| **账单操作** | 发送账单、导入账单、复制账单、清空下注、导入下注 |
| **开奖控制** | 开奖选择、通道选择、修正开奖、通道3备用 |
| **禁言控制** | 启停禁言群、开完本期停、支持竞昵称 |
| **账单管理** | 下注汇总、清除零分、导出账单、开始算账 |
| **详细操作** | 详细盈利、删除账单、历史账单、全体禁言、全体解禁 |
| **客户框** | 玩家旺旺号、玩家昵称、分数、留分、下注内容、时间 |
| **玩家操作** | 搜索玩家、修改信息、显示托玩家、加10个/减10个 |

### 核心功能按钮

```
┌────────────────────────────────────────────────────┐
│  发送账单  导入账单  导入下注  开奖选择  加拿大 ▼   │
│  复制账单  清空下注  修正开奖  通道 ▼   通道3备用   │
│  下注汇总  清除零分  导出账单  开始算账            │
│  详细盈利  删除账单  历史账单  全体禁言  全体解禁   │
│                              校准时间   聊天日志   │
└────────────────────────────────────────────────────┘
```

---

## 上下分处理功能 (F11可隐藏)

### 上分管理

| 功能 | 说明 |
|------|------|
| **喊话内容** | 玩家请求上分时发送的消息内容 |
| **请求上分** | 玩家请求的上分金额 |
| **修改上分** | 管理员修改上分金额 |
| **@喊到** | 确认玩家上分成功，@通知 |
| **@喊没到** | 通知玩家上分未到账 |
| **忽略** | 忽略此上分请求 |

### 下分管理

| 功能 | 说明 |
|------|------|
| **喊话内容** | 玩家请求下分时发送的消息内容 |
| **请求下分** | 玩家请求的下分金额 |
| **修改下分** | 管理员修改下分金额 |
| **余粮** | 玩家当前余额 |
| **@喊查** | 查询玩家下分状态 |
| **@拒绝** | 拒绝玩家下分请求 |
| **忽略** | 忽略此下分请求 |

### 玩家列表字段

| 字段 | 说明 |
|------|------|
| 玩家 | 玩家旺旺号 |
| 昵称 | 玩家群内昵称 |
| 信息 | 上下分请求详情 |
| 余粮 | 当前余额 |
| 次数 | 请求次数 |

---

## 数据来源

旺商聊客户端会将群成员信息缓存到本地存储：

**存储位置**: `%APPDATA%\wangshangliao\config.json`

**关键字段**:
- `gMembers_{NIM群ID}` - 群成员列表
- `gMembersTag_{NIM群ID}` - 成员列表版本标签
- `gNotices_{NIM群ID}` - 群公告

## 群号与NIM群ID映射

| 群号 | NIM群ID | 说明 |
|------|---------|------|
| 3962369093 | 1176721 | 测试交流群 |

**映射方法**: 在 config.json 中搜索 `"groupAccount": "{群号}"`，其前面的 `gMembers_` 键名包含NIM群ID。

## 群成员数据结构

```json
{
  "gMembers_1176721": {
    "v": "0",
    "groupMemberInfo": [
      {
        "userId": 9903485,           // 旺旺号
        "groupRole": "GROUP_ROLE_OWNER",  // 角色
        "userAvatar": "791610986162052788",
        "userNick": "法拉利",        // 用户昵称
        "initialPinyin": "F",
        "groupMemberNick": "法拉",   // 群内昵称
        "accountId": 0,
        "nimId": 1948408648,         // NIM ID
        "isPretty": false,
        "vipLevel": "VL_VIP_0",
        "vipIcon": "0",
        "accountState": "ACCOUNT_STATE_GOOD",
        "entLevel": "ENT_LEVEL_0"
      }
    ]
  }
}
```

## 角色类型

| 角色 | 标识 | 说明 |
|------|------|------|
| 群主 | `GROUP_ROLE_OWNER` | 群创建者 |
| 管理员 | `GROUP_ROLE_ADMIN` | 群管理员 |
| 成员 | `GROUP_ROLE_MEMBER` | 普通成员 |

## C# 实现

```csharp
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

/// <summary>
/// 旺商聊群成员读取器
/// </summary>
public class WangShangLiaoGroupReader
{
    /// <summary>
    /// 读取群成员列表
    /// </summary>
    /// <param name="nimGroupId">NIM群ID</param>
    public static List<GroupMember> GetGroupMembers(long nimGroupId)
    {
        var configPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "wangshangliao", "config.json"
        );
        
        if (!File.Exists(configPath))
            return new List<GroupMember>();
            
        var json = File.ReadAllText(configPath);
        var config = JsonSerializer.Deserialize<JsonElement>(json);
        
        var key = $"gMembers_{nimGroupId}";
        if (!config.TryGetProperty(key, out var groupData))
            return new List<GroupMember>();
            
        if (!groupData.TryGetProperty("groupMemberInfo", out var members))
            return new List<GroupMember>();
            
        var result = new List<GroupMember>();
        foreach (var member in members.EnumerateArray())
        {
            result.Add(new GroupMember
            {
                UserId = member.GetProperty("userId").GetInt64(),
                NimId = member.GetProperty("nimId").GetInt64(),
                UserNick = member.GetProperty("userNick").GetString(),
                GroupNick = member.GetProperty("groupMemberNick").GetString(),
                Role = ParseRole(member.GetProperty("groupRole").GetString()),
                Avatar = member.GetProperty("userAvatar").GetString(),
                AccountState = member.GetProperty("accountState").GetString()
            });
        }
        
        return result;
    }
    
    /// <summary>
    /// 通过群号查找NIM群ID
    /// </summary>
    public static long? FindNimGroupId(string groupAccount)
    {
        var configPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "wangshangliao", "config.json"
        );
        
        var json = File.ReadAllText(configPath);
        
        // 搜索 "groupAccount": "{群号}"
        var searchPattern = $"\"groupAccount\": \"{groupAccount}\"";
        var idx = json.IndexOf(searchPattern);
        if (idx < 0) return null;
        
        // 向前搜索 gMembers_ 键
        var prefix = "\"gMembers_";
        var startIdx = json.LastIndexOf(prefix, idx);
        if (startIdx < 0) return null;
        
        startIdx += prefix.Length;
        var endIdx = json.IndexOf("\"", startIdx);
        var nimIdStr = json.Substring(startIdx, endIdx - startIdx);
        
        return long.TryParse(nimIdStr, out var nimId) ? nimId : null;
    }
    
    private static GroupRole ParseRole(string role) => role switch
    {
        "GROUP_ROLE_OWNER" => GroupRole.Owner,
        "GROUP_ROLE_ADMIN" => GroupRole.Admin,
        _ => GroupRole.Member
    };
}

/// <summary>
/// 群成员信息
/// </summary>
public class GroupMember
{
    public long UserId { get; set; }        // 旺旺号
    public long NimId { get; set; }         // NIM ID
    public string UserNick { get; set; }    // 用户昵称
    public string GroupNick { get; set; }   // 群内昵称
    public GroupRole Role { get; set; }     // 角色
    public string Avatar { get; set; }      // 头像ID
    public string AccountState { get; set; }
}

public enum GroupRole
{
    Owner,   // 群主
    Admin,   // 管理员
    Member   // 成员
}
```

## 使用示例

```csharp
// 通过群号查找NIM群ID
var nimGroupId = WangShangLiaoGroupReader.FindNimGroupId("3962369093");
Console.WriteLine($"NIM群ID: {nimGroupId}"); // 输出: 1176721

// 获取群成员列表
var members = WangShangLiaoGroupReader.GetGroupMembers(1176721);
Console.WriteLine($"群成员数: {members.Count}"); // 输出: 184

// 遍历成员
foreach (var m in members)
{
    Console.WriteLine($"[{m.Role}] {m.UserNick} (旺旺:{m.UserId}, NIM:{m.NimId})");
}
```

## 实测数据 (群号: 3962369093)

| 统计项 | 数量 |
|--------|------|
| 群主 | 1 |
| 管理员 | 2 |
| 普通成员 | 181 |
| **总计** | **184** |

### 部分成员列表

| 角色 | 昵称 | 旺旺号 | NIM ID |
|------|------|--------|--------|
| 群主 | 法拉利 | 9903485 | 1948408648 |
| 管理员 | 机器人 | 9502248 | 1628907626 |
| 管理员 | logo | 14996839 | 1391351554 |
| 成员 | 聚沙成塔 | 5801908 | 2013375204 |
| 成员 | 桔子 | 8697894 | 2092166259 |
| ... | ... | ... | ... |

## 玩家数据结构 (客户框)

```csharp
/// <summary>
/// 玩家信息 (客户框显示)
/// </summary>
public class PlayerInfo
{
    public long WangWangId { get; set; }    // 玩家旺旺号
    public string NickName { get; set; }     // 玩家昵称
    public decimal Score { get; set; }       // 分数
    public decimal Reserve { get; set; }     // 留分
    public string BetContent { get; set; }   // 下注内容
    public DateTime Time { get; set; }       // 时间
    public long NimId { get; set; }          // NIM ID (用于消息发送)
}
```

## 上下分请求数据结构

```csharp
/// <summary>
/// 上分请求
/// </summary>
public class DepositRequest
{
    public long PlayerId { get; set; }       // 玩家旺旺号
    public string NickName { get; set; }     // 昵称
    public string Content { get; set; }      // 喊话内容
    public decimal RequestAmount { get; set; } // 请求上分金额
    public int Count { get; set; }           // 请求次数
    public DateTime Time { get; set; }       // 请求时间
}

/// <summary>
/// 下分请求
/// </summary>
public class WithdrawRequest
{
    public long PlayerId { get; set; }       // 玩家旺旺号
    public string NickName { get; set; }     // 昵称
    public string Content { get; set; }      // 喊话内容
    public decimal RequestAmount { get; set; } // 请求下分金额
    public decimal Balance { get; set; }     // 余粮（当前余额）
    public int Count { get; set; }           // 请求次数
    public DateTime Time { get; set; }       // 请求时间
}
```

## 群成员与玩家关联

```csharp
/// <summary>
/// 通过群成员信息初始化玩家
/// </summary>
public static PlayerInfo FromGroupMember(GroupMember member)
{
    return new PlayerInfo
    {
        WangWangId = member.UserId,
        NickName = member.GroupNick ?? member.UserNick,
        NimId = member.NimId,
        Score = 0,
        Reserve = 0
    };
}

/// <summary>
/// 批量初始化群内所有玩家
/// </summary>
public static List<PlayerInfo> InitPlayersFromGroup(long nimGroupId)
{
    var members = WangShangLiaoGroupReader.GetGroupMembers(nimGroupId);
    return members
        .Where(m => m.Role == GroupRole.Member)  // 只包含普通成员
        .Select(FromGroupMember)
        .ToList();
}
```

## 注意事项

1. **数据实时性**: 本地缓存可能不是最新的，群成员变动后需要刷新
2. **NIM群ID**: 每个群有唯一的NIM群ID，与旺商聊群号不同
3. **权限**: 只能获取已加入群的成员信息
4. **头像URL**: 头像ID需要通过 `https://yiyong-static.nosdn.127.net/avatar/{头像ID}` 转换
5. **玩家筛选**: 客户框通常只显示普通成员，过滤掉群主和管理员
6. **上下分处理**: 需要监听群消息，匹配上下分关键词自动识别请求
