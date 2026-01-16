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
        #region System APIs - 系统API
        
        /// <summary>
        /// Check if NIM SDK is connected.
        /// 检查NIM SDK连接状态
        /// </summary>
        public async Task<(bool Success, bool NimConnected, string Message)> IsNimConnectedAsync()
        {
            if (!IsConnected || Mode != ConnectionMode.CDP)
                return (false, false, "未连接或非CDP模式");

            try
            {
                var script = @"
(function() {
    var result = { success:false, nimConnected:false, error:null };
    try {
        if (!window.nim || typeof window.nim.isConnected !== 'function') {
            result.error = 'isConnected not available';
            return JSON.stringify(result);
        }
        result.nimConnected = window.nim.isConnected();
        result.success = true;
    } catch(e) {
        result.error = e.message;
    }
    return JSON.stringify(result);
})();";

                var resp = await ExecuteScriptWithResultAsync(script, false);
                var ok = !string.IsNullOrEmpty(resp) && resp.Contains("\"success\":true");
                var nimConnected = resp?.Contains("\"nimConnected\":true") == true;
                return ok ? (true, nimConnected, "OK") : (false, false, ExtractJsonField(resp, "error") ?? resp);
            }
            catch (Exception ex) { return (false, false, ex.Message); }
        }
        
        /// <summary>
        /// Get NIM SDK login status.
        /// 获取NIM SDK登录状态
        /// </summary>
        public async Task<(bool Success, int Status, string StatusText, string Message)> GetNimLoginStatusAsync()
        {
            if (!IsConnected || Mode != ConnectionMode.CDP)
                return (false, -1, null, "未连接或非CDP模式");

            try
            {
                var script = @"
(function() {
    var result = { success:false, status:-1, statusText:null, error:null };
    try {
        if (!window.nim || typeof window.nim.getLoginStatus !== 'function') {
            result.error = 'getLoginStatus not available';
            return JSON.stringify(result);
        }
        result.status = window.nim.getLoginStatus();
        result.statusText = ['未登录', '已登录', '登录中', '登录失败'][result.status] || '未知';
        result.success = true;
    } catch(e) {
        result.error = e.message;
    }
    return JSON.stringify(result);
})();";

                var resp = await ExecuteScriptWithResultAsync(script, false);
                var ok = !string.IsNullOrEmpty(resp) && resp.Contains("\"success\":true");
                int.TryParse(ExtractJsonField(resp, "status"), out int status);
                var statusText = ExtractJsonField(resp, "statusText");
                return ok ? (true, status, statusText, "OK") : (false, -1, null, ExtractJsonField(resp, "error") ?? resp);
            }
            catch (Exception ex) { return (false, -1, null, ex.Message); }
        }
        
        /// <summary>
        /// Get server time.
        /// 获取服务器时间
        /// </summary>
        public async Task<(bool Success, long ServerTime, string Message)> GetServerTimeAsync()
        {
            if (!IsConnected || Mode != ConnectionMode.CDP)
                return (false, 0, "未连接或非CDP模式");

            try
            {
                var script = @"
(async function() {
    var result = { success:false, serverTime:0, error:null };
    try {
        if (!window.nim || typeof window.nim.getServerTime !== 'function') {
            result.error = 'getServerTime not available';
            return JSON.stringify(result);
        }
        var time = await new Promise((resolve, reject) => {
            window.nim.getServerTime({
                done: (err, time) => {
                    if (err) reject(err);
                    else resolve(time);
                }
            });
            setTimeout(() => reject(new Error('Timeout')), 10000);
        });
        result.success = true;
        result.serverTime = time;
    } catch(e) {
        result.error = e.message;
    }
    return JSON.stringify(result);
})();";

                var resp = await ExecuteScriptWithResultAsync(script, true);
                var ok = !string.IsNullOrEmpty(resp) && resp.Contains("\"success\":true");
                long.TryParse(ExtractJsonField(resp, "serverTime"), out long serverTime);
                return ok ? (true, serverTime, "OK") : (false, 0, ExtractJsonField(resp, "error") ?? resp);
            }
            catch (Exception ex) { return (false, 0, ex.Message); }
        }
        
        /// <summary>
        /// Update my profile info.
        /// 更新我的信息
        /// </summary>
        public async Task<(bool Success, string Message)> UpdateMyInfoAsync(string nick = null, string avatar = null, string custom = null)
        {
            if (!IsConnected || Mode != ConnectionMode.CDP)
                return (false, "未连接或非CDP模式");

            try
            {
                var updates = new List<string>();
                if (!string.IsNullOrEmpty(nick)) updates.Add($"nick: {ToJsonString(nick)}");
                if (!string.IsNullOrEmpty(avatar)) updates.Add($"avatar: {ToJsonString(avatar)}");
                if (!string.IsNullOrEmpty(custom)) updates.Add($"custom: {ToJsonString(custom)}");
                
                if (updates.Count == 0)
                    return (false, "没有要更新的字段");
                
                var script = $@"
(async function() {{
    var result = {{ success:false, error:null }};
    try {{
        if (!window.nim || typeof window.nim.updateMyInfo !== 'function') {{
            result.error = 'updateMyInfo not available';
            return JSON.stringify(result);
        }}
        await new Promise((resolve, reject) => {{
            window.nim.updateMyInfo({{
                {string.Join(", ", updates)},
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
                return ok ? (true, "信息已更新") : (false, ExtractJsonField(resp, "error") ?? resp);
            }
            catch (Exception ex) { return (false, ex.Message); }
        }
        
        /// <summary>
        /// Get local messages by message type.
        /// 按类型获取本地消息
        /// </summary>
        public async Task<(bool Success, int Count, string Message)> GetLocalMsgsAsync(string scene, string to, int limit = 50)
        {
            if (!IsConnected || Mode != ConnectionMode.CDP)
                return (false, 0, "未连接或非CDP模式");

            try
            {
                var script = $@"
(async function() {{
    var result = {{ success:false, count:0, msgs:[], error:null }};
    try {{
        if (!window.nim || typeof window.nim.getLocalMsgs !== 'function') {{
            result.error = 'getLocalMsgs not available';
            return JSON.stringify(result);
        }}
        var res = await new Promise((resolve, reject) => {{
            window.nim.getLocalMsgs({{
                scene: {ToJsonString(scene)},
                to: {ToJsonString(to)},
                limit: {limit},
                done: (err, res) => {{
                    if (err) reject(err);
                    else resolve(res);
                }}
            }});
            setTimeout(() => reject(new Error('Timeout')), 10000);
        }});
        result.success = true;
        result.count = res?.msgs?.length || 0;
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
        
        #endregion

    }
}
