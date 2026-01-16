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
        #region Message APIs - 消息API
        
        /// <summary>
        /// Send a custom message.
        /// 发送自定义消息
        /// </summary>
        public async Task<(bool Success, string IdServer, string Message)> SendCustomMsgAsync(string scene, string to, object content)
        {
            if (!IsConnected || Mode != ConnectionMode.CDP)
                return (false, null, "未连接或非CDP模式");
            if (string.IsNullOrWhiteSpace(scene) || string.IsNullOrWhiteSpace(to))
                return (false, null, "scene/to为空");
            if (content == null)
                return (false, null, "content为空");

            try
            {
                var contentJson = content is string ? (string)content : new System.Web.Script.Serialization.JavaScriptSerializer().Serialize(content);
                var script = $@"
(async function() {{
    var result = {{ success:false, idServer:null, error:null }};
    try {{
        if (!window.nim || typeof window.nim.sendCustomMsg !== 'function') {{
            result.error = 'sendCustomMsg not available';
            return JSON.stringify(result);
        }}
        var msg = await new Promise((resolve, reject) => {{
            window.nim.sendCustomMsg({{
                scene: {ToJsonString(scene)},
                to: {ToJsonString(to)},
                content: {ToJsonString(contentJson)},
                done: (err, msg) => {{
                    if (err) reject(err);
                    else resolve(msg);
                }}
            }});
            setTimeout(() => reject(new Error('Timeout')), 10000);
        }});
        result.success = true;
        result.idServer = msg?.idServer;
    }} catch(e) {{
        result.error = e.message;
    }}
    return JSON.stringify(result);
}})();";

                var resp = await ExecuteScriptWithResultAsync(script, true);
                var ok = !string.IsNullOrEmpty(resp) && resp.Contains("\"success\":true");
                var idServer = ExtractJsonField(resp, "idServer");
                return ok ? (true, idServer, "发送成功") : (false, null, ExtractJsonField(resp, "error") ?? resp);
            }
            catch (Exception ex) { return (false, null, ex.Message); }
        }
        
        /// <summary>
        /// Send a tip message.
        /// 发送提示消息
        /// </summary>
        public async Task<(bool Success, string IdServer, string Message)> SendTipMsgAsync(string scene, string to, string tip)
        {
            if (!IsConnected || Mode != ConnectionMode.CDP)
                return (false, null, "未连接或非CDP模式");
            if (string.IsNullOrWhiteSpace(scene) || string.IsNullOrWhiteSpace(to))
                return (false, null, "scene/to为空");
            if (string.IsNullOrWhiteSpace(tip))
                return (false, null, "tip为空");

            try
            {
                var script = $@"
(async function() {{
    var result = {{ success:false, idServer:null, error:null }};
    try {{
        if (!window.nim || typeof window.nim.sendTipMsg !== 'function') {{
            result.error = 'sendTipMsg not available';
            return JSON.stringify(result);
        }}
        var msg = await new Promise((resolve, reject) => {{
            window.nim.sendTipMsg({{
                scene: {ToJsonString(scene)},
                to: {ToJsonString(to)},
                tip: {ToJsonString(tip)},
                done: (err, msg) => {{
                    if (err) reject(err);
                    else resolve(msg);
                }}
            }});
            setTimeout(() => reject(new Error('Timeout')), 10000);
        }});
        result.success = true;
        result.idServer = msg?.idServer;
    }} catch(e) {{
        result.error = e.message;
    }}
    return JSON.stringify(result);
}})();";

                var resp = await ExecuteScriptWithResultAsync(script, true);
                var ok = !string.IsNullOrEmpty(resp) && resp.Contains("\"success\":true");
                var idServer = ExtractJsonField(resp, "idServer");
                return ok ? (true, idServer, "发送成功") : (false, null, ExtractJsonField(resp, "error") ?? resp);
            }
            catch (Exception ex) { return (false, null, ex.Message); }
        }
        
        /// <summary>
        /// Mark message as read.
        /// 标记消息已读
        /// </summary>
        public async Task<(bool Success, string Message)> MarkMsgReadAsync(string scene, string to)
        {
            if (!IsConnected || Mode != ConnectionMode.CDP)
                return (false, "未连接或非CDP模式");
            if (string.IsNullOrWhiteSpace(scene) || string.IsNullOrWhiteSpace(to))
                return (false, "scene/to为空");

            try
            {
                var script = $@"
(function() {{
    var result = {{ success:false, error:null }};
    try {{
        if (!window.nim || typeof window.nim.markMsgRead !== 'function') {{
            result.error = 'markMsgRead not available';
            return JSON.stringify(result);
        }}
        window.nim.markMsgRead({{
            scene: {ToJsonString(scene)},
            to: {ToJsonString(to)}
        }});
        result.success = true;
    }} catch(e) {{
        result.error = e.message;
    }}
    return JSON.stringify(result);
}})();";

                var resp = await ExecuteScriptWithResultAsync(script, false);
                var ok = !string.IsNullOrEmpty(resp) && resp.Contains("\"success\":true");
                return ok ? (true, "已标记已读") : (false, ExtractJsonField(resp, "error") ?? resp);
            }
            catch (Exception ex) { return (false, ex.Message); }
        }
        
        /// <summary>
        /// Delete a message locally.
        /// 删除本地消息
        /// </summary>
        public async Task<(bool Success, string Message)> DeleteMsgSelfAsync(string idClient, string scene, string to)
        {
            if (!IsConnected || Mode != ConnectionMode.CDP)
                return (false, "未连接或非CDP模式");
            if (string.IsNullOrWhiteSpace(idClient) || string.IsNullOrWhiteSpace(scene) || string.IsNullOrWhiteSpace(to))
                return (false, "idClient/scene/to为空");

            try
            {
                var script = $@"
(async function() {{
    var result = {{ success:false, error:null }};
    try {{
        if (!window.nim || typeof window.nim.deleteMsgSelf !== 'function') {{
            result.error = 'deleteMsgSelf not available';
            return JSON.stringify(result);
        }}
        await new Promise((resolve, reject) => {{
            window.nim.deleteMsgSelf({{
                msg: {{
                    idClient: {ToJsonString(idClient)},
                    scene: {ToJsonString(scene)},
                    to: {ToJsonString(to)}
                }},
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
                return ok ? (true, "已删除") : (false, ExtractJsonField(resp, "error") ?? resp);
            }
            catch (Exception ex) { return (false, ex.Message); }
        }
        
        /// <summary>
        /// Get local system messages.
        /// 获取本地系统消息
        /// </summary>
        public async Task<(bool Success, int Count, string Message)> GetLocalSysMsgsAsync(int limit = 50)
        {
            if (!IsConnected || Mode != ConnectionMode.CDP)
                return (false, 0, "未连接或非CDP模式");

            try
            {
                var script = $@"
(async function() {{
    var result = {{ success:false, count:0, sysMsgs:[], error:null }};
    try {{
        if (!window.nim || typeof window.nim.getLocalSysMsgs !== 'function') {{
            result.error = 'getLocalSysMsgs not available';
            return JSON.stringify(result);
        }}
        var res = await new Promise((resolve, reject) => {{
            window.nim.getLocalSysMsgs({{
                limit: {limit},
                done: (err, res) => {{
                    if (err) reject(err);
                    else resolve(res);
                }}
            }});
            setTimeout(() => reject(new Error('Timeout')), 10000);
        }});
        result.success = true;
        result.count = res?.sysMsgs?.length || 0;
        result.sysMsgs = (res?.sysMsgs || []).slice(0, 20).map(m => ({{
            type: m.type,
            from: m.from,
            to: m.to,
            time: m.time,
            scene: m.scene
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
        /// Send custom system message.
        /// 发送自定义系统消息
        /// </summary>
        public async Task<(bool Success, string Message)> SendCustomSysMsgAsync(string scene, string to, object content)
        {
            if (!IsConnected || Mode != ConnectionMode.CDP)
                return (false, "未连接或非CDP模式");
            if (string.IsNullOrWhiteSpace(scene) || string.IsNullOrWhiteSpace(to))
                return (false, "scene/to为空");
            if (content == null)
                return (false, "content为空");

            try
            {
                var contentJson = content is string ? (string)content : new System.Web.Script.Serialization.JavaScriptSerializer().Serialize(content);
                var script = $@"
(async function() {{
    var result = {{ success:false, error:null }};
    try {{
        if (!window.nim || typeof window.nim.sendCustomSysMsg !== 'function') {{
            result.error = 'sendCustomSysMsg not available';
            return JSON.stringify(result);
        }}
        await new Promise((resolve, reject) => {{
            window.nim.sendCustomSysMsg({{
                scene: {ToJsonString(scene)},
                to: {ToJsonString(to)},
                content: JSON.stringify({contentJson}),
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
                return ok ? (true, "发送成功") : (false, ExtractJsonField(resp, "error") ?? resp);
            }
            catch (Exception ex) { return (false, ex.Message); }
        }
        
        #endregion

    }
}
