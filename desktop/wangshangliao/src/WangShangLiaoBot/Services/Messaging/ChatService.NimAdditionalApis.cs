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
        #region Additional NIM APIs - 完整API接入
        
        /// <summary>
        /// Reject a team apply request.
        /// 拒绝入群申请
        /// </summary>
        public async Task<(bool Success, string Message)> RejectTeamApplyAsync(string teamId, string account, string ps = "")
        {
            if (!IsConnected || Mode != ConnectionMode.CDP)
                return (false, "未连接或非CDP模式");
            if (string.IsNullOrWhiteSpace(teamId) || string.IsNullOrWhiteSpace(account))
                return (false, "teamId/account为空");

            try
            {
                var script = $@"
(async function() {{
    var result = {{ success:false, message:'', error:null }};
    try {{
        if (!window.nim || typeof window.nim.rejectTeamApply !== 'function') {{
            result.error = 'rejectTeamApply not available';
            return JSON.stringify(result);
        }}
        await new Promise((resolve, reject) => {{
            window.nim.rejectTeamApply({{
                teamId: {ToJsonString(teamId)},
                from: {ToJsonString(account)},
                ps: {ToJsonString(ps)},
                done: (err) => {{
                    if (err) reject(err);
                    else resolve();
                }}
            }});
            setTimeout(() => reject(new Error('Timeout')), 10000);
        }});
        result.success = true;
        result.message = '已拒绝入群申请';
    }} catch(e) {{
        result.error = e.message;
        result.message = e.message;
    }}
    return JSON.stringify(result);
}})();";

                var resp = await ExecuteScriptWithResultAsync(script, true);
                var ok = !string.IsNullOrEmpty(resp) && resp.Contains("\"success\":true");
                var msg = ExtractJsonField(resp, "message") ?? resp;
                return ok ? (true, msg) : (false, msg);
            }
            catch (Exception ex) { return (false, ex.Message); }
        }
        
        /// <summary>
        /// Accept a team invite.
        /// 接受入群邀请
        /// </summary>
        public async Task<(bool Success, string Message)> AcceptTeamInviteAsync(string teamId, string from)
        {
            if (!IsConnected || Mode != ConnectionMode.CDP)
                return (false, "未连接或非CDP模式");
            if (string.IsNullOrWhiteSpace(teamId) || string.IsNullOrWhiteSpace(from))
                return (false, "teamId/from为空");

            try
            {
                var script = $@"
(async function() {{
    var result = {{ success:false, message:'', error:null }};
    try {{
        if (!window.nim || typeof window.nim.acceptTeamInvite !== 'function') {{
            result.error = 'acceptTeamInvite not available';
            return JSON.stringify(result);
        }}
        await new Promise((resolve, reject) => {{
            window.nim.acceptTeamInvite({{
                teamId: {ToJsonString(teamId)},
                from: {ToJsonString(from)},
                done: (err) => {{
                    if (err) reject(err);
                    else resolve();
                }}
            }});
            setTimeout(() => reject(new Error('Timeout')), 10000);
        }});
        result.success = true;
        result.message = '已接受入群邀请';
    }} catch(e) {{
        result.error = e.message;
    }}
    return JSON.stringify(result);
}})();";

                var resp = await ExecuteScriptWithResultAsync(script, true);
                var ok = !string.IsNullOrEmpty(resp) && resp.Contains("\"success\":true");
                return ok ? (true, "已接受") : (false, ExtractJsonField(resp, "error") ?? resp);
            }
            catch (Exception ex) { return (false, ex.Message); }
        }
        
        /// <summary>
        /// Reject a team invite.
        /// 拒绝入群邀请
        /// </summary>
        public async Task<(bool Success, string Message)> RejectTeamInviteAsync(string teamId, string from, string ps = "")
        {
            if (!IsConnected || Mode != ConnectionMode.CDP)
                return (false, "未连接或非CDP模式");
            if (string.IsNullOrWhiteSpace(teamId) || string.IsNullOrWhiteSpace(from))
                return (false, "teamId/from为空");

            try
            {
                var script = $@"
(async function() {{
    var result = {{ success:false, error:null }};
    try {{
        if (!window.nim || typeof window.nim.rejectTeamInvite !== 'function') {{
            result.error = 'rejectTeamInvite not available';
            return JSON.stringify(result);
        }}
        await new Promise((resolve, reject) => {{
            window.nim.rejectTeamInvite({{
                teamId: {ToJsonString(teamId)},
                from: {ToJsonString(from)},
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
                return ok ? (true, "已拒绝") : (false, ExtractJsonField(resp, "error") ?? resp);
            }
            catch (Exception ex) { return (false, ex.Message); }
        }
        
        /// <summary>
        /// Create a new team/group.
        /// 创建群
        /// </summary>
        public async Task<(bool Success, string TeamId, string Message)> CreateTeamAsync(string name, List<string> accounts, string intro = "", string announcement = "")
        {
            if (!IsConnected || Mode != ConnectionMode.CDP)
                return (false, null, "未连接或非CDP模式");
            if (string.IsNullOrWhiteSpace(name))
                return (false, null, "群名称为空");
            if (accounts == null || accounts.Count == 0)
                return (false, null, "成员列表为空");

            try
            {
                var accountsJson = "[" + string.Join(",", accounts.Where(a => !string.IsNullOrWhiteSpace(a)).Select(a => ToJsonString(a))) + "]";
                var script = $@"
(async function() {{
    var result = {{ success:false, teamId:null, error:null }};
    try {{
        if (!window.nim || typeof window.nim.createTeam !== 'function') {{
            result.error = 'createTeam not available';
            return JSON.stringify(result);
        }}
        var res = await new Promise((resolve, reject) => {{
            window.nim.createTeam({{
                type: 'advanced',
                name: {ToJsonString(name)},
                accounts: {accountsJson},
                intro: {ToJsonString(intro)},
                announcement: {ToJsonString(announcement)},
                joinMode: 'needVerify',
                done: (err, team) => {{
                    if (err) reject(err);
                    else resolve(team);
                }}
            }});
            setTimeout(() => reject(new Error('Timeout')), 15000);
        }});
        result.success = true;
        result.teamId = res.teamId || res.team?.teamId;
    }} catch(e) {{
        result.error = e.message;
    }}
    return JSON.stringify(result);
}})();";

                var resp = await ExecuteScriptWithResultAsync(script, true);
                var ok = !string.IsNullOrEmpty(resp) && resp.Contains("\"success\":true");
                var teamId = ExtractJsonField(resp, "teamId");
                return ok ? (true, teamId, "创建成功") : (false, null, ExtractJsonField(resp, "error") ?? resp);
            }
            catch (Exception ex) { return (false, null, ex.Message); }
        }
        
        /// <summary>
        /// Dismiss (delete) a team. Only owner can do this.
        /// 解散群（仅群主）
        /// </summary>
        public async Task<(bool Success, string Message)> DismissTeamAsync(string teamId)
        {
            if (!IsConnected || Mode != ConnectionMode.CDP)
                return (false, "未连接或非CDP模式");
            if (string.IsNullOrWhiteSpace(teamId))
                return (false, "teamId为空");

            try
            {
                var script = $@"
(async function() {{
    var result = {{ success:false, error:null }};
    try {{
        if (!window.nim || typeof window.nim.dismissTeam !== 'function') {{
            result.error = 'dismissTeam not available';
            return JSON.stringify(result);
        }}
        await new Promise((resolve, reject) => {{
            window.nim.dismissTeam({{
                teamId: {ToJsonString(teamId)},
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
                return ok ? (true, "群已解散") : (false, ExtractJsonField(resp, "error") ?? resp);
            }
            catch (Exception ex) { return (false, ex.Message); }
        }
        
        /// <summary>
        /// Transfer team ownership to another member.
        /// 转让群主
        /// </summary>
        public async Task<(bool Success, string Message)> TransferTeamAsync(string teamId, string newOwnerAccount, bool leave = false)
        {
            if (!IsConnected || Mode != ConnectionMode.CDP)
                return (false, "未连接或非CDP模式");
            if (string.IsNullOrWhiteSpace(teamId) || string.IsNullOrWhiteSpace(newOwnerAccount))
                return (false, "teamId/newOwnerAccount为空");

            try
            {
                var script = $@"
(async function() {{
    var result = {{ success:false, error:null }};
    try {{
        if (!window.nim || typeof window.nim.transferTeam !== 'function') {{
            result.error = 'transferTeam not available';
            return JSON.stringify(result);
        }}
        await new Promise((resolve, reject) => {{
            window.nim.transferTeam({{
                teamId: {ToJsonString(teamId)},
                account: {ToJsonString(newOwnerAccount)},
                leave: {(leave ? "true" : "false")},
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
                return ok ? (true, "群主已转让") : (false, ExtractJsonField(resp, "error") ?? resp);
            }
            catch (Exception ex) { return (false, ex.Message); }
        }
        
        /// <summary>
        /// Leave a team.
        /// 退出群
        /// </summary>
        public async Task<(bool Success, string Message)> LeaveTeamAsync(string teamId)
        {
            if (!IsConnected || Mode != ConnectionMode.CDP)
                return (false, "未连接或非CDP模式");
            if (string.IsNullOrWhiteSpace(teamId))
                return (false, "teamId为空");

            try
            {
                var script = $@"
(async function() {{
    var result = {{ success:false, error:null }};
    try {{
        if (!window.nim || typeof window.nim.leaveTeam !== 'function') {{
            result.error = 'leaveTeam not available';
            return JSON.stringify(result);
        }}
        await new Promise((resolve, reject) => {{
            window.nim.leaveTeam({{
                teamId: {ToJsonString(teamId)},
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
                return ok ? (true, "已退出群") : (false, ExtractJsonField(resp, "error") ?? resp);
            }
            catch (Exception ex) { return (false, ex.Message); }
        }
        
        /// <summary>
        /// Apply to join a team.
        /// 申请加入群
        /// </summary>
        public async Task<(bool Success, string Message)> ApplyTeamAsync(string teamId, string ps = "")
        {
            if (!IsConnected || Mode != ConnectionMode.CDP)
                return (false, "未连接或非CDP模式");
            if (string.IsNullOrWhiteSpace(teamId))
                return (false, "teamId为空");

            try
            {
                var script = $@"
(async function() {{
    var result = {{ success:false, error:null }};
    try {{
        if (!window.nim || typeof window.nim.applyTeam !== 'function') {{
            result.error = 'applyTeam not available';
            return JSON.stringify(result);
        }}
        await new Promise((resolve, reject) => {{
            window.nim.applyTeam({{
                teamId: {ToJsonString(teamId)},
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
                return ok ? (true, "申请已发送") : (false, ExtractJsonField(resp, "error") ?? resp);
            }
            catch (Exception ex) { return (false, ex.Message); }
        }
        
        /// <summary>
        /// Add members to a team.
        /// 添加群成员
        /// </summary>
        public async Task<(bool Success, string Message)> AddTeamMembersAsync(string teamId, List<string> accounts, string ps = "")
        {
            if (!IsConnected || Mode != ConnectionMode.CDP)
                return (false, "未连接或非CDP模式");
            if (string.IsNullOrWhiteSpace(teamId))
                return (false, "teamId为空");
            if (accounts == null || accounts.Count == 0)
                return (false, "成员列表为空");

            try
            {
                var accountsJson = "[" + string.Join(",", accounts.Where(a => !string.IsNullOrWhiteSpace(a)).Select(a => ToJsonString(a))) + "]";
                var script = $@"
(async function() {{
    var result = {{ success:false, error:null }};
    try {{
        if (!window.nim || typeof window.nim.addTeamMembers !== 'function') {{
            result.error = 'addTeamMembers not available';
            return JSON.stringify(result);
        }}
        await new Promise((resolve, reject) => {{
            window.nim.addTeamMembers({{
                teamId: {ToJsonString(teamId)},
                accounts: {accountsJson},
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
                return ok ? (true, "已添加成员") : (false, ExtractJsonField(resp, "error") ?? resp);
            }
            catch (Exception ex) { return (false, ex.Message); }
        }
        
        #endregion

    }
}
