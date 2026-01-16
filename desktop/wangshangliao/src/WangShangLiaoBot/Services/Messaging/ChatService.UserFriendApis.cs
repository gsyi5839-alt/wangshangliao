using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.WebSockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Automation;
using WangShangLiaoBot.Models;
using WangShangLiaoBot.Utils;

namespace WangShangLiaoBot.Services
{
    public partial class ChatService
    {
        #region User and Friend APIs - 用户和好友API
        
        /// <summary>
        /// Get user info by account.
        /// 获取用户信息
        /// </summary>
        public async Task<(bool Success, string Nick, string Avatar, string Message)> GetUserAsync(string account)
        {
            if (!IsConnected || Mode != ConnectionMode.CDP)
                return (false, null, null, "未连接或非CDP模式");
            if (string.IsNullOrWhiteSpace(account))
                return (false, null, null, "account为空");

            try
            {
                var script = $@"
(async function() {{
    var result = {{ success:false, nick:null, avatar:null, error:null }};
    try {{
        if (!window.nim || typeof window.nim.getUser !== 'function') {{
            result.error = 'getUser not available';
            return JSON.stringify(result);
        }}
        var user = await new Promise((resolve, reject) => {{
            window.nim.getUser({{
                account: {ToJsonString(account)},
                done: (err, user) => {{
                    if (err) reject(err);
                    else resolve(user);
                }}
            }});
            setTimeout(() => reject(new Error('Timeout')), 10000);
        }});
        result.success = true;
        result.nick = user?.nick;
        result.avatar = user?.avatar;
        result.account = user?.account;
        result.custom = user?.custom;
    }} catch(e) {{
        result.error = e.message;
    }}
    return JSON.stringify(result);
}})();";

                var resp = await ExecuteScriptWithResultAsync(script, true);
                var ok = !string.IsNullOrEmpty(resp) && resp.Contains("\"success\":true");
                var nick = ExtractJsonField(resp, "nick");
                var avatar = ExtractJsonField(resp, "avatar");
                return ok ? (true, nick, avatar, "OK") : (false, null, null, ExtractJsonField(resp, "error") ?? resp);
            }
            catch (Exception ex) { return (false, null, null, ex.Message); }
        }
        
        /// <summary>
        /// Get multiple users' info.
        /// 批量获取用户信息
        /// </summary>
        public async Task<(bool Success, List<UserInfo> Users, string Message)> GetUsersAsync(List<string> accounts)
        {
            if (!IsConnected || Mode != ConnectionMode.CDP)
                return (false, null, "未连接或非CDP模式");
            if (accounts == null || accounts.Count == 0)
                return (false, null, "账号列表为空");

            try
            {
                var accountsJson = "[" + string.Join(",", accounts.Where(a => !string.IsNullOrWhiteSpace(a)).Select(a => ToJsonString(a))) + "]";
                var script = $@"
(async function() {{
    var result = {{ success:false, users:[], error:null }};
    try {{
        if (!window.nim || typeof window.nim.getUsers !== 'function') {{
            result.error = 'getUsers not available';
            return JSON.stringify(result);
        }}
        var users = await new Promise((resolve, reject) => {{
            window.nim.getUsers({{
                accounts: {accountsJson},
                done: (err, users) => {{
                    if (err) reject(err);
                    else resolve(users);
                }}
            }});
            setTimeout(() => reject(new Error('Timeout')), 10000);
        }});
        result.success = true;
        result.users = (users || []).map(u => ({{
            account: u.account,
            nick: u.nick,
            avatar: u.avatar,
            custom: u.custom,
            gender: u.gender,
            createTime: u.createTime,
            updateTime: u.updateTime
        }}));
    }} catch(e) {{
        result.error = e.message;
    }}
    return JSON.stringify(result);
}})();";

                var resp = await ExecuteScriptWithResultAsync(script, true);
                var ok = !string.IsNullOrEmpty(resp) && resp.Contains("\"success\":true");
                
                // Parse users from JSON response
                var users = new List<UserInfo>();
                if (ok)
                {
                    try
                    {
                        // Extract users array from response using simple parsing
                        var usersMatch = System.Text.RegularExpressions.Regex.Match(resp, @"\""users\""\s*:\s*\[(.*?)\]", System.Text.RegularExpressions.RegexOptions.Singleline);
                        if (usersMatch.Success)
                        {
                            var usersArray = usersMatch.Groups[1].Value;
                            // Parse individual user objects
                            var userMatches = System.Text.RegularExpressions.Regex.Matches(usersArray, @"\{[^{}]+\}");
                            foreach (System.Text.RegularExpressions.Match m in userMatches)
                            {
                                var userJson = m.Value;
                                users.Add(new UserInfo
                                {
                                    Account = ExtractJsonFieldFromString(userJson, "account"),
                                    Nick = ExtractJsonFieldFromString(userJson, "nick"),
                                    Avatar = ExtractJsonFieldFromString(userJson, "avatar"),
                                    Custom = ExtractJsonFieldFromString(userJson, "custom"),
                                    Gender = ExtractJsonFieldFromString(userJson, "gender")
                                });
                            }
                        }
                    }
                    catch { /* ignore parse errors */ }
                }
                
                return ok ? (true, users, "OK") : (false, null, ExtractJsonField(resp, "error") ?? resp);
            }
            catch (Exception ex) { return (false, null, ex.Message); }
        }
        
        /// <summary>
        /// Helper to extract JSON field from a string (for parsing nested objects)
        /// </summary>
        private string ExtractJsonFieldFromString(string json, string field)
        {
            if (string.IsNullOrEmpty(json)) return null;
            // Try string value first
            var pattern = $@"\""{field}\"":\s*\""([^""]*)\""";
            var match = System.Text.RegularExpressions.Regex.Match(json, pattern);
            if (match.Success) return match.Groups[1].Value;
            // Try numeric value
            pattern = $@"\""{field}\"":\s*([0-9]+)";
            match = System.Text.RegularExpressions.Regex.Match(json, pattern);
            return match.Success ? match.Groups[1].Value : null;
        }
        
        /// <summary>
        /// Get friends list.
        /// 获取好友列表
        /// </summary>
        public async Task<(bool Success, int Count, string Message)> GetFriendsAsync()
        {
            if (!IsConnected || Mode != ConnectionMode.CDP)
                return (false, 0, "未连接或非CDP模式");

            try
            {
                var script = @"
(async function() {
    var result = { success:false, count:0, friends:[], error:null };
    try {
        if (!window.nim || typeof window.nim.getFriends !== 'function') {
            result.error = 'getFriends not available';
            return JSON.stringify(result);
        }
        var friends = await new Promise((resolve, reject) => {
            window.nim.getFriends({
                done: (err, friends) => {
                    if (err) reject(err);
                    else resolve(friends);
                }
            });
            setTimeout(() => reject(new Error('Timeout')), 10000);
        });
        result.success = true;
        result.count = friends?.length || 0;
        result.friends = (friends || []).slice(0, 50).map(f => ({
            account: f.account,
            alias: f.alias,
            valid: f.valid
        }));
    } catch(e) {
        result.error = e.message;
    }
    return JSON.stringify(result);
})();";

                var resp = await ExecuteScriptWithResultAsync(script, true);
                var ok = !string.IsNullOrEmpty(resp) && resp.Contains("\"success\":true");
                int.TryParse(ExtractJsonField(resp, "count"), out int count);
                return ok ? (true, count, "OK") : (false, 0, ExtractJsonField(resp, "error") ?? resp);
            }
            catch (Exception ex) { return (false, 0, ex.Message); }
        }
        
        /// <summary>
        /// Check if a user is my friend.
        /// 检查是否好友
        /// </summary>
        public async Task<(bool Success, bool IsFriend, string Message)> IsMyFriendAsync(string account)
        {
            if (!IsConnected || Mode != ConnectionMode.CDP)
                return (false, false, "未连接或非CDP模式");
            if (string.IsNullOrWhiteSpace(account))
                return (false, false, "account为空");

            try
            {
                var script = $@"
(function() {{
    var result = {{ success:false, isFriend:false, error:null }};
    try {{
        if (!window.nim || typeof window.nim.isMyFriend !== 'function') {{
            result.error = 'isMyFriend not available';
            return JSON.stringify(result);
        }}
        result.isFriend = window.nim.isMyFriend({{ account: {ToJsonString(account)} }});
        result.success = true;
    }} catch(e) {{
        result.error = e.message;
    }}
    return JSON.stringify(result);
}})();";

                var resp = await ExecuteScriptWithResultAsync(script, false);
                var ok = !string.IsNullOrEmpty(resp) && resp.Contains("\"success\":true");
                var isFriend = resp?.Contains("\"isFriend\":true") == true;
                return ok ? (true, isFriend, "OK") : (false, false, ExtractJsonField(resp, "error") ?? resp);
            }
            catch (Exception ex) { return (false, false, ex.Message); }
        }
        
        /// <summary>
        /// Add a friend directly (no verification needed if allowed).
        /// 直接添加好友
        /// </summary>
        public async Task<(bool Success, string Message)> AddFriendAsync(string account, string ps = "")
        {
            if (!IsConnected || Mode != ConnectionMode.CDP)
                return (false, "未连接或非CDP模式");
            if (string.IsNullOrWhiteSpace(account))
                return (false, "account为空");

            try
            {
                var script = $@"
(async function() {{
    var result = {{ success:false, error:null }};
    try {{
        if (!window.nim || typeof window.nim.addFriend !== 'function') {{
            result.error = 'addFriend not available';
            return JSON.stringify(result);
        }}
        await new Promise((resolve, reject) => {{
            window.nim.addFriend({{
                account: {ToJsonString(account)},
                ps: {ToJsonString(ps)},
                done: (err) => {{
                    if (err) reject(err);
                    else resolve();
                }}
            }});
            setTimeout(() => reject(new Error('Timeout')), 10000);
        }});
        result.success = true;
    }} catch(e) {{
        result.error = e.message;
    }}
    return JSON.stringify(result);
}})();";

                var resp = await ExecuteScriptWithResultAsync(script, true);
                var ok = !string.IsNullOrEmpty(resp) && resp.Contains("\"success\":true");
                return ok ? (true, "已添加好友") : (false, ExtractJsonField(resp, "error") ?? resp);
            }
            catch (Exception ex) { return (false, ex.Message); }
        }
        
        /// <summary>
        /// Apply to add a friend (with verification).
        /// 申请添加好友
        /// </summary>
        public async Task<(bool Success, string Message)> ApplyFriendAsync(string account, string ps = "")
        {
            if (!IsConnected || Mode != ConnectionMode.CDP)
                return (false, "未连接或非CDP模式");
            if (string.IsNullOrWhiteSpace(account))
                return (false, "account为空");

            try
            {
                var script = $@"
(async function() {{
    var result = {{ success:false, error:null }};
    try {{
        if (!window.nim || typeof window.nim.applyFriend !== 'function') {{
            result.error = 'applyFriend not available';
            return JSON.stringify(result);
        }}
        await new Promise((resolve, reject) => {{
            window.nim.applyFriend({{
                account: {ToJsonString(account)},
                ps: {ToJsonString(ps)},
                done: (err) => {{
                    if (err) reject(err);
                    else resolve();
                }}
            }});
            setTimeout(() => reject(new Error('Timeout')), 10000);
        }});
        result.success = true;
    }} catch(e) {{
        result.error = e.message;
    }}
    return JSON.stringify(result);
}})();";

                var resp = await ExecuteScriptWithResultAsync(script, true);
                var ok = !string.IsNullOrEmpty(resp) && resp.Contains("\"success\":true");
                return ok ? (true, "好友申请已发送") : (false, ExtractJsonField(resp, "error") ?? resp);
            }
            catch (Exception ex) { return (false, ex.Message); }
        }
        
        /// <summary>
        /// Delete a friend.
        /// 删除好友
        /// </summary>
        public async Task<(bool Success, string Message)> DeleteFriendAsync(string account)
        {
            if (!IsConnected || Mode != ConnectionMode.CDP)
                return (false, "未连接或非CDP模式");
            if (string.IsNullOrWhiteSpace(account))
                return (false, "account为空");

            try
            {
                var script = $@"
(async function() {{
    var result = {{ success:false, error:null }};
    try {{
        if (!window.nim || typeof window.nim.deleteFriend !== 'function') {{
            result.error = 'deleteFriend not available';
            return JSON.stringify(result);
        }}
        await new Promise((resolve, reject) => {{
            window.nim.deleteFriend({{
                account: {ToJsonString(account)},
                done: (err) => {{
                    if (err) reject(err);
                    else resolve();
                }}
            }});
            setTimeout(() => reject(new Error('Timeout')), 10000);
        }});
        result.success = true;
    }} catch(e) {{
        result.error = e.message;
    }}
    return JSON.stringify(result);
}})();";

                var resp = await ExecuteScriptWithResultAsync(script, true);
                var ok = !string.IsNullOrEmpty(resp) && resp.Contains("\"success\":true");
                return ok ? (true, "已删除好友") : (false, ExtractJsonField(resp, "error") ?? resp);
            }
            catch (Exception ex) { return (false, ex.Message); }
        }
        
        /// <summary>
        /// Pass (accept) a friend apply.
        /// 通过好友申请
        /// </summary>
        public async Task<(bool Success, string Message)> PassFriendApplyAsync(string account)
        {
            if (!IsConnected || Mode != ConnectionMode.CDP)
                return (false, "未连接或非CDP模式");
            if (string.IsNullOrWhiteSpace(account))
                return (false, "account为空");

            try
            {
                var script = $@"
(async function() {{
    var result = {{ success:false, error:null }};
    try {{
        if (!window.nim || typeof window.nim.passFriendApply !== 'function') {{
            result.error = 'passFriendApply not available';
            return JSON.stringify(result);
        }}
        await new Promise((resolve, reject) => {{
            window.nim.passFriendApply({{
                account: {ToJsonString(account)},
                done: (err) => {{
                    if (err) reject(err);
                    else resolve();
                }}
            }});
            setTimeout(() => reject(new Error('Timeout')), 10000);
        }});
        result.success = true;
    }} catch(e) {{
        result.error = e.message;
    }}
    return JSON.stringify(result);
}})();";

                var resp = await ExecuteScriptWithResultAsync(script, true);
                var ok = !string.IsNullOrEmpty(resp) && resp.Contains("\"success\":true");
                return ok ? (true, "已通过好友申请") : (false, ExtractJsonField(resp, "error") ?? resp);
            }
            catch (Exception ex) { return (false, ex.Message); }
        }
        
        /// <summary>
        /// Reject a friend apply.
        /// 拒绝好友申请
        /// </summary>
        public async Task<(bool Success, string Message)> RejectFriendApplyAsync(string account, string ps = "")
        {
            if (!IsConnected || Mode != ConnectionMode.CDP)
                return (false, "未连接或非CDP模式");
            if (string.IsNullOrWhiteSpace(account))
                return (false, "account为空");

            try
            {
                var script = $@"
(async function() {{
    var result = {{ success:false, error:null }};
    try {{
        if (!window.nim || typeof window.nim.rejectFriendApply !== 'function') {{
            result.error = 'rejectFriendApply not available';
            return JSON.stringify(result);
        }}
        await new Promise((resolve, reject) => {{
            window.nim.rejectFriendApply({{
                account: {ToJsonString(account)},
                ps: {ToJsonString(ps)},
                done: (err) => {{
                    if (err) reject(err);
                    else resolve();
                }}
            }});
            setTimeout(() => reject(new Error('Timeout')), 10000);
        }});
        result.success = true;
    }} catch(e) {{
        result.error = e.message;
    }}
    return JSON.stringify(result);
}})();";

                var resp = await ExecuteScriptWithResultAsync(script, true);
                var ok = !string.IsNullOrEmpty(resp) && resp.Contains("\"success\":true");
                return ok ? (true, "已拒绝好友申请") : (false, ExtractJsonField(resp, "error") ?? resp);
            }
            catch (Exception ex) { return (false, ex.Message); }
        }
        
        /// <summary>
        /// Update friend alias.
        /// 更新好友备注
        /// </summary>
        public async Task<(bool Success, string Message)> UpdateFriendAsync(string account, string alias)
        {
            if (!IsConnected || Mode != ConnectionMode.CDP)
                return (false, "未连接或非CDP模式");
            if (string.IsNullOrWhiteSpace(account))
                return (false, "account为空");

            try
            {
                var script = $@"
(async function() {{
    var result = {{ success:false, error:null }};
    try {{
        if (!window.nim || typeof window.nim.updateFriend !== 'function') {{
            result.error = 'updateFriend not available';
            return JSON.stringify(result);
        }}
        await new Promise((resolve, reject) => {{
            window.nim.updateFriend({{
                account: {ToJsonString(account)},
                alias: {ToJsonString(alias)},
                done: (err) => {{
                    if (err) reject(err);
                    else resolve();
                }}
            }});
            setTimeout(() => reject(new Error('Timeout')), 10000);
        }});
        result.success = true;
    }} catch(e) {{
        result.error = e.message;
    }}
    return JSON.stringify(result);
}})();";

                var resp = await ExecuteScriptWithResultAsync(script, true);
                var ok = !string.IsNullOrEmpty(resp) && resp.Contains("\"success\":true");
                return ok ? (true, "好友备注已更新") : (false, ExtractJsonField(resp, "error") ?? resp);
            }
            catch (Exception ex) { return (false, ex.Message); }
        }
        
        /// <summary>
        /// Check if user is in blacklist.
        /// 检查是否在黑名单
        /// Note: This API requires {account: string} object, not direct string
        /// </summary>
        public async Task<(bool Success, bool InBlacklist, string Message)> IsUserInBlackListAsync(string account)
        {
            if (!IsConnected || Mode != ConnectionMode.CDP)
                return (false, false, "未连接或非CDP模式");
            if (string.IsNullOrWhiteSpace(account))
                return (false, false, "account为空");

            try
            {
                // API requires object format: nim.isUserInBlackList({account: 'xxx'})
                var script = $@"
(function() {{
    var result = {{ success:false, inBlacklist:false, error:null }};
    try {{
        if (!window.nim || typeof window.nim.isUserInBlackList !== 'function') {{
            result.error = 'isUserInBlackList not available';
            return JSON.stringify(result);
        }}
        // Must pass object with account property, not direct string
        var checkResult = window.nim.isUserInBlackList({{ account: {ToJsonString(account)} }});
        result.inBlacklist = checkResult === true;
        result.success = true;
    }} catch(e) {{
        result.error = e.message;
    }}
    return JSON.stringify(result);
}})();";

                var resp = await ExecuteScriptWithResultAsync(script, false);
                var ok = !string.IsNullOrEmpty(resp) && resp.Contains("\"success\":true");
                var inBlacklist = resp?.Contains("\"inBlacklist\":true") == true;
                return ok ? (true, inBlacklist, "OK") : (false, false, ExtractJsonField(resp, "error") ?? resp);
            }
            catch (Exception ex) { return (false, false, ex.Message); }
        }
        
        /// <summary>
        /// Add user to mute list.
        /// 添加到静音列表
        /// </summary>
        public async Task<(bool Success, string Message)> AddToMutelistAsync(string account)
        {
            if (!IsConnected || Mode != ConnectionMode.CDP)
                return (false, "未连接或非CDP模式");
            if (string.IsNullOrWhiteSpace(account))
                return (false, "account为空");

            try
            {
                var script = $@"
(async function() {{
    var result = {{ success:false, error:null }};
    try {{
        if (!window.nim || typeof window.nim.addToMutelist !== 'function') {{
            result.error = 'addToMutelist not available';
            return JSON.stringify(result);
        }}
        await new Promise((resolve, reject) => {{
            window.nim.addToMutelist({{
                account: {ToJsonString(account)},
                done: (err) => {{
                    if (err) reject(err);
                    else resolve();
                }}
            }});
            setTimeout(() => reject(new Error('Timeout')), 10000);
        }});
        result.success = true;
    }} catch(e) {{
        result.error = e.message;
    }}
    return JSON.stringify(result);
}})();";

                var resp = await ExecuteScriptWithResultAsync(script, true);
                var ok = !string.IsNullOrEmpty(resp) && resp.Contains("\"success\":true");
                return ok ? (true, "已加入静音列表") : (false, ExtractJsonField(resp, "error") ?? resp);
            }
            catch (Exception ex) { return (false, ex.Message); }
        }
        
        /// <summary>
        /// Remove user from mute list.
        /// 从静音列表移除
        /// </summary>
        public async Task<(bool Success, string Message)> RemoveFromMutelistAsync(string account)
        {
            if (!IsConnected || Mode != ConnectionMode.CDP)
                return (false, "未连接或非CDP模式");
            if (string.IsNullOrWhiteSpace(account))
                return (false, "account为空");

            try
            {
                var script = $@"
(async function() {{
    var result = {{ success:false, error:null }};
    try {{
        if (!window.nim || typeof window.nim.removeFromMutelist !== 'function') {{
            result.error = 'removeFromMutelist not available';
            return JSON.stringify(result);
        }}
        await new Promise((resolve, reject) => {{
            window.nim.removeFromMutelist({{
                account: {ToJsonString(account)},
                done: (err) => {{
                    if (err) reject(err);
                    else resolve();
                }}
            }});
            setTimeout(() => reject(new Error('Timeout')), 10000);
        }});
        result.success = true;
    }} catch(e) {{
        result.error = e.message;
    }}
    return JSON.stringify(result);
}})();";

                var resp = await ExecuteScriptWithResultAsync(script, true);
                var ok = !string.IsNullOrEmpty(resp) && resp.Contains("\"success\":true");
                return ok ? (true, "已从静音列表移除") : (false, ExtractJsonField(resp, "error") ?? resp);
            }
            catch (Exception ex) { return (false, ex.Message); }
        }
        
        #endregion

    }
}
