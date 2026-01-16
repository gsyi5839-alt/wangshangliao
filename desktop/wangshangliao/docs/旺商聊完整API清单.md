# 旺商聊 NIM SDK 完整API清单

**逆向时间**: 2026-01-09  
**API总数**: 336个方法  
**当前群ID**: 40821608989 (186成员)

---

## API统计

| 分类 | 数量 | 说明 |
|------|------|------|
| 群组API (Team) | 47 | 群管理核心 |
| 超级群API (SuperTeam) | 26 | 超大群支持 |
| 消息API | 72 | 消息收发核心 |
| 用户API | 12 | 用户信息 |
| 好友/黑名单API | 19 | 关系管理 |
| 会话API | 27 | 会话管理 |
| 文件API | 19 | 文件上传下载 |
| 其他API | 114 | 辅助功能 |

---

## 一、群组API (47个) ⭐核心

### 群信息获取
| API | 功能 | 参数 | 状态 |
|-----|------|------|------|
| `getTeam` | 获取单个群信息 | `{teamId, done}` | ✅ 已验证 |
| `getTeams` | 获取群列表 | `{done}` | ✅ 已验证 |
| `getLocalTeams` | 获取本地缓存群 | `{teamIds, done}` | ✅ 需要teamIds |
| `getTeamsById` | 按ID获取群 | `{teamIds, done}` | ✅ 存在 |
| `getTeamsFromDB` | 从数据库获取 | `{done}` | ✅ 存在 |
| `findTeam` | 查找群 | `{teamId}` | ✅ 存在 |

### 群成员获取
| API | 功能 | 参数 | 状态 |
|-----|------|------|------|
| `getTeamMembers` | 获取群成员列表 | `{teamId, done}` | ✅ 已验证 |
| `getLocalTeamMembers` | 获取本地成员 | `{teamId, accounts, done}` | ✅ 需要accounts |
| `getMutedTeamMembers` | 获取禁言成员 | `{teamId, done}` | ✅ 已验证 |
| `getMyTeamMembers` | 获取我在群中的信息 | `{teamId, done}` | ✅ 存在 |
| `getTeamMemberByTeamIdAndAccount` | 按账号获取成员 | `{teamId, account}` | ✅ 存在 |
| `getTeamMemberInvitorAccid` | 获取邀请人 | `{teamId, account}` | ✅ 存在 |
| `getTeamMembersFromDB` | 从数据库获取成员 | `{teamId, done}` | ✅ 存在 |
| `findTeamMember` | 查找成员 | `{teamId, account}` | ✅ 存在 |

### 群管理操作
| API | 功能 | 参数 | 状态 |
|-----|------|------|------|
| `createTeam` | 创建群 | `{name, type, ...}` | ✅ 存在 |
| `updateTeam` | 更新群信息 | `{teamId, name, intro, announcement, ...}` | ✅ 已验证 |
| `dismissTeam` | 解散群 | `{teamId, done}` | ✅ 存在 |
| `leaveTeam` | 退出群 | `{teamId, done}` | ✅ 存在 |
| `transferTeam` | 转让群主 | `{teamId, account, leave, done}` | ✅ 存在 |

### 成员管理
| API | 功能 | 参数 | 状态 |
|-----|------|------|------|
| `addTeamMembers` | 添加群成员 | `{teamId, accounts, done}` | ✅ 已验证 |
| `removeTeamMembers` | 移除群成员 | `{teamId, accounts, done}` | ✅ 已验证 |
| `addTeamManagers` | 添加管理员 | `{teamId, accounts, done}` | ✅ 存在 |
| `removeTeamManagers` | 移除管理员 | `{teamId, accounts, done}` | ✅ 存在 |
| `updateNickInTeam` | 修改群昵称 | `{teamId, account, nickInTeam, done}` | ✅ 已验证 |
| `updateInfoInTeam` | 更新群内信息 | `{teamId, muteTeam, muteNotiType, ...}` | ✅ 已验证 |
| `addTeamMembersFollow` | 成员关注 | `{...}` | ✅ 存在 |
| `removeTeamMembersFollow` | 取消关注 | `{...}` | ✅ 存在 |

### 禁言管理 ⭐重要
| API | 功能 | 参数 | 状态 |
|-----|------|------|------|
| `muteTeamAll` | 全员禁言/解禁 | `{teamId, mute, done}` | ✅ 已验证 |
| `updateMuteStateInTeam` | 单成员禁言 | `{teamId, account, mute, done}` | ✅ 已验证 |

### 入群审核
| API | 功能 | 参数 | 状态 |
|-----|------|------|------|
| `applyTeam` | 申请入群 | `{teamId, msg, done}` | ✅ 存在 |
| `passTeamApply` | 通过入群申请 | `{teamId, from, idServer, done}` | ✅ 已验证 |
| `rejectTeamApply` | 拒绝入群申请 | `{teamId, from, idServer, ps, done}` | ✅ 存在 |
| `acceptTeamInvite` | 接受邀请 | `{teamId, from, done}` | ✅ 存在 |
| `rejectTeamInvite` | 拒绝邀请 | `{teamId, from, done}` | ✅ 存在 |

### 消息已读回执
| API | 功能 | 参数 | 状态 |
|-----|------|------|------|
| `getTeamMsgReads` | 获取消息已读情况 | `{teamMsgReceipts, done}` | ✅ 存在 |
| `getTeamMsgReadAccounts` | 获取已读账号列表 | `{teamMsgReceipt, done}` | ✅ 存在 |
| `sendTeamMsgReceipt` | 发送群消息回执 | `{teamMsgReceipts, done}` | ✅ 存在 |
| `notifyForNewTeamMsg` | 新消息通知设置 | `{teamId, type, done}` | ✅ 存在 |

### 内部方法
| API | 功能 | 状态 |
|-----|------|------|
| `assembleTeamMembers` | 组装成员数据 | 内部 |
| `assembleTeamOwner` | 组装群主数据 | 内部 |
| `cutTeamMembers` | 裁剪成员数据 | 内部 |
| `cutTeamMembersByAccounts` | 按账号裁剪 | 内部 |
| `cutTeams` | 裁剪群数据 | 内部 |
| `deleteLocalTeam` | 删除本地群 | 内部 |
| `genTeamMemberId` | 生成成员ID | 内部 |
| `mergeTeamMembers` | 合并成员 | 内部 |
| `mergeTeams` | 合并群数据 | 内部 |

---

## 二、消息API (72个) ⭐核心

### 发送消息
| API | 功能 | 参数 | 状态 |
|-----|------|------|------|
| `sendText` | 发送文本 | `{scene, to, text, done}` | ✅ 核心 |
| `sendFile` | 发送文件 | `{scene, to, type, blob/file, done}` | ✅ 核心 |
| `sendCustomMsg` | 发送自定义消息 | `{scene, to, content, done}` | ✅ 存在 |
| `sendTipMsg` | 发送提示消息 | `{scene, to, tip, done}` | ✅ 存在 |
| `sendGeo` | 发送地理位置 | `{scene, to, geo, done}` | ✅ 存在 |
| `sendRobotMsg` | 发送机器人消息 | `{...}` | ✅ 存在 |
| `sendFileWithUI` | UI方式发送文件 | `{...}` | ✅ 存在 |
| `sendMsg` | 通用发送消息 | `{...}` | ✅ 存在 |
| `sendCustomSysMsg` | 发送自定义系统消息 | `{...}` | ✅ 存在 |
| `sendG2Msg` | G2消息 | `{...}` | ✅ 存在 |
| `sendCmd` | 发送命令 | `{...}` | 内部 |

### 消息操作
| API | 功能 | 参数 | 状态 |
|-----|------|------|------|
| `recallMsg` | 撤回消息 | `{msg, done}` | ✅ 已验证 |
| `forwardMsg` | 转发消息 | `{msg, scene, to, done}` | ✅ 存在 |
| `resendMsg` | 重发消息 | `{msg, done}` | ✅ 存在 |
| `deleteMsg` | 删除消息 | `{msg, done}` | ✅ 存在 |
| `deleteLocalMsg` | 删除本地消息 | `{msg}` | ✅ 存在 |
| `deleteLocalMsgs` | 批量删除本地 | `{msgs}` | ✅ 存在 |
| `deleteMsgSelf` | 单向删除消息 | `{msg, done}` | ✅ 存在 |
| `deleteMsgSelfBatch` | 批量单向删除 | `{msgs, done}` | ✅ 存在 |
| `deleteLocalMsgsBySession` | 按会话删除 | `{scene, to}` | ✅ 存在 |
| `deleteLocalMsgsByTime` | 按时间删除 | `{time}` | ✅ 存在 |
| `deleteAllLocalMsgs` | 删除所有本地消息 | `{}` | ✅ 存在 |
| `updateLocalMsg` | 更新本地消息 | `{idClient, ...}` | ✅ 存在 |
| `modifyMessage` | 修改消息 | `{msg, done}` | ✅ 存在 |

### 消息获取
| API | 功能 | 参数 | 状态 |
|-----|------|------|------|
| `getHistoryMsgs` | 获取历史消息 | `{scene, to, limit, done}` | ✅ 核心 |
| `getLocalMsgs` | 获取本地消息 | `{scene, to, limit, done}` | ✅ 核心 |
| `getLocalMsgByIdClient` | 按idClient获取 | `{idClient}` | ✅ 存在 |
| `getLocalMsgsByIdClients` | 批量获取 | `{idClients}` | ✅ 存在 |
| `getMsgsByIdServer` | 按服务端ID获取 | `{idServer}` | ✅ 存在 |
| `getThreadMsgs` | 获取thread消息 | `{...}` | ✅ 存在 |
| `searchHistoryMsgs` | 搜索历史消息 | `{keyword, ...}` | ✅ 存在 |
| `getLocalMsgsInUnread` | 获取未读消息 | `{...}` | ✅ 存在 |
| `clearServerHistoryMsgs` | 清除服务端历史 | `{...}` | ✅ 存在 |

### 已读回执
| API | 功能 | 参数 | 状态 |
|-----|------|------|------|
| `sendMsgReceipt` | 发送已读回执 | `{msg, done}` | ✅ 存在 |
| `markMsgRead` | 标记已读 | `{msg}` | ✅ 存在 |
| `isMsgRemoteRead` | 是否已读 | `{msg}` | ✅ 存在 |

### Pin消息
| API | 功能 | 参数 | 状态 |
|-----|------|------|------|
| `getMsgPins` | 获取Pin消息 | `{done}` | ✅ 存在 |
| `addMsgPin` | 添加Pin | `{msg, done}` | ✅ 存在 |
| `deleteMsgPin` | 删除Pin | `{msg, done}` | ✅ 存在 |
| `updateMsgPin` | 更新Pin | `{msg, done}` | ✅ 存在 |

### 快捷评论
| API | 功能 | 状态 |
|-----|------|------|
| `addQuickComment` | 添加快捷评论 | ✅ 存在 |
| `deleteQuickComment` | 删除快捷评论 | ✅ 存在 |
| `getQuickComments` | 获取快捷评论 | ✅ 存在 |

---

## 三、超级群API (26个)

| API | 功能 | 状态 |
|-----|------|------|
| `getSuperTeam` | 获取超级群信息 | ✅ |
| `getSuperTeams` | 获取超级群列表 | ✅ |
| `getAllSuperTeamMembers` | 获取所有成员 | ✅ |
| `getSuperTeamMembersByAccounts` | 按账号获取成员 | ✅ |
| `getSuperTeamMembersByJoinTime` | 按时间获取成员 | ✅ |
| `getMySuperTeamMembers` | 获取我的信息 | ✅ |
| `getMutedSuperTeamMembers` | 获取禁言成员 | ✅ |
| `addSuperTeamMembers` | 添加成员 | ✅ |
| `removeSuperTeamMembers` | 移除成员 | ✅ |
| `addSuperTeamManagers` | 添加管理员 | ✅ |
| `removeSuperTeamManagers` | 移除管理员 | ✅ |
| `updateSuperTeam` | 更新超级群 | ✅ |
| `updateNickInSuperTeam` | 修改昵称 | ✅ |
| `updateInfoInSuperTeam` | 更新群内信息 | ✅ |
| `updateSuperTeamMembersMute` | 成员禁言 | ✅ |
| `updateSuperTeamMute` | 全员禁言 | ✅ |
| `transferSuperTeam` | 转让群主 | ✅ |
| `applySuperTeam` | 申请入群 | ✅ |
| `passSuperTeamApply` | 通过申请 | ✅ |
| `rejectSuperTeamApply` | 拒绝申请 | ✅ |
| `acceptSuperTeamInvite` | 接受邀请 | ✅ |
| `rejectSuperTeamInvite` | 拒绝邀请 | ✅ |
| `leaveSuperTeam` | 退出超级群 | ✅ |
| `resetSuperTeamSessionsUnread` | 重置未读 | ✅ |
| `addSuperTeamMembersFollow` | 成员关注 | ✅ |
| `removeSuperTeamMembersFollow` | 取消关注 | ✅ |

---

## 四、用户API (12个)

| API | 功能 | 参数 | 状态 |
|-----|------|------|------|
| `getMyInfo` | 获取我的信息 | `{done}` | ✅ 核心 |
| `updateMyInfo` | 更新我的信息 | `{nick, avatar, ...}` | ✅ 存在 |
| `getUser` | 获取用户信息 | `{account, done}` | ✅ 核心 |
| `getUsers` | 批量获取用户 | `{accounts, done}` | ✅ 核心 |
| `getUsersFromDB` | 从数据库获取 | `{accounts}` | ✅ 存在 |
| `findUser` | 查找用户 | `{account}` | ✅ 存在 |
| `mergeUsers` | 合并用户数据 | `{...}` | 内部 |
| `isUserInBlackList` | 是否在黑名单 | `{account}` | ✅ 存在 |
| `getAIUserList` | 获取AI用户列表 | `{...}` | ✅ 存在 |
| `querySubscribeEventsByAccounts` | 查询订阅事件 | `{...}` | ✅ 存在 |
| `unSubscribeEventsByAccounts` | 取消订阅 | `{...}` | ✅ 存在 |
| `cutFriendsByAccounts` | 裁剪好友数据 | `{...}` | 内部 |

---

## 五、好友/黑名单API (19个)

### 好友管理
| API | 功能 | 参数 | 状态 |
|-----|------|------|------|
| `getFriends` | 获取好友列表 | `{done}` | ✅ 存在 |
| `getFriendsFromDB` | 从数据库获取 | `{done}` | ✅ 存在 |
| `addFriend` | 添加好友 | `{account, ps, done}` | ✅ 存在 |
| `deleteFriend` | 删除好友 | `{account, done}` | ✅ 存在 |
| `updateFriend` | 更新好友 | `{account, alias, done}` | ✅ 存在 |
| `applyFriend` | 好友申请 | `{account, ps, done}` | ✅ 存在 |
| `passFriendApply` | 通过好友申请 | `{account, done}` | ✅ 存在 |
| `rejectFriendApply` | 拒绝好友申请 | `{account, ps, done}` | ✅ 存在 |
| `friendRequest` | 好友请求 | `{...}` | ✅ 存在 |
| `isMyFriend` | 是否是好友 | `{account}` | ✅ 存在 |
| `findFriend` | 查找好友 | `{account}` | ✅ 存在 |

### 黑名单/静音
| API | 功能 | 参数 | 状态 |
|-----|------|------|------|
| `addToBlacklist` | 添加到黑名单 | `{account, done}` | ✅ 存在 |
| `removeFromBlacklist` | 从黑名单移除 | `{account, done}` | ✅ 存在 |
| `markInBlacklist` | 标记黑名单 | `{account, isAdd}` | ✅ 存在 |
| `addToMutelist` | 添加到静音列表 | `{account, done}` | ✅ 存在 |
| `removeFromMutelist` | 从静音列表移除 | `{account, done}` | ✅ 存在 |
| `markInMutelist` | 标记静音 | `{account, isAdd}` | ✅ 存在 |

---

## 六、会话API (27个)

### 会话获取
| API | 功能 | 参数 | 状态 |
|-----|------|------|------|
| `getLocalSessions` | 获取本地会话 | `{done}` | ✅ 存在 |
| `getLocalSession` | 获取单个会话 | `{sessionId}` | ✅ 存在 |
| `getServerSessions` | 获取服务器会话 | `{done}` | ✅ 存在 |
| `getServerSession` | 获取单个服务器会话 | `{sessionId}` | ✅ 存在 |
| `getSessionsWithMoreRoaming` | 获取漫游会话 | `{done}` | ✅ 存在 |
| `findSession` | 查找会话 | `{sessionId}` | ✅ 存在 |

### 会话操作
| API | 功能 | 参数 | 状态 |
|-----|------|------|------|
| `setCurrSession` | 设置当前会话 | `{sessionId}` | ✅ 核心 |
| `resetCurrSession` | 重置当前会话 | `{}` | ✅ 存在 |
| `insertLocalSession` | 插入本地会话 | `{...}` | ✅ 存在 |
| `updateLocalSession` | 更新本地会话 | `{...}` | ✅ 存在 |
| `updateServerSession` | 更新服务器会话 | `{...}` | ✅ 存在 |
| `deleteLocalSession` | 删除本地会话 | `{sessionId}` | ✅ 存在 |
| `deleteSession` | 删除会话 | `{sessionId, done}` | ✅ 存在 |
| `deleteSessions` | 批量删除 | `{sessionIds}` | ✅ 存在 |
| `deleteServerSessions` | 删除服务器会话 | `{...}` | ✅ 存在 |

### 未读管理
| API | 功能 | 参数 | 状态 |
|-----|------|------|------|
| `resetSessionUnread` | 重置单个未读 | `{sessionId}` | ✅ 存在 |
| `resetSessionsUnread` | 批量重置未读 | `{sessionIds}` | ✅ 存在 |
| `resetAllSessionUnread` | 重置所有未读 | `{}` | ✅ 存在 |

### 置顶
| API | 功能 | 参数 | 状态 |
|-----|------|------|------|
| `getStickTopSessions` | 获取置顶会话 | `{done}` | ✅ 存在 |
| `addStickTopSession` | 添加置顶 | `{sessionId, done}` | ✅ 存在 |
| `deleteStickTopSession` | 删除置顶 | `{sessionId, done}` | ✅ 存在 |
| `updateStickTopSession` | 更新置顶 | `{...}` | ✅ 存在 |

---

## 七、文件API (19个)

| API | 功能 | 参数 | 状态 |
|-----|------|------|------|
| `previewFile` | 预览上传文件 | `{type, blob, done, ...}` | ✅ 核心 |
| `getFile` | 获取文件 | `{...}` | ✅ 存在 |
| `getFileList` | 获取文件列表 | `{...}` | ✅ 存在 |
| `fetchFile` | 拉取文件 | `{...}` | ✅ 存在 |
| `fetchFileList` | 拉取文件列表 | `{...}` | ✅ 存在 |
| `deleteFile` | 删除文件 | `{...}` | ✅ 存在 |
| `removeFile` | 移除文件 | `{...}` | ✅ 存在 |
| `getNosToken` | 获取NOS token | `{...}` | ✅ 存在 |
| `getNosTokenTrans` | 获取转换token | `{...}` | ✅ 存在 |
| `getSimpleNosToken` | 获取简单token | `{...}` | ✅ 存在 |
| `getNosAccessToken` | 获取访问token | `{...}` | ✅ 存在 |
| `deleteNosAccessToken` | 删除访问token | `{...}` | ✅ 存在 |
| `getNosOriginUrl` | 获取源URL | `{...}` | ✅ 存在 |
| `fileQuickTransfer` | 文件快传 | `{...}` | ✅ 存在 |
| `packFileDownloadName` | 打包下载名 | `{...}` | ✅ 存在 |
| `assembleUploadParams` | 组装上传参数 | `{...}` | 内部 |
| `uploadSdkLogUrl` | 上传SDK日志 | `{...}` | 内部 |
| `_doPreviewFile` | 执行预览 | `{...}` | 内部 |
| `_previewFileNonResume` | 非断点预览 | `{...}` | 内部 |

---

## 八、事件回调 (nim.options)

### 群组事件 (28个) ⭐核心
| 事件 | 说明 | 用途 |
|------|------|------|
| `onteams` | 群列表同步 | 获取群列表 |
| `onteammembers` | 成员列表同步 | 获取成员 |
| `onCreateTeam` | 创建群 | 监听建群 |
| `onUpdateTeam` | 更新群信息 | 监听群变更 |
| `onDismissTeam` | 解散群 | 监听解散 |
| `onTransferTeam` | 转让群主 | 监听转让 |
| `onAddTeamMembers` | 添加成员 | 监听入群 |
| `onRemoveTeamMembers` | 移除成员 | 监听踢人 |
| `onUpdateTeamManagers` | 管理员变更 | 监听管理员 |
| `onUpdateTeamMembersMute` | 禁言变更 | 监听禁言 |
| `onMyTeamMembers` | 我的群成员信息 | - |
| `onupdateteammember` | 成员更新 | - |
| `onsyncteammembersdone` | 成员同步完成 | - |
| `onTeamMsgReceipt` | 群消息回执 | - |
| `onsynccreateteam` | 同步创建群 | - |

### 消息事件 (23个) ⭐核心
| 事件 | 说明 | 用途 |
|------|------|------|
| `onmsg` | 收到消息 | **核心监听** |
| `onmsgs` | 批量消息 | **核心监听** |
| `onsysmsg` | 系统消息 | **入群审核** |
| `onofflinemsgs` | 离线消息 | 同步 |
| `onroamingmsgs` | 漫游消息 | 同步 |
| `oncustomsysmsg` | 自定义系统消息 | - |
| `onbroadcastmsg` | 广播消息 | - |
| `onMsgReceipts` | 已读回执 | - |
| `onPinMsgChange` | Pin变更 | - |
| `onDeleteMsgSelf` | 单向删除 | - |

### 同步事件 (6个)
| 事件 | 说明 |
|------|------|
| `onsyncdone` | 数据同步完成 |
| `onsyncfriendaction` | 好友操作同步 |
| `onSyncUpdateServerSession` | 会话同步 |
| `onsyncmarkinblacklist` | 黑名单同步 |
| `onsyncmarkinmutelist` | 静音同步 |

### 连接事件 (4个)
| 事件 | 说明 |
|------|------|
| `onconnect` | 连接成功 |
| `ondisconnect` | 断开连接 |
| `onwillreconnect` | 即将重连 |
| `onerror` | 错误 |

---

## 九、数据结构

### 群信息 (Team)
```json
{
  "teamId": "40821608989",
  "name": "群名称(可能加密)",
  "type": "advanced",
  "owner": "1948408648",
  "level": 5000,
  "valid": true,
  "memberNum": 186,
  "memberUpdateTime": 1767837139985,
  "createTime": 1748697115682,
  "updateTime": 1767837139997,
  "intro": "群介绍",
  "announcement": "群公告",
  "joinMode": "needVerify",
  "custom": "{\"forbidShareCard\":false,...}",
  "serverCustom": "{\"group_id\":1176721,...}",
  "avatar": "8671447222758755297",
  "beInviteMode": "needVerify",
  "inviteMode": "manager",
  "updateTeamMode": "manager",
  "updateCustomMode": "manager",
  "mute": true,
  "muteType": "normal"
}
```

### 成员信息 (TeamMember)
```json
{
  "teamId": "40821608989",
  "account": "1954086367",
  "type": "normal|manager|owner",
  "nickInTeam": "群昵称",
  "active": true,
  "valid": true,
  "joinTime": 1749105234485,
  "updateTime": 1767837139985,
  "mute": false,
  "invitorAccid": "1948408648",
  "id": "40821608989-1954086367"
}
```

### 消息结构 (Message)
```json
{
  "scene": "team|p2p",
  "to": "40821608989",
  "from": "1948408648",
  "fromNick": "发送者昵称",
  "type": "text|image|file|custom|tip|geo",
  "text": "消息内容",
  "idClient": "本地ID",
  "idServer": "服务端ID",
  "time": 1767837139985,
  "status": "success|fail",
  "flow": "in|out"
}
```

---

## 十、解密逻辑

### AES-256-CBC 解密
```javascript
// 密钥 (32字节)
const key = 'd6ba6647b7c43b79d0e42ceb2790e342';

// 初始向量 (16字节)
const iv = 'kgWRyiiODMjSCh0m';

// 解密函数
function decrypt(ciphertext) {
    return CryptoJS.AES.decrypt(ciphertext, key, {
        iv: iv,
        mode: CryptoJS.mode.CBC,
        padding: CryptoJS.pad.Pkcs7
    }).toString(CryptoJS.enc.Utf8);
}
```

### 加密字段位置
| 字段 | 位置 | 说明 |
|------|------|------|
| `nickname_ciphertext` | `user.custom` | 用户昵称 |
| `nickname_ciphertext` | `team.serverCustom` | 群名称 |
| `nicknameCiphertext` | 同上 | 驼峰写法 |

---

## 十一、WangShangLiaoBot 使用的API

### 核心API (必须)
| API | 用途 | C#方法 |
|-----|------|--------|
| `sendText` | 发送消息 | `SendTextAsync` |
| `sendFile` | 发送文件 | `SendFileAsync` |
| `previewFile` | 上传文件 | `SendImageAsync` |
| `getTeam` | 获取群信息 | `GetTeamInfoAsync` |
| `getTeams` | 获取群列表 | `GetAllTeamsAsync` |
| `getTeamMembers` | 获取成员 | `GetTeamMembersViaNimAsync` |
| `passTeamApply` | 通过入群 | `ProcessTeamJoinRequestsAsync` |
| `muteTeamAll` | 全员禁言 | `MuteAllAsync` |

### 已使用API
| API | 用途 | C#方法 |
|-----|------|--------|
| `updateMuteStateInTeam` | 单人禁言 | `MuteTeamMemberAsync` |
| `removeTeamMembers` | 踢人 | `KickTeamMemberAsync` |
| `recallMsg` | 撤回 | `RecallMessageAsync` |
| `getUsers` | 用户信息 | - |
| `getLocalMsgs` | 本地消息 | - |

### 待扩展API
| API | 建议用途 |
|-----|---------|
| `updateNickInTeam` | 修改群昵称 |
| `addTeamManagers` | 设置管理员 |
| `removeTeamManagers` | 取消管理员 |
| `getHistoryMsgs` | 历史消息同步 |
| `getMutedTeamMembers` | 禁言列表 |
| `updateTeam` | 修改群公告 |

---

## 十二、API详细参数说明

### sendText - 发送文本消息 ⭐核心
```javascript
nim.sendText({
    scene: 'team',              // 'p2p' 私聊 | 'team' 群聊 (必须)
    to: '40821608989',          // 目标ID (必须)
    text: '消息内容',            // 文本内容 (必须)
    custom: '{}',               // 自定义扩展字段 (可选)
    pushContent: '推送文案',     // 推送显示内容 (可选)
    pushPayload: '{}',          // 推送payload (可选)
    needPushNick: true,         // 推送是否带昵称 (可选)
    needMsgReceipt: false,      // 是否需要已读回执 (可选)
    isHistoryable: true,        // 是否存云端历史 (可选)
    isRoamingable: true,        // 是否支持漫游 (可选)
    isUnreadable: true,         // 是否计入未读 (可选)
    isSyncable: true,           // 是否同步到其他端 (可选)
    isPushable: true,           // 是否需要推送 (可选)
    isOfflinable: true,         // 是否支持离线 (可选)
    antiSpamOption: {},         // 反垃圾选项 (可选)
    done: (err, msg) => {}      // 回调 (必须)
});
```

### sendFile / previewFile - 发送文件 ⭐核心
```javascript
// 步骤1: 上传文件
nim.previewFile({
    type: 'image',              // 'image' | 'audio' | 'video' | 'file' (必须)
    blob: fileBlob,             // Blob对象 (必须)
    uploadprogress: (obj) => {  // 上传进度回调 (可选)
        console.log(obj.percentage + '%');
    },
    done: (err, result) => {
        if (err) return;
        // result.fileObj 包含上传后的文件信息
        // { name, size, md5, url, ext }
        
        // 步骤2: 发送文件消息
        nim.sendFile({
            scene: 'team',
            to: '40821608989',
            type: 'image',
            file: result.fileObj,   // 或直接传 blob
            done: (err, msg) => {}
        });
    }
});
```

### muteTeamAll - 全员禁言 ⭐核心
```javascript
nim.muteTeamAll({
    teamId: '40821608989',      // 群ID (必须)
    mute: true,                 // true禁言, false解禁 (必须)
    done: (err, result) => {}   // 回调
});
// 权限: 群主或管理员
```

### updateMuteStateInTeam - 单人禁言 ⭐核心
```javascript
nim.updateMuteStateInTeam({
    teamId: '40821608989',      // 群ID (必须)
    account: '1954086367',      // 成员账号 (必须)
    mute: true,                 // true禁言, false解禁 (必须)
    done: (err, result) => {}   // 回调
});
// 权限: 群主或管理员
```

### passTeamApply - 通过入群申请 ⭐核心
```javascript
nim.passTeamApply({
    teamId: '40821608989',      // 群ID (必须)
    from: '申请人账号',          // 申请人 (必须, 从系统消息获取)
    idServer: '服务端ID',        // 系统消息的idServer (必须)
    ps: '欢迎加入',              // 附言 (可选)
    done: (err, result) => {}   // 回调
});

// 系统消息格式 (从 onsysmsg 事件获取)
// { type: 'applyTeam', from: '申请人', to: '群主', teamId: '群ID', idServer: 'xxx' }
```

### getTeamMembers - 获取群成员 ⭐核心
```javascript
nim.getTeamMembers({
    teamId: '40821608989',      // 群ID (必须)
    done: (err, result) => {
        const members = result.members;
        // 成员字段: teamId, account, type, nickInTeam, active, valid,
        //          joinTime, updateTime, mute, invitorAccid, id
    }
});
```

### getHistoryMsgs - 获取历史消息
```javascript
nim.getHistoryMsgs({
    scene: 'team',              // 场景 (必须)
    to: '40821608989',          // 目标ID (必须)
    beginTime: 0,               // 开始时间戳 (可选)
    endTime: Date.now(),        // 结束时间戳 (可选)
    lastMsgId: '',              // 上一页最后消息ID (可选, 分页用)
    limit: 100,                 // 数量 (可选, 默认100)
    reverse: false,             // 是否倒序 (可选)
    msgTypes: [],               // 消息类型过滤 (可选)
    done: (err, result) => {
        const msgs = result.msgs;
    }
});
```

### removeTeamMembers - 移出群成员
```javascript
nim.removeTeamMembers({
    teamId: '40821608989',      // 群ID (必须)
    accounts: ['1954086367'],   // 账号列表 (必须)
    done: (err, result) => {}   // 回调
});
// 权限: 管理员可踢普通成员, 群主可踢任何人
```

### updateNickInTeam - 修改群昵称
```javascript
nim.updateNickInTeam({
    teamId: '40821608989',      // 群ID (必须)
    account: '1954086367',      // 成员账号 (可选, 不填改自己)
    nickInTeam: '新昵称',        // 新昵称 (必须)
    done: (err, result) => {}   // 回调
});
// 权限: 自己/管理员可改普通成员/群主可改任何人
```

### addTeamManagers / removeTeamManagers - 管理员操作
```javascript
// 添加管理员 (仅群主)
nim.addTeamManagers({
    teamId: '40821608989',
    accounts: ['1954086367'],
    done: (err, result) => {}
});

// 移除管理员 (仅群主)
nim.removeTeamManagers({
    teamId: '40821608989',
    accounts: ['1954086367'],
    done: (err, result) => {}
});
```

### updateTeam - 更新群信息
```javascript
nim.updateTeam({
    teamId: '40821608989',      // 群ID (必须)
    name: '新群名',              // 群名称 (可选)
    avatar: 'url',              // 群头像 (可选)
    intro: '群介绍',             // 群介绍 (可选)
    announcement: '群公告',      // 群公告 (可选)
    joinMode: 'needVerify',     // 入群方式: noVerify/needVerify/rejectAll (可选)
    beInviteMode: 'needVerify', // 被邀请方式 (可选)
    inviteMode: 'manager',      // 邀请权限: manager/all (可选)
    updateTeamMode: 'manager',  // 谁可以修改群资料 (可选)
    updateCustomMode: 'manager',// 谁可以修改自定义字段 (可选)
    custom: '{}',               // 自定义字段 (可选)
    done: (err, result) => {}   // 回调
});
```

### updateInfoInTeam - 消息免打扰设置
```javascript
nim.updateInfoInTeam({
    teamId: '40821608989',      // 群ID (必须)
    muteTeam: true,             // 消息免打扰 true/false (可选)
    muteNotiType: 0,            // 0=全部, 1=仅@我, 2=不接收 (可选)
    custom: '{}',               // 自定义字段 (可选)
    done: (err, result) => {}   // 回调
});
```

### recallMsg - 撤回消息
```javascript
nim.recallMsg({
    msg: messageObject,         // 消息对象 (必须, 包含idClient/idServer/scene/to)
    ps: '撤回原因',              // 附言 (可选)
    done: (err, result) => {}   // 回调
});
// 限制: 管理员可撤他人消息, 普通人只能撤自己的, 2分钟内
```

---

## 十三、群信息字段详解

```json
{
  "teamId": "40821608989",          // 群ID
  "name": "加密后的群名",            // 群名称 (可能是加密的)
  "type": "advanced",               // 群类型: normal/advanced
  "owner": "1948408648",            // 群主账号
  "level": 5000,                    // 群等级
  "valid": true,                    // 是否有效
  "memberNum": 186,                 // 成员数量
  "memberUpdateTime": 1767837139985,// 成员更新时间
  "createTime": 1748697115682,      // 创建时间
  "updateTime": 1767837139997,      // 更新时间
  "validToCurrentUser": true,       // 对当前用户是否有效
  "intro": "群介绍",                // 群介绍
  "announcement": "群公告",         // 群公告
  "joinMode": "needVerify",         // 入群方式
  "custom": "{...}",                // 自定义字段(JSON)
  "serverCustom": "{...}",          // 服务端自定义字段(JSON,含加密昵称)
  "avatar": "8671447222758755297",  // 群头像
  "beInviteMode": "needVerify",     // 被邀请方式
  "inviteMode": "manager",          // 邀请权限
  "updateTeamMode": "manager",      // 修改群资料权限
  "updateCustomMode": "manager",    // 修改自定义权限
  "mute": true,                     // 是否全员禁言
  "muteType": "normal"              // 禁言类型
}
```

### custom 字段解析
```json
{
  "forbidShareCard": false,         // 禁止分享名片
  "forbidSendFile": false,          // 禁止发送文件
  "forbidGroupCall": true,          // 禁止群通话
  "forbidChangeNick": true          // 禁止修改昵称
}
```

### serverCustom 字段解析
```json
{
  "group_id": 1176721,              // 群ID (旺商聊内部)
  "forbid_change_nick_name": true,  // 禁止修改昵称
  "nickname_ciphertext": "xxx",     // 加密的群名称 (需AES解密)
  "av_chat_mode": true,             // 语音聊天模式
  "share_card_mode": "..."          // 名片分享模式
}
```

---

## 十四、成员类型说明

| type | 说明 | 权限 |
|------|------|------|
| `owner` | 群主 | 最高权限,可操作所有人 |
| `manager` | 管理员 | 可踢/禁言普通成员 |
| `normal` | 普通成员 | 基本权限 |

---

**文档生成时间**: 2026-01-09
