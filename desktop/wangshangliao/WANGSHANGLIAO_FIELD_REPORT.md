# 旺商聊字段探索报告

**探索时间**: 2026-01-08
**目标**: 为 OnlyMemberBet (只接群成员下注) 功能提供完整的字段兼容性

## 🎯 核心发现

### 1. NIM SDK 成功集成 ✅

- **可用方法**: 352个
- **关键方法**:
  - `getTeamMembers` - 获取群成员列表
  - `getTeam` - 获取群信息
  - `getHistoryMsgs` - 获取历史消息
  - `sendText` - 发送文本消息

### 2. 群成员 API 完全可用 ✅

| 字段 | 类型 | 说明 | 用途 |
|------|------|------|------|
| `account` | string | 用户账号ID | **OnlyMemberBet 验证核心字段** |
| `nickInTeam` | string | 群内昵称 (MD5哈希) | 显示名称 |
| `type` | string | normal/owner/manager | 成员类型 |
| `joinTime` | number | 加入时间戳 | 统计 |
| `updateTime` | number | 更新时间戳 | 缓存刷新 |
| `active` | boolean | 是否活跃 | 过滤 |
| `valid` | boolean | 是否有效 | 过滤 |
| `mute` | boolean | 是否禁言 | 权限控制 |
| `invitorAccid` | string | 邀请者账号 | 溯源 |
| `custom` | string | 自定义数据 JSON | 扩展 |

**测试群数据**:
- Team ID: `21654357327`
- 成员数量: **1965人**
- 成员类型分布: normal(1962) + owner(1) + manager(2)

### 3. 消息结构字段 ✅

| 字段 | 类型 | 说明 |
|------|------|------|
| `from` | string | **发送者账号 (与群成员 account 匹配)** |
| `fromNick` | string | 发送者昵称 |
| `to` | string | 接收者/群ID |
| `time` | number | 时间戳 (毫秒) |
| `type` | string | text/custom/image |
| `text` | string | 文本内容 |
| `scene` | string | p2p/team |
| `flow` | string | in/out |
| `idClient` | string | 客户端消息ID |
| `idServer` | string | 服务器消息ID |
| `sessionId` | string | 会话ID |
| `content` | string | 自定义消息内容 (JSON) |
| `isHistoryable` | boolean | 是否可记录历史 |
| `isRoamingable` | boolean | 是否可漫游 |
| `isSyncable` | boolean | 是否可同步 |
| `isPushable` | boolean | 是否推送 |
| `isOfflinable` | boolean | 是否离线 |
| `isUnreadable` | boolean | 是否计入未读 |
| `needPushNick` | boolean | 是否推送昵称 |
| `needMsgReceipt` | boolean | 是否需要回执 |
| `status` | string | 消息状态 |

### 4. 当前用户信息 ✅

| 字段 | 类型 | 说明 |
|------|------|------|
| `account` | string | 用户账号 |
| `nick` | string | 昵称 |
| `avatar` | string | 头像URL |
| `gender` | string | 性别 |
| `custom` | string | 自定义数据 |
| `createTime` | number | 创建时间 |
| `updateTime` | number | 更新时间 |

### 5. 群/Team 信息 ✅

| 字段 | 类型 | 说明 |
|------|------|------|
| `teamId` | string | 群ID |
| `name` | string | 群名称 |
| `type` | string | advanced/normal |
| `owner` | string | 群主账号 |
| `memberNum` | number | 成员数量 |
| `joinMode` | string | noVerify/needVerify/rejectAll |
| `avatar` | string | 群头像 |
| `intro` | string | 群介绍 |
| `announcement` | string | 群公告 |
| `level` | number | 群等级 |
| `valid` | boolean | 是否有效 |
| `createTime` | number | 创建时间 |
| `updateTime` | number | 更新时间 |
| `memberUpdateTime` | number | 成员更新时间 |

## 🔧 OnlyMemberBet 功能验证

### 实现位置
- **BetLedgerService.cs** (Line 70-85): 检查逻辑
- **ChatService.cs** (Line 4186-4192): `IsTeamMemberAsync` 方法
- **ChatService.cs** (Line 4002-4100): `GetTeamMembersViaNimAsync` 方法

### 工作流程

```
1. 收到私聊下注消息
2. 检查 OnlyMemberBet 设置是否启用
3. 获取配置中的 GroupId
4. 调用 ChatService.IsTeamMemberAsync(groupId, senderId)
5. IsTeamMemberAsync 调用 GetTeamMembersViaNimAsync(teamId)
6. GetTeamMembersViaNimAsync 使用 NIM SDK 的 getTeamMembers API
7. 返回成员账号 HashSet，检查 senderId 是否在其中
8. 如果不是群成员，拒绝下注
```

### 关键匹配

| 消息字段 | 群成员字段 | 说明 |
|----------|------------|------|
| `msg.SenderId` (from `from`) | `member.account` | **完全匹配** ✅ |

## 📁 生成的数据文件

1. `wangshangliao_full_data_*.json` - 完整探索数据
2. `member_accounts_21654357327.json` - 群成员账号列表

## ✅ 结论

**OnlyMemberBet 功能完全兼容旺商聊！**

- 消息中的 `from` 字段与群成员的 `account` 字段格式一致（纯数字字符串）
- NIM SDK 的 `getTeamMembers` API 可正常获取群成员列表
- 代码实现正确，可以准确验证发送者是否为群成员

## 📋 探索工具

创建的探索脚本可供后续使用：
- `explore_window.js` - 窗口对象探索
- `explore_full_members.js` - 完整成员/消息探索
- `explore_cdp.js` - CDP 基础探索

