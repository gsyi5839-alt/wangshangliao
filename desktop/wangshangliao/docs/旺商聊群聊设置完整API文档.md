# 旺商聊群聊设置完整API文档

> 📅 更新时间: 2026-01-08  
> 📍 数据来源: 从旺商聊源代码 `zh-cn-acff1ed5.js` 和运行时CDP提取  
> ⚠️ 本文档通过逆向分析获得，仅供学习研究

---

## 一、解密逻辑（源码级分析）

### 1.1 AES加密配置

**源码位置**: `zh-cn-acff1ed5.js` 第128行左右

```javascript
// 从源码中提取的原始代码
const key = CryptoJS.enc.Utf8.parse("d6ba6647b7c43b79d0e42ceb2790e342");
const iv = CryptoJS.enc.Utf8.parse("kgWRyiiODMjSCh0m");

const AES = {
    encrypt: function(g) {
        return CryptoJS.AES.encrypt(g, key, {
            iv: iv,
            mode: CryptoJS.mode.CBC,
            padding: CryptoJS.pad.Pkcs7
        }).toString();
    },
    decrypt: function(g) {
        return CryptoJS.AES.decrypt(g, key, {
            iv: iv,
            mode: CryptoJS.mode.CBC,
            padding: CryptoJS.pad.Pkcs7
        }).toString(CryptoJS.enc.Utf8);
    }
};
```

### 1.2 解密参数详情

| 参数 | 值 | 说明 |
|------|-----|------|
| **算法** | AES-256-CBC | 高级加密标准，256位密钥，CBC模式 |
| **密钥 (Key)** | `d6ba6647b7c43b79d0e42ceb2790e342` | 32字节UTF-8字符串 |
| **初始向量 (IV)** | `kgWRyiiODMjSCh0m` | 16字节UTF-8字符串 |
| **填充模式** | PKCS7 | 标准填充方式 |
| **密文编码** | Base64 | 加密后的数据格式 |
| **明文编码** | UTF-8 | 解密后的字符串编码 |

### 1.3 昵称解密函数（源码）

```javascript
// 用户昵称解密 - 从 user.custom 中解密
const decryptNick = (g) => {
    if (g != null && g.custom) {
        try {
            const k = JSON.parse(g.custom);
            const i = k.nickname_ciphertext ?? k.nicknameCiphertext;
            if (i) {
                g.nick = AES.decrypt(i);
            }
        } catch(e) {}
        return g;
    }
};

// 群昵称解密 - 从 team.serverCustom 中解密
const decryptTeamNick = (g) => {
    if (g != null && g.serverCustom) {
        try {
            const k = JSON.parse(g.serverCustom);
            const i = k.nickname_ciphertext ?? k.nicknameCiphertext;
            if (i) {
                g.name = AES.decrypt(i);
            }
            return k;
        } catch(e) {}
        return g;
    }
};

// 辅助函数 - 判断是否Base64并解密
const isBase64 = (g) => {
    if (g === "" || g.trim() === "") return false;
    try {
        return btoa(atob(g)) == g;
    } catch {
        return false;
    }
};

const AES_decryptNick = (g = "") => isBase64(g) ? AES.decrypt(g) : g;
```

### 1.4 Node.js 解密实现

```javascript
const crypto = require('crypto');

const key = Buffer.from('d6ba6647b7c43b79d0e42ceb2790e342', 'utf8');
const iv = Buffer.from('kgWRyiiODMjSCh0m', 'utf8');

function decrypt(ciphertextBase64) {
    try {
        const decipher = crypto.createDecipheriv('aes-256-cbc', key, iv);
        decipher.setAutoPadding(true);
        let decrypted = decipher.update(Buffer.from(ciphertextBase64, 'base64'));
        decrypted = Buffer.concat([decrypted, decipher.final()]);
        return decrypted.toString('utf8');
    } catch (e) {
        return null;
    }
}
```

### 1.5 C# 解密实现

```csharp
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

public static class WangShangLiaoDecrypt
{
    private static readonly byte[] AesKey = Encoding.UTF8.GetBytes("d6ba6647b7c43b79d0e42ceb2790e342");
    private static readonly byte[] AesIv = Encoding.UTF8.GetBytes("kgWRyiiODMjSCh0m");

    public static string DecryptNickname(string ciphertextBase64)
    {
        if (string.IsNullOrWhiteSpace(ciphertextBase64))
            return null;

        try
        {
            byte[] cipherBytes = Convert.FromBase64String(ciphertextBase64);

            using (var aes = Aes.Create())
            {
                aes.Key = AesKey;
                aes.IV = AesIv;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                using (var decryptor = aes.CreateDecryptor())
                using (var ms = new MemoryStream(cipherBytes))
                using (var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
                using (var sr = new StreamReader(cs, Encoding.UTF8))
                {
                    return sr.ReadToEnd();
                }
            }
        }
        catch
        {
            return null;
        }
    }
}
```

---

## 二、NIM SDK 完整API清单

### 2.1 API统计

| 类别 | 数量 | 说明 |
|------|------|------|
| NIM方法总数 | **351** | 从prototype链完整提取 |
| Options事件回调 | **134** | 所有on*事件处理器 |
| DB(数据库)方法 | **179** | IndexedDB本地操作 |

### 2.2 消息操作API (107个)

#### 发送消息

| 方法名 | 参数数量 | 说明 |
|--------|----------|------|
| `sendText` | 1 | 发送文本消息 |
| `sendFile` | 1 | 发送文件 |
| `sendCustomMsg` | 1 | 发送自定义消息 |
| `sendGeo` | 1 | 发送地理位置 |
| `sendTipMsg` | 1 | 发送提示消息 |
| `sendRobotMsg` | 1 | 发送机器人消息 |
| `sendG2Msg` | 1 | 发送G2消息 |
| `sendCustomSysMsg` | 1 | 发送自定义系统消息 |
| `sendFileWithUI` | 1 | 带UI的发送文件 |
| `forwardMsg` | 1 | 转发消息 |
| `resendMsg` | 1 | 重发消息 |

#### 消息查询

| 方法名 | 参数数量 | 说明 |
|--------|----------|------|
| `getHistoryMsgs` | 1 | 获取历史消息 |
| `getLocalMsgs` | 1 | 获取本地消息 |
| `getLocalMsgByIdClient` | 1 | 按客户端ID获取本地消息 |
| `getLocalMsgsByIdClients` | 1 | 批量获取本地消息 |
| `getLocalMsgsInUnread` | 1 | 获取未读消息 |
| `getMsgsByIdServer` | 1 | 按服务器ID获取消息 |
| `searchHistoryMsgs` | 1 | 搜索历史消息 |
| `getThreadMsgs` | 1 | 获取Thread消息 |
| `msgFtsInServer` | 1 | 服务器全文搜索 |

#### 消息操作

| 方法名 | 参数数量 | 说明 |
|--------|----------|------|
| `recallMsg` | 1 | 撤回消息 |
| `deleteMsg` | 2 | 删除消息 |
| `deleteMsgSelf` | 1 | 删除自己的消息 |
| `deleteMsgSelfBatch` | 1 | 批量删除自己的消息 |
| `deleteLocalMsg` | 1 | 删除本地消息 |
| `deleteLocalMsgs` | 1 | 批量删除本地消息 |
| `deleteLocalMsgsBySession` | 1 | 按会话删除本地消息 |
| `deleteLocalMsgsByTime` | 1 | 按时间删除本地消息 |
| `deleteAllLocalMsgs` | 1 | 删除所有本地消息 |
| `updateLocalMsg` | 1 | 更新本地消息 |
| `modifyMessage` | 1 | 修改消息 |

#### 消息回执

| 方法名 | 参数数量 | 说明 |
|--------|----------|------|
| `markMsgRead` | 1 | 标记消息已读 |
| `sendMsgReceipt` | 1 | 发送消息回执 |
| `sendTeamMsgReceipt` | 1 | 发送群消息回执 |
| `getTeamMsgReads` | 1 | 获取群消息已读状态 |
| `getTeamMsgReadAccounts` | 1 | 获取群消息已读账号 |
| `isMsgRemoteRead` | 1 | 检查消息是否远程已读 |

#### 消息Pin

| 方法名 | 参数数量 | 说明 |
|--------|----------|------|
| `addMsgPin` | 1 | 添加消息Pin |
| `deleteMsgPin` | 1 | 删除消息Pin |
| `updateMsgPin` | 1 | 更新消息Pin |
| `getMsgPins` | 1 | 获取Pin消息列表 |

#### 快捷评论

| 方法名 | 参数数量 | 说明 |
|--------|----------|------|
| `addQuickComment` | 1 | 添加快捷评论 |
| `deleteQuickComment` | 1 | 删除快捷评论 |
| `getQuickComments` | 1 | 获取快捷评论 |

#### 图片处理

| 方法名 | 参数数量 | 说明 |
|--------|----------|------|
| `previewFile` | 2 | 预览文件 |
| `processImage` | 1 | 处理图片 |
| `cropImage` | 1 | 裁剪图片 |
| `rotateImage` | 1 | 旋转图片 |
| `blurImage` | 1 | 模糊图片 |
| `qualityImage` | 1 | 调整图片质量 |
| `thumbnailImage` | 1 | 生成缩略图 |
| `interlaceImage` | 1 | 交错图片 |
| `stripImageMeta` | 1 | 去除图片元信息 |

---

### 2.3 群组操作API (69个)

#### 群信息获取

| 方法名 | 参数数量 | 说明 | 示例 |
|--------|----------|------|------|
| `getTeam` | 1 | 获取单个群信息 | `nim.getTeam({teamId:'xxx', done:callback})` |
| `getTeams` | 1 | 获取所有群列表 | `nim.getTeams({done:callback})` |
| `getTeamsById` | 1 | 按ID获取群列表 | `nim.getTeamsById({teamIds:[], done:callback})` |
| `getLocalTeams` | 1 | 获取本地群列表 | `nim.getLocalTeams({done:callback})` |
| `getTeamsFromDB` | 1 | 从数据库获取群 | - |

#### 群成员操作

| 方法名 | 参数数量 | 说明 | 示例 |
|--------|----------|------|------|
| `getTeamMembers` | 1 | 获取群成员 | `nim.getTeamMembers({teamId:'xxx', done:callback})` |
| `getLocalTeamMembers` | 1 | 获取本地群成员 | - |
| `getMyTeamMembers` | 1 | 获取我的群成员信息 | - |
| `getMutedTeamMembers` | 1 | 获取被禁言成员 | - |
| `addTeamMembers` | 1 | 添加群成员 | `nim.addTeamMembers({teamId, accounts, done})` |
| `removeTeamMembers` | 1 | 移除群成员 | `nim.removeTeamMembers({teamId, accounts, done})` |
| `addTeamManagers` | 1 | 设置管理员 | `nim.addTeamManagers({teamId, accounts, done})` |
| `removeTeamManagers` | 1 | 移除管理员 | `nim.removeTeamManagers({teamId, accounts, done})` |

#### 群设置

| 方法名 | 参数数量 | 说明 | 示例 |
|--------|----------|------|------|
| `createTeam` | 1 | 创建群 | `nim.createTeam({name, accounts, done})` |
| `updateTeam` | 1 | 更新群信息 | `nim.updateTeam({teamId, name, intro, done})` |
| `dismissTeam` | 1 | 解散群 | `nim.dismissTeam({teamId, done})` |
| `leaveTeam` | 1 | 退出群 | `nim.leaveTeam({teamId, done})` |
| `transferTeam` | 1 | 转让群主 | `nim.transferTeam({teamId, account, leave, done})` |

#### 群禁言

| 方法名 | 参数数量 | 说明 | 示例 |
|--------|----------|------|------|
| `muteTeamAll` | 1 | 全员禁言/解禁 | `nim.muteTeamAll({teamId, mute:true/false, done})` |
| `updateMuteStateInTeam` | 1 | 更新禁言状态 | - |

#### 群申请

| 方法名 | 参数数量 | 说明 | 示例 |
|--------|----------|------|------|
| `applyTeam` | 1 | 申请加群 | `nim.applyTeam({teamId, ps, done})` |
| `passTeamApply` | 1 | 通过加群申请 | `nim.passTeamApply({idServer, from, done})` |
| `rejectTeamApply` | 1 | 拒绝加群申请 | `nim.rejectTeamApply({idServer, from, ps, done})` |

#### 群邀请

| 方法名 | 参数数量 | 说明 |
|--------|----------|------|
| `acceptTeamInvite` | 1 | 接受群邀请 |
| `rejectTeamInvite` | 1 | 拒绝群邀请 |

#### 群内信息更新

| 方法名 | 参数数量 | 说明 | 示例 |
|--------|----------|------|------|
| `updateNickInTeam` | 1 | 更新群昵称 | `nim.updateNickInTeam({teamId, nick, done})` |
| `updateInfoInTeam` | 1 | 更新群内信息 | `nim.updateInfoInTeam({teamId, custom, done})` |

---

### 2.4 超大群操作API (25个)

| 方法名 | 参数数量 | 说明 |
|--------|----------|------|
| `getSuperTeam` | 1 | 获取超大群信息 |
| `getSuperTeams` | 1 | 获取超大群列表 |
| `getAllSuperTeamMembers` | 1 | 获取所有超大群成员 |
| `getSuperTeamMembersByAccounts` | 1 | 按账号获取成员 |
| `getSuperTeamMembersByJoinTime` | 1 | 按加入时间获取成员 |
| `getMutedSuperTeamMembers` | 1 | 获取超大群禁言成员 |
| `getMySuperTeamMembers` | 1 | 获取我在超大群的信息 |
| `addSuperTeamMembers` | 1 | 添加超大群成员 |
| `removeSuperTeamMembers` | 1 | 移除超大群成员 |
| `addSuperTeamManagers` | 1 | 添加超大群管理员 |
| `removeSuperTeamManagers` | 1 | 移除超大群管理员 |
| `applySuperTeam` | 1 | 申请加入超大群 |
| `passSuperTeamApply` | 1 | 通过超大群申请 |
| `rejectSuperTeamApply` | 1 | 拒绝超大群申请 |
| `acceptSuperTeamInvite` | 1 | 接受超大群邀请 |
| `rejectSuperTeamInvite` | 1 | 拒绝超大群邀请 |
| `updateSuperTeam` | 1 | 更新超大群信息 |
| `transferSuperTeam` | 1 | 转让超大群 |
| `leaveSuperTeam` | 1 | 退出超大群 |
| `updateNickInSuperTeam` | 1 | 更新超大群昵称 |
| `updateInfoInSuperTeam` | 1 | 更新超大群内信息 |
| `updateSuperTeamMembersMute` | 1 | 更新超大群成员禁言 |
| `updateSuperTeamMute` | 1 | 更新超大群禁言 |
| `resetSuperTeamSessionsUnread` | 1 | 重置超大群未读 |

---

### 2.5 用户操作API (9个)

| 方法名 | 参数数量 | 说明 | 示例 |
|--------|----------|------|------|
| `getMyInfo` | 1 | 获取自己信息 | `nim.getMyInfo({done:callback})` |
| `updateMyInfo` | 1 | 更新自己信息 | `nim.updateMyInfo({nick, avatar, done})` |
| `getUser` | 1 | 获取用户信息 | `nim.getUser({account, done})` |
| `getUsers` | 1 | 批量获取用户 | `nim.getUsers({accounts:[], done})` |
| `getUsersFromDB` | 1 | 从数据库获取用户 | - |
| `getAIUserList` | 1 | 获取AI用户列表 | - |
| `findUser` | 2 | 查找用户 | - |
| `mergeUsers` | 2 | 合并用户数据 | - |
| `isUserInBlackList` | 1 | 检查用户是否在黑名单 | - |

---

### 2.6 好友操作API (14个)

| 方法名 | 参数数量 | 说明 | 示例 |
|--------|----------|------|------|
| `addFriend` | 1 | 添加好友 | `nim.addFriend({account, ps, done})` |
| `deleteFriend` | 1 | 删除好友 | `nim.deleteFriend({account, done})` |
| `getFriends` | 1 | 获取好友列表 | `nim.getFriends({done:callback})` |
| `getFriendsFromDB` | 1 | 从数据库获取好友 | - |
| `updateFriend` | 1 | 更新好友信息 | `nim.updateFriend({account, alias, done})` |
| `applyFriend` | 1 | 申请好友 | - |
| `friendRequest` | 1 | 好友请求 | - |
| `passFriendApply` | 1 | 通过好友申请 | `nim.passFriendApply({idServer, from, done})` |
| `rejectFriendApply` | 1 | 拒绝好友申请 | `nim.rejectFriendApply({idServer, from, ps, done})` |
| `isMyFriend` | 1 | 检查是否是好友 | - |
| `findFriend` | 2 | 查找好友 | - |
| `cutFriends` | 2 | 切割好友 | - |
| `cutFriendsByAccounts` | 2 | 按账号切割好友 | - |
| `mergeFriends` | 2 | 合并好友数据 | - |

---

### 2.7 黑名单API (6个)

| 方法名 | 参数数量 | 说明 | 示例 |
|--------|----------|------|------|
| `addToBlacklist` | 1 | 添加到黑名单 | `nim.addToBlacklist({account, done})` |
| `removeFromBlacklist` | 1 | 从黑名单移除 | `nim.removeFromBlacklist({account, done})` |
| `markInBlacklist` | 1 | 标记黑名单状态 | - |
| `addToMutelist` | 1 | 添加到静音列表 | `nim.addToMutelist({account, done})` |
| `removeFromMutelist` | 1 | 从静音列表移除 | `nim.removeFromMutelist({account, done})` |
| `markInMutelist` | 1 | 标记静音状态 | - |

---

### 2.8 会话操作API (27个)

| 方法名 | 参数数量 | 说明 | 示例 |
|--------|----------|------|------|
| `setCurrSession` | 2 | 设置当前会话 | `nim.setCurrSession('team-xxx')` |
| `resetCurrSession` | 0 | 重置当前会话 | `nim.resetCurrSession()` |
| `getLocalSession` | 1 | 获取本地会话 | - |
| `getLocalSessions` | 1 | 获取本地会话列表 | - |
| `getServerSession` | 1 | 获取服务器会话 | - |
| `getServerSessions` | 1 | 获取服务器会话列表 | - |
| `deleteSession` | 2 | 删除会话 | `nim.deleteSession({scene, to, done})` |
| `deleteSessions` | 1 | 批量删除会话 | - |
| `deleteLocalSession` | 1 | 删除本地会话 | - |
| `deleteServerSessions` | 1 | 删除服务器会话 | - |
| `insertLocalSession` | 1 | 插入本地会话 | - |
| `updateLocalSession` | 1 | 更新本地会话 | - |
| `updateServerSession` | 1 | 更新服务器会话 | - |
| `resetSessionUnread` | 1 | 重置会话未读数 | `nim.resetSessionUnread({scene, to})` |
| `resetSessionsUnread` | 1 | 批量重置未读 | - |
| `resetAllSessionUnread` | 0 | 重置所有未读 | - |
| `addStickTopSession` | 1 | 置顶会话 | - |
| `deleteStickTopSession` | 1 | 取消置顶 | - |
| `updateStickTopSession` | 1 | 更新置顶 | - |
| `getStickTopSessions` | 1 | 获取置顶列表 | - |
| `getSessionsWithMoreRoaming` | 1 | 获取更多漫游会话 | - |
| `updateSessionsWithMoreRoaming` | 1 | 更新漫游会话 | - |
| `deleteSessionsWithMoreRoaming` | 1 | 删除漫游会话 | - |

---

### 2.9 系统消息API

| 方法名 | 参数数量 | 说明 |
|--------|----------|------|
| `getLocalSysMsgs` | 1 | 获取本地系统消息 |
| `deleteLocalSysMsg` | 1 | 删除本地系统消息 |
| `deleteAllLocalSysMsgs` | 1 | 删除所有本地系统消息 |
| `markSysMsgRead` | 1 | 标记系统消息已读 |
| `updateLocalSysMsg` | 1 | 更新本地系统消息 |
| `findSysMsg` | 2 | 查找系统消息 |
| `cutSysMsgs` | 2 | 切割系统消息 |
| `cutSysMsgsByIdServers` | 2 | 按服务器ID切割 |
| `mergeSysMsgs` | 2 | 合并系统消息 |
| `formatReturnSysMsg` | 1 | 格式化系统消息 |

---

### 2.10 数据库操作API (8个)

| 方法名 | 参数数量 | 说明 |
|--------|----------|------|
| `clearDB` | 1 | 清空数据库 |
| `closeDB` | 1 | 关闭数据库 |
| `reinitDB` | 0 | 重新初始化数据库 |
| `removeDB` | 1 | 移除数据库 |
| `searchLocal` | 1 | 本地搜索 |
| `getDBStatus` | 0 | 获取数据库状态 |
| `getDBLastOpenError` | 0 | 获取最后错误 |

---

### 2.11 文件操作API (10个)

| 方法名 | 参数数量 | 说明 |
|--------|----------|------|
| `previewFile` | 2 | 预览文件 |
| `getFile` | 1 | 获取文件 |
| `getFileList` | 1 | 获取文件列表 |
| `fetchFile` | 1 | 下载文件 |
| `fetchFileList` | 1 | 批量下载文件 |
| `deleteFile` | 1 | 删除文件 |
| `removeFile` | 1 | 移除文件 |
| `getNosToken` | 1 | 获取NOS Token |
| `getNosOriginUrl` | 1 | 获取NOS原始URL |
| `uploadSdkLogUrl` | 1 | 上传SDK日志 |

---

## 三、事件回调 (Options)

### 3.1 连接事件

| 事件名 | 说明 |
|--------|------|
| `onconnect` | 连接成功 |
| `onwillreconnect` | 即将重连 |
| `ondisconnect` | 断开连接 |
| `onerror` | 连接错误 |
| `onsyncdone` | 同步完成 |

### 3.2 消息事件

| 事件名 | 说明 |
|--------|------|
| `onmsg` | 收到单条消息 |
| `onmsgs` | 收到多条消息 |
| `onofflinemsgs` | 收到离线消息 |
| `onroamingmsgs` | 收到漫游消息 |
| `onofflinefiltermsgs` | 收到过滤离线消息 |
| `onProxyMsg` | 代理消息 |

### 3.3 系统消息事件

| 事件名 | 说明 |
|--------|------|
| `onsysmsg` | 收到系统消息 |
| `onofflinecustomsysmsgs` | 收到离线自定义系统消息 |
| `oncustomsysmsg` | 收到自定义系统消息 |
| `onbroadcastmsg` | 广播消息 |
| `onbroadcastmsgs` | 批量广播消息 |
| `onsysmsgunread` | 系统消息未读数 |
| `onupdatesysmsg` | 更新系统消息 |
| `onupdatesysmsgunread` | 更新系统消息未读 |

### 3.4 会话事件

| 事件名 | 说明 |
|--------|------|
| `onsessions` | 会话列表 |
| `onupdatesession` | 更新会话 |
| `onupdatesessions` | 批量更新会话 |
| `onStickTopSessions` | 置顶会话 |
| `onSessionsWithMoreRoaming` | 漫游会话 |
| `onSyncUpdateServerSession` | 同步服务器会话 |

### 3.5 群组事件

| 事件名 | 说明 |
|--------|------|
| `onteams` | 群列表 |
| `onteammembers` | 群成员列表 |
| `onUpdateTeam` | 更新群信息 |
| `onupdateteammember` | 更新群成员 |
| `onMyTeamMembers` | 我的群成员信息 |
| `onCreateTeam` | 创建群 |
| `onDismissTeam` | 解散群 |
| `onTransferTeam` | 转让群 |
| `onAddTeamMembers` | 添加群成员 |
| `onRemoveTeamMembers` | 移除群成员 |
| `onUpdateTeamManagers` | 更新群管理员 |
| `onUpdateTeamMembersMute` | 更新群成员禁言 |
| `onTeamMsgReceipt` | 群消息回执 |
| `onsyncteammembersdone` | 同步群成员完成 |
| `onsynccreateteam` | 同步创建群 |

### 3.6 超大群事件

| 事件名 | 说明 |
|--------|------|
| `onSuperTeams` | 超大群列表 |
| `onCreateSuperTeam` | 创建超大群 |
| `onUpdateSuperTeam` | 更新超大群 |
| `onDismissSuperTeam` | 解散超大群 |
| `onTransferSuperTeam` | 转让超大群 |
| `onAddSuperTeamMembers` | 添加超大群成员 |
| `onRemoveSuperTeamMembers` | 移除超大群成员 |
| `onUpdateSuperTeamManagers` | 更新超大群管理员 |
| `onUpdateSuperTeamMember` | 更新超大群成员信息 |
| `onMySuperTeamMembers` | 我的超大群成员信息 |
| `onUpdateSuperTeamMembersMute` | 更新超大群成员禁言 |
| `onsyncsuperteammembersdone` | 同步超大群成员完成 |
| `onsyncsupercreateteam` | 同步创建超大群 |

### 3.7 好友/用户事件

| 事件名 | 说明 |
|--------|------|
| `onmyinfo` | 自己的信息 |
| `onupdatemyinfo` | 更新自己的信息 |
| `onfriends` | 好友列表 |
| `onsyncfriendaction` | 同步好友操作 |
| `onusers` | 用户列表 |
| `onupdateuser` | 更新用户信息 |
| `onblacklist` | 黑名单列表 |
| `onmutelist` | 静音列表 |
| `onsyncmarkinblacklist` | 同步黑名单标记 |
| `onsyncmarkinmutelist` | 同步静音标记 |

### 3.8 其他事件

| 事件名 | 说明 |
|--------|------|
| `onloginportschange` | 多端登录变化 |
| `onMsgReceipts` | 消息回执 |
| `onQuickComment` | 快捷评论 |
| `onDeleteQuickComment` | 删除快捷评论 |
| `onPinMsgChange` | Pin消息变化 |
| `onDeleteMsgSelf` | 删除自己消息 |
| `onpushevents` | 推送事件 |
| `onrobots` | 机器人列表 |

---

## 四、Pinia Store API

### 4.1 App Store 方法

| 方法名 | 说明 |
|--------|------|
| `setCurrentSession` | 设置当前会话 |
| `setUserInfo` | 设置用户信息 |
| `setAppSetting` | 设置应用配置 |
| `setFriendList` | 设置好友列表 |
| `setGroupList` | 设置群列表 |
| `getAppSetting` | 获取应用设置 |
| `getFriendList` | 获取好友列表 |
| `getGroupList` | 获取群列表 |
| `getSensitiveWords` | 获取敏感词 |
| `getReplyState` | 获取自动回复状态 |
| `updateReplyState` | 更新自动回复状态 |
| `findUser` | 查找用户 |
| `findGroup` | 查找群 |
| `updateNimUser` | 更新NIM用户 |
| `updateUserInfo` | 更新用户信息 |
| `updateTeammember` | 更新群成员 |
| `resetAll` | 重置所有状态 |

### 4.2 SDK Store 方法

| 方法名 | 说明 |
|--------|------|
| 存储群成员Map | `groupMembersMap` |
| 存储群信息Map | `groupInfoMap` |
| 存储群公告Map | `groupNoticeMap` |
| 存储置顶公告Map | `topNoticeMap` |

### 4.3 Cache Store 方法

| 方法名 | 说明 |
|--------|------|
| `getGroupMembers` | 获取群成员缓存 |
| `getGroupMemberInfo` | 获取群成员信息 |
| `getGroupInfo` | 获取群信息 |
| `getNoticeList` | 获取公告列表 |
| `getTopNotice` | 获取置顶公告 |
| `getUser` | 获取用户缓存 |
| `findCachedImage` | 查找缓存图片 |

---

## 五、使用示例

### 5.1 发送文本消息

```javascript
window.nim.sendText({
    scene: 'team',
    to: '40821608989',  // 群ID
    text: '测试消息',
    done: function(err, msg) {
        if (err) {
            console.error('发送失败:', err);
        } else {
            console.log('发送成功:', msg);
        }
    }
});
```

### 5.2 获取群成员并解密昵称

```javascript
window.nim.getTeamMembers({
    teamId: '40821608989',
    done: function(err, result) {
        if (!err && result && result.members) {
            result.members.forEach(member => {
                // 解密昵称
                if (member.custom) {
                    try {
                        const customData = JSON.parse(member.custom);
                        const ciphertext = customData.nickname_ciphertext || customData.nicknameCiphertext;
                        if (ciphertext) {
                            // 使用AES解密
                            member.decryptedNick = AES.decrypt(ciphertext);
                        }
                    } catch(e) {}
                }
                console.log(`账号: ${member.account}, 昵称: ${member.decryptedNick || member.nick}`);
            });
        }
    }
});
```

### 5.3 全员禁言

```javascript
window.nim.muteTeamAll({
    teamId: '40821608989',
    mute: true,  // true=开启禁言, false=解除禁言
    done: function(err) {
        if (err) {
            console.error('禁言失败:', err);
        } else {
            console.log('全员禁言已开启');
        }
    }
});
```

### 5.4 通过加群申请

```javascript
window.nim.passTeamApply({
    idServer: '系统消息ID',
    from: '申请人账号',
    done: function(err) {
        if (err) {
            console.error('通过申请失败:', err);
        } else {
            console.log('已通过加群申请');
        }
    }
});
```

### 5.5 Hook消息接收

```javascript
// 保存原始处理函数
const originalOnmsg = window.nim.options.onmsg;

// 注入自定义处理
window.nim.options.onmsg = function(msg) {
    console.log('收到新消息:', msg);
    
    // 解密发送者昵称
    if (msg.from && msg.fromCustom) {
        try {
            const customData = JSON.parse(msg.fromCustom);
            if (customData.nickname_ciphertext) {
                msg.decryptedFromNick = AES.decrypt(customData.nickname_ciphertext);
            }
        } catch(e) {}
    }
    
    // 调用原始处理
    if (originalOnmsg) {
        originalOnmsg(msg);
    }
};
```

### 5.6 使用Pinia Store切换会话

```javascript
// 获取appStore
const appStore = pinia._s.get('app');

// 切换到指定群聊
appStore.setCurrentSession({
    to: '40821608989',
    scene: 'team'
});
```

---

## 六、数据结构

### 6.1 消息对象 (Message)

```typescript
interface Message {
    idClient: string;      // 客户端消息ID
    idServer: string;      // 服务器消息ID
    scene: 'p2p' | 'team'; // 场景: p2p私聊, team群聊
    from: string;          // 发送者账号
    to: string;            // 接收者/群ID
    time: number;          // 时间戳
    type: string;          // 消息类型: text/image/file/audio/video/geo/custom/tip
    text?: string;         // 文本内容
    file?: object;         // 文件信息
    content?: string;      // 自定义消息内容
    custom?: string;       // 扩展字段(JSON字符串)
    fromCustom?: string;   // 发送者扩展字段(包含nickname_ciphertext)
    status?: string;       // 消息状态
    flow?: string;         // in/out
}
```

### 6.2 群组对象 (Team)

```typescript
interface Team {
    teamId: string;        // 群ID
    name: string;          // 群名称(可能需要解密)
    avatar?: string;       // 群头像
    intro?: string;        // 群简介
    announcement?: string; // 群公告
    owner: string;         // 群主账号
    memberNum: number;     // 成员数量
    level: number;         // 群等级
    mute: boolean;         // 是否禁言
    muteType?: string;     // 禁言类型
    joinMode: string;      // 加群方式
    beInviteMode: string;  // 被邀请模式
    inviteMode: string;    // 邀请模式
    updateTeamMode: string; // 更新权限
    updateCustomMode: string; // 更新自定义权限
    serverCustom?: string; // 服务端扩展(包含nickname_ciphertext)
    custom?: string;       // 客户端扩展
    createTime: number;    // 创建时间
    updateTime: number;    // 更新时间
    validToCurrentUser: boolean; // 当前用户是否有效
}
```

### 6.3 群成员对象 (TeamMember)

```typescript
interface TeamMember {
    teamId: string;        // 群ID
    account: string;       // 成员账号
    nick: string;          // 昵称(MD5加密)
    nickInTeam?: string;   // 群内昵称
    avatar?: string;       // 头像
    type: string;          // 成员类型: normal/owner/manager
    joinTime: number;      // 加入时间
    updateTime: number;    // 更新时间
    mute: boolean;         // 是否被禁言
    custom?: string;       // 扩展字段(JSON字符串,包含nickname_ciphertext)
}
```

### 6.4 系统消息对象 (SysMsg)

```typescript
interface SysMsg {
    idServer: string;      // 服务器消息ID
    type: string;          // 类型: teamInvite/applyTeam/passTeamApply...
    from: string;          // 发送者账号
    to: string;            // 接收者账号
    time: number;          // 时间戳
    scene?: string;        // 场景
    state?: string;        // 状态: init/passed/rejected
    attach?: object;       // 附加信息
    ps?: string;           // 附言
    teamId?: string;       // 群ID(群相关系统消息)
}
```

---

## 七、注意事项

1. **昵称解密**: API返回的`nick`字段是MD5哈希值，真实昵称需要从`custom.nickname_ciphertext`解密
2. **群名解密**: 群名可能需要从`serverCustom.nickname_ciphertext`解密
3. **异步操作**: 所有NIM方法都是异步的，结果通过`done`回调返回
4. **错误处理**: 务必检查`done`回调的`err`参数
5. **消息Hook**: Hook消息时要保留原始处理函数并在最后调用
6. **CDP调试**: 需要启用`--remote-debugging-port=9222`参数

---

*文档版本: 1.0.0*  
*最后更新: 2026-01-08*

