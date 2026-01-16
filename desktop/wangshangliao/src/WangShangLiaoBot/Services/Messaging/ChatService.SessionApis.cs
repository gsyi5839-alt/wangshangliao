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
        #region Session APIs - 会话API
        
        /// <summary>
        /// Get local sessions list.
        /// 获取本地会话列表
        /// </summary>
        public async Task<(bool Success, int Count, string Message)> GetLocalSessionsAsync(int limit = 100)
        {
            if (!IsConnected || Mode != ConnectionMode.CDP)
                return (false, 0, "未连接或非CDP模式");

            try
            {
                var script = $@"
(async function() {{
    var result = {{ success:false, count:0, sessions:[], error:null }};
    try {{
        if (!window.nim || typeof window.nim.getLocalSessions !== 'function') {{
            result.error = 'getLocalSessions not available';
            return JSON.stringify(result);
        }}
        var sessions = await new Promise((resolve, reject) => {{
            window.nim.getLocalSessions({{
                limit: {limit},
                done: (err, sessions) => {{
                    if (err) reject(err);
                    else resolve(sessions);
                }}
            }});
            setTimeout(() => reject(new Error('Timeout')), 10000);
        }});
        result.success = true;
        result.count = sessions?.length || 0;
        result.sessions = (sessions || []).slice(0, 50).map(s => ({{
            id: s.id,
            scene: s.scene,
            to: s.to,
            unread: s.unread,
            updateTime: s.updateTime
        }}));
    }} catch(e) {{
        result.error = e.message;
    }}
    return JSON.stringify(result);
}})();";

                var resp = await ExecuteScriptWithResultAsync(script, true);
                var ok = !string.IsNullOrEmpty(resp) && resp.Contains("\"success\":true");
                int.TryParse(ExtractJsonField(resp, "count"), out int count);
                return ok ? (true, count, "OK") : (false, 0, ExtractJsonField(resp, "error") ?? resp);
            }
            catch (Exception ex) { return (false, 0, ex.Message); }
        }
        
        /// <summary>
        /// Set current session.
        /// 设置当前会话
        /// </summary>
        public async Task<(bool Success, string Message)> SetCurrSessionAsync(string sessionId)
        {
            if (!IsConnected || Mode != ConnectionMode.CDP)
                return (false, "未连接或非CDP模式");
            if (string.IsNullOrWhiteSpace(sessionId))
                return (false, "sessionId为空");

            try
            {
                var script = $@"
(function() {{
    var result = {{ success:false, error:null }};
    try {{
        if (!window.nim || typeof window.nim.setCurrSession !== 'function') {{
            result.error = 'setCurrSession not available';
            return JSON.stringify(result);
        }}
        window.nim.setCurrSession({{ id: {ToJsonString(sessionId)} }});
        result.success = true;
    }} catch(e) {{
        result.error = e.message;
    }}
    return JSON.stringify(result);
}})();";

                var resp = await ExecuteScriptWithResultAsync(script, false);
                var ok = !string.IsNullOrEmpty(resp) && resp.Contains("\"success\":true");
                return ok ? (true, "已设置当前会话") : (false, ExtractJsonField(resp, "error") ?? resp);
            }
            catch (Exception ex) { return (false, ex.Message); }
        }
        
        /// <summary>
        /// Reset session unread count.
        /// 重置会话未读数
        /// </summary>
        public async Task<(bool Success, string Message)> ResetSessionUnreadAsync(string sessionId)
        {
            if (!IsConnected || Mode != ConnectionMode.CDP)
                return (false, "未连接或非CDP模式");
            if (string.IsNullOrWhiteSpace(sessionId))
                return (false, "sessionId为空");

            try
            {
                var script = $@"
(function() {{
    var result = {{ success:false, error:null }};
    try {{
        if (!window.nim || typeof window.nim.resetSessionUnread !== 'function') {{
            result.error = 'resetSessionUnread not available';
            return JSON.stringify(result);
        }}
        window.nim.resetSessionUnread({{ id: {ToJsonString(sessionId)} }});
        result.success = true;
    }} catch(e) {{
        result.error = e.message;
    }}
    return JSON.stringify(result);
}})();";

                var resp = await ExecuteScriptWithResultAsync(script, false);
                var ok = !string.IsNullOrEmpty(resp) && resp.Contains("\"success\":true");
                return ok ? (true, "已重置未读数") : (false, ExtractJsonField(resp, "error") ?? resp);
            }
            catch (Exception ex) { return (false, ex.Message); }
        }
        
        /// <summary>
        /// Get stick top (pinned) sessions.
        /// 获取置顶会话
        /// </summary>
        public async Task<(bool Success, int Count, string Message)> GetStickTopSessionsAsync()
        {
            if (!IsConnected || Mode != ConnectionMode.CDP)
                return (false, 0, "未连接或非CDP模式");

            try
            {
                var script = @"
(async function() {
    var result = { success:false, count:0, sessions:[], error:null };
    try {
        if (!window.nim || typeof window.nim.getStickTopSessions !== 'function') {
            result.error = 'getStickTopSessions not available';
            return JSON.stringify(result);
        }
        var sessions = await new Promise((resolve, reject) => {
            window.nim.getStickTopSessions({
                done: (err, sessions) => {
                    if (err) reject(err);
                    else resolve(sessions);
                }
            });
            setTimeout(() => reject(new Error('Timeout')), 10000);
        });
        result.success = true;
        result.count = sessions?.length || 0;
        result.sessions = (sessions || []).map(s => ({
            id: s.id,
            scene: s.scene,
            to: s.to,
            isTop: s.isTop
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
        /// Add a session to stick top (pin).
        /// 置顶会话
        /// </summary>
        public async Task<(bool Success, string Message)> AddStickTopSessionAsync(string sessionId)
        {
            if (!IsConnected || Mode != ConnectionMode.CDP)
                return (false, "未连接或非CDP模式");
            if (string.IsNullOrWhiteSpace(sessionId))
                return (false, "sessionId为空");

            try
            {
                var script = $@"
(async function() {{
    var result = {{ success:false, error:null }};
    try {{
        if (!window.nim || typeof window.nim.addStickTopSession !== 'function') {{
            result.error = 'addStickTopSession not available';
            return JSON.stringify(result);
        }}
        await new Promise((resolve, reject) => {{
            window.nim.addStickTopSession({{
                id: {ToJsonString(sessionId)},
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
                return ok ? (true, "已置顶") : (false, ExtractJsonField(resp, "error") ?? resp);
            }
            catch (Exception ex) { return (false, ex.Message); }
        }
        
        /// <summary>
        /// Remove a session from stick top (unpin).
        /// 取消置顶
        /// </summary>
        public async Task<(bool Success, string Message)> DeleteStickTopSessionAsync(string sessionId)
        {
            if (!IsConnected || Mode != ConnectionMode.CDP)
                return (false, "未连接或非CDP模式");
            if (string.IsNullOrWhiteSpace(sessionId))
                return (false, "sessionId为空");

            try
            {
                var script = $@"
(async function() {{
    var result = {{ success:false, error:null }};
    try {{
        if (!window.nim || typeof window.nim.deleteStickTopSession !== 'function') {{
            result.error = 'deleteStickTopSession not available';
            return JSON.stringify(result);
        }}
        await new Promise((resolve, reject) => {{
            window.nim.deleteStickTopSession({{
                id: {ToJsonString(sessionId)},
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
                return ok ? (true, "已取消置顶") : (false, ExtractJsonField(resp, "error") ?? resp);
            }
            catch (Exception ex) { return (false, ex.Message); }
        }
        
        #endregion

    }
}
