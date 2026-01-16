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
        #region Group Management - Mute/Unmute
        
        /// <summary>
        /// Mute all members in a group chat by simulating UI switch click
        /// 全体禁言 - 通过模拟点击界面开关实现（无频率限制）
        /// </summary>
        /// <param name="groupAccount">群号（如 3962369093），为空则使用当前会话的群</param>
        /// <returns>(success, groupName, message)</returns>
        public async Task<(bool Success, string GroupName, string Message)> MuteAllAsync(string groupAccount = null)
        {
            if (!IsConnected || Mode != ConnectionMode.CDP)
            {
                Log("未连接或非CDP模式，无法执行全体禁言");
                return (false, null, "未连接或非CDP模式");
            }
            
            try
            {
                Log($"开始执行全体禁言... 群号: {groupAccount ?? "当前会话"}");
                
                // DOM AUTOMATION: Click the mute switch directly (most reliable method)
                // This ensures both server and UI state are properly synchronized
                var groupAccountSearch = string.IsNullOrEmpty(groupAccount) ? "null" : ToJsonString(groupAccount);
                
                // FULL AUTOMATION v3: Dropdown -> View Card -> Settings -> Mute switch
                var clickScript = @"
(async function() {
    var result = { success: false, groupName: null, message: '', clicked: false, isNowMuted: null, panelOpen: false, method: 'dom' };
    function sleep(ms) { return new Promise(function(r) { setTimeout(r, ms); }); }
    
    var muteText = '\u5168\u5458\u7981\u8a00';  // 全员禁言
    var viewCardText = '\u67e5\u770b\u7fa4\u540d\u7247';  // 查看群名片
    var settingsText = '\u8bbe\u7f6e';  // 设置
    
    // Helper to find mute switch by checking parent/grandparent/container context
    function findMuteSwitch() {
        var switches = document.querySelectorAll('[class*=""switch""]');
        for (var i = 0; i < switches.length; i++) {
            var sw = switches[i];
            var rect = sw.getBoundingClientRect();
            if (rect.width === 0 || rect.height === 0) continue;
            
            var parent = sw.parentElement;
            var grandparent = parent ? parent.parentElement : null;
            var container = grandparent ? grandparent.parentElement : null;
            var context = (parent ? parent.innerText : '') + ' ' + (grandparent ? grandparent.innerText : '') + ' ' + (container ? container.innerText : '');
            
            if (context.indexOf(muteText) >= 0) {
                return sw;
            }
        }
        return null;
    }
    
    // Helper to get current group name from Pinia
    function getGroupName() {
        try {
            var app = document.querySelector('#app');
            var gp = app && app.__vue_app__ && app.__vue_app__.config && app.__vue_app__.config.globalProperties;
            var pinia = gp && gp.$pinia;
            var appStore = pinia && pinia._s && pinia._s.get && pinia._s.get('app');
            var groupInfoRes = appStore ? appStore.groupInfoRes : null;
            if (groupInfoRes && groupInfoRes.groupInfo) {
                return groupInfoRes.groupInfo.name || groupInfoRes.groupInfo.groupName;
            }
        } catch(e) {}
        return null;
    }
    
    // CRITICAL: Ensure session context is set for backend API (fixes GroupId > 0 error)
    function ensureSessionContext() {
        try {
            var app = document.querySelector('#app');
            var gp = app && app.__vue_app__ && app.__vue_app__.config && app.__vue_app__.config.globalProperties;
            var pinia = gp && gp.$pinia;
            var appStore = pinia && pinia._s && pinia._s.get && pinia._s.get('app');
            
            if (!appStore) return false;
            
            var groupInfoRes = appStore.groupInfoRes;
            var groupInfo = groupInfoRes ? groupInfoRes.groupInfo : null;
            
            if (groupInfo && groupInfo.groupId && appStore.setCurrentSession) {
                appStore.setCurrentSession({
                    id: groupInfo.groupCloudId || groupInfo.teamId,
                    scene: 'team',
                    to: groupInfo.groupCloudId || groupInfo.teamId,
                    groupId: groupInfo.groupId,
                    groupAccount: groupInfo.groupAccount,
                    groupCloudId: groupInfo.groupCloudId,
                    name: groupInfo.name || groupInfo.groupName
                });
                return true;
            }
            return false;
        } catch(e) {
            return false;
        }
    }
    
    try {
        result.groupName = getGroupName();
        
        // IMPORTANT: Set session context before mute operation
        ensureSessionContext();
        
        // STEP 1: Check if mute switch is already visible
        var muteSwitch = findMuteSwitch();
        
        if (!muteSwitch) {
            // STEP 2: Find and click header dropdown (... button)
            var dropdowns = document.querySelectorAll('.el-dropdown');
            var headerDropdown = null;
            
            for (var d = 0; d < dropdowns.length; d++) {
                var dd = dropdowns[d];
                var rect = dd.getBoundingClientRect();
                if (rect.width > 0 && rect.height > 0 && rect.y < 150 && rect.x > 400) {
                    headerDropdown = dd;
                    break;
                }
            }
            
            if (!headerDropdown) {
                result.message = '\u65e0\u6cd5\u627e\u5230\u4e0b\u62c9\u83dc\u5355\u6309\u94ae';
                return JSON.stringify(result);
            }
            
            headerDropdown.click();
            await sleep(800);
            
            // STEP 3: Find and click View Group Card using correct selector
            var menuItems = document.querySelectorAll('.el-dropdown-menu__item');
            var viewCardEl = null;
            
            for (var m = 0; m < menuItems.length; m++) {
                var item = menuItems[m];
                var text = (item.innerText || item.textContent || '').trim();
                if (text.indexOf(viewCardText) >= 0) {
                    viewCardEl = item;
                    break;
                }
            }
            
            if (!viewCardEl) {
                result.message = '\u65e0\u6cd5\u627e\u5230\u67e5\u770b\u7fa4\u540d\u7247\u83dc\u5355\u9879';
                return JSON.stringify(result);
            }
            
            viewCardEl.click();
            await sleep(2000);
            
            // CRITICAL: Wait for groupInfoRes to be populated with valid groupId before proceeding
            var groupIdReady = false;
            for (var gRetry = 0; gRetry < 15; gRetry++) {
                try {
                    var app2 = document.querySelector('#app');
                    var gp2 = app2 && app2.__vue_app__ && app2.__vue_app__.config && app2.__vue_app__.config.globalProperties;
                    var pinia2 = gp2 && gp2.$pinia;
                    var appStore2 = pinia2 && pinia2._s && pinia2._s.get && pinia2._s.get('app');
                    var gi2 = appStore2 && appStore2.groupInfoRes && appStore2.groupInfoRes.groupInfo;
                    if (gi2 && gi2.groupId && gi2.groupId > 0) {
                        groupIdReady = true;
                        result.groupId = gi2.groupId;
                        break;
                    }
                } catch(e2) {}
                await sleep(500);
            }
            
            if (!groupIdReady) {
                result.message = 'GroupId\u672a\u5c31\u7eea\uff0c\u8bf7\u7a0d\u540e\u91cd\u8bd5';
                return JSON.stringify(result);
            }
            
            // STEP 4: Find and click Settings button in sidebar
            var allElements = document.querySelectorAll('*');
            var settingsEl = null;
            
            for (var e = 0; e < allElements.length; e++) {
                var el = allElements[e];
                var text = (el.innerText || el.textContent || '').trim();
                if (text === settingsText) {
                    var rect = el.getBoundingClientRect();
                    if (rect.width > 50 && rect.height > 20 && rect.height < 60 && rect.x < 400 && rect.x > 150) {
                        settingsEl = el;
                        break;
                    }
                }
            }
            
            if (!settingsEl) {
                result.message = '\u65e0\u6cd5\u627e\u5230\u8bbe\u7f6e\u6309\u94ae';
                return JSON.stringify(result);
            }
            
            settingsEl.click();
            await sleep(1000);
            
            // STEP 5: Wait for mute switch to appear
            for (var retry = 0; retry < 10; retry++) {
                muteSwitch = findMuteSwitch();
                if (muteSwitch) break;
                await sleep(300);
            }
        }
        
        if (!muteSwitch) {
            result.message = '\u65e0\u6cd5\u627e\u5230\u7981\u8a00\u5f00\u5173';
            return JSON.stringify(result);
        }
        
        result.panelOpen = true;
        
        // STEP 6: Check current state and click to ENABLE mute
        var isCurrentlyOn = muteSwitch.classList.contains('is-checked') || 
                           muteSwitch.getAttribute('aria-checked') === 'true' ||
                           (muteSwitch.querySelector('.is-checked') !== null);
        
        if (!isCurrentlyOn) {
            muteSwitch.click();
            result.clicked = true;
            await sleep(1500);
            
            var isNowOn = muteSwitch.classList.contains('is-checked') || 
                         muteSwitch.getAttribute('aria-checked') === 'true' ||
                         (muteSwitch.querySelector('.is-checked') !== null);
            
            result.isNowMuted = isNowOn;
            result.success = isNowOn;
            result.message = isNowOn ? '\u5168\u5458\u7981\u8a00\u5df2\u5f00\u542f' : '\u70b9\u51fb\u5931\u8d25\uff0c\u8bf7\u91cd\u8bd5';
        } else {
            result.success = true;
            result.isNowMuted = true;
            result.message = '\u5168\u5458\u7981\u8a00\u5df2\u5904\u4e8e\u5f00\u542f\u72b6\u6001';
        }
    } catch(e) {
        result.message = '\u9519\u8bef: ' + e.message;
    }
    return JSON.stringify(result);
})();";
                
                var clickResponse = await ExecuteScriptWithResultAsync(clickScript, true);
                Log($"DOM点击禁言结果: {clickResponse}");
                
                if (!string.IsNullOrEmpty(clickResponse))
                {
                    var clickMessage = ExtractJsonField(clickResponse, "message");
                    var clickGroupName = ExtractJsonField(clickResponse, "groupName");
                    
                    // Check if success
                    if (clickResponse.Contains("\"success\":true"))
                    {
                        return (true, clickGroupName, clickMessage ?? "全体禁言已开启");
                    }
                    
                    // Return the specific error message from the auto-open process
                    if (!string.IsNullOrEmpty(clickMessage))
                    {
                        return (false, clickGroupName, clickMessage);
                    }
                }
                
                // If DOM click didn't work, fall back to API method
                Log("DOM点击方式未成功，尝试API方式...");
                
                var script = $@"
(async function() {{
    function sleep(ms) {{ return new Promise(function(r) {{ setTimeout(r, ms); }}); }}
    async function callUpdateTeamMuteTypeWithRetry(teamId, mute) {{
        var last = null;
        // muteType: 0=unmute all, 2=mute all
        var muteType = mute ? 2 : 0;
        var delays = [0, 1000, 3000, 6000, 10000];
        for (var attempt = 0; attempt < delays.length; attempt++) {{
            if (attempt > 0) await sleep(delays[attempt]);
            last = await new Promise(function(resolve) {{
                try {{
                    if (!window.nim || typeof window.nim.updateTeam !== 'function') {{
                        resolve({{ success: false, error: 'updateTeam not available' }});
                        return;
                    }}
                    window.nim.updateTeam({{
                        teamId: String(teamId),
                        muteType: muteType,
                        done: function(err, obj) {{
                            if (err) resolve({{ success: false, error: err.message || String(err), code: err.code || null }});
                            else resolve({{ success: true }});
                        }}
                    }});
                }} catch(e) {{
                    resolve({{ success: false, error: e.message }});
                }}
                setTimeout(function() {{ resolve({{ success: false, error: 'Timeout' }}); }}, 8000);
            }});
            if (last && last.success) return {{ success: true, attempts: attempt + 1, via: 'updateTeam' }};
            var isRate = last && (last.code === 416 || (last.error && last.error.indexOf('频率') >= 0));
            if (!isRate) return last;
        }}
        return last || {{ success: false, error: 'Unknown error' }};
    }}
    async function callMuteTeamAllWithRetry(teamId, mute) {{
        var last = null;
        // progressive backoff for rate-limit (416)
        // progressive backoff for rate-limit (416)
        var delays = [0, 1000, 3000, 6000, 10000];
        for (var attempt = 0; attempt < delays.length; attempt++) {{
            if (attempt > 0) await sleep(delays[attempt]);
            last = await new Promise(function(resolve) {{
                window.nim.muteTeamAll({{
                    teamId: String(teamId),
                    mute: !!mute,
                    done: function(err, obj) {{
                        if (err) resolve({{ success: false, error: err.message || String(err), code: err.code || null }});
                        else resolve({{ success: true }});
                    }}
                }});
                setTimeout(function() {{ resolve({{ success: false, error: 'Timeout' }}); }}, 8000);
            }});
            if (last && last.success) return {{ success: true, attempts: attempt + 1 }};
            var isRate = last && (last.code === 416 || (last.error && last.error.indexOf('频率') >= 0));
            if (!isRate) return last;
        }}
        // Fallback: updateTeam(muteType) may be routed differently and sometimes bypasses muteTeamAll throttling.
        try {{
            var fb = await callUpdateTeamMuteTypeWithRetry(teamId, mute);
            if (fb && fb.success) return fb;
        }} catch(e) {{}}
        return last || {{ success: false, error: 'Unknown error' }};
    }}

    var result = {{ success: false, message: '', teamId: null, groupId: 0, groupName: null, groupAccount: null, meRole: null, meIsMute: null, error: null, code: null, attempts: 0, confirmedMute: null, teamMuteRaw: null }};
    var targetGroupAccount = {groupAccountSearch};
    
    try {{
        // Prefer Pinia currentSession (most accurate) -> groupList by groupAccount -> URL fallback -> getTeams fallback
        var teamId = null;
        var groupId = 0;
        var groupName = null;
        var groupAccount = null;

        var app = document.querySelector('#app');
        var gp = app && app.__vue_app__ && app.__vue_app__.config && app.__vue_app__.config.globalProperties;
        var pinia = gp && gp.$pinia;
        var appStore = pinia && pinia._s && pinia._s.get && pinia._s.get('app');
        var cacheStore = pinia && pinia._s && pinia._s.get && pinia._s.get('cache');

        function isTeamMuted(team) {{
            if (!team) return null;
            try {{
                // Prefer muteType if present: 2=mute all, 0=unmute all
                if (team.muteType !== undefined && team.muteType !== null) {{
                    return Number(team.muteType) === 2;
                }}
                if (team.muteAll !== undefined && team.muteAll !== null) {{
                    return (team.muteAll === true || team.muteAll === 1 || team.muteAll === '1');
                }}
                var v = team.mute;
                return (v === true || v === 1 || v === '1');
            }} catch(e) {{
                return null;
            }}
        }}

        function teamMuteSnapshot(team) {{
            try {{
                return JSON.stringify({{ mute: team.mute, muteAll: team.muteAll, muteType: team.muteType }}).substring(0, 200);
            }} catch(e) {{
                return null;
            }}
        }}

        async function refreshGroupInfo(gid) {{
            try {{
                if (!gid || Number(gid) <= 0) return;
                if (!cacheStore || typeof cacheStore.getGroupInfo !== 'function') return;
                // Try multiple calling conventions
                try {{ await Promise.resolve(cacheStore.getGroupInfo(Number(gid))); }} catch(e) {{}}
                try {{ await Promise.resolve(cacheStore.getGroupInfo({{ groupId: Number(gid) }})); }} catch(e) {{}}
            }} catch(e) {{}}
        }}

        // current session
        try {{
            var s = appStore ? (appStore.currentSession || appStore.currSession) : null;
            if (s && (s.scene === 'team' || s.sessionType === 'team') && s.to) {{
                teamId = String(s.to);
                if (s.group) {{
                    groupId = Number(s.group.groupId || groupId || 0);
                    groupName = s.group.groupName || s.group.name || groupName;
                    groupAccount = s.group.groupAccount || groupAccount;
                    if (s.group.me) {{
                        result.meRole = s.group.me.role || result.meRole;
                        if (s.group.me.isMute !== undefined) result.meIsMute = s.group.me.isMute;
                    }}
                }}
            }}
        }} catch(e) {{}}

        // groupList search by groupAccount (if provided)
        if (targetGroupAccount && appStore && appStore.groupList) {{
            try {{
                var all = [];
                if (Array.isArray(appStore.groupList.owner)) all = all.concat(appStore.groupList.owner);
                if (Array.isArray(appStore.groupList.member)) all = all.concat(appStore.groupList.member);
                var found = all.find(function(g) {{ return String(g.groupAccount || '') === String(targetGroupAccount); }});
                if (found) {{
                    teamId = String(found.groupCloudId || found.teamId || found.nimGroupId || teamId);
                    groupId = Number(found.groupId || groupId || 0);
                    groupName = found.groupName || found.name || groupName;
                    groupAccount = found.groupAccount || groupAccount;
                    if (found.me) {{
                        result.meRole = found.me.role || result.meRole;
                        if (found.me.isMute !== undefined) result.meIsMute = found.me.isMute;
                    }}
                }}
            }} catch(e) {{}}
        }}

        // URL fallback
        if (!teamId) {{
            try {{
                var urlMatch = window.location.href.match(/team-(\d+)/);
                teamId = urlMatch ? urlMatch[1] : null;
            }} catch(e) {{}}
        }}
        
        // If no teamId from URL, try to get from groups list
        if (!teamId) {{
            var teams = await new Promise(function(resolve) {{
                window.nim.getTeams({{
                    done: function(err, teams) {{
                        resolve(err ? [] : teams);
                    }}
                }});
                setTimeout(function() {{ resolve([]); }}, 3000);
            }});
            
            if (teams.length > 0) {{
                // Use first owned team
                teamId = teams[0].teamId;
                groupName = teams[0].name;
            }}
        }}
        
        if (!teamId) {{
            result.error = 'No teamId found';
            result.message = 'Please open a group chat first';
            return JSON.stringify(result);
        }}

        result.teamId = String(teamId);
        result.groupId = Number(groupId || 0);
        result.groupName = groupName;
        result.groupAccount = groupAccount;
        
        // CRITICAL: Set current session BEFORE calling mute API
        // WangShangLiao backend extracts groupId from currentSession, so we must set it first
        if (groupId > 0 && appStore && typeof appStore.setCurrentSession === 'function') {{
            try {{
                var groupObj = null;
                if (appStore.groupList) {{
                    var allGroups = [];
                    if (Array.isArray(appStore.groupList.owner)) allGroups = allGroups.concat(appStore.groupList.owner);
                    if (Array.isArray(appStore.groupList.member)) allGroups = allGroups.concat(appStore.groupList.member);
                    groupObj = allGroups.find(function(g) {{ return Number(g.groupId) === groupId; }});
                }}
                if (groupObj) {{
                    var groupSession = {{
                        id: 'team-' + teamId,
                        scene: 'team',
                        to: teamId,
                        group: groupObj
                    }};
                    await Promise.resolve(appStore.setCurrentSession(groupSession));
                    result.debug = (result.debug || '') + '[setCurrentSession:ok]';
                }}
            }} catch(e) {{
                result.debug = (result.debug || '') + '[setCurrentSession:' + e.message + ']';
            }}
            await sleep(300);  // Brief wait for session to propagate
        }}
        
        // Get current team info (refresh name + current mute)
        var teamInfo = await new Promise(function(resolve) {{
            window.nim.getTeam({{
                teamId: teamId,
                done: function(err, team) {{
                    resolve(err ? null : team);
                }}
            }});
            setTimeout(function() {{ resolve(null); }}, 3000);
        }});
        
        if (teamInfo) {{
            groupName = teamInfo.name || groupName;
            result.groupName = groupName;
            
            // Check if already muted
            var isMuted = isTeamMuted(teamInfo);
            result.teamMuteRaw = teamMuteSnapshot(teamInfo);
            if (isMuted) {{
                result.success = true;
                result.message = 'Already muted';
                result.confirmedMute = 'true';
                return JSON.stringify(result);
            }}
        }}
        
        // Call muteTeamAll API (with retry for frequency control)
        var apiResult = await callMuteTeamAllWithRetry(teamId, true);
        result.attempts = apiResult.attempts || 1;
        
        if (apiResult.success) {{
            result.success = true;
            result.message = 'Muted successfully';
            // Confirm mute state by polling getTeam a few times (UI may lag)
            try {{
                for (var i=0;i<6;i++) {{
                    var t = await new Promise(function(resolve) {{
                        window.nim.getTeam({{
                            teamId: teamId,
                            done: function(err, team) {{ resolve(err ? null : team); }}
                        }});
                        setTimeout(function() {{ resolve(null); }}, 1200);
                    }});
                    if (t) {{
                        result.teamMuteRaw = teamMuteSnapshot(t) || result.teamMuteRaw;
                        var mv = isTeamMuted(t);
                        if (mv === true) {{ result.confirmedMute = 'true'; break; }}
                    }}
                    await new Promise(function(r){{ setTimeout(r, 300); }});
                }}
            }} catch(e) {{}}
            // Force refresh group info cache so the UI switch can reflect quickly.
            try {{ await refreshGroupInfo(result.groupId); }} catch(e) {{}}
        }} else {{
            result.error = apiResult.error;
            result.code = apiResult.code;
            // Check for frequency control error
            if (apiResult.code === 416 || (apiResult.error && apiResult.error.indexOf('频率') >= 0)) {{
                result.message = '频率限制(416)，已自动重试 ' + (result.attempts || 1) + ' 次，仍未成功；请稍后再试';
            }} else {{
                result.message = apiResult.error;
            }}
        }}
        
    }} catch(e) {{
        result.error = e.message;
        result.message = 'Exception: ' + e.message;
    }}
    
    return JSON.stringify(result);
}})();";
                
                var response = await ExecuteScriptWithResultAsync(script, true);
                Log($"全体禁言执行结果: {response}");
                
                // Parse result
                var groupName = ExtractJsonField(response, "groupName");
                var message = ExtractJsonField(response, "message") ?? response;
                var error = ExtractJsonField(response, "error");
                var confirmedMute = ExtractJsonField(response, "confirmedMute");
                var attempts = ExtractJsonField(response, "attempts");
                var meRole = ExtractJsonField(response, "meRole");
                var meIsMute = ExtractJsonField(response, "meIsMute");
                var teamMuteRaw = ExtractJsonField(response, "teamMuteRaw");
                
                if (response != null && response.Contains("\"success\":true"))
                {
                    // If we managed to confirm, append it for clarity in UI toast.
                    var msg = message;
                    if (!string.IsNullOrEmpty(confirmedMute))
                    {
                        msg = $"{message} (confirmedMute={confirmedMute})";
                    }
                    if (!string.IsNullOrEmpty(attempts))
                    {
                        msg = $"{msg} (attempts={attempts})";
                    }
                    if (!string.IsNullOrEmpty(meRole))
                    {
                        msg = $"{msg} (meRole={meRole})";
                    }
                    if (!string.IsNullOrEmpty(meIsMute))
                    {
                        msg = $"{msg} (meIsMute={meIsMute})";
                    }
                    if (!string.IsNullOrEmpty(teamMuteRaw))
                    {
                        msg = $"{msg} (teamMuteRaw={teamMuteRaw})";
                    }
                    Log($"全体禁言成功: {groupName}");
                    return (true, groupName, msg);
                }
                else
                {
                    // Return error message if available, otherwise return message
                    var errorMsg = !string.IsNullOrEmpty(error) ? error : message;
                    Log($"全体禁言失败: {errorMsg}");
                    return (false, groupName, errorMsg);
                }
            }
            catch (Exception ex)
            {
                Log($"全体禁言异常: {ex.Message}");
                return (false, null, ex.Message);
            }
        }

        /// <summary>
        /// Mute all members by explicit teamId/groupCloudId (no need to open the group chat UI)
        /// 通过 groupCloudId(teamId) 执行全体禁言（推荐用于集成）
        /// </summary>
        public async Task<(bool Success, string GroupName, string Message)> MuteAllByGroupCloudIdAsync(string groupCloudId)
        {
            if (!IsConnected || Mode != ConnectionMode.CDP)
            {
                Log("未连接或非CDP模式，无法执行全体禁言(groupCloudId)");
                return (false, null, "未连接或非CDP模式");
            }

            if (string.IsNullOrWhiteSpace(groupCloudId))
            {
                return (false, null, "groupCloudId 为空");
            }

            try
            {
                Log($"开始执行全体禁言(groupCloudId)... teamId={groupCloudId}");
                var teamIdJson = ToJsonString(groupCloudId);
                var script = $@"
(async function() {{
    var result = {{ success: false, message: '', teamId: null, groupName: null, error: null, code: null }};
    try {{
        if (!window.nim || typeof window.nim.muteTeamAll !== 'function') {{
            result.error = 'window.nim.muteTeamAll not available';
            result.message = result.error;
            return JSON.stringify(result);
        }}

        var teamId = String({teamIdJson});
        result.teamId = teamId;

        // Resolve group name (best-effort)
        try {{
            var teamInfo = await new Promise(function(resolve) {{
                window.nim.getTeam({{
                    teamId: teamId,
                    done: function(err, team) {{ resolve(err ? null : team); }}
                }});
                setTimeout(function() {{ resolve(null); }}, 3000);
            }});
            if (teamInfo && teamInfo.name) result.groupName = teamInfo.name;
        }} catch(e) {{}}

        var apiResult = await new Promise(function(resolve) {{
            window.nim.muteTeamAll({{
                teamId: teamId,
                mute: true,
                done: function(err, obj) {{
                    if (err) resolve({{ success: false, error: err.message || String(err), code: err.code || null }});
                    else resolve({{ success: true }});
                }}
            }});
            setTimeout(function() {{ resolve({{ success: false, error: 'Timeout' }}); }}, 8000);
        }});

        if (apiResult.success) {{
            result.success = true;
            result.message = 'Muted successfully';
        }} else {{
            result.error = apiResult.error;
            result.code = apiResult.code;
            result.message = apiResult.error;
        }}
    }} catch(e) {{
        result.error = e.message;
        result.message = 'Exception: ' + e.message;
    }}
    return JSON.stringify(result);
}})();";

                var response = await ExecuteScriptWithResultAsync(script, true);
                Log($"全体禁言(groupCloudId)结果: {response}");

                var success = !string.IsNullOrEmpty(response) && response.Contains("\"success\":true");
                var groupName = ExtractJsonField(response, "groupName");
                var message = ExtractJsonField(response, "message") ?? response;
                var error = ExtractJsonField(response, "error");

                return success
                    ? (true, groupName, message)
                    : (false, groupName, !string.IsNullOrEmpty(error) ? error : message);
            }
            catch (Exception ex)
            {
                Log($"全体禁言(groupCloudId)异常: {ex.Message}");
                return (false, null, ex.Message);
            }
        }
        
        /// <summary>
        /// Unmute all members in a group chat by group account number
        /// 全体解禁 - 使用 NIM SDK updateTeam 方法设置 muteType=0
        /// </summary>
        /// <param name="groupAccount">群号（如 3962369093），为空则使用当前会话的群</param>
        /// <returns>(success, groupName, message)</returns>
        public async Task<(bool Success, string GroupName, string Message)> UnmuteAllAsync(string groupAccount = null)
        {
            if (!IsConnected || Mode != ConnectionMode.CDP)
            {
                Log("未连接或非CDP模式，无法执行全体解禁");
                return (false, null, "未连接或非CDP模式");
            }
            
            try
            {
                Log($"开始执行全体解禁... 群号: {groupAccount ?? "当前会话"}");
                
                // DOM AUTOMATION: Click the mute switch directly (most reliable method)
                // This ensures both server and UI state are properly synchronized
                var groupAccountSearch = string.IsNullOrEmpty(groupAccount) ? "null" : ToJsonString(groupAccount);
                
                // FULL AUTOMATION v3: Dropdown -> View Card -> Settings -> Unmute switch
                var clickScript = @"
(async function() {
    var result = { success: false, groupName: null, message: '', clicked: false, isNowMuted: null, panelOpen: false, method: 'dom' };
    function sleep(ms) { return new Promise(function(r) { setTimeout(r, ms); }); }
    
    var muteText = '\u5168\u5458\u7981\u8a00';  // 全员禁言
    var viewCardText = '\u67e5\u770b\u7fa4\u540d\u7247';  // 查看群名片
    var settingsText = '\u8bbe\u7f6e';  // 设置
    
    // Helper to find mute switch by checking parent/grandparent/container context
    function findMuteSwitch() {
        var switches = document.querySelectorAll('[class*=""switch""]');
        for (var i = 0; i < switches.length; i++) {
            var sw = switches[i];
            var rect = sw.getBoundingClientRect();
            if (rect.width === 0 || rect.height === 0) continue;
            
            var parent = sw.parentElement;
            var grandparent = parent ? parent.parentElement : null;
            var container = grandparent ? grandparent.parentElement : null;
            var context = (parent ? parent.innerText : '') + ' ' + (grandparent ? grandparent.innerText : '') + ' ' + (container ? container.innerText : '');
            
            if (context.indexOf(muteText) >= 0) {
                return sw;
            }
        }
        return null;
    }
    
    // Helper to get current group name from Pinia
    function getGroupName() {
        try {
            var app = document.querySelector('#app');
            var gp = app && app.__vue_app__ && app.__vue_app__.config && app.__vue_app__.config.globalProperties;
            var pinia = gp && gp.$pinia;
            var appStore = pinia && pinia._s && pinia._s.get && pinia._s.get('app');
            var groupInfoRes = appStore ? appStore.groupInfoRes : null;
            if (groupInfoRes && groupInfoRes.groupInfo) {
                return groupInfoRes.groupInfo.name || groupInfoRes.groupInfo.groupName;
            }
        } catch(e) {}
        return null;
    }
    
    // CRITICAL: Ensure session context is set for backend API (fixes GroupId > 0 error)
    function ensureSessionContext() {
        try {
            var app = document.querySelector('#app');
            var gp = app && app.__vue_app__ && app.__vue_app__.config && app.__vue_app__.config.globalProperties;
            var pinia = gp && gp.$pinia;
            var appStore = pinia && pinia._s && pinia._s.get && pinia._s.get('app');
            
            if (!appStore) return false;
            
            var groupInfoRes = appStore.groupInfoRes;
            var groupInfo = groupInfoRes ? groupInfoRes.groupInfo : null;
            
            if (groupInfo && groupInfo.groupId && appStore.setCurrentSession) {
                appStore.setCurrentSession({
                    id: groupInfo.groupCloudId || groupInfo.teamId,
                    scene: 'team',
                    to: groupInfo.groupCloudId || groupInfo.teamId,
                    groupId: groupInfo.groupId,
                    groupAccount: groupInfo.groupAccount,
                    groupCloudId: groupInfo.groupCloudId,
                    name: groupInfo.name || groupInfo.groupName
                });
                return true;
            }
            return false;
        } catch(e) {
            return false;
        }
    }
    
    try {
        result.groupName = getGroupName();
        
        // IMPORTANT: Set session context before unmute operation
        ensureSessionContext();
        
        // STEP 1: Check if mute switch is already visible
        var muteSwitch = findMuteSwitch();
        
        if (!muteSwitch) {
            // STEP 2: Find and click header dropdown (... button)
            var dropdowns = document.querySelectorAll('.el-dropdown');
            var headerDropdown = null;
            
            for (var d = 0; d < dropdowns.length; d++) {
                var dd = dropdowns[d];
                var rect = dd.getBoundingClientRect();
                if (rect.width > 0 && rect.height > 0 && rect.y < 150 && rect.x > 400) {
                    headerDropdown = dd;
                    break;
                }
            }
            
            if (!headerDropdown) {
                result.message = '\u65e0\u6cd5\u627e\u5230\u4e0b\u62c9\u83dc\u5355\u6309\u94ae';
                return JSON.stringify(result);
            }
            
            headerDropdown.click();
            await sleep(800);
            
            // STEP 3: Find and click View Group Card using correct selector
            var menuItems = document.querySelectorAll('.el-dropdown-menu__item');
            var viewCardEl = null;
            
            for (var m = 0; m < menuItems.length; m++) {
                var item = menuItems[m];
                var text = (item.innerText || item.textContent || '').trim();
                if (text.indexOf(viewCardText) >= 0) {
                    viewCardEl = item;
                    break;
                }
            }
            
            if (!viewCardEl) {
                result.message = '\u65e0\u6cd5\u627e\u5230\u67e5\u770b\u7fa4\u540d\u7247\u83dc\u5355\u9879';
                return JSON.stringify(result);
            }
            
            viewCardEl.click();
            await sleep(2000);
            
            // CRITICAL: Wait for groupInfoRes to be populated with valid groupId before proceeding
            var groupIdReady = false;
            for (var gRetry = 0; gRetry < 15; gRetry++) {
                try {
                    var app2 = document.querySelector('#app');
                    var gp2 = app2 && app2.__vue_app__ && app2.__vue_app__.config && app2.__vue_app__.config.globalProperties;
                    var pinia2 = gp2 && gp2.$pinia;
                    var appStore2 = pinia2 && pinia2._s && pinia2._s.get && pinia2._s.get('app');
                    var gi2 = appStore2 && appStore2.groupInfoRes && appStore2.groupInfoRes.groupInfo;
                    if (gi2 && gi2.groupId && gi2.groupId > 0) {
                        groupIdReady = true;
                        result.groupId = gi2.groupId;
                        break;
                    }
                } catch(e2) {}
                await sleep(500);
            }
            
            if (!groupIdReady) {
                result.message = 'GroupId\u672a\u5c31\u7eea\uff0c\u8bf7\u7a0d\u540e\u91cd\u8bd5';
                return JSON.stringify(result);
            }
            
            // STEP 4: Find and click Settings button in sidebar
            var allElements = document.querySelectorAll('*');
            var settingsEl = null;
            
            for (var e = 0; e < allElements.length; e++) {
                var el = allElements[e];
                var text = (el.innerText || el.textContent || '').trim();
                if (text === settingsText) {
                    var rect = el.getBoundingClientRect();
                    if (rect.width > 50 && rect.height > 20 && rect.height < 60 && rect.x < 400 && rect.x > 150) {
                        settingsEl = el;
                        break;
                    }
                }
            }
            
            if (!settingsEl) {
                result.message = '\u65e0\u6cd5\u627e\u5230\u8bbe\u7f6e\u6309\u94ae';
                return JSON.stringify(result);
            }
            
            settingsEl.click();
            await sleep(1000);
            
            // STEP 5: Wait for mute switch to appear
            for (var retry = 0; retry < 10; retry++) {
                muteSwitch = findMuteSwitch();
                if (muteSwitch) break;
                await sleep(300);
            }
        }
        
        if (!muteSwitch) {
            result.message = '\u65e0\u6cd5\u627e\u5230\u7981\u8a00\u5f00\u5173';
            return JSON.stringify(result);
        }
        
        result.panelOpen = true;
        
        // STEP 6: Check current state and click to DISABLE mute (unmute)
        var isCurrentlyOn = muteSwitch.classList.contains('is-checked') || 
                           muteSwitch.getAttribute('aria-checked') === 'true' ||
                           (muteSwitch.querySelector('.is-checked') !== null);
        
        if (isCurrentlyOn) {
            muteSwitch.click();
            result.clicked = true;
            await sleep(1500);
            
            var isNowOn = muteSwitch.classList.contains('is-checked') || 
                         muteSwitch.getAttribute('aria-checked') === 'true' ||
                         (muteSwitch.querySelector('.is-checked') !== null);
            
            result.isNowMuted = isNowOn;
            result.success = !isNowOn;  // Success if now OFF
            result.message = !isNowOn ? '\u5168\u5458\u7981\u8a00\u5df2\u89e3\u9664' : '\u70b9\u51fb\u5931\u8d25\uff0c\u8bf7\u91cd\u8bd5';
        } else {
            result.success = true;
            result.isNowMuted = false;
            result.message = '\u5168\u5458\u7981\u8a00\u5df2\u5904\u4e8e\u5173\u95ed\u72b6\u6001';
        }
    } catch(e) {
        result.message = '\u9519\u8bef: ' + e.message;
    }
    return JSON.stringify(result);
})();";
                
                var clickResponse = await ExecuteScriptWithResultAsync(clickScript, true);
                Log($"DOM点击解禁结果: {clickResponse}");
                
                if (!string.IsNullOrEmpty(clickResponse))
                {
                    var clickMessage = ExtractJsonField(clickResponse, "message");
                    var clickGroupName = ExtractJsonField(clickResponse, "groupName");
                    
                    // Check if success
                    if (clickResponse.Contains("\"success\":true"))
                    {
                        return (true, clickGroupName, clickMessage ?? "全员禁言已解除");
                    }
                    
                    // Return the specific error message from the auto-open process
                    if (!string.IsNullOrEmpty(clickMessage))
                    {
                        return (false, clickGroupName, clickMessage);
                    }
                }
                
                // If DOM click didn't work, fall back to API method
                Log("DOM点击方式未成功，尝试API方式...");
                
                // Use muteTeamAll API to unmute group (fallback)
                var script = $@"
(async function() {{
    function sleep(ms) {{ return new Promise(function(r) {{ setTimeout(r, ms); }}); }}
    async function callUpdateTeamMuteTypeWithRetry(teamId, mute) {{
        var last = null;
        var muteType = mute ? 2 : 0;
        var delays = [0, 1000, 3000, 6000, 10000];
        for (var attempt = 0; attempt < delays.length; attempt++) {{
            if (attempt > 0) await sleep(delays[attempt]);
            last = await new Promise(function(resolve) {{
                try {{
                    if (!window.nim || typeof window.nim.updateTeam !== 'function') {{
                        resolve({{ success: false, error: 'updateTeam not available' }});
                        return;
                    }}
                    window.nim.updateTeam({{
                        teamId: String(teamId),
                        muteType: muteType,
                        done: function(err, obj) {{
                            if (err) resolve({{ success: false, error: err.message || String(err), code: err.code || null }});
                            else resolve({{ success: true }});
                        }}
                    }});
                }} catch(e) {{
                    resolve({{ success: false, error: e.message }});
                }}
                setTimeout(function() {{ resolve({{ success: false, error: 'Timeout' }}); }}, 8000);
            }});
            if (last && last.success) return {{ success: true, attempts: attempt + 1, via: 'updateTeam' }};
            var isRate = last && (last.code === 416 || (last.error && last.error.indexOf('频率') >= 0));
            if (!isRate) return last;
        }}
        return last || {{ success: false, error: 'Unknown error' }};
    }}
    async function callMuteTeamAllWithRetry(teamId, mute) {{
        var last = null;
        var delays = [0, 1000, 3000, 6000, 10000];
        for (var attempt = 0; attempt < delays.length; attempt++) {{
            if (attempt > 0) await sleep(delays[attempt]);
            last = await new Promise(function(resolve) {{
                window.nim.muteTeamAll({{
                    teamId: String(teamId),
                    mute: !!mute,
                    done: function(err, obj) {{
                        if (err) resolve({{ success: false, error: err.message || String(err), code: err.code || null }});
                        else resolve({{ success: true }});
                    }}
                }});
                setTimeout(function() {{ resolve({{ success: false, error: 'Timeout' }}); }}, 8000);
            }});
            if (last && last.success) return {{ success: true, attempts: attempt + 1 }};
            var isRate = last && (last.code === 416 || (last.error && last.error.indexOf('频率') >= 0));
            if (!isRate) return last;
        }}
        try {{
            var fb = await callUpdateTeamMuteTypeWithRetry(teamId, mute);
            if (fb && fb.success) return fb;
        }} catch(e) {{}}
        return last || {{ success: false, error: 'Unknown error' }};
    }}

    var result = {{ success: false, message: '', teamId: null, groupId: 0, groupName: null, groupAccount: null, meRole: null, meIsMute: null, error: null, code: null, attempts: 0, confirmedMute: null, teamMuteRaw: null }};
    var targetGroupAccount = {groupAccountSearch};
    
    try {{
        // Prefer Pinia currentSession (most accurate) -> groupList by groupAccount -> URL fallback -> getTeams fallback
        var teamId = null;
        var groupId = 0;
        var groupName = null;
        var groupAccount = null;

        var app = document.querySelector('#app');
        var gp = app && app.__vue_app__ && app.__vue_app__.config && app.__vue_app__.config.globalProperties;
        var pinia = gp && gp.$pinia;
        var appStore = pinia && pinia._s && pinia._s.get && pinia._s.get('app');
        var cacheStore = pinia && pinia._s && pinia._s.get && pinia._s.get('cache');

        function isTeamMuted(team) {{
            if (!team) return null;
            try {{
                if (team.muteType !== undefined && team.muteType !== null) {{
                    return Number(team.muteType) === 2;
                }}
                if (team.muteAll !== undefined && team.muteAll !== null) {{
                    return (team.muteAll === true || team.muteAll === 1 || team.muteAll === '1');
                }}
                var v = team.mute;
                return (v === true || v === 1 || v === '1');
            }} catch(e) {{
                return null;
            }}
        }}

        function teamMuteSnapshot(team) {{
            try {{
                return JSON.stringify({{ mute: team.mute, muteAll: team.muteAll, muteType: team.muteType }}).substring(0, 200);
            }} catch(e) {{
                return null;
            }}
        }}

        async function refreshGroupInfo(gid) {{
            try {{
                if (!gid || Number(gid) <= 0) return;
                if (!cacheStore || typeof cacheStore.getGroupInfo !== 'function') return;
                try {{ await Promise.resolve(cacheStore.getGroupInfo(Number(gid))); }} catch(e) {{}}
                try {{ await Promise.resolve(cacheStore.getGroupInfo({{ groupId: Number(gid) }})); }} catch(e) {{}}
            }} catch(e) {{}}
        }}

        // current session
        try {{
            var s = appStore ? (appStore.currentSession || appStore.currSession) : null;
            if (s && (s.scene === 'team' || s.sessionType === 'team') && s.to) {{
                teamId = String(s.to);
                if (s.group) {{
                    groupId = Number(s.group.groupId || groupId || 0);
                    groupName = s.group.groupName || s.group.name || groupName;
                    groupAccount = s.group.groupAccount || groupAccount;
                    if (s.group.me) {{
                        result.meRole = s.group.me.role || result.meRole;
                        if (s.group.me.isMute !== undefined) result.meIsMute = s.group.me.isMute;
                    }}
                }}
            }}
        }} catch(e) {{}}

        // groupList search by groupAccount (if provided)
        if (targetGroupAccount && appStore && appStore.groupList) {{
            try {{
                var all = [];
                if (Array.isArray(appStore.groupList.owner)) all = all.concat(appStore.groupList.owner);
                if (Array.isArray(appStore.groupList.member)) all = all.concat(appStore.groupList.member);
                var found = all.find(function(g) {{ return String(g.groupAccount || '') === String(targetGroupAccount); }});
                if (found) {{
                    teamId = String(found.groupCloudId || found.teamId || found.nimGroupId || teamId);
                    groupId = Number(found.groupId || groupId || 0);
                    groupName = found.groupName || found.name || groupName;
                    groupAccount = found.groupAccount || groupAccount;
                    if (found.me) {{
                        result.meRole = found.me.role || result.meRole;
                        if (found.me.isMute !== undefined) result.meIsMute = found.me.isMute;
                    }}
                }}
            }} catch(e) {{}}
        }}

        // URL fallback
        if (!teamId) {{
            try {{
                var urlMatch = window.location.href.match(/team-(\d+)/);
                teamId = urlMatch ? urlMatch[1] : null;
            }} catch(e) {{}}
        }}
        
        // If no teamId from URL, try to get from groups list
        if (!teamId) {{
            var teams = await new Promise(function(resolve) {{
                window.nim.getTeams({{
                    done: function(err, teams) {{
                        resolve(err ? [] : teams);
                    }}
                }});
                setTimeout(function() {{ resolve([]); }}, 3000);
            }});
            
            if (teams.length > 0) {{
                teamId = teams[0].teamId;
                groupName = teams[0].name;
            }}
        }}
        
        if (!teamId) {{
            result.error = 'No teamId found';
            result.message = 'Please open a group chat first';
            return JSON.stringify(result);
        }}

        result.teamId = String(teamId);
        result.groupId = Number(groupId || 0);
        result.groupName = groupName;
        result.groupAccount = groupAccount;
        
        // CRITICAL: Set current session BEFORE calling unmute API
        // WangShangLiao backend extracts groupId from currentSession, so we must set it first
        if (groupId > 0 && appStore && typeof appStore.setCurrentSession === 'function') {{
            try {{
                var groupObj = null;
                if (appStore.groupList) {{
                    var allGroups = [];
                    if (Array.isArray(appStore.groupList.owner)) allGroups = allGroups.concat(appStore.groupList.owner);
                    if (Array.isArray(appStore.groupList.member)) allGroups = allGroups.concat(appStore.groupList.member);
                    groupObj = allGroups.find(function(g) {{ return Number(g.groupId) === groupId; }});
                }}
                if (groupObj) {{
                    var groupSession = {{
                        id: 'team-' + teamId,
                        scene: 'team',
                        to: teamId,
                        group: groupObj
                    }};
                    await Promise.resolve(appStore.setCurrentSession(groupSession));
                    result.debug = (result.debug || '') + '[setCurrentSession:ok]';
                }}
            }} catch(e) {{
                result.debug = (result.debug || '') + '[setCurrentSession:' + e.message + ']';
            }}
            await sleep(300);  // Brief wait for session to propagate
        }}
        
        // Get current team info
        var teamInfo = await new Promise(function(resolve) {{
            window.nim.getTeam({{
                teamId: teamId,
                done: function(err, team) {{
                    resolve(err ? null : team);
                }}
            }});
            setTimeout(function() {{ resolve(null); }}, 3000);
        }});
        
        if (teamInfo) {{
            groupName = teamInfo.name || groupName;
            result.groupName = groupName;
            
            // Check if already unmuted
            var isMuted = isTeamMuted(teamInfo);
            result.teamMuteRaw = teamMuteSnapshot(teamInfo);
            if (!isMuted) {{
                result.success = true;
                result.message = 'Already unmuted';
                result.confirmedMute = 'false';
                return JSON.stringify(result);
            }}
        }}
        
        // Call muteTeamAll API with mute=false (retry on 416)
        var apiResult = await callMuteTeamAllWithRetry(teamId, false);
        result.attempts = apiResult.attempts || 1;
        
        if (apiResult.success) {{
            result.success = true;
            result.message = 'Unmuted successfully';
            // Confirm mute state by polling getTeam a few times
            try {{
                for (var i=0;i<6;i++) {{
                    var t = await new Promise(function(resolve) {{
                        window.nim.getTeam({{
                            teamId: teamId,
                            done: function(err, team) {{ resolve(err ? null : team); }}
                        }});
                        setTimeout(function() {{ resolve(null); }}, 1200);
                    }});
                    if (t) {{
                        result.teamMuteRaw = teamMuteSnapshot(t) || result.teamMuteRaw;
                        var mv = isTeamMuted(t);
                        if (mv === false) {{ result.confirmedMute = 'false'; break; }}
                    }}
                    await new Promise(function(r){{ setTimeout(r, 300); }});
                }}
            }} catch(e) {{}}
            // Force refresh group info cache so the UI switch can reflect quickly.
            try {{ await refreshGroupInfo(result.groupId); }} catch(e) {{}}
        }} else {{
            result.error = apiResult.error;
            result.code = apiResult.code;
            // Check for frequency control error
            if (apiResult.code === 416 || (apiResult.error && apiResult.error.indexOf('频率') >= 0)) {{
                result.message = '频率限制(416)，已自动重试 ' + (result.attempts || 1) + ' 次，仍未成功；请稍后再试';
            }} else {{
                result.message = apiResult.error;
            }}
        }}
        
    }} catch(e) {{
        result.error = e.message;
        result.message = 'Exception: ' + e.message;
    }}
    
    return JSON.stringify(result);
}})();";
                
                var response = await ExecuteScriptWithResultAsync(script, true);
                Log($"全体解禁执行结果: {response}");
                
                // Parse result
                var groupName = ExtractJsonField(response, "groupName");
                var message = ExtractJsonField(response, "message") ?? response;
                var error = ExtractJsonField(response, "error");
                var confirmedMute = ExtractJsonField(response, "confirmedMute");
                var attempts = ExtractJsonField(response, "attempts");
                var meRole = ExtractJsonField(response, "meRole");
                var meIsMute = ExtractJsonField(response, "meIsMute");
                var teamMuteRaw = ExtractJsonField(response, "teamMuteRaw");
                
                if (response != null && response.Contains("\"success\":true"))
                {
                    Log($"全体解禁成功: {groupName}");
                    var msg = message;
                    if (!string.IsNullOrEmpty(confirmedMute))
                    {
                        msg = $"{message} (confirmedMute={confirmedMute})";
                    }
                    if (!string.IsNullOrEmpty(attempts))
                    {
                        msg = $"{msg} (attempts={attempts})";
                    }
                    if (!string.IsNullOrEmpty(meRole))
                    {
                        msg = $"{msg} (meRole={meRole})";
                    }
                    if (!string.IsNullOrEmpty(meIsMute))
                    {
                        msg = $"{msg} (meIsMute={meIsMute})";
                    }
                    if (!string.IsNullOrEmpty(teamMuteRaw))
                    {
                        msg = $"{msg} (teamMuteRaw={teamMuteRaw})";
                    }
                    return (true, groupName, msg);
                }
                else
                {
                    // Return error message if available, otherwise return message
                    var errorMsg = !string.IsNullOrEmpty(error) ? error : message;
                    Log($"全体解禁失败: {errorMsg}");
                    return (false, groupName, errorMsg);
                }
            }
            catch (Exception ex)
            {
                Log($"全体解禁异常: {ex.Message}");
                return (false, null, ex.Message);
            }
        }

        /// <summary>
        /// Unmute all members by explicit teamId/groupCloudId (no need to open the group chat UI)
        /// 通过 groupCloudId(teamId) 执行全体解禁（推荐用于集成）
        /// </summary>
        public async Task<(bool Success, string GroupName, string Message)> UnmuteAllByGroupCloudIdAsync(string groupCloudId)
        {
            if (!IsConnected || Mode != ConnectionMode.CDP)
            {
                Log("未连接或非CDP模式，无法执行全体解禁(groupCloudId)");
                return (false, null, "未连接或非CDP模式");
            }

            if (string.IsNullOrWhiteSpace(groupCloudId))
            {
                return (false, null, "groupCloudId 为空");
            }

            try
            {
                Log($"开始执行全体解禁(groupCloudId)... teamId={groupCloudId}");
                var teamIdJson = ToJsonString(groupCloudId);
                var script = $@"
(async function() {{
    var result = {{ success: false, message: '', teamId: null, groupName: null, error: null, code: null }};
    try {{
        if (!window.nim || typeof window.nim.muteTeamAll !== 'function') {{
            result.error = 'window.nim.muteTeamAll not available';
            result.message = result.error;
            return JSON.stringify(result);
        }}

        var teamId = String({teamIdJson});
        result.teamId = teamId;

        // Resolve group name (best-effort)
        try {{
            var teamInfo = await new Promise(function(resolve) {{
                window.nim.getTeam({{
                    teamId: teamId,
                    done: function(err, team) {{ resolve(err ? null : team); }}
                }});
                setTimeout(function() {{ resolve(null); }}, 3000);
            }});
            if (teamInfo && teamInfo.name) result.groupName = teamInfo.name;
        }} catch(e) {{}}

        var apiResult = await new Promise(function(resolve) {{
            window.nim.muteTeamAll({{
                teamId: teamId,
                mute: false,
                done: function(err, obj) {{
                    if (err) resolve({{ success: false, error: err.message || String(err), code: err.code || null }});
                    else resolve({{ success: true }});
                }}
            }});
            setTimeout(function() {{ resolve({{ success: false, error: 'Timeout' }}); }}, 8000);
        }});

        if (apiResult.success) {{
            result.success = true;
            result.message = 'Unmuted successfully';
        }} else {{
            result.error = apiResult.error;
            result.code = apiResult.code;
            result.message = apiResult.error;
        }}
    }} catch(e) {{
        result.error = e.message;
        result.message = 'Exception: ' + e.message;
    }}
    return JSON.stringify(result);
}})();";

                var response = await ExecuteScriptWithResultAsync(script, true);
                Log($"全体解禁(groupCloudId)结果: {response}");

                var success = !string.IsNullOrEmpty(response) && response.Contains("\"success\":true");
                var groupName = ExtractJsonField(response, "groupName");
                var message = ExtractJsonField(response, "message") ?? response;
                var error = ExtractJsonField(response, "error");

                return success
                    ? (true, groupName, message)
                    : (false, groupName, !string.IsNullOrEmpty(error) ? error : message);
            }
            catch (Exception ex)
            {
                Log($"全体解禁(groupCloudId)异常: {ex.Message}");
                return (false, null, ex.Message);
            }
        }

        /// <summary>
        /// Resolve group identifiers from Pinia store by groupAccount (external group number).
        /// 根据群号(groupAccount，如 3962369093)解析 groupId(数值) 与 groupCloudId(teamId)
        /// </summary>
        public async Task<(bool Success, string GroupName, string GroupAccount, long GroupId, string GroupCloudId, string Message)> ResolveGroupIdentityAsync(string groupAccount)
        {
            if (!IsConnected || Mode != ConnectionMode.CDP)
            {
                return (false, null, groupAccount, 0, null, "未连接或非CDP模式");
            }

            if (string.IsNullOrWhiteSpace(groupAccount))
            {
                return (false, null, null, 0, null, "群号为空");
            }

            try
            {
                var ga = ToJsonString(groupAccount);
                var script = $@"
(async function() {{
    var result = {{ success:false, groupName:null, groupAccount:null, groupId:0, groupCloudId:null, error:null }};
    try {{
        var app = document.querySelector('#app');
        var gp = app && app.__vue_app__ && app.__vue_app__.config && app.__vue_app__.config.globalProperties;
        var pinia = gp && gp.$pinia;
        var appStore = pinia && pinia._s && pinia._s.get && pinia._s.get('app');
        if (!appStore || !appStore.groupList) {{
            result.error = 'Pinia appStore.groupList not available';
            return JSON.stringify(result);
        }}

        var target = String({ga});
        var all = [];
        if (Array.isArray(appStore.groupList.owner)) all = all.concat(appStore.groupList.owner);
        if (Array.isArray(appStore.groupList.member)) all = all.concat(appStore.groupList.member);
        var found = all.find(function(g) {{ return String(g.groupAccount || '') === target; }});

        // fallback: currentSession
        if (!found) {{
            try {{
                var s = appStore.currentSession || appStore.currSession;
                if (s && s.group && String(s.group.groupAccount || '') === target) found = s.group;
            }} catch(e) {{}}
        }}

        if (!found) {{
            result.error = 'Group not found by groupAccount=' + target;
            return JSON.stringify(result);
        }}

        result.success = true;
        result.groupAccount = String(found.groupAccount || target);
        result.groupId = Number(found.groupId || 0);
        result.groupCloudId = String(found.groupCloudId || found.teamId || found.nimGroupId || '');
        result.groupName = found.groupName || found.name || null;
    }} catch(e) {{
        result.error = e.message;
    }}
    return JSON.stringify(result);
}})();";

                var response = await ExecuteScriptWithResultAsync(script, true);
                Log($"解析群标识结果: {response}");

                var ok = !string.IsNullOrEmpty(response) && response.Contains("\"success\":true");
                var name = ExtractJsonField(response, "groupName");
                var acc = ExtractJsonField(response, "groupAccount") ?? groupAccount;
                var cloud = ExtractJsonField(response, "groupCloudId");
                var err = ExtractJsonField(response, "error");

                long gid = 0;
                var gidStr = ExtractJsonField(response, "groupId");
                if (!string.IsNullOrWhiteSpace(gidStr)) long.TryParse(gidStr, out gid);

                return ok
                    ? (true, name, acc, gid, cloud, "OK")
                    : (false, name, acc, gid, cloud, err ?? response);
            }
            catch (Exception ex)
            {
                Log($"解析群标识异常: {ex.Message}");
                return (false, null, groupAccount, 0, null, ex.Message);
            }
        }

        /// <summary>
        /// Get group info via Pinia cacheStore.getGroupInfo using numeric groupId (NOT groupCloudId).
        /// 通过 cacheStore.getGroupInfo 获取群信息（修复 GroupId=0 导致的“无效GetGroupInfo请求”）
        /// </summary>
        public async Task<(bool Success, string GroupName, string Message)> GetGroupInfoByGroupAccountAsync(string groupAccount)
        {
            if (!IsConnected || Mode != ConnectionMode.CDP)
            {
                return (false, null, "未连接或非CDP模式");
            }

            var identity = await ResolveGroupIdentityAsync(groupAccount);
            if (!identity.Success || identity.GroupId <= 0)
            {
                return (false, identity.GroupName, $"解析群失败或 groupId 无效: {identity.GroupId}，{identity.Message}");
            }

            try
            {
                var gid = identity.GroupId.ToString();
                var script = $@"
(async function() {{
    var result = {{ success:false, groupId:{gid}, groupName:null, error:null }};
    try {{
        var app = document.querySelector('#app');
        var gp = app && app.__vue_app__ && app.__vue_app__.config && app.__vue_app__.config.globalProperties;
        var pinia = gp && gp.$pinia;
        var cacheStore = pinia && pinia._s && pinia._s.get && pinia._s.get('cache');
        if (!cacheStore || typeof cacheStore.getGroupInfo !== 'function') {{
            result.error = 'cacheStore.getGroupInfo not available';
            return JSON.stringify(result);
        }}

        // Try multiple calling conventions (the app may accept number or object).
        var info = null;
        try {{ info = await Promise.resolve(cacheStore.getGroupInfo({gid})); }} catch(e) {{}}
        if (!info) {{
            try {{ info = await Promise.resolve(cacheStore.getGroupInfo({{ groupId: {gid} }})); }} catch(e) {{}}
        }}
        if (!info) {{
            try {{ info = await Promise.resolve(cacheStore.getGroupInfo({{ groupId: {gid}, force: true }})); }} catch(e) {{}}
        }}

        if (!info) {{
            result.error = 'getGroupInfo returned null';
            return JSON.stringify(result);
        }}

        // Try to find a human name from returned payload.
        try {{
            result.groupName = info.groupName || info.name || (info.data && (info.data.groupName || info.data.name)) || null;
        }} catch(e) {{}}

        result.success = true;
        // Return a small snippet to avoid huge payloads.
        result.snippet = (function() {{
            try {{ return JSON.stringify(info).substring(0, 1500); }} catch(e) {{ return String(info).substring(0, 1500); }}
        }})();
    }} catch(e) {{
        result.error = e.message;
    }}
    return JSON.stringify(result);
}})();";

                var response = await ExecuteScriptWithResultAsync(script, true);
                Log($"GetGroupInfo结果: {response}");

                var ok = !string.IsNullOrEmpty(response) && response.Contains("\"success\":true");
                var name = ExtractJsonField(response, "groupName") ?? identity.GroupName;
                var err = ExtractJsonField(response, "error");
                var snippet = ExtractJsonField(response, "snippet");
                return ok
                    ? (true, name, snippet ?? "OK")
                    : (false, name, err ?? response);
            }
            catch (Exception ex)
            {
                Log($"GetGroupInfo异常: {ex.Message}");
                return (false, identity.GroupName, ex.Message);
            }
        }
        
        #endregion

    }
}
