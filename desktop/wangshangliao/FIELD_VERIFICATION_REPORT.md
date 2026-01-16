# 旺商聊字段核对报告

**核对时间**: 2026-01-08
**状态**: ⚠️ 部分字段需要补充

## 📊 NIM SDK 实际返回字段 vs 代码实现对比

### 消息字段 (Message Fields)

| NIM SDK字段 | ChatMessage属性 | 解析状态 | 说明 |
|-------------|----------------|----------|------|
| `scene` | Scene | ✅ 已解析 | p2p/team |
| `from` | SenderId | ✅ 已解析 | **OnlyMemberBet核心字段** |
| `fromNick` | SenderName | ✅ 已解析 | 发送者昵称 |
| `fromClientType` | SenderClientType | ⚠️ 未解析 | 模型有字段，正则未提取 |
| `fromDeviceId` | SenderDeviceId | ⚠️ 未解析 | 模型有字段，正则未提取 |
| `to` | GroupId | ✅ 已解析 | 群ID |
| `time` | Time | ✅ 已解析 | 时间戳 |
| `type` | TypeRaw | ✅ 已解析 | text/custom/image |
| `text` | Content | ✅ 已解析 | 文本内容 |
| `idClient` | IdClient | ✅ 已解析 | 客户端消息ID |
| `idServer` | Id | ⚠️ 未解析 | 模型有字段，正则未提取 |
| `content` | RawContent | ✅ 已解析 | 自定义消息内容 |
| `flow` | Flow | ✅ 已解析 | in/out |
| `sessionId` | SessionId | ✅ 构造 | scene-to |
| `status` | Status | ⚠️ 未解析 | 模型有字段，正则未提取 |
| `isHistoryable` | IsHistoryable | ⚠️ 未解析 | 模型有字段，正则未提取 |
| `isRoamingable` | IsRoamingable | ⚠️ 未解析 | 模型有字段，正则未提取 |
| `isSyncable` | IsSyncable | ⚠️ 未解析 | 模型有字段，正则未提取 |
| `isPushable` | IsPushable | ⚠️ 未解析 | 模型有字段，正则未提取 |
| `isOfflinable` | IsOfflinable | ⚠️ 未解析 | 模型有字段，正则未提取 |
| `isUnreadable` | IsUnreadable | ⚠️ 未解析 | 模型有字段，正则未提取 |
| `needPushNick` | NeedPushNick | ⚠️ 未解析 | 模型有字段，正则未提取 |
| `needMsgReceipt` | NeedMsgReceipt | ⚠️ 未解析 | 模型有字段，正则未提取 |
| `isLocal` | IsLocal | ⚠️ 未解析 | 模型有字段，正则未提取 |
| `resend` | IsResend | ⚠️ 未解析 | 模型有字段，正则未提取 |
| `cc` | - | ❌ 未映射 | 抄送字段(可选) |
| `userUpdateTime` | - | ❌ 未映射 | 可选 |
| `needUpdateSession` | - | ❌ 未映射 | 可选 |
| `target` | - | ❌ 未映射 | 可选 |

### 群成员字段 (TeamMember Fields)

| NIM SDK字段 | TeamMember属性 | 状态 | 说明 |
|-------------|----------------|------|------|
| `id` | Id | ✅ 完整 | teamId-account |
| `teamId` | TeamId | ✅ 完整 | 群ID |
| `account` | Account | ✅ 完整 | **OnlyMemberBet核心字段** |
| `nickInTeam` | NickInTeam | ✅ 完整 | 群内昵称 |
| `type` | Type | ✅ 完整 | normal/owner/manager |
| `joinTime` | JoinTime | ✅ 完整 | 加入时间 |
| `updateTime` | UpdateTime | ✅ 完整 | 更新时间 |
| `active` | Active | ✅ 完整 | 是否活跃 |
| `valid` | Valid | ✅ 完整 | 是否有效 |
| `mute` | Mute | ✅ 完整 | 是否禁言 |
| `invitorAccid` | InvitorAccid | ✅ 完整 | 邀请者账号 |
| `custom` | Custom | ✅ 完整 | 自定义数据 |

### 群信息字段 (TeamInfo Fields)

| NIM SDK字段 | TeamInfo属性 | 状态 |
|-------------|-------------|------|
| `teamId` | TeamId | ✅ 完整 |
| `name` | Name | ✅ 完整 |
| `type` | Type | ✅ 完整 |
| `owner` | Owner | ✅ 完整 |
| `level` | Level | ✅ 完整 |
| `valid` | Valid | ✅ 完整 |
| `validToCurrentUser` | ValidToCurrentUser | ✅ 完整 |
| `memberNum` | MemberNum | ✅ 完整 |
| `memberUpdateTime` | MemberUpdateTime | ✅ 完整 |
| `createTime` | CreateTime | ✅ 完整 |
| `updateTime` | UpdateTime | ✅ 完整 |
| `avatar` | Avatar | ✅ 完整 |
| `intro` | Intro | ✅ 完整 |
| `announcement` | Announcement | ✅ 完整 |
| `joinMode` | JoinMode | ✅ 完整 |
| `beInviteMode` | BeInviteMode | ✅ 完整 |
| `inviteMode` | InviteMode | ✅ 完整 |
| `updateTeamMode` | UpdateTeamMode | ✅ 完整 |
| `updateCustomMode` | UpdateCustomMode | ✅ 完整 |
| `mute` | Mute | ✅ 完整 |
| `muteType` | MuteType | ✅ 完整 |
| `serverCustom` | ServerCustom | ✅ 完整 |
| `custom` | Custom | ✅ 完整 |

## ✅ OnlyMemberBet 核心功能验证

### 字段匹配检验
```
消息: msg.SenderId = "1229181167" (from字段)
               ↓
成员: member.Account = "1229181167" (account字段)
               ↓
        完全匹配 ✅
```

### 测试数据验证
- 群ID: `21654357327`
- 成员数量: **1965人**
- 成员类型: normal(1962) + owner(1) + manager(2)
- 账号格式: 纯数字字符串 (如 `1229181167`)

### API调用链验证
```
BetLedgerService.HandleMessage()
    ↓
ChatService.IsTeamMemberAsync(groupId, msg.SenderId)
    ↓
ChatService.GetTeamMembersViaNimAsync(teamId)
    ↓
NIM SDK: window.nim.getTeamMembers({teamId, done})
    ↓
返回: HashSet<account>
    ↓
Contains(senderId) → true/false
```

## 📋 结论

| 功能 | 状态 | 说明 |
|------|------|------|
| **OnlyMemberBet** | ✅ 完全兼容 | 核心字段(from/account)完美匹配 |
| **消息接收** | ✅ 核心功能正常 | 关键字段已解析 |
| **群成员获取** | ✅ 完整 | 所有字段已映射 |
| **群信息获取** | ✅ 完整 | 所有字段已映射 |
| **消息扩展字段** | ⚠️ 可选优化 | 部分标志字段未解析 |

**核心功能已完全兼容旺商聊！** OnlyMemberBet所需的关键字段（消息发送者ID与群成员账号）匹配完美。

