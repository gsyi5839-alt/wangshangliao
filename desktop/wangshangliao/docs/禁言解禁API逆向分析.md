# 旺商聊禁言/解禁 API 逆向分析

## 1. 旺商聊后端 API (HTTP)

从 `groupSetting-1ba0d372.js` 逆向分析得到：

### 1.1 全群禁言 API
```javascript
setGroupMute: async (e) => await o.handleReq(o.url.setGroupMute, e)
```

**请求参数**:
```json
{
  "groupId": "群ID",
  "groupCloudId": "云群ID",
  "muteMode": "MUTE_MEMBER" | "NONE"  // MUTE_MEMBER=全员禁言, NONE=解除禁言
}
```

### 1.2 单成员禁言 API
```javascript
setMemberMute: (e) => o.handleReq(o.url.setMemberMute, e)
```

**请求参数**:
```json
{
  "groupId": "群ID",
  "nimId": "用户NIM ID",
  "muteSeconds": 禁言秒数  // 0 = 永久禁言, >0 = 指定时间
}
```

### 1.3 取消成员禁言 API
```javascript
memberNuteCancel: (e) => o.handleReq(o.url.memberNuteCancel, e)
```

**请求参数**:
```json
{
  "groupId": "群ID",
  "nimId": "用户NIM ID"
}
```

## 2. NIM SDK API (底层)

旺商聊基于网易云信 NIM SDK，底层禁言 API：

### 2.1 全群禁言
```javascript
window.nim.updateTeam({
  teamId: "群云ID",
  muteType: 1,  // 0=取消禁言, 1=普通成员禁言(管理员可说话), 2=全员禁言
  done: (err, team) => {}
});
```

### 2.2 成员禁言
```javascript
window.nim.muteTeamMember({
  teamId: "群云ID",
  account: "用户NIM ID",
  mute: true,  // true=禁言, false=解禁
  done: (err, data) => {}
});
```

### 2.3 群禁言模式枚举
```javascript
groupMuteMode:
- "NONE"        // 无禁言
- "MUTE_MEMBER" // 普通成员禁言(管理员可说话)
- "MUTE_ALL"    // 全员禁言
```

## 3. ZCG 旧程序禁言实现

从 ZCG 程序分析，它使用类似的 API 调用模式：

### 3.1 全体禁言
通过 HPSocket 发送消息到旺商聊框架执行 NIM SDK 调用。

### 3.2 消息格式 (HPSocket)
```json
{
  "cmd": "muteAll",
  "groupId": "群ID",
  "mute": true/false
}
```

或

```json
{
  "cmd": "muteUser",
  "groupId": "群ID",
  "userId": "用户旺旺号",
  "nimId": "用户NIM ID",
  "mute": true/false,
  "duration": 秒数  // 可选
}
```

## 4. C# 实现代码示例

### 4.1 全群禁言
```csharp
/// <summary>
/// 全群禁言
/// </summary>
/// <param name="groupCloudId">群云ID</param>
/// <param name="mute">true=禁言, false=解禁</param>
public async Task<bool> MuteAllMembersAsync(string groupCloudId, bool mute)
{
    // 方法1: 通过 CDP 执行 JS
    string script = $@"
        window.nim.updateTeam({{
            teamId: '{groupCloudId}',
            muteType: {(mute ? 1 : 0)},
            done: (err, team) => {{
                console.log('禁言结果:', err, team);
            }}
        }});
    ";
    return await ExecuteJavaScriptAsync(script);
    
    // 方法2: 通过旺商聊 HTTP API
    // var response = await _httpClient.PostAsync("/v1/group/set-group-mute", 
    //     new { groupId = groupId, muteMode = mute ? "MUTE_MEMBER" : "NONE" });
}

/// <summary>
/// 单个成员禁言
/// </summary>
public async Task<bool> MuteMemberAsync(string groupCloudId, string memberNimId, bool mute)
{
    string script = $@"
        window.nim.muteTeamMember({{
            teamId: '{groupCloudId}',
            account: '{memberNimId}',
            mute: {mute.ToString().ToLower()},
            done: (err, data) => {{
                console.log('成员禁言结果:', err, data);
            }}
        }});
    ";
    return await ExecuteJavaScriptAsync(script);
}
```

### 4.2 通过 FrameworkServer 发送禁言命令
```csharp
/// <summary>
/// 发送全群禁言命令
/// </summary>
public async Task SendMuteAllCommand(long groupId, bool mute)
{
    var command = new
    {
        cmd = "muteAll",
        groupId = groupId.ToString(),
        mute = mute
    };
    
    string json = JsonConvert.SerializeObject(command);
    await SendToFrameworkAsync(json);
}

/// <summary>
/// 发送单成员禁言命令
/// </summary>
public async Task SendMuteMemberCommand(long groupId, long userId, string nimId, bool mute, int durationSeconds = 0)
{
    var command = new
    {
        cmd = "muteUser",
        groupId = groupId.ToString(),
        userId = userId.ToString(),
        nimId = nimId,
        mute = mute,
        duration = durationSeconds
    };
    
    string json = JsonConvert.SerializeObject(command);
    await SendToFrameworkAsync(json);
}
```

## 5. 禁言状态检查

```javascript
// 检查用户是否被禁言
checkIsMute: async (h) => {
    if (!h.checked && h?.groupId) {
        const x = await getGroupInfo(h?.groupId);
        if (!x.error) {
            const { me, groupInfo } = x;
            // 检查是否全群禁言
            const isGroupMuted = groupInfo.groupMuteMode === "MUTE_MEMBER";
            // 检查当前用户角色
            if (me.role === "GROUP_ROLE_OWNER" || me.role === "GROUP_ROLE_MANAGER") {
                return false;  // 群主和管理员不受禁言影响
            }
            return isGroupMuted || me.muted;
        }
    }
    return false;
}
```

## 6. 关键字段说明

| 字段 | 说明 |
|------|------|
| `groupId` | 旺商聊内部群ID |
| `groupCloudId` | NIM 群云ID (teamId) |
| `nimId` | 用户在 NIM 中的账号 |
| `muteType` | 0=解禁, 1=普通成员禁言, 2=全员禁言 |
| `groupMuteMode` | NONE/MUTE_MEMBER/MUTE_ALL |
| `muted` | 成员禁言状态 (true/false) |

## 7. 注意事项

1. **权限要求**: 只有群主和管理员可以执行禁言操作
2. **禁言类型**: 
   - `muteType=1`: 普通成员禁言，管理员可发言
   - `muteType=2`: 全员禁言，包括管理员
3. **成员禁言**: 可以针对单个成员禁言，不影响其他人
4. **禁言时长**: 可以设置禁言时长，0表示永久禁言
