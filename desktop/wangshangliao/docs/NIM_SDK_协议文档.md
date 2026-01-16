# 旺商聊 NIM SDK (网易云信) 完整协议文档

> **注意**: 本文档是通过 Chrome DevTools Protocol 从旺商聊客户端提取的完整 NIM SDK API 列表。

---

## 通信架构

```
┌─────────────────────────────────────────────────────────────────────────┐
│                        旺商聊通信架构                                    │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                          │
│  ┌──────────────┐     ┌──────────────┐     ┌──────────────────────────┐ │
│  │ 旺商聊机器人  │◄────►│    CDP       │◄────►│    旺商聊 (Electron)     │ │
│  │ (C# WinForms) │     │ WebSocket    │      │    内置 NIM SDK          │ │
│  └──────────────┘     └──────────────┘     └──────────────────────────┘ │
│         │                    │                         │                 │
│         │              Port 9333                       │                 │
│         │        (Chrome DevTools)                     ▼                 │
│         │                                  ┌──────────────────────────┐ │
│         │                                  │   网易云信服务器           │ │
│         └──────────────────────────────────►│   (NIM Server)           │ │
│                  通过 JS 注入调用            └──────────────────────────┘ │
│                                                                          │
└─────────────────────────────────────────────────────────────────────────┘
```

---

## NIM SDK 事件回调 (window.nim.options)

以下是旺商聊注册的所有消息事件回调：

### 连接状态事件
| 回调名 | 描述 |
|--------|------|
| `onconnect` | 连接成功 |
| `onwillreconnect` | 即将重新连接 |
| `ondisconnect` | 断开连接 |
| `onerror` | 错误事件 |
| `onsyncdone` | 数据同步完成 |

### 消息事件 (核心)
| 回调名 | 描述 |
|--------|------|
| `onmsg` | **接收单条消息** (核心 Hook 点) |
| `onmsgs` | **接收批量消息** (核心 Hook 点) |
| `onofflinemsgs` | 离线消息 |
| `onroamingmsgs` | 漫游消息 |
| `onofflinefiltermsgs` | 过滤后的离线消息 |
| `onProxyMsg` | 代理消息 |

### 系统消息事件
| 回调名 | 描述 |
|--------|------|
| `onsysmsg` | 系统消息 |
| `oncustomsysmsg` | 自定义系统消息 |
| `onofflinecustomsysmsgs` | 离线自定义系统消息 |
| `onofflinefiltercustomsysmsgs` | 过滤后的离线自定义系统消息 |
| `onbroadcastmsg` | 广播消息 |
| `onbroadcastmsgs` | 批量广播消息 |
| `onsysmsgunread` | 系统消息未读数 |
| `onupdatesysmsgunread` | 更新系统消息未读数 |
| `onupdatesysmsg` | 更新系统消息 |

### 会话事件
| 回调名 | 描述 |
|--------|------|
| `onsessions` | 会话列表 |
| `onupdatesession` | 更新单个会话 |
| `onupdatesessions` | 更新多个会话 |
| `onStickTopSessions` | 置顶会话 |
| `onSessionsWithMoreRoaming` | 更多漫游会话 |
| `onSyncUpdateServerSession` | 同步服务器会话 |

### 群组/Team 事件
| 回调名 | 描述 |
|--------|------|
| `onteams` | 群组列表 |
| `onUpdateTeam` | 更新群组信息 |
| `onCreateTeam` | 创建群组 |
| `onDismissTeam` | 解散群组 |
| `onTransferTeam` | 转让群主 |
| `onteammembers` | 群成员列表 |
| `onupdateteammember` | 更新群成员 |
| `onMyTeamMembers` | 我的群成员信息 |
| `onAddTeamMembers` | 添加群成员 |
| `onRemoveTeamMembers` | 移除群成员 |
| `onUpdateTeamManagers` | 更新群管理员 |
| `onUpdateTeamMembersMute` | 更新群成员禁言状态 |
| `onTeamMsgReceipt` | 群消息回执 |
| `onsyncteammembersdone` | 同步群成员完成 |
| `onsynccreateteam` | 同步创建群组 |

### 好友/用户事件
| 回调名 | 描述 |
|--------|------|
| `onfriends` | 好友列表 |
| `onsyncfriendaction` | 同步好友操作 |
| `onupdateuser` | 更新用户信息 |
| `onupdatemyinfo` | 更新我的信息 |
| `onmyinfo` | 我的信息 |
| `onusers` | 用户列表 |
| `onrobots` | 机器人列表 |
| `onblacklist` | 黑名单 |
| `onmutelist` | 静音列表 |
| `onsyncmarkinblacklist` | 同步黑名单标记 |
| `onsyncmarkinmutelist` | 同步静音标记 |

### 其他事件
| 回调名 | 描述 |
|--------|------|
| `onloginportschange` | 登录端口变化 |
| `onQuickComment` | 快捷评论 |
| `onDeleteQuickComment` | 删除快捷评论 |
| `onPinMsgChange` | 置顶消息变化 |
| `onDeleteMsgSelf` | 删除自己的消息 |
| `onMsgReceipts` | 消息回执 |
| `onpushevents` | 推送事件 |
| `onPushNotificationMultiportConfig` | 推送配置 |
| `onPushNotificationMultiportConfigUpdate` | 更新推送配置 |

---

## NIM SDK 完整方法列表 (window.nim.*)

### 发送消息方法 (最重要)

| 方法名 | 描述 | 参数 |
|--------|------|------|
| **`sendText`** | **发送文本消息** | `{ scene, to, text, done }` |
| **`sendCustomMsg`** | **发送自定义消息** | `{ scene, to, content, done }` |
| `sendFile` | 发送文件 | `{ scene, to, file, done }` |
| `sendGeo` | 发送地理位置 | `{ scene, to, geo, done }` |
| `sendMsg` | 发送消息(通用) | `{ scene, to, type, body, done }` |
| `sendTipMsg` | 发送提示消息 | `{ to, scene, tip, done }` |
| `sendRobotMsg` | 发送机器人消息 | `{ scene, to, robotAccid, content, done }` |
| `sendG2Msg` | 发送G2消息 | - |
| `forwardMsg` | 转发消息 | `{ msg, scene, to, done }` |
| `resendMsg` | 重发消息 | `{ msg, done }` |
| `recallMsg` | 撤回消息 | `{ msg, done }` |

### 群组/Team 管理方法 (重要)

| 方法名 | 描述 | 参数 |
|--------|------|------|
| **`getTeam`** | **获取群信息** | `{ teamId, done }` |
| **`getTeams`** | **获取群列表** | `{ done }` |
| **`muteTeamAll`** | **全员禁言** | `{ teamId, mute, done }` |
| **`updateTeam`** | **更新群信息** | `{ teamId, muteType, done }` |
| `getTeamMembers` | 获取群成员列表 | `{ teamId, done }` |
| `createTeam` | 创建群组 | `{ type, name, accounts, done }` |
| `dismissTeam` | 解散群组 | `{ teamId, done }` |
| `leaveTeam` | 离开群组 | `{ teamId, done }` |
| `addTeamMembers` | 添加群成员 | `{ teamId, accounts, done }` |
| `removeTeamMembers` | 移除群成员 | `{ teamId, accounts, done }` |
| `transferTeam` | 转让群主 | `{ teamId, account, leave, done }` |
| `updateInfoInTeam` | 更新群内信息 | `{ teamId, ..., done }` |
| `updateNickInTeam` | 更新群昵称 | `{ teamId, nick, done }` |
| `updateMuteStateInTeam` | 更新禁言状态 | `{ teamId, account, mute, done }` |
| `getMutedTeamMembers` | 获取被禁言成员 | `{ teamId, done }` |
| `addTeamManagers` | 添加管理员 | `{ teamId, accounts, done }` |
| `removeTeamManagers` | 移除管理员 | `{ teamId, accounts, done }` |
| `getTeamMemberByTeamIdAndAccount` | 获取指定群成员 | `{ teamId, account, done }` |
| `sendTeamMsgReceipt` | 发送群消息回执 | `{ teamMsgReceipt, done }` |

### 历史消息方法

| 方法名 | 描述 | 参数 |
|--------|------|------|
| **`getHistoryMsgs`** | **获取云端历史消息** | `{ scene, to, beginTime, endTime, limit, done }` |
| **`getLocalMsgs`** | **获取本地消息** | `{ sessionId, start, end, limit, done }` |
| `searchHistoryMsgs` | 搜索历史消息 | `{ keyword, done }` |
| `getMsgsByIdServer` | 通过服务器ID获取消息 | `{ idServers, done }` |
| `getLocalMsgByIdClient` | 通过客户端ID获取本地消息 | `{ idClient, done }` |
| `getLocalMsgsByIdClients` | 批量获取本地消息 | `{ idClients, done }` |
| `deleteMsg` | 删除消息 | `{ msg, done }` |
| `deleteMsgSelf` | 删除自己的消息 | `{ msg, done }` |
| `deleteLocalMsg` | 删除本地消息 | `{ msg, done }` |
| `clearServerHistoryMsgs` | 清除服务器历史 | `{ scene, to, done }` |

### 会话管理方法

| 方法名 | 描述 | 参数 |
|--------|------|------|
| **`setCurrSession`** | **设置当前会话** | `{ scene, to }` |
| `resetCurrSession` | 重置当前会话 | - |
| `getLocalSessions` | 获取本地会话列表 | `{ done }` |
| `getLocalSession` | 获取单个本地会话 | `{ sessionId, done }` |
| `deleteLocalSession` | 删除本地会话 | `{ id, done }` |
| `deleteSession` | 删除会话 | `{ scene, to, done }` |
| `resetSessionUnread` | 重置会话未读数 | `{ id, done }` |
| `resetAllSessionUnread` | 重置所有未读数 | `{ done }` |
| `insertLocalSession` | 插入本地会话 | `{ session, done }` |
| `updateLocalSession` | 更新本地会话 | `{ id, ..., done }` |

### 好友/用户方法

| 方法名 | 描述 | 参数 |
|--------|------|------|
| `getUser` | 获取用户信息 | `{ account, done }` |
| `getUsers` | 批量获取用户 | `{ accounts, done }` |
| `updateMyInfo` | 更新我的信息 | `{ ..., done }` |
| `getMyInfo` | 获取我的信息 | `{ done }` |
| `addFriend` | 添加好友 | `{ account, done }` |
| `deleteFriend` | 删除好友 | `{ account, done }` |
| `updateFriend` | 更新好友备注 | `{ account, alias, done }` |
| `getFriends` | 获取好友列表 | `{ done }` |
| `isMyFriend` | 是否是好友 | `{ account }` |
| `addToBlacklist` | 加入黑名单 | `{ account, done }` |
| `removeFromBlacklist` | 移出黑名单 | `{ account, done }` |
| `addToMutelist` | 加入静音列表 | `{ account, done }` |
| `removeFromMutelist` | 移出静音列表 | `{ account, done }` |

### 系统消息方法

| 方法名 | 描述 | 参数 |
|--------|------|------|
| `sendCustomSysMsg` | 发送自定义系统消息 | `{ to, content, done }` |
| `markSysMsgRead` | 标记系统消息已读 | `{ sysMsgs, done }` |
| `getLocalSysMsgs` | 获取本地系统消息 | `{ done }` |
| `deleteLocalSysMsg` | 删除本地系统消息 | `{ idServer, done }` |
| `deleteAllLocalSysMsgs` | 删除所有本地系统消息 | `{ done }` |

### 消息状态方法

| 方法名 | 描述 | 参数 |
|--------|------|------|
| `markMsgRead` | 标记消息已读 | `{ msg, done }` |
| `sendMsgReceipt` | 发送消息回执 | `{ msg, done }` |
| `isMsgRemoteRead` | 检查消息是否远程已读 | `{ msg }` |

### 数据库方法

| 方法名 | 描述 |
|--------|------|
| `clearDB` | 清空数据库 |
| `removeDB` | 移除数据库 |
| `closeDB` | 关闭数据库 |
| `reinitDB` | 重新初始化数据库 |
| `getDBStatus` | 获取数据库状态 |
| `getDBLastOpenError` | 获取最后一次数据库打开错误 |

### 文件相关方法

| 方法名 | 描述 |
|--------|------|
| `previewFile` | 预览文件 |
| `fetchFile` | 获取文件 |
| `fetchFileList` | 获取文件列表 |
| `getFile` | 获取文件 |
| `getFileList` | 获取文件列表 |
| `removeFile` | 移除文件 |
| `deleteFile` | 删除文件 |

### 事件监听方法

| 方法名 | 描述 |
|--------|------|
| `on` | 添加事件监听 |
| `off` | 移除事件监听 |
| `once` | 添加一次性事件监听 |
| `emit` | 触发事件 |
| `addListener` | 添加监听器 |
| `removeListener` | 移除监听器 |
| `removeAllListeners` | 移除所有监听器 |
| `listeners` | 获取监听器列表 |
| `listenerCount` | 获取监听器数量 |
| `eventNames` | 获取事件名列表 |

### 其他方法

| 方法名 | 描述 |
|--------|------|
| `connect` | 连接 |
| `disconnect` | 断开连接 |
| `logout` | 登出 |
| `kick` | 踢人 |
| `destroy` | 销毁实例 |
| `getLoginStatus` | 获取登录状态 |
| `isConnected` | 是否已连接 |
| `getServerTime` | 获取服务器时间 |
| `audioToText` | 语音转文字 |
| `audioToMp3` | 语音转MP3 |

---

## Pinia Store API (旺商聊封装层)

### SDK Store (`pinia._s.get("sdk")`)

| 方法名 | 描述 |
|--------|------|
| **`sendNimMsg`** | 发送 NIM 消息 |
| **`sendNimAutoReplyMsg`** | 发送自动回复消息 |
| `sendNimRtcMsg` | 发送 RTC 消息 |
| `sendLocalTipMsg` | 发送本地提示消息 |
| `sendTipMsg` | 发送提示消息 |
| `sendNoticeCustomMsg` | 发送通知自定义消息 |
| `getNoticeContent` | 获取通知内容 |
| `isMyFriend` | 是否是好友 |
| `nimDestroy` | 销毁 NIM |
| `nimLogout` | NIM 登出 |
| `deleteLocalSession` | 删除本地会话 |
| `deleteSessionAndMsg` | 删除会话和消息 |

### App Store (`pinia._s.get("app")`)

| 方法名 | 描述 |
|--------|------|
| **`setCurrentSession`** | 设置当前会话 |
| `getGroupList` | 获取群列表 |
| `getFriendList` | 获取好友列表 |
| `findGroup` | 查找群组 |
| `findGroupByGroupId` | 通过群ID查找群组 |
| `findUser` | 查找用户 |
| `findUserById` | 通过ID查找用户 |
| `findUserByUid` | 通过UID查找用户 |
| `updateGroup` | 更新群组 |
| `updateUser` | 更新用户 |
| `getReplyState` | 获取回复状态 |
| `updateReplyState` | 更新回复状态 |
| `getSensitiveWords` | 获取敏感词 |
| `setMuteTeams` | 设置禁言群 |
| ...等 |

---

## 核心使用示例

### 1. 发送文本消息 (最常用)

```javascript
// 发送群消息
window.nim.sendText({
    scene: "team",           // "team" = 群聊, "p2p" = 私聊
    to: "333338888",         // teamId 或 对方账号
    text: "消息内容",
    done: function(err, msg) {
        if (err) {
            console.error("发送失败:", err.message, "code:", err.code);
        } else {
            console.log("发送成功:", msg);
        }
    }
});

// 发送私聊消息
window.nim.sendText({
    scene: "p2p",
    to: "user123456",
    text: "私聊内容",
    done: function(err, msg) { ... }
});
```

### 2. 监听消息 (Hook 消息接收)

```javascript
// 方式1: Hook options.onmsg
var origOnmsg = window.nim.options.onmsg;
window.nim.options.onmsg = function(msg) {
    console.log("收到消息:", {
        from: msg.from,
        to: msg.to,
        text: msg.text,
        type: msg.type,
        scene: msg.scene,
        time: msg.time
    });
    // 调用原始处理
    if (origOnmsg) origOnmsg(msg);
};

// 方式2: 使用 on 方法
window.nim.on("msg", function(msg) {
    console.log("收到消息:", msg);
});
```

### 3. 全员禁言

```javascript
// 禁言
window.nim.muteTeamAll({
    teamId: "333338888",
    mute: true,
    done: function(err, obj) {
        if (err) {
            console.error("禁言失败:", err.message);
        } else {
            console.log("禁言成功");
        }
    }
});

// 解除禁言
window.nim.muteTeamAll({
    teamId: "333338888",
    mute: false,
    done: function(err, obj) { ... }
});
```

### 4. 获取群信息

```javascript
window.nim.getTeam({
    teamId: "333338888",
    done: function(err, team) {
        if (!err) {
            console.log("群名称:", team.name);
            console.log("成员数:", team.memberNum);
            console.log("禁言状态:", team.mute);
        }
    }
});
```

### 5. 获取群成员列表

```javascript
window.nim.getTeamMembers({
    teamId: "333338888",
    done: function(err, result) {
        if (!err) {
            console.log("成员列表:", result.members);
        }
    }
});
```

### 6. 获取历史消息

```javascript
window.nim.getHistoryMsgs({
    scene: "team",
    to: "333338888",
    limit: 100,
    done: function(err, result) {
        if (!err) {
            console.log("历史消息:", result.msgs);
        }
    }
});
```

---

## 已在桌面端实现的 API

| API | C# 方法 | 状态 |
|-----|---------|------|
| `sendText` | `SendTextAsync(scene, to, text)` | ✅ 已实现 |
| `sendText` (当前会话) | `SendTextToCurrentSessionAsync(text)` | ✅ 已实现 |
| `muteTeamAll` | `MuteGroupAsync(groupCloudId)` | ✅ 已实现 |
| `muteTeamAll` (解除) | `UnmuteGroupAsync(groupCloudId)` | ✅ 已实现 |
| `updateTeam` | 内部使用 | ✅ 已实现 |
| `getTeam` | 内部使用 | ✅ 已实现 |
| `getTeams` | 内部使用 | ✅ 已实现 |
| `options.onmsg` Hook | `InjectMessageHookAsync()` | ✅ 已实现 |
| `options.onmsgs` Hook | `InjectMessageHookAsync()` | ✅ 已实现 |

## 待实现的 API (自动发消息需要)

| API | 用途 | 优先级 |
|-----|------|--------|
| `sendCustomMsg` | 发送自定义消息(如红包、名片) | 中 |
| `sendFile` | 发送文件/图片 | 中 |
| `getHistoryMsgs` | 获取历史消息 | 低 |
| `getTeamMembers` | 获取群成员列表 | 低 |
| `updateMuteStateInTeam` | 单人禁言 | 低 |

---

## 总结

**自动发消息功能已具备基础**：
1. `window.nim.sendText()` - 已在 `ChatService.cs` 中封装
2. 消息接收 Hook - 已通过 `options.onmsg` 实现
3. 禁言功能 - 已通过 `muteTeamAll()` 实现

**不需要 AI 大模型**，因为：
- 通信协议是固定的 NIM SDK API
- 自动回复逻辑已在 `AutoReplyService.cs` 中实现
- 只需调用 `SendTextAsync()` 即可发送消息

**建议优化方向**：
1. 增加消息发送重试机制
2. 添加消息发送队列，避免频繁调用
3. 实现更多消息类型支持 (图片、文件等)

