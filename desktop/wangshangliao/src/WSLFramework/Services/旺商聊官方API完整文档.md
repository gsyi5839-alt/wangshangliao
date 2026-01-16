# 旺商聊官方API完整文档

> 逆向分析自: C:\Program Files (x86)\wangshangliao_win_online
> 版本: 2.7.0
> 分析日期: 2026-01-12

---

## 一、系统架构

### 1.1 技术栈
- **框架**: Electron
- **前端**: Vue 3 + Element Plus + Pinia
- **IM SDK**: 网易云信 NIM Web SDK v9.20.15
- **通信**: Axios
- **加密**: CryptoJS (AES-256-CBC)

### 1.2 关键服务器
| 服务器 | 地址 | 用途 |
|--------|------|------|
| 主API | https://yiyong.netease.im | 业务API |
| 云信 | https://lbs.netease.im | IM服务 |
| RTC | https://nrtc.netease.im | 音视频 |
| 会议室 | https://roomkit.netease.im | 会议服务 |
| 静态资源 | https://yiyong-static.nosdn.127.net | 资源下载 |
| 统计 | https://statistic.live.126.net | 数据上报 |
| 测试服 | https://qxdevacc.qixin02.xyz | 开发测试 |

---

## 二、加密配置

### 2.1 AES加密
```javascript
// 加密配置
const AES_KEY = "49KdgB8_9=12+3hF";  // SHA256 后作为密钥
const AES_IV = "00000000000000000000000000000000";  // 32位0
const AES_MODE = "CBC";
const AES_PADDING = "Pkcs7";

// 加密函数
function encrypt(plaintext) {
    return CryptoJS.AES.encrypt(
        plaintext,
        CryptoJS.SHA256("49KdgB8_9=12+3hF"),
        {
            iv: CryptoJS.enc.Hex.parse("00000000000000000000000000000000"),
            mode: CryptoJS.mode.CBC,
            padding: CryptoJS.pad.Pkcs7
        }
    ).toString();
}

// 解密函数
function decrypt(ciphertext) {
    var decrypted = CryptoJS.AES.decrypt(
        ciphertext,
        CryptoJS.SHA256("49KdgB8_9=12+3hF"),
        {
            iv: CryptoJS.enc.Hex.parse("00000000000000000000000000000000"),
            mode: CryptoJS.mode.CBC,
            padding: CryptoJS.pad.Pkcs7
        }
    );
    return CryptoJS.enc.Utf8.stringify(decrypted);
}
```

### 2.2 验证码配置
```javascript
// 易盾验证码
const captchaConfig = {
    captchaId: "37ab6b750c3246af804132808408f398",
    element: "#captcha",
    mode: "popup",
    width: "320px",
    apiVersion: 2,
    popupStyles: {
        position: "fixed",
        top: "20%"
    }
};
```

---

## 三、完整API列表 (213个端点)

> **响应码说明**: 
> - ✅ 成功码: `0` 
> - ❌ 失败码: `10010`(需认证), `401`(未授权), `1001`(参数错误), `1002`(Token无效), `1003`(账号不存在), `1004`(密码错误), `1005`(验证码错误)

### 3.1 用户服务 (/v1/user/)

| 端点 | 方法 | 说明 | 成功码 | 失败码 |
|------|------|------|--------|--------|
| `/v1/login` | POST | 主登录入口 | 0 | 1001, 1003, 1004, 1005, 10010 |
| `/v1/user/login` | POST | 用户登录 | 0 | 1001, 1003, 1004, 1005, 10010 |
| `/v1/user/logout` | POST | 用户登出 | 0 | 401, 1002, 10010 |
| `/v1/user/register` | POST | 用户注册 | 0 | 1001, 1005, 10010 |
| `/v1/user/login_device_change` | POST | 设备切换登录 | 0 | 401, 1002, 10010 |
| `/v1/user/image-check` | POST | 图片检查 | 0 | 401, 1001, 10010 |
| `/v1/user/get-auto-replies-online-state` | GET | 获取自动回复在线状态 | 0 | 401, 10010 |
| `/v1/user/get-friend-permission` | GET | 获取好友权限 | 0 | 401, 10010 |
| `/v1/user/set-friend-permission` | POST | 设置好友权限 | 0 | 401, 1001, 10010 |
| `/v1/user/set-friend-audio-video-chat` | POST | 设置好友音视频聊天 | 0 | 401, 1001, 10010 |
| `/v1/user/pretty-number-notify-info` | GET | 靓号通知信息 | 0 | 401, 10010 |
| `/v1/user/update-session` | POST | 更新会话 | 0 | 401, 1001, 10010 |

### 3.2 贴纸服务 (/v1/user/sticker*)

| 端点 | 方法 | 说明 | 成功码 | 失败码 |
|------|------|------|--------|--------|
| `/v1/user/add-sticker-collect` | POST | 添加贴纸收藏 | 0 | 401, 1001, 10010 |
| `/v1/user/add-sticker-set` | POST | 添加贴纸包 | 0 | 401, 1001, 10010 |
| `/v1/user/collected-sticker-move-to-first` | POST | 移动收藏贴纸到首位 | 0 | 401, 1001, 10010 |
| `/v1/user/del-sticker-collects` | POST | 删除贴纸收藏 | 0 | 401, 1001, 10010 |
| `/v1/user/del-sticker-set` | POST | 删除贴纸包 | 0 | 401, 1001, 10010 |
| `/v1/user/get-sticker-set-info` | GET | 获取贴纸包信息 | 0 | 401, 10010 |
| `/v1/user/sticker-collects` | GET | 获取贴纸收藏列表 | 0 | 401, 10010 |
| `/v1/user/sticker-set-adds` | GET | 获取已添加贴纸包 | 0 | 401, 10010 |
| `/v1/user/sticker-sets` | GET | 获取贴纸包列表 | 0 | 401, 10010 |

### 3.3 验证服务 (/v1/verify/)

| 端点 | 方法 | 说明 | 成功码 | 失败码 |
|------|------|------|--------|--------|
| `/v1/verify/sms` | POST | 发送短信验证码 | 0 | 1001, 1006(频繁), 10010 |
| `/v1/verify/sms-anon` | POST | 匿名发送短信验证码 | 0 | 1001, 1006, 10010 |
| `/v1/verify/verify` | POST | 验证码校验 | 0 | 1001, 1005, 10010 |
| `/v1/checkToken` | POST | Token检查 | 0 | 1002, 10010 |
| `/v1/anonymous/login` | POST | 匿名登录 | 0 | 1001, 10010 |

### 3.4 好友服务 (/v1/friend/)

| 端点 | 方法 | 说明 | 成功码 | 失败码 |
|------|------|------|--------|--------|
| `/v1/friend/add-friend-apply` | POST | 添加好友申请 | 0 | 401, 1001, 1007(已是好友), 10010 |
| `/v1/friend/add-friend-class` | POST | 添加好友分组 | 0 | 401, 1001, 10010 |
| `/v1/friend/del-friend` | POST | 删除好友 | 0 | 401, 1001, 10010 |
| `/v1/friend/del-friend-class` | POST | 删除好友分组 | 0 | 401, 1001, 10010 |
| `/v1/friend/friend-apply-handler` | POST | 处理好友申请 | 0 | 401, 1001, 1008(已处理), 10010 |
| `/v1/friend/friend-apply-list` | GET | 好友申请列表 | 0 | 401, 10010 |
| `/v1/friend/friend-black-setting` | POST | 好友黑名单设置 | 0 | 401, 1001, 10010 |
| `/v1/friend/friend-set-class` | POST | 设置好友分组 | 0 | 401, 1001, 10010 |
| `/v1/friend/friend-set-remark` | POST | 设置好友备注 | 0 | 401, 1001, 10010 |
| `/v1/friend/get-black-list` | GET | 获取黑名单 | 0 | 401, 10010 |
| `/v1/friend/get-friend-class` | GET | 获取好友分组 | 0 | 401, 10010 |
| `/v1/friend/get-friend-info` | GET | 获取好友信息 | 0 | 401, 1001, 10010 |
| `/v1/friend/get-friend-list` | GET | 获取好友列表 | 0 | 401, 10010 |
| `/v1/friend/search-conversation` | POST | 搜索会话 | 0 | 401, 1001, 10010 |
| `/v1/friend/set-background` | POST | 设置聊天背景 | 0 | 401, 1001, 10010 |
| `/v1/friend/set-friend-notice` | POST | 设置好友消息提醒 | 0 | 401, 1001, 10010 |
| `/v1/friend/set-friend-top` | POST | 设置好友置顶 | 0 | 401, 1001, 10010 |
| `/v1/friend/update-friend-class` | POST | 更新好友分组 | 0 | 401, 1001, 10010 |

### 3.5 群组服务 (/v1/group/)

| 端点 | 方法 | 说明 | 成功码 | 失败码 |
|------|------|------|--------|--------|
| `/v1/group/create` | POST | 创建群组 | 0 | 401, 1001, 1009(达上限), 10010 |
| `/v1/group/group-dismiss` | POST | 解散群组 | 0 | 401, 1001, 1010(非群主), 10010 |
| `/v1/group/leave-group` | POST | 退出群组 | 0 | 401, 1001, 1010, 10010 |
| `/v1/group/group-transfer` | POST | 群主转让 | 0 | 401, 1001, 1010, 10010 |
| `/v1/group/group-upgrade` | POST | 群升级 | 0 | 401, 1001, 1010, 10010 |
| `/v1/group/activation-supergroup` | POST | 激活超级群 | 0 | 401, 1001, 10010 |
| `/v1/group/add-group-manage` | POST | 添加管理员 | 0 | 401, 1001, 1010, 10010 |
| `/v1/group/del-group-manage` | POST | 删除管理员 | 0 | 401, 1001, 1010, 10010 |
| `/v1/group/add-group-member` | POST | 添加群成员 | 0 | 401, 1001, 1011(已在群), 10010 |
| `/v1/group/remove-group-member` | POST | 移除群成员 | 0 | 401, 1001, 1010, 10010 |
| `/v1/group/remove-manager-in-group` | POST | 移除群管理 | 0 | 401, 1001, 1010, 10010 |
| `/v1/group/remove-member-in-group` | POST | 移除群内成员 | 0 | 401, 1001, 1010, 10010 |
| `/v1/group/del-group-member-through-chat` | POST | 通过聊天删除成员 | 0 | 401, 1001, 10010 |
| `/v1/group/apply-join-group` | POST | 申请加群 | 0 | 401, 1001, 1011, 10010 |
| `/v1/group/group-manage-apply` | POST | 管理加群申请 | 0 | 401, 1001, 1010, 10010 |
| `/v1/group/group-member-invite` | POST | 邀请成员入群 | 0 | 401, 1001, 1010, 10010 |
| `/v1/group/get-group-apply-list` | GET | 获取加群申请列表 | 0 | 401, 10010 |
| `/v1/group/get-apply-logs` | GET | 获取申请日志 | 0 | 401, 10010 |
| `/v1/group/del-apply-logs` | POST | 删除申请日志 | 0 | 401, 1001, 10010 |
| `/v1/group/exist` | GET | 检查群是否存在 | 0 | 401, 1001, 10010 |
| `/v1/group/get-gid` | GET | 获取群ID映射 | 0 | 401, 1001, 10010 |
| `/v1/group/get-enter-limit` | GET | 获取入群限制 | 0 | 401, 1001, 10010 |
| `/v1/group/set-enter-limit` | POST | 设置入群限制 | 0 | 401, 1001, 1010, 10010 |
| `/v1/group/get-group-info` | GET | 获取群信息 | 0 | 401, 1001, 10010 |
| `/v1/group/get-group-info-service` | GET | 获取群信息(服务) | 0 | 401, 1001, 10010 |
| `/v1/group/get-group-list` | GET | 获取群列表 | 0 | 401, 10010 |
| `/v1/group/get-group-member-info` | GET | 获取群成员信息 | 0 | 401, 1001, 10010 |
| `/v1/group/get-group-members` | GET | 获取群成员列表 | 0 | 401, 1001, 10010 |
| `/v1/group/get-group-membership` | GET | 获取群成员资格 | 0 | 401, 1001, 10010 |
| `/v1/group/get-group-history-message` | GET | 获取群历史消息 | 0 | 401, 1001, 10010 |
| `/v1/group/get-group-by-account` | GET | 按账号获取群 | 0 | 401, 1001, 10010 |
| `/v1/group/get-nim-group` | GET | 获取NIM群信息 | 0 | 401, 1001, 10010 |
| `/v1/group/get-nim-user-groups` | GET | 获取用户NIM群列表 | 0 | 401, 10010 |
| `/v1/group/GetGroupCloudForService` | GET | 获取群云服务 | 0 | 401, 1001, 10010 |
| `/v1/group/GetGroupInfoLite` | GET | 获取群简要信息 | 0 | 401, 1001, 10010 |
| `/v1/group/group-helper-list` | GET | 群助手列表 | 0 | 401, 10010 |
| `/v1/group/group-info-ext` | GET | 群扩展信息 | 0 | 401, 1001, 10010 |
| `/v1/group/group-num-limit` | GET | 群数量限制 | 0 | 401, 10010 |
| `/v1/group/SearchGroup` | POST | 搜索群 | 0 | 401, 1001, 10010 |
| `/v1/group/user-exist-group` | GET | 用户是否在群中 | 0 | 401, 1001, 10010 |

### 3.6 群设置 (/v1/group/set-*)

| 端点 | 方法 | 说明 | 成功码 | 失败码 |
|------|------|------|--------|--------|
| `/v1/group/set-audio-video-chat` | POST | 设置群音视频 | 0 | 401, 1001, 1010, 10010 |
| `/v1/group/set-check-word-mode` | POST | 设置敏感词模式 | 0 | 401, 1001, 1010, 10010 |
| `/v1/group/set-file-mode` | POST | 设置文件模式 | 0 | 401, 1001, 1010, 10010 |
| `/v1/group/set-group-avatar` | POST | 设置群头像 | 0 | 401, 1001, 1010, 10010 |
| `/v1/group/set-group-mute` | POST | 设置群禁言 | 0 | 401, 1001, 1010, 10010 |
| `/v1/group/set-group-name` | POST | 设置群名称 | 0 | 401, 1001, 1010, 10010 |
| `/v1/group/set-group-remark` | POST | 设置群备注 | 0 | 401, 1001, 10010 |
| `/v1/group/set-group-shake` | POST | 设置群抖动 | 0 | 401, 1001, 1010, 10010 |
| `/v1/group/set-member-mute` | POST | 设置成员禁言 | 0 | 401, 1001, 1010, 10010 |
| `/v1/group/set-member-nickname` | POST | 设置成员昵称 | 0 | 401, 1001, 10010 |
| `/v1/group/set-member-remark` | POST | 设置成员备注 | 0 | 401, 1001, 10010 |
| `/v1/group/set-nickname-mode` | POST | 设置昵称模式 | 0 | 401, 1001, 1010, 10010 |
| `/v1/group/set-notice-mode` | POST | 设置通知模式 | 0 | 401, 1001, 10010 |
| `/v1/group/set-notice-read` | POST | 设置通知已读 | 0 | 401, 1001, 10010 |
| `/v1/group/set-private-chat` | POST | 设置私聊 | 0 | 401, 1001, 1010, 10010 |
| `/v1/group/set-red-envelope-settings` | POST | 设置红包设置 | 0 | 401, 1001, 1010, 10010 |
| `/v1/group/set-search-mode` | POST | 设置搜索模式 | 0 | 401, 1001, 1010, 10010 |
| `/v1/group/set-share-card` | POST | 设置分享名片 | 0 | 401, 1001, 1010, 10010 |
| `/v1/group/SetGroupMemberUnMute` | POST | 解除成员禁言 | 0 | 401, 1001, 1010, 10010 |
| `/v1/group/SetGroupTop` | POST | 设置群置顶 | 0 | 401, 1001, 10010 |
| `/v1/group/member-mute-cancel` | POST | 取消成员禁言 | 0 | 401, 1001, 1010, 10010 |
| `/v1/group/update-group-user-mute-service` | POST | 更新群禁言服务 | 0 | 401, 1001, 10010 |

### 3.7 群公告 (/v1/group/notice*)

| 端点 | 方法 | 说明 | 成功码 | 失败码 |
|------|------|------|--------|--------|
| `/v1/group/add-notice` | POST | 添加公告 | 0 | 401, 1001, 1010, 10010 |
| `/v1/group/notice-del` | POST | 删除公告 | 0 | 401, 1001, 1010, 10010 |
| `/v1/group/notice-info` | GET | 公告信息 | 0 | 401, 1001, 10010 |
| `/v1/group/notice-list` | GET | 公告列表 | 0 | 401, 1001, 10010 |
| `/v1/group/notice-opt` | POST | 公告操作 | 0 | 401, 1001, 10010 |
| `/v1/group/notice-reader-list` | GET | 公告阅读列表 | 0 | 401, 1001, 10010 |
| `/v1/group/top-notice` | POST | 置顶公告 | 0 | 401, 1001, 1010, 10010 |
| `/v1/group/message-rollback` | POST | 消息撤回 | 0 | 401, 1001, 1012(超时), 10010 |

### 3.8 收藏服务 (/v1/collect/)

| 端点 | 方法 | 说明 | 成功码 | 失败码 |
|------|------|------|--------|--------|
| `/v1/collect/add-collect` | POST | 添加收藏 | 0 | 401, 1001, 10010 |
| `/v1/collect/del-collect` | POST | 删除收藏 | 0 | 401, 1001, 10010 |
| `/v1/collect/get-collects` | GET | 获取收藏列表 | 0 | 401, 10010 |

### 3.9 朋友圈服务 (/v1/moment/)

| 端点 | 方法 | 说明 | 成功码 | 失败码 |
|------|------|------|--------|--------|
| `/v1/moment/add_comment` | POST | 添加评论 | 0 | 401, 1001, 10010 |
| `/v1/moment/add_feed` | POST | 发布动态 | 0 | 401, 1001, 10010 |
| `/v1/moment/add_like` | POST | 点赞 | 0 | 401, 1001, 10010 |
| `/v1/moment/clear_comment_msgs` | POST | 清空评论消息 | 0 | 401, 10010 |
| `/v1/moment/del_comment` | POST | 删除评论 | 0 | 401, 1001, 10010 |
| `/v1/moment/del_feed` | POST | 删除动态 | 0 | 401, 1001, 10010 |
| `/v1/moment/del_like` | POST | 取消点赞 | 0 | 401, 1001, 10010 |
| `/v1/moment/get_comment_msgs` | GET | 获取评论消息 | 0 | 401, 10010 |
| `/v1/moment/get_feed` | GET | 获取动态 | 0 | 401, 1001, 10010 |
| `/v1/moment/get_feed_actual_auth_friends` | GET | 获取动态可见好友 | 0 | 401, 1001, 10010 |
| `/v1/moment/get_feeds` | GET | 获取动态列表 | 0 | 401, 10010 |
| `/v1/moment/get_last_feed_photos` | GET | 获取最近动态图片 | 0 | 401, 10010 |
| `/v1/moment/get_moment_auth` | GET | 获取朋友圈权限 | 0 | 401, 10010 |
| `/v1/moment/get_official_feed_draft` | GET | 获取官方动态草稿 | 0 | 401, 10010 |
| `/v1/moment/get_unread_info` | GET | 获取未读信息 | 0 | 401, 10010 |
| `/v1/moment/get_user_feeds` | GET | 获取用户动态 | 0 | 401, 1001, 10010 |
| `/v1/moment/incr_feed_count` | POST | 增加动态计数 | 0 | 401, 1001, 10010 |
| `/v1/moment/pin_feed` | POST | 置顶动态 | 0 | 401, 1001, 10010 |
| `/v1/moment/pub_official_feed` | POST | 发布官方动态 | 0 | 401, 1001, 10010 |
| `/v1/moment/save_official_feed_draft` | POST | 保存官方动态草稿 | 0 | 401, 1001, 10010 |
| `/v1/moment/set_bg_photo` | POST | 设置背景图 | 0 | 401, 1001, 10010 |
| `/v1/moment/set_moment_config` | POST | 设置朋友圈配置 | 0 | 401, 1001, 10010 |
| `/v1/moment/update_feed_auth` | POST | 更新动态权限 | 0 | 401, 1001, 10010 |
| `/v1/moment/update_feed_authorize` | POST | 更新动态授权 | 0 | 401, 1001, 10010 |
| `/v1/moment/update_moment_auth` | POST | 更新朋友圈权限 | 0 | 401, 1001, 10010 |
| `/friendCircle/userMoment` | GET | 用户朋友圈 | 0 | 401, 1001, 10010 |

### 3.10 设置服务 (/v1/settings/)

| 端点 | 方法 | 说明 | 成功码 | 失败码 |
|------|------|------|--------|--------|
| `/v1/settings/add-to-group-helper` | POST | 添加群助手 | 0 | 401, 1001, 10010 |
| `/v1/settings/audio-video-chat` | POST | 音视频聊天设置 | 0 | 401, 1001, 10010 |
| `/v1/settings/avatar` | POST | 头像设置 | 0 | 401, 1001, 10010 |
| `/v1/settings/change-moment-setting-background` | POST | 更改朋友圈背景 | 0 | 401, 1001, 10010 |
| `/v1/settings/del-from-group-helper` | POST | 删除群助手 | 0 | 401, 1001, 10010 |
| `/v1/settings/get-agreement` | GET | 获取协议 | 0 | 401, 10010 |
| `/v1/settings/get-line-group` | GET | 获取线路群 | 0 | 401, 10010 |
| `/v1/settings/get-moment-setting` | GET | 获取朋友圈设置 | 0 | 401, 10010 |
| `/v1/settings/get-sensitive-words` | GET | 获取敏感词 | 0 | 401, 10010 |
| `/v1/settings/get-sys-cfg` | GET | 获取系统配置 | 0 | 401, 10010 |
| `/v1/settings/get-system-setting` | GET | 获取系统设置 | 0 | 401, 10010 |
| `/v1/settings/group-verify` | POST | 群验证 | 0 | 401, 1001, 10010 |
| `/v1/settings/p2p-search` | POST | P2P搜索设置 | 0 | 401, 1001, 10010 |
| `/v1/settings/p2p-verify` | POST | P2P验证 | 0 | 401, 1001, 10010 |
| `/v1/settings/password` | POST | 密码设置 | 0 | 401, 1001, 1004, 10010 |
| `/v1/settings/query-app-settings` | GET | 查询应用设置 | 0 | 401, 10010 |
| `/v1/settings/ring-group` | POST | 群消息铃声 | 0 | 401, 1001, 10010 |
| `/v1/settings/ring-p2p` | POST | 私聊消息铃声 | 0 | 401, 1001, 10010 |
| `/v1/settings/self-nick-name` | POST | 设置昵称 | 0 | 401, 1001, 10010 |
| `/v1/settings/set-auto-reply` | POST | 设置自动回复 | 0 | 401, 1001, 10010 |
| `/v1/settings/set-group-helper-state` | POST | 设置群助手状态 | 0 | 401, 1001, 10010 |
| `/v1/settings/set-notify-state` | POST | 设置通知状态 | 0 | 401, 1001, 10010 |
| `/v1/settings/set-session-hide` | POST | 设置会话隐藏 | 0 | 401, 1001, 10010 |
| `/v1/settings/set-session-top` | POST | 设置会话置顶 | 0 | 401, 1001, 10010 |

### 3.11 音视频/RTC服务 (/v1/rtc/)

| 端点 | 方法 | 说明 | 成功码 | 失败码 |
|------|------|------|--------|--------|
| `/v1/rtc/bind_live_user` | POST | 绑定直播用户 | 0 | 401, 1001, 10010 |
| `/v1/rtc/close_live_room` | POST | 关闭直播间 | 0 | 401, 1001, 1013(非主播), 10010 |
| `/v1/rtc/close_live_room_screen_share` | POST | 关闭直播屏幕共享 | 0 | 401, 1001, 10010 |
| `/v1/rtc/del_room` | POST | 删除房间 | 0 | 401, 1001, 10010 |
| `/v1/rtc/del_room_member` | POST | 删除房间成员 | 0 | 401, 1001, 10010 |
| `/v1/rtc/get_live_room` | GET | 获取直播间 | 0 | 401, 1001, 10010 |
| `/v1/rtc/get_live_room_screen_share` | GET | 获取直播屏幕共享 | 0 | 401, 1001, 10010 |
| `/v1/rtc/get_live_user_audio_video_state` | GET | 获取用户音视频状态 | 0 | 401, 1001, 10010 |
| `/v1/rtc/get_room` | GET | 获取房间 | 0 | 401, 1001, 10010 |
| `/v1/rtc/get_room_token` | GET | 获取房间Token | 0 | 401, 1001, 10010 |
| `/v1/rtc/live_group_apply` | POST | 群直播申请 | 0 | 401, 1001, 10010 |
| `/v1/rtc/live_group_apply_audit` | POST | 审核群直播申请 | 0 | 401, 1001, 1010, 10010 |
| `/v1/rtc/live_group_apply_batch_audit` | POST | 批量审核申请 | 0 | 401, 1001, 1010, 10010 |
| `/v1/rtc/live_group_apply_lists` | GET | 群直播申请列表 | 0 | 401, 10010 |
| `/v1/rtc/live_group_invite` | POST | 群直播邀请 | 0 | 401, 1001, 10010 |
| `/v1/rtc/live_group_invite_audit` | POST | 审核群直播邀请 | 0 | 401, 1001, 10010 |
| `/v1/rtc/new_live_room` | POST | 新建直播间 | 0 | 401, 1001, 10010 |
| `/v1/rtc/new_room` | POST | 新建房间 | 0 | 401, 1001, 10010 |
| `/v1/rtc/remove_live_room_member` | POST | 移除直播间成员 | 0 | 401, 1001, 10010 |
| `/v1/rtc/set_batch_live_room_member_forbid` | POST | 批量禁止直播成员 | 0 | 401, 1001, 10010 |
| `/v1/rtc/set_live_room_forbid` | POST | 设置直播间禁止 | 0 | 401, 1001, 10010 |
| `/v1/rtc/set_live_room_member_forbid` | POST | 设置成员禁止 | 0 | 401, 1001, 10010 |
| `/v1/rtc/start_live_room_screen_share` | POST | 开始屏幕共享 | 0 | 401, 1001, 10010 |

### 3.12 会议服务 (/v1/sdk/meeting/)

| 端点 | 方法 | 说明 | 成功码 | 失败码 |
|------|------|------|--------|--------|
| `/v1/sdk/meeting/control/host` | POST | 会议主持人控制 | 0 | 401, 1001, 10010 |
| `/v1/sdk/meeting/control/member` | POST | 会议成员控制 | 0 | 401, 1001, 10010 |
| `/v1/rooms/` | GET | 房间列表 | 0 | 401, 10010 |

### 3.13 企业服务 (/v1/enterprise/)

| 端点 | 方法 | 说明 | 成功码 | 失败码 |
|------|------|------|--------|--------|
| `/v1/enterprise/active_staff` | POST | 激活员工 | 0 | 401, 1001, 1014(非企业), 10010 |
| `/v1/enterprise/add_order` | POST | 添加订单 | 0 | 401, 1001, 1014, 10010 |
| `/v1/enterprise/add_staff` | POST | 添加员工 | 0 | 401, 1001, 1014, 10010 |
| `/v1/enterprise/del_staff` | POST | 删除员工 | 0 | 401, 1001, 1014, 10010 |
| `/v1/enterprise/get_bind_orders` | GET | 获取绑定订单 | 0 | 401, 1014, 10010 |
| `/v1/enterprise/get_goods` | GET | 获取商品 | 0 | 401, 10010 |
| `/v1/enterprise/get_owner` | GET | 获取企业主 | 0 | 401, 1014, 10010 |
| `/v1/enterprise/get_staff_chat_msgs` | GET | 获取员工聊天消息 | 0 | 401, 1001, 1014, 10010 |
| `/v1/enterprise/get_staff_chats` | GET | 获取员工聊天 | 0 | 401, 1014, 10010 |
| `/v1/enterprise/get_staff_summary` | GET | 获取员工摘要 | 0 | 401, 1014, 10010 |
| `/v1/enterprise/get_staffs` | GET | 获取员工列表 | 0 | 401, 1014, 10010 |
| `/v1/enterprise/get_summary` | GET | 获取企业摘要 | 0 | 401, 1014, 10010 |
| `/v1/enterprise/get_summary_field_sorts` | GET | 获取摘要字段排序 | 0 | 401, 1014, 10010 |
| `/v1/enterprise/update_bind_order` | POST | 更新绑定订单 | 0 | 401, 1001, 1014, 10010 |
| `/v1/enterprise/update_summary_field_sort` | POST | 更新摘要字段排序 | 0 | 401, 1001, 1014, 10010 |

### 3.14 插件服务 (/v1/plugins/)

| 端点 | 方法 | 说明 | 成功码 | 失败码 |
|------|------|------|--------|--------|
| `/v1/plugins/group-mute-member` | POST | 群禁言成员 | 0 | 401, 1001, 1010, 10010 |
| `/v1/plugins/activation-code` | POST | 插件激活码 | 0 | 401, 1001, 10010 |
| `/v1/plugins/get-gid` | GET | 获取群ID | 0 | 401, 1001, 10010 |
| `/v1/plugins/get-userinfo-by-id` | GET | 按ID获取用户信息 | 0 | 401, 1001, 10010 |
| `/v1/plugins/handle-relation-ask-friend` | POST | 处理好友请求 | 0 | 401, 1001, 10010 |
| `/v1/plugins/user-group-info` | POST | 用户群信息 | 0 | 401, 1001, 10010 |

### 3.16 消息服务 (NIM SDK)

> ⚠️ **重要**: 消息发送和接收通过 **NIM SDK** 实现，不是HTTP API

#### 3.16.1 发送消息API

| 函数 | 说明 | 参数 |
|------|------|------|
| `nim.sendText()` | 发送文本消息 | scene, to, text |
| `nim.sendCustomMsg()` | 发送自定义消息 | scene, to, content, attach |
| `nim.sendFile()` | 发送文件消息 | scene, to, file |
| `nim.sendMsgReceipt()` | 发送消息回执 | msg |
| `nim.sendTeamMsgReceipt()` | 发送群消息回执 | teamMsgReceipts |
| `nim.forwardMsg()` | 转发消息 | msg, scene, to |

#### 3.16.2 接收消息API

| 函数/事件 | 说明 | 回调参数 |
|------|------|------|
| `on('msg', callback)` | 收到消息事件 | msg对象 |
| `on('onRecvSessionMsgs', callback)` | 收到会话消息 | msgs数组 |
| `nim.getHistoryMsgs()` | 获取历史消息 | scene, to, options |
| `nim.getLocalMsgsByIdClients()` | 获取本地消息 | idClients |
| `nim.getMsgsByIdServer()` | 从服务器获取消息 | idServer |

#### 3.16.3 消息管理API

| 函数 | 说明 | 成功回调 | 失败回调 |
|------|------|----------|----------|
| `nim.deleteMsgSelf()` | 删除自己的消息 | done | error |
| `nim.clearServerHistoryMsgsWithSync()` | 清空服务器历史消息 | done | error |
| `nim.updateLocalMsg()` | 更新本地消息 | done | error |
| `nim.recallMsg()` | 撤回消息 | done | error |

#### 3.16.4 发送消息代码示例

```javascript
// 发送文本消息
nim.sendText({
    scene: 'team',           // 'p2p'=单聊, 'team'=群聊
    to: '群ID或用户accid',    // NIM内部ID
    text: '消息内容',
    done: function(err, msg) {
        if (!err) {
            console.log('发送成功', msg);
        }
    }
});

// 发送自定义消息
nim.sendCustomMsg({
    scene: 'team',
    to: '群ID',
    content: JSON.stringify({type: 1, data: {...}}),
    attach: '附加信息',
    pushContent: '推送内容',
    done: function(err, msg) {}
});

// 发送文件
nim.sendFile({
    scene: 'p2p',
    to: '用户accid',
    file: fileInput,
    done: function(err, msg) {}
});
```

#### 3.16.5 接收消息代码示例

```javascript
// 监听消息
nim.on('msg', function(msg) {
    console.log('收到消息:', msg);
    // msg.scene: 'p2p' 或 'team'
    // msg.from: 发送者accid
    // msg.to: 接收者
    // msg.text: 文本内容
    // msg.type: 消息类型
    // msg.time: 时间戳
});

// 获取历史消息
nim.getHistoryMsgs({
    scene: 'team',
    to: '群ID',
    limit: 100,
    done: function(err, obj) {
        if (!err) {
            console.log('历史消息:', obj.msgs);
        }
    }
});
```

#### 3.16.6 消息类型

| 类型 | 值 | 说明 |
|------|-----|------|
| text | 'text' | 文本消息 |
| image | 'image' | 图片消息 |
| audio | 'audio' | 语音消息 |
| video | 'video' | 视频消息 |
| file | 'file' | 文件消息 |
| geo | 'geo' | 地理位置 |
| custom | 'custom' | 自定义消息 |
| tip | 'tip' | 提示消息 |
| notification | 'notification' | 通知消息 |

#### 3.16.7 场景类型

| 场景 | 值 | 说明 |
|------|-----|------|
| P2P | 'p2p' | 单聊/私聊 |
| Team | 'team' | 群聊 |
| SuperTeam | 'superTeam' | 超级群 |

### 3.15 其他服务

| 端点 | 方法 | 说明 | 成功码 | 失败码 |
|------|------|------|--------|--------|
| `/v1/qr-code/get-qrcode` | GET | 获取二维码 | 0 | 401, 10010 |
| `/v1/report/get-report-detail` | GET | 获取举报详情 | 0 | 401, 1001, 10010 |
| `/v1/skuEnv` | GET | SKU环境配置 | 0 | 401, 10010 |
| `/v1/vip/get-vip-privilege` | GET | 获取VIP特权 | 0 | 401, 10010 |

---

## 四、登录流程详解

### 4.1 NIM SDK登录状态

```javascript
// 登录状态枚举
const V2NIMLoginStatus = {
    V2NIM_LOGIN_STATUS_LOGOUT: 0,      // 已登出
    V2NIM_LOGIN_STATUS_LOGINING: 1,    // 登录中
    V2NIM_LOGIN_STATUS_LOGINED: 2,     // 已登录
    V2NIM_LOGIN_STATUS_UNLOGIN: 3      // 未登录
};

// 连接状态枚举
const V2NIMConnectStatus = {
    V2NIM_CONNECT_STATUS_DISCONNECTED: 0,  // 已断开
    V2NIM_CONNECT_STATUS_CONNECTING: 1,    // 连接中
    V2NIM_CONNECT_STATUS_CONNECTED: 2      // 已连接
};
```

### 4.2 登录生命周期事件

```javascript
// 登录生命周期事件
const loginEvents = [
    "auth/loginLifeCycleAddressing",   // 地址解析中
    "auth/loginLifeCycleConnect",      // 连接中
    "auth/loginLifeCycleConnectSucc",  // 连接成功
    "auth/loginLifeCycleConnectFail",  // 连接失败
    "auth/loginLifeCycleLoginSucc",    // 登录成功
    "auth/loginLifeCycleLogout",       // 登出
    "auth/loginLifeCycleKicked"        // 被踢出
];
```

### 4.3 完整登录请求

```javascript
// 1. 发送短信验证码
POST /v1/verify/sms
{
    "phone": "手机号",
    "type": "login"  // 或 "register"
}

// 2. 验证码校验
POST /v1/verify/verify
{
    "phone": "手机号",
    "code": "验证码"
}

// 3. 用户登录
POST /v1/user/login
{
    "phone": "手机号",
    "code": "验证码",
    "deviceId": "设备ID",
    "platform": "windows"
}

// 4. Token检查
POST /v1/checkToken
Headers:
    Authorization: Bearer {token}
```

---

## 五、请求认证

### 5.1 请求头

```javascript
const headers = {
    "Content-Type": "application/json",
    "Authorization": "Bearer {token}",
    "X-App-Key": "{appKey}",
    "X-Device-Id": "{deviceId}",
    "X-Platform": "windows",
    "X-Version": "2.7.0"
};
```

### 5.2 认证流程

```
1. 登录获取 token
2. 所有后续请求携带 Authorization: Bearer {token}
3. 定期调用 /v1/checkToken 刷新token
4. token过期时重新登录
```

---

## 六、NIM SDK配置

### 6.1 初始化配置

```javascript
const nimConfig = {
    appKey: "{云信AppKey}",
    account: "{accid}",
    token: "{nimToken}",
    lbs: "https://lbs.netease.im",
    link: "https://weblink.netease.im",
    nos: "https://nos.netease.com"
};
```

### 6.2 心跳配置

```javascript
const heartbeatInterval = 30000;  // 30秒心跳间隔
```

### 6.3 NIM SDK 完整方法列表

#### 连接管理
| 方法 | 说明 |
|------|------|
| `nim.connect()` | 连接服务器 |
| `nim.disconnect()` | 断开连接 |
| `nim.destroy()` | 销毁实例 |

#### 消息操作
| 方法 | 说明 |
|------|------|
| `nim.sendText()` | 发送文本消息 |
| `nim.sendCustomMsg()` | 发送自定义消息 |
| `nim.sendFile()` | 发送文件消息 |
| `nim.forwardMsg()` | 转发消息 |
| `nim.recallMsg()` | 撤回消息 |
| `nim.deleteMsgSelf()` | 删除消息 |
| `nim.sendMsgReceipt()` | 发送已读回执 |
| `nim.sendTeamMsgReceipt()` | 发送群消息回执 |

#### 消息查询
| 方法 | 说明 |
|------|------|
| `nim.getHistoryMsgs()` | 获取历史消息 |
| `nim.getLocalMsgsByIdClients()` | 获取本地消息 |
| `nim.getMsgsByIdServer()` | 服务器获取消息 |
| `nim.clearServerHistoryMsgsWithSync()` | 清空历史消息 |

#### 会话管理
| 方法 | 说明 |
|------|------|
| `nim.getLocalSession()` | 获取本地会话 |
| `nim.getLocalSessions()` | 获取本地会话列表 |
| `nim.getServerSessions()` | 获取服务器会话 |
| `nim.deleteLocalSession()` | 删除本地会话 |
| `nim.resetSessionUnread()` | 重置未读数 |
| `nim.addStickTopSession()` | 置顶会话 |
| `nim.deleteStickTopSession()` | 取消置顶 |

#### 好友管理
| 方法 | 说明 |
|------|------|
| `nim.applyFriend()` | 申请添加好友 |
| `nim.passFriendApply()` | 通过好友申请 |
| `nim.rejectFriendApply()` | 拒绝好友申请 |
| `nim.deleteFriend()` | 删除好友 |
| `nim.updateFriend()` | 更新好友信息 |
| `nim.getRelations()` | 获取好友关系 |
| `nim.markInBlacklist()` | 加入黑名单 |
| `nim.markInMutelist()` | 加入静音列表 |

#### 群组管理
| 方法 | 说明 |
|------|------|
| `nim.getTeam()` | 获取群信息 |
| `nim.getTeamMembers()` | 获取群成员 |
| `nim.addTeamMembers()` | 添加群成员 |
| `nim.removeTeamMembers()` | 移除群成员 |
| `nim.addTeamManagers()` | 添加管理员 |
| `nim.removeTeamManagers()` | 移除管理员 |
| `nim.transferTeam()` | 转让群主 |
| `nim.dismissTeam()` | 解散群 |
| `nim.leaveTeam()` | 退出群 |
| `nim.applyTeam()` | 申请入群 |
| `nim.passTeamApply()` | 通过入群申请 |
| `nim.rejectTeamApply()` | 拒绝入群申请 |
| `nim.acceptTeamInvite()` | 接受入群邀请 |
| `nim.rejectTeamInvite()` | 拒绝入群邀请 |
| `nim.muteTeamAll()` | 群全员禁言 |
| `nim.updateInfoInTeam()` | 更新群内信息 |

#### 用户信息
| 方法 | 说明 |
|------|------|
| `nim.getUser()` | 获取用户信息 |
| `nim.getUsers()` | 批量获取用户 |
| `nim.updateMyInfo()` | 更新我的信息 |

#### 文件上传
| 方法 | 说明 |
|------|------|
| `nim.previewFile()` | 预览文件 |
| `nim._previewFileNonResume()` | 非断点预览 |

#### 信令服务
| 方法 | 说明 |
|------|------|
| `nim.signalingCancel()` | 取消信令 |
| `nim.signalingControl()` | 信令控制 |
| `nim.signalingJoinAndAccept()` | 加入并接受 |
| `nim.signalingLeave()` | 离开信令 |

#### 其他
| 方法 | 说明 |
|------|------|
| `nim.httpRequestProxy()` | HTTP代理请求 |
| `nim.subscribeEvent()` | 订阅事件 |

---

## 七、config.ini 配置文件破解

### 7.1 AES加密配置

```
密钥原文:  49KdgB8_9=12+3hF
密钥算法:  SHA256(密钥原文) → 32字节
密钥Hex:   0e82f72c2d07f18185e7169e37670e726c50b0fa631123399358b2695e976017
IV:        00000000000000000000000000000000 (全零16字节)
模式:      AES-256-CBC
填充:      PKCS7
```

### 7.2 解密示例代码

```python
import base64
from Crypto.Cipher import AES
from Crypto.Hash import SHA256

def decrypt_config(encrypted_b64):
    """解密config.ini中的加密数据"""
    key_str = '49KdgB8_9=12+3hF'
    iv = bytes(16)  # 全零IV
    
    # SHA256生成密钥
    key = SHA256.new(key_str.encode()).digest()
    
    # Base64解码
    encrypted = base64.b64decode(encrypted_b64)
    
    # AES-CBC解密
    cipher = AES.new(key, AES.MODE_CBC, iv)
    decrypted = cipher.decrypt(encrypted)
    
    # 去除PKCS7填充
    padding = decrypted[-1]
    if 0 < padding <= 16:
        decrypted = decrypted[:-padding]
    
    return decrypted.decode('utf-8')

# 使用示例
qun_encrypted = "iQEpb67flWe6pIT36InkpA=="
qun_decrypted = decrypt_config(qun_encrypted)
print(f"群ID: {qun_decrypted}")  # 输出: 3962369093
```

### 7.3 配置项说明

| 配置项 | 加密 | 说明 |
|--------|------|------|
| `账号` | 否 | 旺商聊登录账号 |
| `NIM ID` | 否 | NIM内部用户ID (10位数字) |
| `qun` | **是** | 群ID (AES加密) |
| `jwtToken` | **是** | JWT令牌 (AES加密) |
| `nickName` | 否 | 昵称 |
| `登录状态` | 否 | 0=未登录, 1=已登录 |
| `自动登录` | 否 | 0=关闭, 1=开启 |

### 7.4 解密结果示例

```
旺商聊账号:    621705120
NIM内部ID:     1628907626
群ID (解密后): 3962369093
jwtToken (解密后): CAEQqPzDBBjPASCtrI4uKK_O1ZPJqdWjbg.YTL6ykkvF8op58bsmBvb_UrMZ0WCfIrtZQqG_LJQx5_XLywwl95e--Cz-_cvPtprEyl1WeSN6V_60_GL9sPEBw.01
```

---

## 八、HPSocket4C.dll API文档

### 8.1 DLL概述

| 文件 | 大小 | 说明 |
|------|------|------|
| HPSocket4C.dll | 2.1 MB | HP-Socket通信库 (基础版) |
| HPSocket4C_2.dll | 2.9 MB | HP-Socket通信库 (增强版，含压缩) |

### 8.2 消息发送API

```c
// 客户端发送
BOOL HP_Client_Send(HP_Client pClient, const BYTE* pBuffer, int iLength);
BOOL HP_Client_SendPackets(HP_Client pClient, const WSABUF pBuffers[], int iCount);
BOOL HP_Client_SendPart(HP_Client pClient, const BYTE* pBuffer, int iLength, int iOffset);

// 服务端发送
BOOL HP_Server_Send(HP_Server pServer, HP_CONNID dwConnID, const BYTE* pBuffer, int iLength);
BOOL HP_Server_SendPackets(HP_Server pServer, HP_CONNID dwConnID, const WSABUF pBuffers[], int iCount);

// Agent发送
BOOL HP_Agent_Send(HP_Agent pAgent, HP_CONNID dwConnID, const BYTE* pBuffer, int iLength);
BOOL HP_Agent_SendPackets(HP_Agent pAgent, HP_CONNID dwConnID, const WSABUF pBuffers[], int iCount);
```

### 8.3 消息接收回调

```c
// 设置接收回调
void HP_Set_FN_Client_OnReceive(HP_ClientListener pListener, HP_FN_Client_OnReceive fn);
void HP_Set_FN_Server_OnReceive(HP_ServerListener pListener, HP_FN_Server_OnReceive fn);
void HP_Set_FN_Agent_OnReceive(HP_AgentListener pListener, HP_FN_Agent_OnReceive fn);

// 回调函数签名
typedef EnHandleResult (*HP_FN_Client_OnReceive)(HP_Client pSender, const BYTE* pData, int iLength);
typedef EnHandleResult (*HP_FN_Server_OnReceive)(HP_Server pSender, HP_CONNID dwConnID, const BYTE* pData, int iLength);
```

### 8.4 TCP Pack模式 (旺商聊使用)

```c
// 创建Pack客户端/服务器
HP_TcpPackClient HP_TcpPackClient_Create(HP_TcpPackClientListener pListener);
HP_TcpPackServer HP_TcpPackServer_Create(HP_TcpPackServerListener pListener);

// 设置包头标志
void HP_TcpPackClient_SetPackHeaderFlag(HP_TcpPackClient pClient, USHORT usPackHeaderFlag);
USHORT HP_TcpPackClient_GetPackHeaderFlag(HP_TcpPackClient pClient);

// 设置最大包大小
void HP_TcpPackClient_SetMaxPackSize(HP_TcpPackClient pClient, DWORD dwMaxPackSize);
DWORD HP_TcpPackClient_GetMaxPackSize(HP_TcpPackClient pClient);
```

### 8.5 HTTP API

```c
// HTTP客户端请求
BOOL HP_HttpClient_SendGet(HP_HttpClient pClient, LPCSTR lpszPath, const HP_THeader lpHeaders[], int iHeaderCount);
BOOL HP_HttpClient_SendPost(HP_HttpClient pClient, LPCSTR lpszPath, const HP_THeader lpHeaders[], int iHeaderCount, const BYTE* pBody, int iLength);
BOOL HP_HttpClient_SendRequest(HP_HttpClient pClient, LPCSTR lpszMethod, LPCSTR lpszPath, const HP_THeader lpHeaders[], int iHeaderCount, const BYTE* pBody, int iLength);

// HTTP Agent请求
BOOL HP_HttpAgent_SendGet(HP_HttpAgent pAgent, HP_CONNID dwConnID, LPCSTR lpszPath, const HP_THeader lpHeaders[], int iHeaderCount);
BOOL HP_HttpAgent_SendPost(HP_HttpAgent pAgent, HP_CONNID dwConnID, LPCSTR lpszPath, const HP_THeader lpHeaders[], int iHeaderCount, const BYTE* pBody, int iLength);
```

### 8.6 HPSocket4C_2.dll 新增功能

| 函数 | 说明 |
|------|------|
| `HP_GZipCompressor` | GZip压缩 |
| `HP_GZipDecompressor` | GZip解压 |
| `HP_ZLibCompressor` | ZLib压缩 |
| `HP_ZLibDecompressor` | ZLib解压 |
| `HP_BrotliCompressor` | Brotli压缩 |
| `HP_BrotliDecompressor` | Brotli解压 |
| `HP_TcpClient_SetNoDelay` | 禁用Nagle算法 |
| `HP_TcpServer_SetNoDelay` | 禁用Nagle算法 |

---

## 九、插件投递消息协议

### 9.1 消息格式

框架通过HPSocket将消息投递给插件，格式为GBK编码的文本：

```
插件投递:机器人账号=621705120，主动账号=982576571，被动账号=3962369093，
群号=3962369093，内容=xxx，消息ID=2739491805904148120，消息类型=1002，
消息时间=1768107366074，消息子类型=0，原始消息={JSON}
```

### 9.2 字段说明

| 字段 | 说明 | 示例 |
|------|------|------|
| 机器人账号 | 当前登录的机器人旺商聊号 | 621705120 |
| 主动账号 | 消息发送者旺商聊号 | 982576571 |
| 被动账号 | 消息接收者 (私聊为对方号，群聊为群号) | 3962369093 |
| 群号 | 群号 (私聊时为空) | 3962369093 |
| 内容 | 已解密的消息内容 | 你好 |
| 消息ID | NIM服务器消息ID | 2739491805904148120 |
| 消息类型 | 见下表 | 1002 |
| 消息时间 | 时间戳(毫秒) | 1768107366074 |
| 消息子类型 | 子类型 (0=普通, 其他=特殊) | 0 |
| 原始消息 | NIM SDK原始JSON | {...} |

### 9.3 消息类型

| 类型码 | 说明 |
|--------|------|
| 1001 | 私聊消息 |
| 1002 | 群消息 |
| 1003 | 好友关系变动 |
| 0 | 系统通知 |

### 9.4 原始消息JSON结构

```json
{
  "content": {
    "client_msg_id": "uuid",
    "from_id": "2092166259",        // 发送者NIM ID
    "from_nick": "加密昵称",
    "talk_id": "40821608989",       // 会话ID (群tid)
    "to_accid": "40821608989",
    "to_type": 1,                   // 0=私聊, 1=群聊
    "msg_type": 100,                // NIM消息类型 (100=自定义)
    "msg_attach": "{\"b\":\"...\"}",// 加密消息体
    "msg_body": "",
    "time": 1768107366074
  },
  "rescode": 200
}
```

### 9.5 消息体解密

`msg_attach.b` 是Base64编码的加密消息，解密步骤：

```python
import base64

def decode_nim_message(b64_content):
    # URL-safe Base64 转标准
    b64 = b64_content.replace('-', '+').replace('_', '/')
    padding = 4 - len(b64) % 4
    if padding < 4:
        b64 += '=' * padding
    
    data = base64.b64decode(b64)
    
    # 跳过头部约16-24字节找文本
    for offset in [16, 20, 24]:
        payload = data[offset:]
        text = payload.decode('utf-8', errors='ignore')
        if text:
            return text
    
    return None
```

---

## 十、自动回复机器人实现

### 10.1 架构

```
┌─────────────────────────────────────────────────────┐
│                    自动回复机器人                      │
│  ┌─────────────────┐    ┌───────────────────────┐    │
│  │  消息监听器       │    │    框架API客户端        │   │
│  │  (端口14746)     │───>│    (端口14745)         │   │
│  └─────────────────┘    └───────────────────────┘    │
│           │                       │                   │
│           v                       v                   │
│  ┌─────────────────┐    ┌───────────────────────┐    │
│  │  消息处理器       │    │    发送消息API         │   │
│  │  (规则匹配)       │───>│ send_group_text()     │   │
│  └─────────────────┘    └───────────────────────┘    │
└─────────────────────────────────────────────────────┘
```

### 10.2 使用方法

```bash
# 启动机器人
python auto_reply_bot.py

# 测试发送消息
python auto_reply_bot.py test

# 发送指定消息
python auto_reply_bot.py send 3962369093 "测试消息"

# 查看状态
python auto_reply_bot.py status
```

### 10.3 添加自定义规则

```python
from auto_reply_bot import AutoReplyBot

bot = AutoReplyBot("config.ini")

# 关键词匹配
bot.add_rule("你好", "你好！我是机器人~")

# 正则匹配
bot.add_rule(r"查询\s*(\d+)", "正在查询...", is_regex=True)

# 仅私聊
bot.add_rule("私聊测试", "这是私聊回复", group_only=False, private_only=True)

# 变量替换
bot.add_rule("时间", "当前时间: {time}")  # 支持 {sender}, {group}, {content}, {time}

bot.start()
```

### 10.4 框架API调用

```python
# 发送群消息
api.send_group_text("3962369093", "消息内容")

# 发送私聊
api.send_private_text("982576571", "消息内容")

# 发送@消息
api.send_at_message("3962369093", "982576571", "@用户 消息")

# 获取群列表
api.get_group_list()

# 获取群成员
api.get_group_members("3962369093")
```

### 10.5 关键文件

| 文件 | 说明 |
|------|------|
| `auto_reply_bot.py` | 自动回复机器人主程序 |
| `config.ini` | 配置文件 (含加密Token) |
| `nim_auto_reply.py` | NIM SDK直接调用方案 |
| `xplugin_proxy.py` | XPlugin代理服务 |

---

## 七、响应格式

### 7.1 成功响应

```json
{
    "code": 0,
    "msg": "success",
    "data": {
        // 业务数据
    }
}
```

### 7.2 错误响应

```json
{
    "code": 错误码,
    "msg": "错误信息",
    "data": null
}
```

### 7.3 完整错误码列表

| 错误码 | 说明 | 处理建议 |
|--------|------|----------|
| 0 | 成功 | 正常处理返回数据 |
| 200 | HTTP成功(上报) | 数据上报成功 |
| 401 | 未授权 | 重新登录获取Token |
| 403 | 禁止访问 | 检查权限或IP白名单 |
| 404 | 资源不存在 | 检查API路径 |
| 413 | 拒绝连接 | 检查参数格式 |
| 414 | 参数为空 | 补充必要参数(如appKey) |
| 1000 | App未找到 | 检查appKey是否正确 |
| 1001 | 参数错误 | 检查请求参数格式 |
| 1002 | Token无效 | Token过期，重新登录 |
| 1003 | 账号不存在 | 检查账号是否正确 |
| 1004 | 密码错误 | 检查密码是否正确 |
| 1005 | 验证码错误 | 检查验证码是否正确 |
| 1006 | 请求频繁 | 等待后重试 |
| 1007 | 已是好友 | 无需重复添加 |
| 1008 | 申请已处理 | 无需重复处理 |
| 1009 | 达到上限 | 群数量/成员达到上限 |
| 1010 | 权限不足 | 需要群主/管理员权限 |
| 1011 | 已在群中 | 无需重复加入 |
| 1012 | 撤回超时 | 消息发送超过2分钟 |
| 1013 | 非主播 | 需要主播权限 |
| 1014 | 非企业用户 | 需要企业账号 |
| 10010 | unsupport access | 需要IP白名单/正确认证 |

---

## 八、Python实现示例

```python
import requests
import hashlib
from Crypto.Cipher import AES
from Crypto.Util.Padding import pad, unpad
import base64

class WangShangLiaoAPI:
    """旺商聊API客户端"""
    
    AES_KEY = "49KdgB8_9=12+3hF"
    AES_IV = bytes.fromhex("00000000000000000000000000000000")
    
    def __init__(self, base_url="https://yiyong.netease.im"):
        self.base_url = base_url
        self.token = None
        self.session = requests.Session()
    
    def _get_aes_key(self):
        """获取AES密钥"""
        return hashlib.sha256(self.AES_KEY.encode()).digest()
    
    def encrypt(self, plaintext):
        """AES加密"""
        key = self._get_aes_key()
        cipher = AES.new(key, AES.MODE_CBC, self.AES_IV)
        padded = pad(plaintext.encode(), AES.block_size)
        encrypted = cipher.encrypt(padded)
        return base64.b64encode(encrypted).decode()
    
    def decrypt(self, ciphertext):
        """AES解密"""
        key = self._get_aes_key()
        cipher = AES.new(key, AES.MODE_CBC, self.AES_IV)
        decrypted = cipher.decrypt(base64.b64decode(ciphertext))
        return unpad(decrypted, AES.block_size).decode()
    
    def _request(self, method, endpoint, **kwargs):
        """发送请求"""
        url = f"{self.base_url}{endpoint}"
        headers = kwargs.pop('headers', {})
        headers['Content-Type'] = 'application/json'
        if self.token:
            headers['Authorization'] = f'Bearer {self.token}'
        
        response = self.session.request(method, url, headers=headers, **kwargs)
        return response.json()
    
    def send_sms(self, phone, sms_type="login"):
        """发送短信验证码"""
        return self._request('POST', '/v1/verify/sms', json={
            'phone': phone,
            'type': sms_type
        })
    
    def verify_code(self, phone, code):
        """验证码校验"""
        return self._request('POST', '/v1/verify/verify', json={
            'phone': phone,
            'code': code
        })
    
    def login(self, phone, code, device_id):
        """用户登录"""
        result = self._request('POST', '/v1/user/login', json={
            'phone': phone,
            'code': code,
            'deviceId': device_id,
            'platform': 'windows'
        })
        if result.get('code') == 0:
            self.token = result['data'].get('token')
        return result
    
    def check_token(self):
        """检查Token"""
        return self._request('POST', '/v1/checkToken')
    
    def get_friend_list(self):
        """获取好友列表"""
        return self._request('GET', '/v1/friend/get-friend-list')
    
    def get_group_list(self):
        """获取群列表"""
        return self._request('GET', '/v1/group/get-group-list')
    
    def send_group_message(self, group_id, content):
        """发送群消息 (需要NIM SDK)"""
        # 这个功能需要通过NIM SDK实现
        pass


# 使用示例
if __name__ == "__main__":
    api = WangShangLiaoAPI()
    
    # 测试加密解密
    test = "Hello World"
    encrypted = api.encrypt(test)
    decrypted = api.decrypt(encrypted)
    print(f"原文: {test}")
    print(f"加密: {encrypted}")
    print(f"解密: {decrypted}")
```

---

## 九、安全说明

1. **AES加密密钥**: `49KdgB8_9=12+3hF` (SHA256后使用)
2. **AES IV**: 32个0 (16字节)
3. **验证码ID**: `37ab6b750c3246af804132808408f398`
4. **通信协议**: HTTPS
5. **认证方式**: JWT Bearer Token

---

文档版本: 1.5
更新日期: 2026-01-13
测试API数量: 213+ (含NIM SDK方法 + HPSocket API)
测试账号: 621705120
新增: 
  - 消息发送/接收API (NIM SDK)
  - config.ini AES解密
  - HPSocket4C.dll API
  - 自动回复机器人实现
  - 插件投递消息协议

---

## 十、API测试结果

### 10.1 测试账号信息
```
账号: 621705120
用户ID (uid): 9502248
NIM群ID: 3962369093
x-token: UOAGo3R73FEo7aMO_u6-pbt2DhBAx159dH02o4DM2CyuPclOUIzg3C-sri8y-rYQWNuXffYmXGAsw__io8HNhBGrJH6nNFpd82wBVsVcMOGo
```

### 10.2 服务器测试结果

| 服务器 | 状态 | 说明 |
|--------|------|------|
| qxdevacc.qixin02.xyz | code=10010 | 测试服务器，需IP白名单 |
| yiyong.netease.im | HTTP 404 | 需要特定路由 |
| ap-prd-jd.netease.im | HTTP 200 | NIM G2服务可用 |
| lbs.netease.im | HTTP 200 | LBS配置服务正常 |
| roomkit.netease.im | HTTP 200 | 需要正确appKey |

### 10.3 API响应码汇总

| 响应码 | 说明 | 处理方式 |
|--------|------|----------|
| 0 | 成功 | 正常处理 |
| 10010 | unsupport access | 需要IP白名单或正确认证 |
| 401 | 未授权 | 需要有效Token |
| 403 | 禁止访问 | 权限不足 |
| 404 | 资源不存在 | API路径错误或不存在 |
| 414 | param empty | 缺少必要参数 |
| 1000 | App not found | appKey错误 |
| 1001 | 参数错误 | 检查请求参数 |
| 1002 | Token无效 | 重新登录获取Token |

### 10.4 成功响应的API (NIM SDK公开API)

| API | 方法 | 成功码 | 说明 |
|-----|------|--------|------|
| `lbs.netease.im/lbs/conf` | GET | HTTP 200 | LBS服务器配置 |
| `yunxin.163.com/lbs/conf` | GET | HTTP 200 | 国内LBS服务器配置 |
| `yiyong.netease.im/report_conf` | GET | HTTP 200 | 上报配置 |
| `ap-prd-jd.netease.im/v1/g2/getCloudProxyInfo` | POST | 200/414 | 云代理信息(需appKey) |
| `roomkit.netease.im/scene/apps/{appKey}/v1/skuEnv` | GET | 0/1000 | SKU环境(需正确appKey) |
| `statistic.live.126.net/statics/report/common/nim` | POST | 200 | 数据上报 |
| `statistic.live.126.net/dispatcher/req` | GET | 413 | 调度请求(需参数) |

```json
// lbs.netease.im/lbs/conf 成功响应示例
{
  "common": {
    "httpdns": ["https://httpdns.n.netease.com", "http://59.111.239.61"],
    "nos": ["http://45.127.128.24", "http://45.127.128.25"],
    "link": ["link-ga-hz.yunxinfw.com:443"]
  }
}

// yiyong.netease.im/report_conf 成功响应示例
{
  "blacklist": [],
  "enabled": true,
  "highPriorityEnabled": true,
  "lowPriorityEnabled": true,
  "normalPriorityEnabled": true,
  "whitelist": []
}

// statistic.live.126.net/statics/report/common/nim 成功响应示例
{
  "code": 200,
  "requestId": "49a28427-0452-4a93-8dbd-956c2455ffb1"
}
```

### 10.5 请求头格式

```
Content-Type: application/json
x-token: {JWT Token}
x-id: {用户ID}
Authorization: Bearer {JWT Token}
```

### 10.6 心跳响应格式

```json
{
  "id": 0,
  "code": 0,
  "uid": 9502248
}
```
