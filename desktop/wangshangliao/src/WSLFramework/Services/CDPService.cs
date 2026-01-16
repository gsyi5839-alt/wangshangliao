using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using WSLFramework.Utils;

namespace WSLFramework.Services
{
    /// <summary>
    /// CDP (Chrome DevTools Protocol) 服务
    /// 用于从旺商聊 Electron 应用获取真实数据
    /// </summary>
    public class CDPService
    {
        private static CDPService _instance;
        public static CDPService Instance => _instance ?? (_instance = new CDPService());

        /// <summary>CDP 调试端口</summary>
        public int DebugPort { get; set; } = 9222;

        /// <summary>WebSocket 端点</summary>
        public string WebSocketUrl { get; private set; }

        /// <summary>是否已连接</summary>
        public bool IsConnected { get; private set; }

        /// <summary>日志事件</summary>
        public event Action<string> OnLog;

        private readonly JavaScriptSerializer _serializer;

        private CDPService()
        {
            _serializer = new JavaScriptSerializer { MaxJsonLength = int.MaxValue };
        }

        private void Log(string message)
        {
            OnLog?.Invoke($"[CDP] {message}");
            Logger.Info($"[CDPService] {message}");
        }

        /// <summary>
        /// 检查旺商聊是否在调试模式运行
        /// </summary>
        public async Task<bool> CheckConnectionAsync()
        {
            try
            {
                var url = $"http://127.0.0.1:{DebugPort}/json";
                var request = WebRequest.CreateHttp(url);
                request.Timeout = 3000;

                using (var response = await request.GetResponseAsync())
                using (var stream = response.GetResponseStream())
                using (var reader = new StreamReader(stream))
                {
                    var json = await reader.ReadToEndAsync();
                    var targets = _serializer.Deserialize<List<CDPTarget>>(json);

                    if (targets != null && targets.Count > 0)
                    {
                        var page = targets.Find(t => t.type == "page" && !t.url.Contains("login"));
                        if (page != null)
                        {
                            WebSocketUrl = page.webSocketDebuggerUrl;
                            IsConnected = true;
                            Log($"已连接: {page.url}");
                            return true;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log($"连接失败: {ex.Message}");
            }

            IsConnected = false;
            return false;
        }

        /// <summary>
        /// 获取当前登录用户信息
        /// </summary>
        public async Task<WslUserInfo> GetCurrentUserAsync()
        {
            try
            {
                if (!IsConnected && !await CheckConnectionAsync())
                {
                    Log("未连接到旺商聊");
                    return null;
                }

                var js = @"
(function() {
    var result = { success: false };
    try {
        var appEl = document.getElementById('app');
        if (appEl && appEl.__vue_app__) {
            var app = appEl.__vue_app__;
            var pinia = app.config.globalProperties.$pinia;
            if (pinia && pinia._s) {
                var appStore = pinia._s.get('app');
                if (appStore && appStore.$state && appStore.$state.userInfo) {
                    var u = appStore.$state.userInfo;
                    result.success = true;
                    result.uid = u.uid;
                    // wwid 实际上是 uid，accountId 是用户看到的账号ID
                    result.wwid = String(u.uid || '');
                    result.nickname = u.nickName || u.nick || u.name || '';
                    result.nimId = String(u.nimId || '');
                    result.nimToken = u.nimToken || '';
                    result.avatar = u.avatar || '';
                    // accountId 是用户看到的账号（如 82840376）
                    result.accountId = String(u.accountId || '');
                    result.phone = u.phone ? u.phone.nationalNumber : '';
                    result.jwtToken = u.jwtToken || '';
                    result.accountState = u.accountState || '';
                    result.vipLevel = u.level || u.vipLevel || '';
                }
            }
        }
    } catch(e) {
        result.error = e.message;
    }
    return JSON.stringify(result);
})();";

                var resultJson = await ExecuteJsAsync(js);
                if (!string.IsNullOrEmpty(resultJson))
                {
                    var data = _serializer.Deserialize<Dictionary<string, object>>(resultJson);
                    if (data != null && data.ContainsKey("success") && (bool)data["success"])
                    {
                        return new WslUserInfo
                        {
                            Uid = data.ContainsKey("uid") ? Convert.ToInt64(data["uid"]) : 0,
                            Wwid = data.ContainsKey("wwid") ? data["wwid"]?.ToString() : "",
                            Nickname = data.ContainsKey("nickname") ? data["nickname"]?.ToString() : "",
                            NimId = data.ContainsKey("nimId") ? data["nimId"]?.ToString() : "",
                            NimToken = data.ContainsKey("nimToken") ? data["nimToken"]?.ToString() : "",
                            Avatar = data.ContainsKey("avatar") ? data["avatar"]?.ToString() : "",
                            AccountId = data.ContainsKey("accountId") ? data["accountId"]?.ToString() : "",
                            Phone = data.ContainsKey("phone") ? data["phone"]?.ToString() : "",
                            JwtToken = data.ContainsKey("jwtToken") ? data["jwtToken"]?.ToString() : "",
                            AccountState = data.ContainsKey("accountState") ? data["accountState"]?.ToString() : "",
                            VipLevel = data.ContainsKey("vipLevel") ? data["vipLevel"]?.ToString() : ""
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                Log($"获取用户信息失败: {ex.Message}");
            }

            return null;
        }

        /// <summary>
        /// 获取群列表
        /// </summary>
        public async Task<List<WslGroupInfo>> GetGroupListAsync()
        {
            var groups = new List<WslGroupInfo>();

            try
            {
                if (!IsConnected && !await CheckConnectionAsync())
                {
                    Log("未连接到旺商聊");
                    return groups;
                }

                var js = @"
(function() {
    var result = { success: false, groups: [] };
    try {
        var appEl = document.getElementById('app');
        if (appEl && appEl.__vue_app__) {
            var app = appEl.__vue_app__;
            var pinia = app.config.globalProperties.$pinia;
            if (pinia && pinia._s) {
                var appStore = pinia._s.get('app');
                
                // 尝试从 groupDetailMap 获取精确的群成员数量
                var groupDetailMap = {};
                if (appStore && appStore.$state && appStore.$state.groupDetailMap) {
                    var detailMap = appStore.$state.groupDetailMap;
                    for (var key in detailMap) {
                        var detail = detailMap[key];
                        if (detail && detail.members) {
                            // 精确成员数量 = members 数组长度
                            groupDetailMap[key] = {
                                memberCount: Array.isArray(detail.members) ? detail.members.length : 0,
                                memberNum: detail.groupMemberNum || detail.memberNum || 0
                            };
                        }
                    }
                }
                
                if (appStore && appStore.$state && appStore.$state.groupList) {
                    var gl = appStore.$state.groupList;
                    
                    // 提取群信息的函数
                    var extractGroup = function(g, role) {
                        // groupAccount 是用户看到的群号（如 3962369093）
                        // groupId 是内部 ID（如 1176721）
                        var internalId = String(g.groupId);
                        
                        // 优先使用 groupDetailMap 中的精确成员数
                        var memberNum = g.groupMemberNum || g.memberNum || 0;
                        if (groupDetailMap[internalId] && groupDetailMap[internalId].memberCount > 0) {
                            memberNum = groupDetailMap[internalId].memberCount;
                        }
                        
                        return {
                            groupId: String(g.groupAccount || g.groupId || g.id),
                            internalId: internalId,
                            cloudId: String(g.groupCloudId || ''),
                            name: g.groupName || g.name || '',
                            memberNum: memberNum,
                            role: role,
                            avatar: g.groupAvatar || g.avatar || '',
                            myRole: g.me ? g.me.role : '',
                            myNickName: g.me ? g.me.selfNickName : ''
                        };
                    };
                    
                    // 我创建的群 (owner)
                    if (gl.owner && Array.isArray(gl.owner)) {
                        gl.owner.forEach(function(g) {
                            result.groups.push(extractGroup(g, 'owner'));
                        });
                    }
                    
                    // 我加入的群 (member)
                    if (gl.member && Array.isArray(gl.member)) {
                        gl.member.forEach(function(g) {
                            result.groups.push(extractGroup(g, 'member'));
                        });
                    }
                    
                    result.success = true;
                }
            }
        }
    } catch(e) {
        result.error = e.message;
    }
    return JSON.stringify(result);
})();";

                var resultJson = await ExecuteJsAsync(js);
                if (!string.IsNullOrEmpty(resultJson))
                {
                    var data = _serializer.Deserialize<Dictionary<string, object>>(resultJson);
                    if (data != null && data.ContainsKey("success") && (bool)data["success"])
                    {
                        var groupsArray = data["groups"] as System.Collections.ArrayList;
                        if (groupsArray != null)
                        {
                            foreach (Dictionary<string, object> g in groupsArray)
                            {
                                groups.Add(new WslGroupInfo
                                {
                                    GroupId = g.ContainsKey("groupId") ? g["groupId"]?.ToString() : "",
                                    InternalId = g.ContainsKey("internalId") ? g["internalId"]?.ToString() : "",
                                    CloudId = g.ContainsKey("cloudId") ? g["cloudId"]?.ToString() : "",
                                    Name = g.ContainsKey("name") ? g["name"]?.ToString() : "",
                                    MemberNum = g.ContainsKey("memberNum") ? Convert.ToInt32(g["memberNum"]) : 0,
                                    Role = g.ContainsKey("role") ? g["role"]?.ToString() : "member",
                                    Avatar = g.ContainsKey("avatar") ? g["avatar"]?.ToString() : "",
                                    MyRole = g.ContainsKey("myRole") ? g["myRole"]?.ToString() : "",
                                    MyNickName = g.ContainsKey("myNickName") ? g["myNickName"]?.ToString() : ""
                                });
                            }
                        }
                    }
                }

                Log($"获取到 {groups.Count} 个群");
                
                // 尝试获取精确的群成员数量
                foreach (var group in groups)
                {
                    var exactCount = await GetExactMemberCountAsync(group.InternalId);
                    if (exactCount > 0)
                    {
                        group.MemberNum = exactCount;
                    }
                }
            }
            catch (Exception ex)
            {
                Log($"获取群列表失败: {ex.Message}");
            }

            return groups;
        }

        /// <summary>
        /// 精确获取群成员数量（从 Pinia groupDetailMap 或 NIM teams 缓存）
        /// </summary>
        public async Task<int> GetExactMemberCountAsync(string teamId)
        {
            try
            {
                if (string.IsNullOrEmpty(teamId)) return 0;
                if (!IsConnected && !await CheckConnectionAsync()) return 0;

                // 从多个数据源获取群成员数量
                var js = $@"
(function() {{
    var result = {{ success: false, memberNum: 0 }};
    try {{
        var teamId = '{teamId}';
        var appEl = document.getElementById('app');
        
        if (appEl && appEl.__vue_app__) {{
            var app = appEl.__vue_app__;
            var pinia = app.config.globalProperties.$pinia;
            
            if (pinia && pinia._s) {{
                var appStore = pinia._s.get('app');
                
                // 方法1: 从 groupDetailMap 获取（已加载过群详情的群）
                if (appStore && appStore.$state && appStore.$state.groupDetailMap) {{
                    var detail = appStore.$state.groupDetailMap[teamId];
                    if (detail && detail.members && Array.isArray(detail.members)) {{
                        result.success = true;
                        result.memberNum = detail.members.length;
                        result.source = 'groupDetailMap';
                        return JSON.stringify(result);
                    }}
                }}
                
                // 方法2: 从 groupMemberMap 获取
                if (appStore && appStore.$state && appStore.$state.groupMemberMap) {{
                    var members = appStore.$state.groupMemberMap[teamId];
                    if (members && Array.isArray(members)) {{
                        result.success = true;
                        result.memberNum = members.length;
                        result.source = 'groupMemberMap';
                        return JSON.stringify(result);
                    }}
                }}
            }}
        }}
        
        // 方法3: 从 NIM SDK teams 缓存获取
        if (window.nim && window.nim.teams && window.nim.teams[teamId]) {{
            var team = window.nim.teams[teamId];
            result.success = true;
            result.memberNum = team.memberNum || 0;
            result.source = 'nimTeams';
        }}
    }} catch(e) {{
        result.error = e.message;
    }}
    return JSON.stringify(result);
}})();";

                var resultJson = await ExecuteJsAsync(js);
                if (!string.IsNullOrEmpty(resultJson))
                {
                    var data = _serializer.Deserialize<Dictionary<string, object>>(resultJson);
                    if (data != null && data.ContainsKey("success") && (bool)data["success"])
                    {
                        var memberNum = data.ContainsKey("memberNum") ? Convert.ToInt32(data["memberNum"]) : 0;
                        if (memberNum > 0)
                        {
                            var source = data.ContainsKey("source") ? data["source"]?.ToString() : "";
                            Log($"获取群 {teamId} 成员数量: {memberNum} (来源: {source})");
                        }
                        return memberNum;
                    }
                }
            }
            catch (Exception ex)
            {
                Log($"获取群成员数量失败: {ex.Message}");
            }
            return 0;
        }

        /// <summary>
        /// 获取好友列表
        /// </summary>
        public async Task<List<WslFriendInfo>> GetFriendListAsync()
        {
            var friends = new List<WslFriendInfo>();

            try
            {
                if (!IsConnected && !await CheckConnectionAsync())
                {
                    Log("未连接到旺商聊");
                    return friends;
                }

                var js = @"
(function() {
    var result = { success: false, friends: [], blackList: [] };
    try {
        var appEl = document.getElementById('app');
        if (appEl && appEl.__vue_app__) {
            var pinia = appEl.__vue_app__.config.globalProperties.$pinia;
            if (pinia && pinia._s) {
                var appStore = pinia._s.get('app');
                if (appStore && appStore.$state && appStore.$state.friendList) {
                    var fl = appStore.$state.friendList;
                    
                    // 好友列表
                    if (fl.friendList && Array.isArray(fl.friendList)) {
                        fl.friendList.forEach(function(f) {
                            result.friends.push({
                                uid: f.uid,
                                nickname: f.nickName || f.name || '',
                                markName: f.markName || '',
                                avatar: f.avatar || '',
                                accountId: String(f.accountId || ''),
                                nimId: String(f.nimId || ''),
                                onlineState: f.onlineState || '',
                                state: f.state || '',
                                vipLevel: f.vipLevel || '',
                                initialPinyin: f.initialPinyin || ''
                            });
                        });
                    }
                    
                    // 黑名单
                    if (fl.blackList && Array.isArray(fl.blackList)) {
                        fl.blackList.forEach(function(f) {
                            result.blackList.push({
                                uid: f.uid,
                                nickname: f.nickName || f.name || ''
                            });
                        });
                    }
                    
                    result.success = true;
                }
            }
        }
    } catch(e) {
        result.error = e.message;
    }
    return JSON.stringify(result);
})();";

                var resultJson = await ExecuteJsAsync(js);
                if (!string.IsNullOrEmpty(resultJson))
                {
                    var data = _serializer.Deserialize<Dictionary<string, object>>(resultJson);
                    if (data != null && data.ContainsKey("success") && (bool)data["success"])
                    {
                        var friendsArray = data["friends"] as System.Collections.ArrayList;
                        if (friendsArray != null)
                        {
                            foreach (Dictionary<string, object> f in friendsArray)
                            {
                                friends.Add(new WslFriendInfo
                                {
                                    Uid = f.ContainsKey("uid") ? Convert.ToInt64(f["uid"]) : 0,
                                    Nickname = f.ContainsKey("nickname") ? f["nickname"]?.ToString() : "",
                                    MarkName = f.ContainsKey("markName") ? f["markName"]?.ToString() : "",
                                    Avatar = f.ContainsKey("avatar") ? f["avatar"]?.ToString() : "",
                                    AccountId = f.ContainsKey("accountId") ? f["accountId"]?.ToString() : "",
                                    NimId = f.ContainsKey("nimId") ? f["nimId"]?.ToString() : "",
                                    OnlineState = f.ContainsKey("onlineState") ? f["onlineState"]?.ToString() : "",
                                    State = f.ContainsKey("state") ? f["state"]?.ToString() : "",
                                    VipLevel = f.ContainsKey("vipLevel") ? f["vipLevel"]?.ToString() : "",
                                    InitialPinyin = f.ContainsKey("initialPinyin") ? f["initialPinyin"]?.ToString() : ""
                                });
                            }
                        }
                    }
                }

                Log($"获取到 {friends.Count} 个好友");
            }
            catch (Exception ex)
            {
                Log($"获取好友列表失败: {ex.Message}");
            }

            return friends;
        }

        /// <summary>
        /// 获取群成员列表
        /// </summary>
        /// <param name="groupInternalId">群内部ID（如 1176721）</param>
        public async Task<List<WslGroupMemberInfo>> GetGroupMembersAsync(string groupInternalId)
        {
            var members = new List<WslGroupMemberInfo>();

            try
            {
                if (!IsConnected && !await CheckConnectionAsync())
                {
                    Log("未连接到旺商聊");
                    return members;
                }

                var js = $@"
(function() {{
    var result = {{ success: false, members: [] }};
    try {{
        var appEl = document.getElementById('app');
        if (appEl && appEl.__vue_app__) {{
            var pinia = appEl.__vue_app__.config.globalProperties.$pinia;
            if (pinia && pinia._s) {{
                var sdkStore = pinia._s.get('sdk');
                if (sdkStore && sdkStore.$state && sdkStore.$state.groupMembersMap) {{
                    var membersMap = sdkStore.$state.groupMembersMap;
                    var groupData = membersMap['{groupInternalId}'];
                    
                    if (groupData && groupData.groupMemberInfo) {{
                        groupData.groupMemberInfo.forEach(function(m) {{
                            result.members.push({{
                                userId: m.userId,
                                userNick: m.userNick || '',
                                groupMemberNick: m.groupMemberNick || '',
                                userAvatar: m.userAvatar || '',
                                accountId: String(m.accountId || ''),
                                nimId: String(m.nimId || ''),
                                groupRole: m.groupRole || 'GROUP_ROLE_MEMBER',
                                vipLevel: m.vipLevel || '',
                                initialPinyin: m.initialPinyin || '',
                                accountState: m.accountState || ''
                            }});
                        }});
                        result.success = true;
                    }}
                }}
            }}
        }}
    }} catch(e) {{
        result.error = e.message;
    }}
    return JSON.stringify(result);
}})();";

                var resultJson = await ExecuteJsAsync(js);
                if (!string.IsNullOrEmpty(resultJson))
                {
                    var data = _serializer.Deserialize<Dictionary<string, object>>(resultJson);
                    if (data != null && data.ContainsKey("success") && (bool)data["success"])
                    {
                        var membersArray = data["members"] as System.Collections.ArrayList;
                        if (membersArray != null)
                        {
                            foreach (Dictionary<string, object> m in membersArray)
                            {
                                members.Add(new WslGroupMemberInfo
                                {
                                    UserId = m.ContainsKey("userId") ? Convert.ToInt64(m["userId"]) : 0,
                                    UserNick = m.ContainsKey("userNick") ? m["userNick"]?.ToString() : "",
                                    GroupMemberNick = m.ContainsKey("groupMemberNick") ? m["groupMemberNick"]?.ToString() : "",
                                    UserAvatar = m.ContainsKey("userAvatar") ? m["userAvatar"]?.ToString() : "",
                                    AccountId = m.ContainsKey("accountId") ? m["accountId"]?.ToString() : "",
                                    NimId = m.ContainsKey("nimId") ? m["nimId"]?.ToString() : "",
                                    GroupRole = m.ContainsKey("groupRole") ? m["groupRole"]?.ToString() : "GROUP_ROLE_MEMBER",
                                    VipLevel = m.ContainsKey("vipLevel") ? m["vipLevel"]?.ToString() : "",
                                    InitialPinyin = m.ContainsKey("initialPinyin") ? m["initialPinyin"]?.ToString() : "",
                                    AccountState = m.ContainsKey("accountState") ? m["accountState"]?.ToString() : ""
                                });
                            }
                        }
                    }
                }

                Log($"获取到群 {groupInternalId} 的 {members.Count} 个成员");
            }
            catch (Exception ex)
            {
                Log($"获取群成员失败: {ex.Message}");
            }

            return members;
        }

        /// <summary>
        /// 执行 JS 代码（通过 HTTP 接口）
        /// </summary>
        private async Task<string> ExecuteJsAsync(string jsCode)
        {
            try
            {
                // 使用简单的 HTTP 轮询方式获取页面信息
                var targetsUrl = $"http://127.0.0.1:{DebugPort}/json";
                var request = WebRequest.CreateHttp(targetsUrl);
                request.Timeout = 5000;

                CDPTarget pageTarget = null;

                using (var response = await request.GetResponseAsync())
                using (var stream = response.GetResponseStream())
                using (var reader = new StreamReader(stream))
                {
                    var json = await reader.ReadToEndAsync();
                    var targets = _serializer.Deserialize<List<CDPTarget>>(json);
                    pageTarget = targets?.Find(t => t.type == "page" && !t.url.Contains("login"));
                }

                if (pageTarget == null)
                {
                    Log("未找到旺商聊页面");
                    return null;
                }

                // 使用 WebSocket 执行 JS
                return await ExecuteJsViaWebSocketAsync(pageTarget.webSocketDebuggerUrl, jsCode);
            }
            catch (Exception ex)
            {
                Log($"执行JS失败: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 通过 WebSocket 执行 JS
        /// </summary>
        private async Task<string> ExecuteJsViaWebSocketAsync(string wsUrl, string jsCode)
        {
            try
            {
                using (var ws = new System.Net.WebSockets.ClientWebSocket())
                {
                    var cts = new System.Threading.CancellationTokenSource(10000);
                    await ws.ConnectAsync(new Uri(wsUrl), cts.Token);

                    // 发送执行命令
                    var command = new
                    {
                        id = 1,
                        method = "Runtime.evaluate",
                        @params = new
                        {
                            expression = jsCode,
                            returnByValue = true
                        }
                    };

                    var cmdJson = _serializer.Serialize(command);
                    var sendBuffer = Encoding.UTF8.GetBytes(cmdJson);
                    await ws.SendAsync(new ArraySegment<byte>(sendBuffer), 
                        System.Net.WebSockets.WebSocketMessageType.Text, true, cts.Token);

                    // 接收结果
                    var receiveBuffer = new byte[65536];
                    var result = await ws.ReceiveAsync(new ArraySegment<byte>(receiveBuffer), cts.Token);
                    var responseJson = Encoding.UTF8.GetString(receiveBuffer, 0, result.Count);

                    await ws.CloseAsync(System.Net.WebSockets.WebSocketCloseStatus.NormalClosure, "", 
                        System.Threading.CancellationToken.None);

                    // 解析结果
                    var response = _serializer.Deserialize<Dictionary<string, object>>(responseJson);
                    if (response != null && response.ContainsKey("result"))
                    {
                        var resultObj = response["result"] as Dictionary<string, object>;
                        if (resultObj != null && resultObj.ContainsKey("result"))
                        {
                            var innerResult = resultObj["result"] as Dictionary<string, object>;
                            if (innerResult != null && innerResult.ContainsKey("value"))
                            {
                                return innerResult["value"]?.ToString();
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log($"WebSocket执行失败: {ex.Message}");
            }

            return null;
        }

        #region 群管理操作 - 踢人/禁言/撤回

        /// <summary>
        /// 踢出群成员
        /// 重要：需要使用 nimId (如 1948408648)，而不是 uid/wwid
        /// </summary>
        /// <param name="teamInternalId">群内部ID（如 1176721）</param>
        /// <param name="nimAccountId">成员NIM账号（如 1948408648）</param>
        public async Task<(bool Success, string Message)> KickMemberAsync(string teamInternalId, string nimAccountId)
        {
            try
            {
                if (!IsConnected && !await CheckConnectionAsync())
                {
                    return (false, "未连接到旺商聊");
                }

                if (string.IsNullOrEmpty(teamInternalId) || string.IsNullOrEmpty(nimAccountId))
                {
                    return (false, "参数不能为空");
                }

                var js = $@"
(async function() {{
    var result = {{ success: false, message: '' }};
    try {{
        if (!window.nim) {{
            result.message = 'NIM SDK not found';
            return JSON.stringify(result);
        }}
        
        var teamId = '{teamInternalId}';
        var account = '{nimAccountId}';
        
        // 尝试不同的踢人方法
        var methods = ['removeTeamMembers', 'kickTeamMembers'];
        
        for (var i = 0; i < methods.length; i++) {{
            var fn = window.nim[methods[i]];
            if (typeof fn !== 'function') continue;
            
            try {{
                var r = await new Promise(function(resolve) {{
                    fn.call(window.nim, {{
                        teamId: teamId,
                        accounts: [account],
                        done: function(err) {{
                            if (err) resolve({{ ok: false, err: err.message || String(err) }});
                            else resolve({{ ok: true }});
                        }}
                    }});
                    setTimeout(function() {{ resolve({{ ok: false, err: 'Timeout' }}); }}, 8000);
                }});
                
                if (r.ok) {{
                    result.success = true;
                    result.message = '踢出成功';
                    return JSON.stringify(result);
                }}
            }} catch(e) {{}}
        }}
        
        result.message = '踢出失败';
    }} catch(e) {{
        result.message = e.message;
    }}
    return JSON.stringify(result);
}})();";

                var resultJson = await ExecuteJsAsync(js);
                if (!string.IsNullOrEmpty(resultJson))
                {
                    var data = _serializer.Deserialize<Dictionary<string, object>>(resultJson);
                    if (data != null)
                    {
                        var success = data.ContainsKey("success") && (bool)data["success"];
                        var message = data.ContainsKey("message") ? data["message"]?.ToString() : "";
                        Log($"踢人结果: {message}");
                        return (success, message);
                    }
                }
            }
            catch (Exception ex)
            {
                Log($"踢人异常: {ex.Message}");
                return (false, ex.Message);
            }

            return (false, "执行失败");
        }

        /// <summary>
        /// 禁言群成员
        /// </summary>
        /// <param name="teamInternalId">群内部ID</param>
        /// <param name="nimAccountId">成员NIM账号</param>
        /// <param name="muteDuration">禁言时长（秒），0表示解除禁言</param>
        public async Task<(bool Success, string Message)> MuteMemberAsync(string teamInternalId, string nimAccountId, int muteDuration)
        {
            try
            {
                if (!IsConnected && !await CheckConnectionAsync())
                {
                    return (false, "未连接到旺商聊");
                }

                var js = $@"
(async function() {{
    var result = {{ success: false, message: '' }};
    try {{
        if (!window.nim) {{
            result.message = 'NIM SDK not found';
            return JSON.stringify(result);
        }}
        
        var teamId = '{teamInternalId}';
        var account = '{nimAccountId}';
        var mute = {(muteDuration > 0 ? "true" : "false")};
        var duration = {muteDuration};
        
        // 尝试禁言方法
        var fn = window.nim.updateMuteStateInTeam || window.nim.muteTeamMember;
        if (typeof fn !== 'function') {{
            result.message = '禁言方法不可用';
            return JSON.stringify(result);
        }}
        
        var r = await new Promise(function(resolve) {{
            fn.call(window.nim, {{
                teamId: teamId,
                account: account,
                mute: mute,
                duration: duration,
                done: function(err) {{
                    if (err) resolve({{ ok: false, err: err.message || String(err) }});
                    else resolve({{ ok: true }});
                }}
            }});
            setTimeout(function() {{ resolve({{ ok: false, err: 'Timeout' }}); }}, 8000);
        }});
        
        result.success = r.ok;
        result.message = r.ok ? (mute ? '禁言成功' : '解除禁言成功') : (r.err || '禁言失败');
    }} catch(e) {{
        result.message = e.message;
    }}
    return JSON.stringify(result);
}})();";

                var resultJson = await ExecuteJsAsync(js);
                if (!string.IsNullOrEmpty(resultJson))
                {
                    var data = _serializer.Deserialize<Dictionary<string, object>>(resultJson);
                    if (data != null)
                    {
                        var success = data.ContainsKey("success") && (bool)data["success"];
                        var message = data.ContainsKey("message") ? data["message"]?.ToString() : "";
                        Log($"禁言结果: {message}");
                        return (success, message);
                    }
                }
            }
            catch (Exception ex)
            {
                Log($"禁言异常: {ex.Message}");
                return (false, ex.Message);
            }

            return (false, "执行失败");
        }

        /// <summary>
        /// 撤回消息
        /// </summary>
        /// <param name="teamInternalId">群内部ID</param>
        /// <param name="idClient">消息客户端ID</param>
        public async Task<(bool Success, string Message)> RecallMessageAsync(string teamInternalId, string idClient)
        {
            try
            {
                if (!IsConnected && !await CheckConnectionAsync())
                {
                    return (false, "未连接到旺商聊");
                }

                var js = $@"
(async function() {{
    var result = {{ success: false, message: '' }};
    try {{
        if (!window.nim) {{
            result.message = 'NIM SDK not found';
            return JSON.stringify(result);
        }}
        
        var msgObj = {{ 
            idClient: '{idClient}', 
            to: '{teamInternalId}', 
            scene: 'team' 
        }};
        
        // 尝试不同的撤回方法
        var methods = ['deleteMsg', 'recallMsg', 'deleteMsgSelf'];
        
        for (var i = 0; i < methods.length; i++) {{
            var fn = window.nim[methods[i]];
            if (typeof fn !== 'function') continue;
            
            try {{
                var r = await new Promise(function(resolve) {{
                    fn.call(window.nim, {{
                        msg: msgObj,
                        done: function(err) {{
                            if (err) resolve({{ ok: false, err: err.message || String(err) }});
                            else resolve({{ ok: true }});
                        }}
                    }});
                    setTimeout(function() {{ resolve({{ ok: false, err: 'Timeout' }}); }}, 5000);
                }});
                
                if (r.ok) {{
                    result.success = true;
                    result.message = '撤回成功';
                    return JSON.stringify(result);
                }}
            }} catch(e) {{}}
        }}
        
        result.message = '撤回失败';
    }} catch(e) {{
        result.message = e.message;
    }}
    return JSON.stringify(result);
}})();";

                var resultJson = await ExecuteJsAsync(js);
                if (!string.IsNullOrEmpty(resultJson))
                {
                    var data = _serializer.Deserialize<Dictionary<string, object>>(resultJson);
                    if (data != null)
                    {
                        var success = data.ContainsKey("success") && (bool)data["success"];
                        var message = data.ContainsKey("message") ? data["message"]?.ToString() : "";
                        Log($"撤回结果: {message}");
                        return (success, message);
                    }
                }
            }
            catch (Exception ex)
            {
                Log($"撤回异常: {ex.Message}");
                return (false, ex.Message);
            }

            return (false, "执行失败");
        }

        /// <summary>
        /// 修改群成员昵称
        /// </summary>
        /// <param name="teamInternalId">群内部ID</param>
        /// <param name="nimAccountId">成员NIM账号</param>
        /// <param name="newNick">新昵称</param>
        public async Task<(bool Success, string Message)> UpdateMemberNickAsync(string teamInternalId, string nimAccountId, string newNick)
        {
            try
            {
                if (!IsConnected && !await CheckConnectionAsync())
                {
                    return (false, "未连接到旺商聊");
                }

                var escapedNick = newNick.Replace("'", "\\'").Replace("\n", "\\n");

                var js = $@"
(async function() {{
    var result = {{ success: false, message: '' }};
    try {{
        if (!window.nim) {{
            result.message = 'NIM SDK not found';
            return JSON.stringify(result);
        }}
        
        var fn = window.nim.updateNickInTeam || window.nim.updateMemberNick;
        if (typeof fn !== 'function') {{
            result.message = '修改昵称方法不可用';
            return JSON.stringify(result);
        }}
        
        var r = await new Promise(function(resolve) {{
            fn.call(window.nim, {{
                teamId: '{teamInternalId}',
                account: '{nimAccountId}',
                nick: '{escapedNick}',
                done: function(err) {{
                    if (err) resolve({{ ok: false, err: err.message || String(err) }});
                    else resolve({{ ok: true }});
                }}
            }});
            setTimeout(function() {{ resolve({{ ok: false, err: 'Timeout' }}); }}, 5000);
        }});
        
        result.success = r.ok;
        result.message = r.ok ? '修改昵称成功' : (r.err || '修改昵称失败');
    }} catch(e) {{
        result.message = e.message;
    }}
    return JSON.stringify(result);
}})();";

                var resultJson = await ExecuteJsAsync(js);
                if (!string.IsNullOrEmpty(resultJson))
                {
                    var data = _serializer.Deserialize<Dictionary<string, object>>(resultJson);
                    if (data != null)
                    {
                        var success = data.ContainsKey("success") && (bool)data["success"];
                        var message = data.ContainsKey("message") ? data["message"]?.ToString() : "";
                        Log($"修改昵称结果: {message}");
                        return (success, message);
                    }
                }
            }
            catch (Exception ex)
            {
                Log($"修改昵称异常: {ex.Message}");
                return (false, ex.Message);
            }

            return (false, "执行失败");
        }

        #endregion
        
        #region 直接发送消息（无需打开聊天窗口）
        
        /// <summary>
        /// 获取群号到NIM云ID的映射
        /// 旺商聊有三种群ID：
        /// - groupAccount: 显示给用户的群号（如 3962369093）
        /// - groupId: 内部ID（如 1176721）
        /// - groupCloudId: NIM云ID（如 40821608989），发送消息必须用这个
        /// </summary>
        public async Task<Dictionary<string, string>> GetGroupIdMappingAsync()
        {
            var mapping = new Dictionary<string, string>();
            
            try
            {
                if (!IsConnected && !await CheckConnectionAsync())
                {
                    return mapping;
                }

                var js = @"
(function() {
    var result = { mappings: [] };
    try {
        var appEl = document.getElementById('app');
        if (appEl && appEl.__vue_app__) {
            var pinia = appEl.__vue_app__.config.globalProperties.$pinia;
            if (pinia && pinia._s) {
                var appStore = pinia._s.get('app');
                if (appStore && appStore.$state && appStore.$state.groupList) {
                    var gl = appStore.$state.groupList;
                    var processGroup = function(g) {
                        if (g.groupState === 'EXISTENCE') {
                            result.mappings.push({
                                groupAccount: String(g.groupAccount || ''),
                                groupId: String(g.groupId || ''),
                                groupCloudId: String(g.groupCloudId || ''),
                                groupName: g.groupName || ''
                            });
                        }
                    };
                    if (gl.owner) gl.owner.forEach(processGroup);
                    if (gl.member) gl.member.forEach(processGroup);
                }
            }
        }
    } catch(e) {
        result.error = e.message;
    }
    return JSON.stringify(result);
})();";

                var resultJson = await ExecuteJsAsync(js);
                if (!string.IsNullOrEmpty(resultJson))
                {
                    var data = _serializer.Deserialize<Dictionary<string, object>>(resultJson);
                    if (data != null && data.ContainsKey("mappings"))
                    {
                        var mappingsArray = data["mappings"] as System.Collections.ArrayList;
                        if (mappingsArray != null)
                        {
                            foreach (Dictionary<string, object> m in mappingsArray)
                            {
                                var groupAccount = m.ContainsKey("groupAccount") ? m["groupAccount"]?.ToString() : "";
                                var groupId = m.ContainsKey("groupId") ? m["groupId"]?.ToString() : "";
                                var groupCloudId = m.ContainsKey("groupCloudId") ? m["groupCloudId"]?.ToString() : "";
                                
                                if (!string.IsNullOrEmpty(groupCloudId))
                                {
                                    // 显示群号 -> NIM云ID
                                    if (!string.IsNullOrEmpty(groupAccount))
                                        mapping[groupAccount] = groupCloudId;
                                    // 内部ID -> NIM云ID
                                    if (!string.IsNullOrEmpty(groupId))
                                        mapping[groupId] = groupCloudId;
                                    // NIM云ID -> NIM云ID（方便查找）
                                    mapping[groupCloudId] = groupCloudId;
                                }
                            }
                        }
                    }
                }
                
                Log($"获取到 {mapping.Count / 3} 个群的ID映射");
            }
            catch (Exception ex)
            {
                Log($"获取群ID映射失败: {ex.Message}");
            }
            
            return mapping;
        }
        
        /// <summary>
        /// 将任意群ID转换为NIM云ID
        /// </summary>
        /// <param name="anyGroupId">任意群ID（可以是显示群号、内部ID或NIM云ID）</param>
        /// <returns>NIM云ID，如果找不到返回原值</returns>
        public async Task<string> ConvertToNimCloudIdAsync(string anyGroupId)
        {
            if (string.IsNullOrEmpty(anyGroupId))
                return anyGroupId;
            
            var mapping = await GetGroupIdMappingAsync();
            if (mapping.TryGetValue(anyGroupId, out var cloudId))
            {
                if (cloudId != anyGroupId)
                {
                    Log($"群ID转换: {anyGroupId} -> {cloudId}");
                }
                return cloudId;
            }
            
            // 如果没找到映射，返回原值（可能已经是NIM云ID）
            return anyGroupId;
        }
        
        /// <summary>
        /// 直接发送文本消息到群（无需打开聊天窗口）
        /// 通过 CDP 调用 NIM SDK 的 sendText 方法
        /// </summary>
        /// <param name="groupId">群ID（可以是显示群号如3962369093，也可以是NIM云ID如40821608989）</param>
        /// <param name="text">消息文本</param>
        /// <returns>发送结果</returns>
        public async Task<(bool Success, string Message, string IdClient)> SendTextToGroupAsync(string groupId, string text)
        {
            try
            {
                if (!IsConnected && !await CheckConnectionAsync())
                {
                    return (false, "未连接到旺商聊", null);
                }

                if (string.IsNullOrEmpty(groupId))
                {
                    return (false, "群ID不能为空", null);
                }

                // 自动转换群ID为NIM云ID
                var groupCloudId = await ConvertToNimCloudIdAsync(groupId);

                // 转义特殊字符
                var escapedText = text
                    .Replace("\\", "\\\\")
                    .Replace("'", "\\'")
                    .Replace("\r", "\\r")
                    .Replace("\n", "\\n")
                    .Replace("\t", "\\t");

                var js = $@"
(async function() {{
    var result = {{ success: false, message: '', idClient: '' }};
    try {{
        if (!window.nim) {{
            result.message = 'NIM SDK 未初始化';
            return JSON.stringify(result);
        }}
        
        var groupCloudId = '{groupCloudId}';
        var text = '{escapedText}';
        
        console.log('[DirectSend] 发送消息到群:', groupCloudId, '内容:', text.substring(0, 50));
        
        var r = await new Promise(function(resolve) {{
            window.nim.sendText({{
                scene: 'team',
                to: groupCloudId,
                text: text,
                done: function(err, msg) {{
                    if (err) {{
                        console.error('[DirectSend] 发送失败:', err);
                        resolve({{ 
                            ok: false, 
                            err: err.message || String(err),
                            code: err.code 
                        }});
                    }} else {{
                        console.log('[DirectSend] 发送成功:', msg.idClient);
                        resolve({{ 
                            ok: true, 
                            idClient: msg.idClient,
                            time: msg.time 
                        }});
                    }}
                }}
            }});
            setTimeout(function() {{ resolve({{ ok: false, err: '发送超时' }}); }}, 10000);
        }});
        
        result.success = r.ok;
        result.message = r.ok ? '发送成功' : (r.err || '发送失败');
        result.idClient = r.idClient || '';
    }} catch(e) {{
        result.message = e.message;
    }}
    return JSON.stringify(result);
}})();";

                var resultJson = await ExecuteJsAsync(js);
                if (!string.IsNullOrEmpty(resultJson))
                {
                    var data = _serializer.Deserialize<Dictionary<string, object>>(resultJson);
                    if (data != null)
                    {
                        var success = data.ContainsKey("success") && (bool)data["success"];
                        var message = data.ContainsKey("message") ? data["message"]?.ToString() : "";
                        var idClient = data.ContainsKey("idClient") ? data["idClient"]?.ToString() : "";
                        Log($"发送群消息结果: {message}");
                        return (success, message, idClient);
                    }
                }
            }
            catch (Exception ex)
            {
                Log($"发送群消息异常: {ex.Message}");
                return (false, ex.Message, null);
            }

            return (false, "执行失败", null);
        }

        /// <summary>
        /// 直接发送文本消息到个人（无需打开聊天窗口）
        /// </summary>
        /// <param name="nimAccountId">目标用户的 NIM 账号ID</param>
        /// <param name="text">消息文本</param>
        public async Task<(bool Success, string Message, string IdClient)> SendTextToUserAsync(string nimAccountId, string text)
        {
            try
            {
                if (!IsConnected && !await CheckConnectionAsync())
                {
                    return (false, "未连接到旺商聊", null);
                }

                if (string.IsNullOrEmpty(nimAccountId))
                {
                    return (false, "用户NIM ID不能为空", null);
                }

                var escapedText = text
                    .Replace("\\", "\\\\")
                    .Replace("'", "\\'")
                    .Replace("\r", "\\r")
                    .Replace("\n", "\\n")
                    .Replace("\t", "\\t");

                var js = $@"
(async function() {{
    var result = {{ success: false, message: '', idClient: '' }};
    try {{
        if (!window.nim) {{
            result.message = 'NIM SDK 未初始化';
            return JSON.stringify(result);
        }}
        
        var nimAccountId = '{nimAccountId}';
        var text = '{escapedText}';
        
        var r = await new Promise(function(resolve) {{
            window.nim.sendText({{
                scene: 'p2p',
                to: nimAccountId,
                text: text,
                done: function(err, msg) {{
                    if (err) {{
                        resolve({{ ok: false, err: err.message || String(err) }});
                    }} else {{
                        resolve({{ ok: true, idClient: msg.idClient }});
                    }}
                }}
            }});
            setTimeout(function() {{ resolve({{ ok: false, err: '发送超时' }}); }}, 10000);
        }});
        
        result.success = r.ok;
        result.message = r.ok ? '发送成功' : (r.err || '发送失败');
        result.idClient = r.idClient || '';
    }} catch(e) {{
        result.message = e.message;
    }}
    return JSON.stringify(result);
}})();";

                var resultJson = await ExecuteJsAsync(js);
                if (!string.IsNullOrEmpty(resultJson))
                {
                    var data = _serializer.Deserialize<Dictionary<string, object>>(resultJson);
                    if (data != null)
                    {
                        var success = data.ContainsKey("success") && (bool)data["success"];
                        var message = data.ContainsKey("message") ? data["message"]?.ToString() : "";
                        var idClient = data.ContainsKey("idClient") ? data["idClient"]?.ToString() : "";
                        Log($"发送私信结果: {message}");
                        return (success, message, idClient);
                    }
                }
            }
            catch (Exception ex)
            {
                Log($"发送私信异常: {ex.Message}");
                return (false, ex.Message, null);
            }

            return (false, "执行失败", null);
        }

        /// <summary>
        /// 获取当前账号加入的所有群（NIM 群列表）
        /// </summary>
        public async Task<List<NimTeamInfo>> GetNimTeamsAsync()
        {
            var teams = new List<NimTeamInfo>();
            
            try
            {
                if (!IsConnected && !await CheckConnectionAsync())
                {
                    Log("未连接到旺商聊");
                    return teams;
                }

                var js = @"
(async function() {
    var result = { success: false, teams: [] };
    try {
        if (!window.nim) {
            result.error = 'NIM SDK 未初始化';
            return JSON.stringify(result);
        }
        
        var r = await new Promise(function(resolve) {
            window.nim.getTeams({
                done: function(err, teamList) {
                    if (err) {
                        resolve({ ok: false, err: err.message || String(err) });
                    } else {
                        resolve({ ok: true, teams: teamList });
                    }
                }
            });
            setTimeout(function() { resolve({ ok: false, err: 'Timeout' }); }, 10000);
        });
        
        if (r.ok && r.teams) {
            r.teams.forEach(function(t) {
                result.teams.push({
                    teamId: t.teamId,
                    name: t.name,
                    owner: t.owner,
                    memberNum: t.memberNum,
                    type: t.type,
                    valid: t.valid,
                    mute: t.mute,
                    avatar: t.avatar
                });
            });
            result.success = true;
        } else {
            result.error = r.err;
        }
    } catch(e) {
        result.error = e.message;
    }
    return JSON.stringify(result);
})();";

                var resultJson = await ExecuteJsAsync(js);
                if (!string.IsNullOrEmpty(resultJson))
                {
                    var data = _serializer.Deserialize<Dictionary<string, object>>(resultJson);
                    if (data != null && data.ContainsKey("success") && (bool)data["success"])
                    {
                        var teamsArray = data["teams"] as System.Collections.ArrayList;
                        if (teamsArray != null)
                        {
                            foreach (Dictionary<string, object> t in teamsArray)
                            {
                                teams.Add(new NimTeamInfo
                                {
                                    TeamId = t.ContainsKey("teamId") ? t["teamId"]?.ToString() : "",
                                    Name = t.ContainsKey("name") ? t["name"]?.ToString() : "",
                                    Owner = t.ContainsKey("owner") ? t["owner"]?.ToString() : "",
                                    MemberNum = t.ContainsKey("memberNum") ? Convert.ToInt32(t["memberNum"]) : 0,
                                    Type = t.ContainsKey("type") ? t["type"]?.ToString() : "",
                                    Valid = t.ContainsKey("valid") && (bool)t["valid"],
                                    Mute = t.ContainsKey("mute") && (bool)t["mute"],
                                    Avatar = t.ContainsKey("avatar") ? t["avatar"]?.ToString() : ""
                                });
                            }
                        }
                    }
                }
                
                Log($"获取到 {teams.Count} 个 NIM 群");
            }
            catch (Exception ex)
            {
                Log($"获取 NIM 群列表失败: {ex.Message}");
            }
            
            return teams;
        }

        #endregion
        
        #region 账号登录 API
        
        /// <summary>
        /// 通过 CDP 调用旺商聊内部 API 获取其他账号的 NIM Token
        /// 用于双账号模式：客户端登录群主，获取机器人的凭证
        /// </summary>
        /// <param name="accountId">机器人账号ID</param>
        /// <param name="password">机器人密码</param>
        /// <returns>登录信息（包含 NimId 和 NimToken）</returns>
        public async Task<BotLoginResult> LoginAccountViaCDPAsync(string accountId, string password)
        {
            var result = new BotLoginResult { Success = false };
            
            try
            {
                if (!IsConnected && !await CheckConnectionAsync())
                {
                    result.Error = "未连接到旺商聊客户端";
                    return result;
                }
                
                Log($"通过 CDP 获取账号 {accountId} 的 NIM 凭证...");
                
                // 使用 AES 加密密码（与旺商聊客户端相同的加密方式）
                // Key: 49KdgB8_9=12+3hF, IV: 00000000000000000000000000000000 (Hex)
                var js = $@"
(async function() {{
    var result = {{ success: false }};
    try {{
        // 获取加密函数
        var CryptoJS = window.CryptoJS;
        if (!CryptoJS) {{
            // 尝试从 require 获取
            try {{ CryptoJS = require('crypto-js'); }} catch(e) {{}}
        }}
        
        var encryptedPassword = '{password}';
        if (CryptoJS) {{
            var key = CryptoJS.enc.Utf8.parse('49KdgB8_9=12+3hF');
            var iv = CryptoJS.enc.Hex.parse('00000000000000000000000000000000');
            encryptedPassword = CryptoJS.AES.encrypt('{password}', key, {{
                iv: iv,
                mode: CryptoJS.mode.CBC,
                padding: CryptoJS.pad.Pkcs7
            }}).toString();
        }}
        
        // 通过 IPC 调用登录 API
        var ipcRenderer = window.require ? window.require('electron').ipcRenderer : null;
        if (!ipcRenderer) {{
            result.error = 'IPC 不可用';
            return JSON.stringify(result);
        }}
        
        // 创建 Promise 来等待响应
        var loginPromise = new Promise((resolve, reject) => {{
            var timeout = setTimeout(() => reject(new Error('登录超时')), 15000);
            
            // 监听响应
            var handler = (event, response) => {{
                clearTimeout(timeout);
                ipcRenderer.removeListener('xclient-response', handler);
                resolve(response);
            }};
            ipcRenderer.on('xclient-response', handler);
            
            // 发送登录请求
            ipcRenderer.send('xclient', {{
                type: 'request',
                url: '/v1/user/login',
                method: 'POST',
                data: {{
                    loginType: 'LOGIN_TYPE_ACCOUNT_PWD',
                    accountId: '{accountId}',
                    password: encryptedPassword
                }}
            }});
        }});
        
        var response = await loginPromise;
        if (response && response.code === 0 && response.data) {{
            result.success = true;
            result.uid = response.data.uid;
            result.nimId = String(response.data.nimId || '');
            result.nimToken = response.data.nimToken || '';
            result.nickname = response.data.nickName || '';
            result.accountId = response.data.accountId || '{accountId}';
            result.jwtToken = response.data.token || '';
        }} else {{
            result.error = response ? (response.msg || response.message || '登录失败') : '无响应';
            result.code = response ? response.code : -1;
        }}
    }} catch(e) {{
        result.error = e.message || e.toString();
    }}
    return JSON.stringify(result);
}})();";
                
                var resultJson = await ExecuteJsAsync(js);
                Log($"CDP 登录响应: {resultJson?.Substring(0, Math.Min(200, resultJson?.Length ?? 0))}...");
                
                if (!string.IsNullOrEmpty(resultJson))
                {
                    var data = _serializer.Deserialize<Dictionary<string, object>>(resultJson);
                    if (data != null)
                    {
                        result.Success = data.ContainsKey("success") && (bool)data["success"];
                        if (result.Success)
                        {
                            result.Uid = data.ContainsKey("uid") ? Convert.ToInt64(data["uid"]) : 0;
                            result.NimId = data.ContainsKey("nimId") ? data["nimId"]?.ToString() : "";
                            result.NimToken = data.ContainsKey("nimToken") ? data["nimToken"]?.ToString() : "";
                            result.Nickname = data.ContainsKey("nickname") ? data["nickname"]?.ToString() : "";
                            result.AccountId = data.ContainsKey("accountId") ? data["accountId"]?.ToString() : "";
                            result.JwtToken = data.ContainsKey("jwtToken") ? data["jwtToken"]?.ToString() : "";
                            Log($"✓ 获取到 {result.Nickname} 的 NIM 凭证: {result.NimId}");
                        }
                        else
                        {
                            result.Error = data.ContainsKey("error") ? data["error"]?.ToString() : "未知错误";
                            Log($"✗ 获取凭证失败: {result.Error}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                result.Error = ex.Message;
                Log($"CDP 登录异常: {ex.Message}");
            }
            
            return result;
        }
        
        /// <summary>
        /// 通过 CDP 使用简化方式获取账号凭证（直接调用 common.handleReq）
        /// </summary>
        public async Task<BotLoginResult> LoginAccountSimpleAsync(string accountId, string password)
        {
            var result = new BotLoginResult { Success = false };
            
            try
            {
                if (!IsConnected && !await CheckConnectionAsync())
                {
                    result.Error = "未连接到旺商聊客户端";
                    return result;
                }
                
                Log($"通过 CDP (简化模式) 获取账号 {accountId} 的凭证...");
                
                // 查找并调用 common.handleReq 方法
                var js = $@"
(async function() {{
    var result = {{ success: false }};
    try {{
        // 查找 common 模块（Vue store 中）
        var appEl = document.getElementById('app');
        if (!appEl || !appEl.__vue_app__) {{
            result.error = '未找到 Vue 应用';
            return JSON.stringify(result);
        }}
        
        var app = appEl.__vue_app__;
        var pinia = app.config.globalProperties.$pinia;
        
        // 尝试从全局或模块中获取 handleReq
        var handleReq = window.common?.handleReq || window.__common__?.handleReq;
        
        if (!handleReq) {{
            // 尝试从 window.__modules__ 获取
            for (var key in window) {{
                if (window[key] && typeof window[key].handleReq === 'function') {{
                    handleReq = window[key].handleReq.bind(window[key]);
                    break;
                }}
            }}
        }}
        
        if (!handleReq) {{
            result.error = 'handleReq 函数不可用，尝试 IPC 方式';
            result.tryIpc = true;
            return JSON.stringify(result);
        }}
        
        // 加密密码
        var CryptoJS = window.CryptoJS;
        var encryptedPassword = '{password}';
        if (CryptoJS) {{
            var key = CryptoJS.enc.Utf8.parse('49KdgB8_9=12+3hF');
            var iv = CryptoJS.enc.Hex.parse('00000000000000000000000000000000');
            encryptedPassword = CryptoJS.AES.encrypt('{password}', key, {{
                iv: iv,
                mode: CryptoJS.mode.CBC,
                padding: CryptoJS.pad.Pkcs7
            }}).toString();
        }}
        
        // 调用登录 API
        var response = await handleReq('/v1/user/login', {{
            loginType: 'LOGIN_TYPE_ACCOUNT_PWD',
            accountId: '{accountId}',
            password: encryptedPassword
        }});
        
        if (response && response.code === 0 && response.data) {{
            result.success = true;
            result.uid = response.data.uid;
            result.nimId = String(response.data.nimId || '');
            result.nimToken = response.data.nimToken || '';
            result.nickname = response.data.nickName || '';
            result.accountId = response.data.accountId || '{accountId}';
        }} else {{
            result.error = response ? (response.msg || '登录失败') : '无响应';
        }}
    }} catch(e) {{
        result.error = e.message;
    }}
    return JSON.stringify(result);
}})();";
                
                var resultJson = await ExecuteJsAsync(js);
                Log($"CDP 简化登录响应: {resultJson?.Substring(0, Math.Min(200, resultJson?.Length ?? 0))}...");
                
                if (!string.IsNullOrEmpty(resultJson))
                {
                    var data = _serializer.Deserialize<Dictionary<string, object>>(resultJson);
                    if (data != null)
                    {
                        // 如果需要尝试 IPC 方式
                        if (data.ContainsKey("tryIpc") && (bool)data["tryIpc"])
                        {
                            Log("简化模式不可用，尝试 IPC 方式...");
                            return await LoginAccountViaCDPAsync(accountId, password);
                        }
                        
                        result.Success = data.ContainsKey("success") && (bool)data["success"];
                        if (result.Success)
                        {
                            result.Uid = data.ContainsKey("uid") ? Convert.ToInt64(data["uid"]) : 0;
                            result.NimId = data.ContainsKey("nimId") ? data["nimId"]?.ToString() : "";
                            result.NimToken = data.ContainsKey("nimToken") ? data["nimToken"]?.ToString() : "";
                            result.Nickname = data.ContainsKey("nickname") ? data["nickname"]?.ToString() : "";
                            result.AccountId = data.ContainsKey("accountId") ? data["accountId"]?.ToString() : "";
                        }
                        else
                        {
                            result.Error = data.ContainsKey("error") ? data["error"]?.ToString() : "未知错误";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                result.Error = ex.Message;
            }
            
            return result;
        }
        
        #endregion

        #region 消息解码

        /// <summary>
        /// 初始化消息解码器
        /// 必须先调用此方法才能使用解码功能
        /// </summary>
        public async Task<bool> InitMessageDecoderAsync()
        {
            try
            {
                if (!IsConnected && !await CheckConnectionAsync())
                {
                    Log("初始化解码器失败：未连接到旺商聊");
                    return false;
                }

                var js = @"
(function() {
    if (window.WSLDecoder) return JSON.stringify({ success: true, exists: true });
    
    window.WSLDecoder = {
        xclient: null,
        
        init: function() {
            if (!this.xclient) {
                try {
                    this.xclient = require('C:/Program Files/wangshangliao_win_online/resources/app/build/xclient.node');
                } catch(e) {
                    return false;
                }
            }
            return this.xclient !== null;
        },
        
        decode: function(bField) {
            if (!this.init()) return null;
            
            try {
                var buffer = Buffer.from(bField, 'utf-8');
                var arrayBuffer = new ArrayBuffer(buffer.length);
                var uint8 = new Uint8Array(arrayBuffer);
                for (var i = 0; i < buffer.length; i++) {
                    uint8[i] = buffer[i];
                }
                return this.xclient.xsync(1, arrayBuffer);
            } catch(e) {
                return null;
            }
        },
        
        extractText: function(decoded) {
            if (!decoded) return null;
            
            var arr = new Uint8Array(decoded);
            var decoder = new TextDecoder('utf-8', { fatal: false });
            var text = decoder.decode(arr);
            
            var blocks = text.split(/[\x00-\x1f]+/).filter(function(s) {
                return s.length > 2 && /[\u4e00-\u9fff]/.test(s);
            });
            
            if (blocks.length > 0) {
                blocks.sort(function(a, b) { return b.length - a.length; });
                return blocks[0].trim();
            }
            return null;
        },
        
        decodeMessage: function(content) {
            var c = typeof content === 'string' ? JSON.parse(content) : content;
            if (!c || !c.b) return null;
            
            var decoded = this.decode(c.b);
            return this.extractText(decoded);
        }
    };
    
    return JSON.stringify({ success: window.WSLDecoder.init() });
})();";

                var result = await ExecuteJsAsync(js);
                if (result != null)
                {
                    var data = _serializer.Deserialize<Dictionary<string, object>>(result.ToString());
                    var success = data != null && data.ContainsKey("success") && (bool)data["success"];
                    if (success)
                    {
                        Log("消息解码器初始化成功");
                    }
                    return success;
                }
            }
            catch (Exception ex)
            {
                Log($"初始化解码器异常: {ex.Message}");
            }

            return false;
        }

        /// <summary>
        /// 解码单条消息的 content
        /// </summary>
        /// <param name="content">消息的 content 字段（JSON格式，包含 b 字段）</param>
        /// <returns>解码后的文本内容</returns>
        public async Task<string> DecodeMessageContentAsync(string content)
        {
            try
            {
                if (!IsConnected && !await CheckConnectionAsync())
                {
                    return null;
                }

                // 确保解码器已初始化
                await InitMessageDecoderAsync();

                var escapedContent = content.Replace("\\", "\\\\").Replace("'", "\\'").Replace("\n", "\\n").Replace("\r", "\\r");

                var js = $@"
(function() {{
    if (!window.WSLDecoder) return null;
    return window.WSLDecoder.decodeMessage('{escapedContent}');
}})();";

                var result = await ExecuteJsAsync(js);
                return result?.ToString();
            }
            catch (Exception ex)
            {
                Log($"解码消息异常: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 获取群聊的已解码消息列表
        /// </summary>
        /// <param name="groupCloudId">群的 NIM Cloud ID</param>
        /// <param name="limit">获取数量限制</param>
        /// <returns>解码后的消息列表</returns>
        public async Task<List<DecodedMessage>> GetDecodedMessagesAsync(string groupCloudId, int limit = 20)
        {
            var messages = new List<DecodedMessage>();

            try
            {
                if (!IsConnected && !await CheckConnectionAsync())
                {
                    return messages;
                }

                // 确保解码器已初始化
                await InitMessageDecoderAsync();

                var js = $@"
(async function() {{
    var result = {{ messages: [] }};
    
    var msgs = await new Promise(function(resolve) {{
        window.nim.getLocalMsgs({{
            scene: 'team',
            to: '{groupCloudId}',
            limit: {limit},
            done: function(err, obj) {{
                if (err) resolve([]);
                else resolve(obj.msgs || []);
            }}
        }});
        setTimeout(function() {{ resolve([]); }}, 10000);
    }});
    
    msgs.forEach(function(msg) {{
        var info = {{
            time: msg.time,
            from: msg.from,
            fromNick: msg.fromNick,
            to: msg.to,
            scene: msg.scene,
            type: msg.type,
            idClient: msg.idClient,
            idServer: msg.idServer,
            flow: msg.flow,
            rawContent: ''
        }};
        
        if (msg.type === 'custom' && msg.content) {{
            info.rawContent = typeof msg.content === 'string' ? msg.content : JSON.stringify(msg.content);
            info.text = window.WSLDecoder ? window.WSLDecoder.decodeMessage(msg.content) : null;
        }} else if (msg.type === 'text') {{
            info.text = msg.text;
        }}
        
        result.messages.push(info);
    }});
    
    return JSON.stringify(result);
}})();";

                var result = await ExecuteJsAsync(js);
                if (result != null)
                {
                    var data = _serializer.Deserialize<Dictionary<string, object>>(result.ToString());
                    if (data != null && data.ContainsKey("messages"))
                    {
                        var msgList = data["messages"] as System.Collections.ArrayList;
                        if (msgList != null)
                        {
                            foreach (Dictionary<string, object> msgData in msgList)
                            {
                                var msg = new DecodedMessage
                                {
                                    Time = msgData.ContainsKey("time") ? Convert.ToInt64(msgData["time"]) : 0,
                                    From = msgData.ContainsKey("from") ? msgData["from"]?.ToString() : "",
                                    FromNick = msgData.ContainsKey("fromNick") ? msgData["fromNick"]?.ToString() : "",
                                    To = msgData.ContainsKey("to") ? msgData["to"]?.ToString() : "",
                                    Scene = msgData.ContainsKey("scene") ? msgData["scene"]?.ToString() : "",
                                    Type = msgData.ContainsKey("type") ? msgData["type"]?.ToString() : "",
                                    Text = msgData.ContainsKey("text") ? msgData["text"]?.ToString() : "",
                                    RawContent = msgData.ContainsKey("rawContent") ? msgData["rawContent"]?.ToString() : "",
                                    IdClient = msgData.ContainsKey("idClient") ? msgData["idClient"]?.ToString() : "",
                                    IdServer = msgData.ContainsKey("idServer") ? msgData["idServer"]?.ToString() : "",
                                    Flow = msgData.ContainsKey("flow") ? msgData["flow"]?.ToString() : ""
                                };
                                messages.Add(msg);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log($"获取解码消息异常: {ex.Message}");
            }

            return messages;
        }

        #endregion

        // CDP Target 结构
        [Serializable]
        private class CDPTarget
        {
            public string description { get; set; }
            public string devtoolsFrontendUrl { get; set; }
            public string id { get; set; }
            public string title { get; set; }
            public string type { get; set; }
            public string url { get; set; }
            public string webSocketDebuggerUrl { get; set; }
        }
    }

    /// <summary>
    /// 旺商聊用户信息
    /// </summary>
    public class WslUserInfo
    {
        /// <summary>用户ID</summary>
        public long Uid { get; set; }
        
        /// <summary>WWID（等同于UID）</summary>
        public string Wwid { get; set; }
        
        /// <summary>昵称</summary>
        public string Nickname { get; set; }
        
        /// <summary>NIM ID（网易云信账号）</summary>
        public string NimId { get; set; }
        
        /// <summary>NIM Token（网易云信Token）</summary>
        public string NimToken { get; set; }
        
        /// <summary>头像ID</summary>
        public string Avatar { get; set; }
        
        /// <summary>登录账号（如手机号）</summary>
        public string AccountId { get; set; }
        
        /// <summary>手机号</summary>
        public string Phone { get; set; }
        
        /// <summary>JWT Token</summary>
        public string JwtToken { get; set; }
        
        /// <summary>账号状态</summary>
        public string AccountState { get; set; }
        
        /// <summary>VIP等级</summary>
        public string VipLevel { get; set; }

        public override string ToString()
        {
            return $"{Nickname} (WWID: {Wwid}, AccountId: {AccountId})";
        }
    }
    
    /// <summary>
    /// 旺商聊好友信息
    /// </summary>
    public class WslFriendInfo
    {
        /// <summary>用户ID</summary>
        public long Uid { get; set; }
        
        /// <summary>昵称</summary>
        public string Nickname { get; set; }
        
        /// <summary>备注名</summary>
        public string MarkName { get; set; }
        
        /// <summary>头像</summary>
        public string Avatar { get; set; }
        
        /// <summary>账号ID</summary>
        public string AccountId { get; set; }
        
        /// <summary>NIM ID</summary>
        public string NimId { get; set; }
        
        /// <summary>在线状态</summary>
        public string OnlineState { get; set; }
        
        /// <summary>好友状态</summary>
        public string State { get; set; }
        
        /// <summary>VIP等级</summary>
        public string VipLevel { get; set; }
        
        /// <summary>首字母拼音</summary>
        public string InitialPinyin { get; set; }
        
        /// <summary>显示名称（优先备注名）</summary>
        public string DisplayName => !string.IsNullOrEmpty(MarkName) ? MarkName : Nickname;

        public override string ToString()
        {
            return $"{DisplayName} (UID: {Uid})";
        }
    }
    
    /// <summary>
    /// 旺商聊群成员信息
    /// </summary>
    public class WslGroupMemberInfo
    {
        /// <summary>用户ID</summary>
        public long UserId { get; set; }
        
        /// <summary>用户昵称</summary>
        public string UserNick { get; set; }
        
        /// <summary>群内昵称</summary>
        public string GroupMemberNick { get; set; }
        
        /// <summary>头像</summary>
        public string UserAvatar { get; set; }
        
        /// <summary>账号ID</summary>
        public string AccountId { get; set; }
        
        /// <summary>NIM ID</summary>
        public string NimId { get; set; }
        
        /// <summary>群角色（GROUP_ROLE_OWNER/GROUP_ROLE_ADMIN/GROUP_ROLE_MEMBER）</summary>
        public string GroupRole { get; set; }
        
        /// <summary>VIP等级</summary>
        public string VipLevel { get; set; }
        
        /// <summary>首字母拼音</summary>
        public string InitialPinyin { get; set; }
        
        /// <summary>账号状态</summary>
        public string AccountState { get; set; }
        
        /// <summary>是否群主</summary>
        public bool IsOwner => GroupRole == "GROUP_ROLE_OWNER";
        
        /// <summary>是否管理员</summary>
        public bool IsAdmin => GroupRole == "GROUP_ROLE_ADMIN";
        
        /// <summary>显示名称（优先群昵称）</summary>
        public string DisplayName => !string.IsNullOrEmpty(GroupMemberNick) ? GroupMemberNick : UserNick;
        
        /// <summary>角色文本</summary>
        public string RoleText => IsOwner ? "群主" : (IsAdmin ? "管理员" : "成员");

        public override string ToString()
        {
            return $"{DisplayName} [{RoleText}]";
        }
    }

    /// <summary>
    /// 旺商聊群信息
    /// </summary>
    public class WslGroupInfo
    {
        /// <summary>群号（用户看到的，如 3962369093）</summary>
        public string GroupId { get; set; }
        
        /// <summary>内部ID（如 1176721）</summary>
        public string InternalId { get; set; }
        
        /// <summary>云端ID</summary>
        public string CloudId { get; set; }
        
        /// <summary>群名称</summary>
        public string Name { get; set; }
        
        /// <summary>成员数量</summary>
        public int MemberNum { get; set; }
        
        /// <summary>在群中的角色（owner/member）</summary>
        public string Role { get; set; }
        
        /// <summary>群头像</summary>
        public string Avatar { get; set; }
        
        /// <summary>我在群中的角色（GROUP_ROLE_ADMIN等）</summary>
        public string MyRole { get; set; }
        
        /// <summary>我在群中的昵称</summary>
        public string MyNickName { get; set; }

        /// <summary>显示文本</summary>
        public string DisplayText
        {
            get
            {
                var roleText = Role == "owner" ? "群主" : 
                               (MyRole == "GROUP_ROLE_ADMIN" ? "管理员" : "成员");
                return $"{GroupId} - {Name} ({MemberNum}人) [{roleText}]";
            }
        }

        public override string ToString()
        {
            return DisplayText;
        }
    }
    
    /// <summary>
    /// 机器人登录结果
    /// </summary>
    public class BotLoginResult
    {
        /// <summary>是否成功</summary>
        public bool Success { get; set; }
        
        /// <summary>用户ID</summary>
        public long Uid { get; set; }
        
        /// <summary>NIM ID</summary>
        public string NimId { get; set; }
        
        /// <summary>NIM Token</summary>
        public string NimToken { get; set; }
        
        /// <summary>昵称</summary>
        public string Nickname { get; set; }
        
        /// <summary>账号ID</summary>
        public string AccountId { get; set; }
        
        /// <summary>JWT Token</summary>
        public string JwtToken { get; set; }
        
        /// <summary>错误信息</summary>
        public string Error { get; set; }
        
        /// <summary>是否有有效的 NIM 凭证</summary>
        public bool HasValidNimCredentials => 
            Success && !string.IsNullOrEmpty(NimId) && !string.IsNullOrEmpty(NimToken);
    }
    
    /// <summary>
    /// NIM 群信息（从 NIM SDK 获取的群信息）
    /// </summary>
    public class NimTeamInfo
    {
        /// <summary>群ID（NIM云端ID，用于发送消息）</summary>
        public string TeamId { get; set; }
        
        /// <summary>群名称</summary>
        public string Name { get; set; }
        
        /// <summary>群主 NIM 账号</summary>
        public string Owner { get; set; }
        
        /// <summary>成员数量</summary>
        public int MemberNum { get; set; }
        
        /// <summary>群类型</summary>
        public string Type { get; set; }
        
        /// <summary>是否有效</summary>
        public bool Valid { get; set; }
        
        /// <summary>是否全员禁言</summary>
        public bool Mute { get; set; }
        
        /// <summary>群头像</summary>
        public string Avatar { get; set; }
        
        public override string ToString()
        {
            return $"{TeamId}: {Name} ({MemberNum}人)";
        }
    }
    
    /// <summary>
    /// 解码后的旺商聊消息
    /// </summary>
    public class DecodedMessage
    {
        /// <summary>消息时间戳</summary>
        public long Time { get; set; }
        
        /// <summary>发送者 NIM ID</summary>
        public string From { get; set; }
        
        /// <summary>发送者昵称</summary>
        public string FromNick { get; set; }
        
        /// <summary>接收者/群 ID</summary>
        public string To { get; set; }
        
        /// <summary>场景 (p2p/team)</summary>
        public string Scene { get; set; }
        
        /// <summary>消息类型</summary>
        public string Type { get; set; }
        
        /// <summary>解码后的消息文本</summary>
        public string Text { get; set; }
        
        /// <summary>原始内容</summary>
        public string RawContent { get; set; }
        
        /// <summary>客户端消息ID</summary>
        public string IdClient { get; set; }
        
        /// <summary>服务器消息ID</summary>
        public string IdServer { get; set; }
        
        /// <summary>消息流向 (in/out)</summary>
        public string Flow { get; set; }
        
        /// <summary>格式化的时间</summary>
        public string TimeString
        {
            get
            {
                if (Time > 0)
                {
                    var dt = DateTimeOffset.FromUnixTimeMilliseconds(Time).LocalDateTime;
                    return dt.ToString("HH:mm:ss");
                }
                return "";
            }
        }
    }
}
