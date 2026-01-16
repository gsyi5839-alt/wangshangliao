# 旺商聊 NIM SDK API 完整参考

## 概述

本文档详细记录了旺商聊中可用的 NIM SDK API，通过 `window.nim` 对象访问。

---

## SDK配置信息

| 参数 | 值 |
|------|-----|
| **AppKey** | `b03cfcd909dbf05c25163cc8c7e7b6cf` |
| **Account** | 当前登录账号 |
| **Debug** | false |
| **syncSessionUnread** | true |
| **syncRoamingMsgs** | true |
| **autoMarkRead** | true |

---

## 加密解密

### 昵称解密 (AES-256-CBC)

```javascript
// 配置
const key = CryptoJS.enc.Utf8.parse("d6ba6647b7c43b79d0e42ceb2790e342");
const iv = CryptoJS.enc.Utf8.parse("kgWRyiiODMjSCh0m");

// 解密函数
AES.decrypt(ciphertextBase64);
```

### 昵称密文位置

| 位置 | 字段路径 |
|------|----------|
| 群成员 | `member.custom.nicknameCiphertext` |
| 用户资料 | `user.custom.nickname_ciphertext` |
| 群名称 | `team.serverCustom.nickname_ciphertext` |
| 通知消息 | `attach.users[].custom.nickname_ciphertext` |

### 解密验证示例

| 密文 | 解密结果 |
|------|----------|
| `HLF4vIVpTTX9OuKnY1gW6g==` | 挪仇 |
| `doeOJLw6MuN+rPE9NGqwSFmPF7Kx8TYQQw/0qxWOjio=` | 法拉利福利 ③裙 |
| `mwVKeo034e4vLZC90HRMnA==` | 不死鸟 |
| `JXMLJnD23sIKDtzgc4x2Mw==` | 王国 |

---

## 消息 API

### 消息类型

| type | 说明 | 内容位置 |
|------|------|----------|
| `text` | 文本消息 | `msg.text` (明文) |
| `image` | 图片消息 | `msg.file` (URL) |
| `audio` | 语音消息 | `msg.file` |
| `video` | 视频消息 | `msg.file` |
| `file` | 文件消息 | `msg.file` |
| `custom` | 自定义消息 | `msg.content.b` (加密二进制) |
| `notification` | 通知消息 | `msg.attach` (结构化数据) |
| `tip` | 提示消息 | `msg.tip` |

### 发送消息

#### sendText - 发送文本消息

```javascript
window.nim.sendText({
    scene: 'team',           // 'p2p' | 'team'
    to: '40821608989',       // 目标ID
    text: '消息内容',
    done: function(err, msg) {
        if (err) console.error('发送失败:', err);
        else console.log('发送成功:', msg);
    }
});
```

#### sendCustomMsg - 发送自定义消息

```javascript
window.nim.sendCustomMsg({
    scene: 'team',
    to: '40821608989',
    content: JSON.stringify({ type: 'custom', data: {} }),
    done: function(err, msg) {}
});
```

#### sendFile - 发送文件

```javascript
window.nim.sendFile({
    scene: 'team',
    to: '40821608989',
    file: fileObject,
    done: function(err, msg) {}
});
```

#### sendTipMsg - 发送提示消息

```javascript
window.nim.sendTipMsg({
    scene: 'team',
    to: '40821608989',
    tip: '这是一条提示',
    done: function(err, msg) {}
});
```

#### forwardMsg - 转发消息

```javascript
window.nim.forwardMsg({
    msg: originalMsg,
    scene: 'team',
    to: '40821608989',
    done: function(err, msg) {}
});
```

### 获取消息

#### getLocalMsgs - 获取本地消息

```javascript
window.nim.getLocalMsgs({
    sessionId: 'team-40821608989',
    limit: 100,
    done: function(err, obj) {
        // obj.msgs - 消息数组
    }
});
```

#### getHistoryMsgs - 获取云端历史消息

```javascript
window.nim.getHistoryMsgs({
    scene: 'team',
    to: '40821608989',
    limit: 100,
    endTime: Date.now(),
    done: function(err, obj) {}
});
```

#### getLocalMsgsByIdClients - 按客户端ID获取消息

```javascript
window.nim.getLocalMsgsByIdClients({
    idClients: ['id1', 'id2'],
    done: function(err, msgs) {}
});
```

#### getMsgsByIdServer - 按服务端ID获取消息

```javascript
window.nim.getMsgsByIdServer({
    idServer: 'serverId',
    done: function(err, msg) {}
});
```

### 消息操作

#### deleteMsgSelf - 删除自己的消息

```javascript
window.nim.deleteMsgSelf({
    msg: msgObject,
    done: function(err) {}
});
```

#### clearServerHistoryMsgsWithSync - 清除历史消息

```javascript
window.nim.clearServerHistoryMsgsWithSync({
    sessionId: 'team-40821608989',
    done: function(err) {}
});
```

---

## 群组 API

### 获取群组信息

#### getTeams - 获取所有群组

```javascript
window.nim.getTeams({
    done: function(err, obj) {
        // obj.teams - 群组数组
    }
});
```

**返回字段：**

```javascript
{
    teamId: '40821608989',
    name: 'cfcd208495d565ef66e7dff9f98764da',  // MD5，需解密serverCustom
    owner: '1948408648',
    memberNum: 186,
    type: 'advanced',
    level: 5000,
    avatar: '8671447222758755297',
    intro: '',
    joinMode: 'noVerify',
    beInviteMode: 'needVerify',
    inviteMode: 'manager',
    mute: true,
    muteType: 'normal',
    serverCustom: '{"group_id":1176721,"nickname_ciphertext":"..."}'
}
```

#### getTeam - 获取单个群组

```javascript
window.nim.getTeam({
    teamId: '40821608989',
    done: function(err, team) {}
});
```

#### getTeamMembers - 获取群成员

```javascript
window.nim.getTeamMembers({
    teamId: '40821608989',
    done: function(err, obj) {
        // obj.members - 成员数组
    }
});
```

**成员字段：**

```javascript
{
    account: '1948408648',
    nick: 'd153f4e86e8c0eda3867e842e507cd93',  // MD5哈希
    nickInTeam: '法拉利客服',  // 群主可能是明文
    type: 'owner',  // owner | manager | normal
    mute: false,
    custom: '{"nicknameCiphertext":"..."}'  // 加密的真实昵称
}
```

### 群组管理

#### dismissTeam - 解散群组

```javascript
window.nim.dismissTeam({
    teamId: '40821608989',
    done: function(err) {}
});
```

#### leaveTeam - 退出群组

```javascript
window.nim.leaveTeam({
    teamId: '40821608989',
    done: function(err) {}
});
```

#### transferTeam - 转让群主

```javascript
window.nim.transferTeam({
    teamId: '40821608989',
    account: 'newOwnerAccount',
    leave: false,
    done: function(err) {}
});
```

### 成员管理

#### addTeamMembers - 添加群成员

```javascript
window.nim.addTeamMembers({
    teamId: '40821608989',
    accounts: ['account1', 'account2'],
    done: function(err, obj) {}
});
```

#### removeTeamMembers - 移除群成员

```javascript
window.nim.removeTeamMembers({
    teamId: '40821608989',
    accounts: ['account1'],
    done: function(err) {}
});
```

#### addTeamManagers - 添加管理员

```javascript
window.nim.addTeamManagers({
    teamId: '40821608989',
    accounts: ['account1'],
    done: function(err) {}
});
```

#### removeTeamManagers - 移除管理员

```javascript
window.nim.removeTeamManagers({
    teamId: '40821608989',
    accounts: ['account1'],
    done: function(err) {}
});
```

#### updateInfoInTeam - 更新群内资料

```javascript
window.nim.updateInfoInTeam({
    teamId: '40821608989',
    nickInTeam: '新昵称',
    done: function(err) {}
});
```

### 禁言管理

#### muteTeamAll - 全员禁言

```javascript
window.nim.muteTeamAll({
    teamId: '40821608989',
    mute: true,
    done: function(err, obj) {}
});
```

### 入群管理

#### applyTeam - 申请入群

```javascript
window.nim.applyTeam({
    teamId: '40821608989',
    ps: '申请理由',
    done: function(err) {}
});
```

#### passTeamApply - 通过入群申请

```javascript
window.nim.passTeamApply({
    teamId: '40821608989',
    from: 'applicantAccount',
    idServer: 'sysMessageId',
    done: function(err) {}
});
```

#### rejectTeamApply - 拒绝入群申请

```javascript
window.nim.rejectTeamApply({
    teamId: '40821608989',
    from: 'applicantAccount',
    idServer: 'sysMessageId',
    ps: '拒绝理由',
    done: function(err) {}
});
```

#### acceptTeamInvite - 接受入群邀请

```javascript
window.nim.acceptTeamInvite({
    teamId: '40821608989',
    from: 'inviterAccount',
    done: function(err) {}
});
```

#### rejectTeamInvite - 拒绝入群邀请

```javascript
window.nim.rejectTeamInvite({
    teamId: '40821608989',
    from: 'inviterAccount',
    ps: '拒绝理由',
    done: function(err) {}
});
```

---

## 用户 API

#### getUser - 获取单个用户

```javascript
window.nim.getUser({
    account: '1948408648',
    done: function(err, user) {
        // user.custom 包含 nickname_ciphertext
    }
});
```

#### getUsers - 批量获取用户

```javascript
window.nim.getUsers({
    accounts: ['account1', 'account2'],
    done: function(err, users) {}
});
```

#### updateMyInfo - 更新个人资料

```javascript
window.nim.updateMyInfo({
    nick: '新昵称',
    avatar: 'avatarUrl',
    sign: '签名',
    done: function(err) {}
});
```

---

## 好友 API

#### applyFriend - 申请添加好友

```javascript
window.nim.applyFriend({
    account: 'targetAccount',
    ps: '请求备注',
    done: function(err) {}
});
```

#### passFriendApply - 通过好友申请

```javascript
window.nim.passFriendApply({
    account: 'applicantAccount',
    idServer: 'sysMessageId',
    done: function(err) {}
});
```

#### rejectFriendApply - 拒绝好友申请

```javascript
window.nim.rejectFriendApply({
    account: 'applicantAccount',
    idServer: 'sysMessageId',
    ps: '拒绝理由',
    done: function(err) {}
});
```

#### deleteFriend - 删除好友

```javascript
window.nim.deleteFriend({
    account: 'friendAccount',
    done: function(err) {}
});
```

#### updateFriend - 更新好友备注

```javascript
window.nim.updateFriend({
    account: 'friendAccount',
    alias: '备注名',
    done: function(err) {}
});
```

#### getRelations - 获取关系列表

```javascript
window.nim.getRelations({
    done: function(err, obj) {
        // obj.blacklist - 黑名单
        // obj.mutelist - 静音列表
    }
});
```

---

## 黑名单/静音 API

#### markInBlacklist - 加入/移出黑名单

```javascript
window.nim.markInBlacklist({
    account: 'targetAccount',
    isAdd: true,
    done: function(err) {}
});
```

#### markInMutelist - 加入/移出静音列表

```javascript
window.nim.markInMutelist({
    account: 'targetAccount',
    isAdd: true,
    done: function(err) {}
});
```

---

## 会话 API

#### getLocalSession - 获取单个会话

```javascript
window.nim.getLocalSession({
    sessionId: 'team-40821608989',
    done: function(err, session) {}
});
```

#### getLocalSessions - 获取所有本地会话

```javascript
window.nim.getLocalSessions({
    limit: 100,
    done: function(err, sessions) {}
});
```

#### deleteLocalSession - 删除本地会话

```javascript
window.nim.deleteLocalSession({
    id: 'team-40821608989',
    done: function(err) {}
});
```

#### getServerSessions - 获取服务端会话

```javascript
window.nim.getServerSessions({
    limit: 100,
    done: function(err, obj) {}
});
```

#### resetSessionUnread - 重置未读数

```javascript
window.nim.resetSessionUnread({
    sessionId: 'team-40821608989',
    done: function(err) {}
});
```

#### addStickTopSession - 置顶会话

```javascript
window.nim.addStickTopSession({
    id: 'team-40821608989',
    done: function(err) {}
});
```

#### deleteStickTopSession - 取消置顶

```javascript
window.nim.deleteStickTopSession({
    id: 'team-40821608989',
    done: function(err) {}
});
```

---

## 消息回执 API

#### sendMsgReceipt - 发送已读回执

```javascript
window.nim.sendMsgReceipt({
    msg: msgObject,
    done: function(err) {}
});
```

#### sendTeamMsgReceipt - 发送群已读回执

```javascript
window.nim.sendTeamMsgReceipt({
    teamMsgReceipts: [{
        teamId: '40821608989',
        idClient: 'msgIdClient',
        idServer: 'msgIdServer'
    }],
    done: function(err) {}
});
```

---

## 信令/通话 API

#### signalingJoinAndAccept - 加入并接受信令

```javascript
window.nim.signalingJoinAndAccept({
    channelId: 'channelId',
    offlineEnabled: true,
    done: function(err, obj) {}
});
```

#### signalingLeave - 离开信令

```javascript
window.nim.signalingLeave({
    channelId: 'channelId',
    done: function(err) {}
});
```

#### signalingCancel - 取消信令

```javascript
window.nim.signalingCancel({
    channelId: 'channelId',
    done: function(err) {}
});
```

#### signalingControl - 信令控制

```javascript
window.nim.signalingControl({
    channelId: 'channelId',
    account: 'targetAccount',
    customInfo: '{}',
    done: function(err) {}
});
```

---

## 连接管理

#### connect - 连接

```javascript
window.nim.connect();
```

#### disconnect - 断开连接

```javascript
window.nim.disconnect();
```

#### destroy - 销毁实例

```javascript
window.nim.destroy();
```

---

## 文件操作

#### previewFile - 预览文件

```javascript
window.nim.previewFile({
    type: 'image',
    fileInput: inputElement,
    done: function(err, obj) {}
});
```

---

## 其他 API

#### httpRequestProxy - HTTP代理请求

```javascript
window.nim.httpRequestProxy({
    path: 'api/path',
    method: 'GET',
    done: function(err, response) {}
});
```

#### subscribeEvent - 订阅事件

```javascript
window.nim.subscribeEvent({
    type: 1,
    accounts: ['account1'],
    done: function(err) {}
});
```

---

## 事件回调

NIM SDK 支持以下事件回调：

| 事件 | 说明 |
|------|------|
| `onconnect` | 连接成功 |
| `ondisconnect` | 连接断开 |
| `onerror` | 发生错误 |
| `onmsg` | 收到消息 |
| `onsysmsg` | 收到系统消息 |
| `onupdatesession` | 会话更新 |

---

## 通知消息类型 (attach.type)

| type | 说明 |
|------|------|
| `leaveTeam` | 成员退群 |
| `kickMember` | 踢出成员 |
| `addTeamMembers` | 添加成员 |
| `updateTeam` | 更新群信息 |
| `transferTeam` | 转让群主 |
| `addManager` | 添加管理员 |
| `removeManager` | 移除管理员 |
| `muteTeamMember` | 禁言成员 |
| `muteTeamAll` | 全员禁言 |

---

## 数据存储

### IndexedDB 数据库

| 数据库名 | 说明 |
|----------|------|
| `NIM-{appKey}-{account}` | NIM主数据库 |
| `nim-{account}` | 本地缓存 |
| `nim-logs-{account}` | 日志数据库 |

---

## 错误代码

| 代码 | 说明 |
|------|------|
| 200 | 成功 |
| 302 | 账号或密码错误 |
| 403 | 禁止操作 |
| 404 | 目标不存在 |
| 408 | 超时 |
| 414 | 参数错误 |
| 801 | 群成员数量超限 |
| 802 | 没有权限 |
| 803 | 群不存在 |
| 804 | 用户不在群内 |
| 806 | 已达群数量上限 |
| 807 | 已在群内 |
| 808 | 群已解散 |

---

## 版本信息

- **文档版本**: 2.0
- **更新日期**: 2026-01-08
- **NIM SDK**: 网易云信
