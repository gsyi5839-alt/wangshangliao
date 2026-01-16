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
        #region UI Automation 连接方式
        
        /// <summary>
        /// 使用 UI Automation 连接到旺商聊（不需要调试端口）
        /// </summary>
        public async Task<bool> ConnectWithUIAutomationAsync()
        {
            return await Task.Run(() =>
            {
                try
                {
                    Log("========== 使用 UI Automation 连接 ==========");
                    
                    // 查找旺商聊进程
                    var processes = Process.GetProcessesByName("wangshangliao_win_online");
                    if (processes.Length == 0)
                    {
                        Log("✗ 旺商聊未运行");
                        return false;
                    }
                    
                    var process = processes[0];
                    _mainWindowHandle = process.MainWindowHandle;
                    
                    if (_mainWindowHandle == IntPtr.Zero)
                    {
                        Log("✗ 无法获取旺商聊主窗口句柄");
                        return false;
                    }
                    
                    Log(string.Format("✓ 找到旺商聊窗口 (句柄: {0})", _mainWindowHandle));
                    
                    // 获取 UI Automation 元素
                    _mainWindow = AutomationElement.FromHandle(_mainWindowHandle);
                    if (_mainWindow == null)
                    {
                        Log("✗ 无法获取 UI Automation 元素");
                        return false;
                    }
                    
                    Log("✓ UI Automation 连接成功！");
                    
                    IsConnected = true;
                    Mode = ConnectionMode.UIAutomation;
                    OnConnectionChanged?.Invoke(true);
                    
                    return true;
                }
                catch (Exception ex)
                {
                    Log(string.Format("UI Automation 连接失败: {0}", ex.Message));
                    return false;
                }
            });
        }
        
        /// <summary>
        /// 通过 UI Automation 获取联系人列表
        /// </summary>
        public List<ContactInfo> GetContactsWithUIAutomation()
        {
            var contacts = new List<ContactInfo>();
            
            if (_mainWindow == null)
            {
                Log("UI Automation 未连接");
                return contacts;
            }
            
            try
            {
                // 查找列表元素
                var listCondition = new PropertyCondition(
                    AutomationElement.ControlTypeProperty, ControlType.List);
                var lists = _mainWindow.FindAll(TreeScope.Descendants, listCondition);
                
                Log(string.Format("找到 {0} 个列表控件", lists.Count));
                
                foreach (AutomationElement list in lists)
                {
                    // 获取列表项
                    var itemCondition = new PropertyCondition(
                        AutomationElement.ControlTypeProperty, ControlType.ListItem);
                    var items = list.FindAll(TreeScope.Children, itemCondition);
                    
                    foreach (AutomationElement item in items)
                    {
                        var name = item.Current.Name;
                        if (!string.IsNullOrEmpty(name))
                        {
                            contacts.Add(new ContactInfo
                            {
                                Name = name,
                                WangShangId = ""
                            });
                        }
                    }
                }
                
                Log(string.Format("通过 UI Automation 获取到 {0} 个联系人", contacts.Count));
            }
            catch (Exception ex)
            {
                Log(string.Format("获取联系人失败: {0}", ex.Message));
            }
            
            return contacts;
        }
        
        /// <summary>
        /// 通过 UI Automation 发送消息（模拟输入）
        /// </summary>
        public bool SendMessageWithUIAutomation(string message)
        {
            if (_mainWindow == null)
            {
                Log("UI Automation 未连接");
                return false;
            }
            
            try
            {
                // 查找输入框
                var editCondition = new PropertyCondition(
                    AutomationElement.ControlTypeProperty, ControlType.Edit);
                var edits = _mainWindow.FindAll(TreeScope.Descendants, editCondition);
                
                AutomationElement inputBox = null;
                foreach (AutomationElement edit in edits)
                {
                    var name = edit.Current.Name ?? "";
                    if (name.Contains("消息") || name.Contains("输入"))
                    {
                        inputBox = edit;
                        break;
                    }
                }
                
                if (inputBox == null && edits.Count > 0)
                {
                    // 使用最后一个输入框
                    inputBox = edits[edits.Count - 1];
                }
                
                if (inputBox == null)
                {
                    Log("找不到输入框");
                    return false;
                }
                
                // 设置焦点
                inputBox.SetFocus();
                
                // 尝试使用 ValuePattern 设置文本
                object pattern;
                if (inputBox.TryGetCurrentPattern(ValuePattern.Pattern, out pattern))
                {
                    ((ValuePattern)pattern).SetValue(message);
                }
                else
                {
                    // 使用键盘模拟输入
                    System.Windows.Forms.SendKeys.SendWait(message);
                }
                
                // 查找发送按钮并点击
                var buttonCondition = new PropertyCondition(
                    AutomationElement.ControlTypeProperty, ControlType.Button);
                var buttons = _mainWindow.FindAll(TreeScope.Descendants, buttonCondition);
                
                foreach (AutomationElement button in buttons)
                {
                    var name = button.Current.Name ?? "";
                    if (name.Contains("发送"))
                    {
                        object invokePattern;
                        if (button.TryGetCurrentPattern(InvokePattern.Pattern, out invokePattern))
                        {
                            ((InvokePattern)invokePattern).Invoke();
                            Log("消息已发送 (UI Automation)");
                            return true;
                        }
                    }
                }
                
                // 如果没找到发送按钮，按回车发送
                System.Windows.Forms.SendKeys.SendWait("{ENTER}");
                Log("消息已发送 (回车)");
                return true;
            }
            catch (Exception ex)
            {
                Log(string.Format("发送消息失败: {0}", ex.Message));
                return false;
            }
        }

        // =====================================================================
        // Team member moderation - mute / kick (CDP + NIM SDK)
        // =====================================================================

        /// <summary>
        /// Mute/unmute a specific team member using updateMuteStateInTeam API.
        /// 单人禁言/解禁 - 使用正确的 NIM SDK API
        /// </summary>
        public async Task<(bool Success, string Message)> MuteTeamMemberAsync(string teamId, string accountId, bool mute)
        {
            if (!IsConnected || Mode != ConnectionMode.CDP)
                return (false, "未连接或非CDP模式");
            if (string.IsNullOrWhiteSpace(teamId) || string.IsNullOrWhiteSpace(accountId))
                return (false, "teamId/accountId为空");

            try
            {
                var teamJson = ToJsonString(teamId.Trim());
                var accJson = ToJsonString(accountId.Trim());
                var muteJs = mute ? "true" : "false";

                var script = $@"
(async function() {{
  var result = {{ success:false, message:'', error:null }};
  try {{
    if (!window.nim || typeof window.nim.updateMuteStateInTeam !== 'function') {{
      result.error = 'nim.updateMuteStateInTeam not available';
      result.message = result.error;
      return JSON.stringify(result);
    }}
    
    var apiResult = await new Promise(function(resolve) {{
      window.nim.updateMuteStateInTeam({{
        teamId: {teamJson},
        account: {accJson},
        mute: {muteJs},
        done: function(err, obj) {{
          if (err) {{
            resolve({{ ok: false, err: err.message || String(err), code: err.code }});
          }} else {{
            resolve({{ ok: true, data: obj }});
          }}
        }}
      }});
      setTimeout(function() {{ resolve({{ ok: false, err: 'Timeout' }}); }}, 8000);
      }});
    
    if (apiResult.ok) {{
      result.success = true;
      result.message = {muteJs} ? '已禁言' : '已解除禁言';
    }} else {{
      result.error = apiResult.err;
      result.message = apiResult.err;
    }}
  }} catch(e) {{
    result.error = e.message;
    result.message = 'Exception: ' + e.message;
  }}
  return JSON.stringify(result);
}})();";

                var resp = await ExecuteScriptWithResultAsync(script, true);
                var ok = !string.IsNullOrEmpty(resp) && resp.Contains("\"success\":true");
                var msg = ExtractJsonField(resp, "message") ?? resp;
                var err = ExtractJsonField(resp, "error");
                return ok ? (true, msg) : (false, !string.IsNullOrEmpty(err) ? err : msg);
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }

        /// <summary>
        /// Kick one member from a team (group chat).
        /// </summary>
        public async Task<(bool Success, string Message)> KickTeamMemberAsync(string teamId, string accountId)
        {
            if (!IsConnected || Mode != ConnectionMode.CDP)
                return (false, "未连接或非CDP模式");
            if (string.IsNullOrWhiteSpace(teamId) || string.IsNullOrWhiteSpace(accountId))
                return (false, "teamId/accountId为空");

            try
            {
                var teamJson = ToJsonString(teamId.Trim());
                var accJson = ToJsonString(accountId.Trim());

                var script = $@"
(async function() {{
  var result = {{ success:false, message:'', error:null }};
  try {{
    if (!window.nim) {{
      result.error = 'NIM SDK not found';
      result.message = result.error;
      return JSON.stringify(result);
    }}
    var teamId = String({teamJson});
    var account = String({accJson});
    function call(fnName, payload) {{
      return new Promise(function(resolve) {{
        if (typeof window.nim[fnName] !== 'function') return resolve({{ ok:false, err:'notfound' }});
        payload.done = function(err, obj) {{
          if (err) resolve({{ ok:false, err: err.message || String(err), code: err.code || null }});
          else resolve({{ ok:true }});
        }};
        try {{ window.nim[fnName](payload); }} catch(e) {{ resolve({{ ok:false, err: e.message }}); }}
        setTimeout(function() {{ resolve({{ ok:false, err:'Timeout' }}); }}, 8000);
      }});
    }}
    var r = await call('removeTeamMembers', {{ teamId: teamId, accounts: [account] }});
    if (!r.ok) r = await call('kickTeamMembers', {{ teamId: teamId, accounts: [account] }});
    if (r.ok) {{
      result.success = true;
      result.message = 'Kicked';
    }} else {{
      result.error = r.err;
      result.message = r.err;
    }}
  }} catch(e) {{
    result.error = e.message;
    result.message = 'Exception: ' + e.message;
  }}
  return JSON.stringify(result);
}})();";

                var resp = await ExecuteScriptWithResultAsync(script, true);
                var ok = !string.IsNullOrEmpty(resp) && resp.Contains("\"success\":true");
                var msg = ExtractJsonField(resp, "message") ?? resp;
                var err = ExtractJsonField(resp, "error");
                return ok ? (true, msg) : (false, !string.IsNullOrEmpty(err) ? err : msg);
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }

        /// <summary>
        /// Recall (delete) a message by idClient.
        /// 撤回消息 - 根据消息的 idClient 撤回
        /// </summary>
        /// <param name="msg">ChatMessage with IdClient, GroupId/SenderId</param>
        /// <returns>(Success, Message)</returns>
        public async Task<(bool Success, string Message)> RecallMessageAsync(ChatMessage msg)
        {
            if (!IsConnected || Mode != ConnectionMode.CDP)
                return (false, "未连接或非CDP模式");
            if (msg == null || string.IsNullOrWhiteSpace(msg.IdClient))
                return (false, "消息ID为空");

            try
            {
                var idClientJson = ToJsonString(msg.IdClient);
                var toJson = ToJsonString(msg.IsGroupMessage ? msg.GroupId : msg.SenderId);
                var sceneStr = msg.IsGroupMessage ? "team" : "p2p";

                var script = $@"
(async function() {{
  var result = {{ success:false, message:'', error:null }};
  try {{
    if (!window.nim) {{
      result.error = 'NIM SDK not found';
      return JSON.stringify(result);
    }}
    var msgObj = {{ idClient: {idClientJson}, to: {toJson}, scene: '{sceneStr}' }};
    function call(fnName, payload) {{
      return new Promise(function(resolve) {{
        if (typeof window.nim[fnName] !== 'function') return resolve({{ ok:false, err:'notfound' }});
        payload.done = function(err, obj) {{
          if (err) resolve({{ ok:false, err: err.message || String(err) }});
          else resolve({{ ok:true }});
        }};
        try {{ window.nim[fnName](payload); }} catch(e) {{ resolve({{ ok:false, err: e.message }}); }}
        setTimeout(function() {{ resolve({{ ok:false, err:'Timeout' }}); }}, 5000);
      }});
    }}
    // Try different recall methods
    var r = await call('deleteMsg', {{ msg: msgObj }});
    if (!r.ok) r = await call('recallMsg', {{ msg: msgObj }});
    if (!r.ok) r = await call('deleteMsgSelf', {{ msg: msgObj }});
    if (r.ok) {{
      result.success = true;
      result.message = 'Recalled';
    }} else {{
      result.error = r.err;
      result.message = r.err;
    }}
  }} catch(e) {{
    result.error = e.message;
  }}
  return JSON.stringify(result);
}})();";

                var resp = await ExecuteScriptWithResultAsync(script, true);
                var ok = !string.IsNullOrEmpty(resp) && resp.Contains("\"success\":true");
                var errMsg = ExtractJsonField(resp, "error") ?? ExtractJsonField(resp, "message") ?? resp;
                return ok ? (true, "Recalled") : (false, errMsg);
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }

        /// <summary>
        /// Update a member's nickname in team using updateNickInTeam API.
        /// 修改群成员昵称
        /// </summary>
        public async Task<(bool Success, string Message)> UpdateNickInTeamAsync(string teamId, string accountId, string newNick)
        {
            if (!IsConnected || Mode != ConnectionMode.CDP)
                return (false, "未连接或非CDP模式");
            if (string.IsNullOrWhiteSpace(teamId) || string.IsNullOrWhiteSpace(accountId))
                return (false, "teamId/accountId为空");

            try
            {
                var teamJson = ToJsonString(teamId.Trim());
                var accJson = ToJsonString(accountId.Trim());
                var nickJson = ToJsonString(newNick ?? "");

                var script = $@"
(async function() {{
  var result = {{ success:false, message:'', error:null }};
  try {{
    if (!window.nim || typeof window.nim.updateNickInTeam !== 'function') {{
      result.error = 'nim.updateNickInTeam not available';
      return JSON.stringify(result);
    }}
    
    var apiResult = await new Promise(function(resolve) {{
      window.nim.updateNickInTeam({{
        teamId: {teamJson},
        account: {accJson},
        nickInTeam: {nickJson},
        done: function(err, obj) {{
          if (err) resolve({{ ok: false, err: err.message || String(err) }});
          else resolve({{ ok: true }});
        }}
      }});
      setTimeout(function() {{ resolve({{ ok: false, err: 'Timeout' }}); }}, 8000);
    }});
    
    if (apiResult.ok) {{
      result.success = true;
      result.message = '昵称已修改';
    }} else {{
      result.error = apiResult.err;
      result.message = apiResult.err;
    }}
  }} catch(e) {{
    result.error = e.message;
  }}
  return JSON.stringify(result);
}})();";

                var resp = await ExecuteScriptWithResultAsync(script, true);
                var ok = !string.IsNullOrEmpty(resp) && resp.Contains("\"success\":true");
                var msg = ExtractJsonField(resp, "message") ?? resp;
                return ok ? (true, msg) : (false, ExtractJsonField(resp, "error") ?? msg);
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }

        /// <summary>
        /// Get muted team members using getMutedTeamMembers API.
        /// 获取群禁言成员列表
        /// Note: If getMutedTeamMembers times out, falls back to filtering from getTeamMembers
        /// </summary>
        public async Task<(bool Success, List<string> MutedAccounts, string Message)> GetMutedTeamMembersAsync(string teamId)
        {
            if (!IsConnected || Mode != ConnectionMode.CDP)
                return (false, null, "未连接或非CDP模式");
            if (string.IsNullOrWhiteSpace(teamId))
                return (false, null, "teamId为空");

            try
            {
                var teamJson = ToJsonString(teamId.Trim());

                // Try getMutedTeamMembers first, then fallback to getTeamMembers filtering
                var script = $@"
(async function() {{
  var result = {{ success:false, accounts:[], message:'', error:null, method:'' }};
  try {{
    if (!window.nim) {{
      result.error = 'nim not available';
      return JSON.stringify(result);
    }}
    
    // Method 1: Try getMutedTeamMembers API (may timeout on some servers)
    if (typeof window.nim.getMutedTeamMembers === 'function') {{
      var apiResult = await new Promise(function(resolve) {{
        var resolved = false;
        window.nim.getMutedTeamMembers({{
          teamId: {teamJson},
          done: function(err, members) {{
            if (resolved) return;
            resolved = true;
            if (err) resolve({{ ok: false, err: err.message || String(err) }});
            else resolve({{ ok: true, members: members || [] }});
          }}
        }});
        setTimeout(function() {{ 
          if (!resolved) {{
            resolved = true;
            resolve({{ ok: false, err: 'Timeout' }}); 
          }}
        }}, 5000);
      }});
      
      if (apiResult.ok) {{
        result.success = true;
        result.accounts = (apiResult.members || []).map(function(m) {{ return m.account || m.id; }});
        result.message = '获取成功';
        result.method = 'getMutedTeamMembers';
        return JSON.stringify(result);
      }}
    }}
    
    // Method 2: Fallback - get all members and filter by mute=true
    if (typeof window.nim.getTeamMembers === 'function') {{
      var membersResult = await new Promise(function(resolve) {{
        window.nim.getTeamMembers({{
          teamId: {teamJson},
          done: function(err, obj) {{
            if (err) resolve({{ ok: false, err: err.message || String(err) }});
            else resolve({{ ok: true, members: obj?.members || [] }});
          }}
        }});
        setTimeout(function() {{ resolve({{ ok: false, err: 'Timeout' }}); }}, 8000);
      }});
      
      if (membersResult.ok) {{
        var mutedMembers = (membersResult.members || []).filter(function(m) {{ return m.mute === true; }});
        result.success = true;
        result.accounts = mutedMembers.map(function(m) {{ return m.account; }});
        result.message = '获取成功(备用方法)';
        result.method = 'getTeamMembers+filter';
        return JSON.stringify(result);
      }}
    }}
    
    result.error = 'Both methods failed';
  }} catch(e) {{
    result.error = e.message;
  }}
  return JSON.stringify(result);
}})();";

                var resp = await ExecuteScriptWithResultAsync(script, true);
                var ok = !string.IsNullOrEmpty(resp) && resp.Contains("\"success\":true");
                
                var accounts = new List<string>();
                if (ok)
                {
                    // Parse accounts array from JSON
                    var match = System.Text.RegularExpressions.Regex.Match(resp, @"""accounts""\s*:\s*\[(.*?)\]");
                    if (match.Success)
                    {
                        var accStr = match.Groups[1].Value;
                        var accMatches = System.Text.RegularExpressions.Regex.Matches(accStr, @"""([^""]+)""");
                        foreach (System.Text.RegularExpressions.Match m in accMatches)
                        {
                            accounts.Add(m.Groups[1].Value);
                        }
                    }
                }
                
                return ok ? (true, accounts, $"共{accounts.Count}人被禁言") : (false, null, ExtractJsonField(resp, "error") ?? resp);
            }
            catch (Exception ex)
            {
                return (false, null, ex.Message);
            }
        }

        /// <summary>
        /// Add team managers using addTeamManagers API.
        /// 设置群管理员
        /// </summary>
        public async Task<(bool Success, string Message)> AddTeamManagersAsync(string teamId, List<string> accounts)
        {
            if (!IsConnected || Mode != ConnectionMode.CDP)
                return (false, "未连接或非CDP模式");
            if (string.IsNullOrWhiteSpace(teamId) || accounts == null || accounts.Count == 0)
                return (false, "teamId/accounts为空");

            try
            {
                var teamJson = ToJsonString(teamId.Trim());
                var validAccounts = accounts.Where(a => !string.IsNullOrWhiteSpace(a)).Select(a => a.Trim()).ToList();
                if (validAccounts.Count == 0)
                    return (false, "有效账号列表为空");
                var accountsJson = "[" + string.Join(",", validAccounts.Select(a => ToJsonString(a))) + "]";

                var script = $@"
(async function() {{
  var result = {{ success:false, message:'', error:null }};
  try {{
    if (!window.nim || typeof window.nim.addTeamManagers !== 'function') {{
      result.error = 'nim.addTeamManagers not available';
      return JSON.stringify(result);
    }}
    
    var apiResult = await new Promise(function(resolve) {{
      window.nim.addTeamManagers({{
        teamId: {teamJson},
        accounts: {accountsJson},
        done: function(err, obj) {{
          if (err) resolve({{ ok: false, err: err.message || String(err) }});
          else resolve({{ ok: true }});
        }}
      }});
      setTimeout(function() {{ resolve({{ ok: false, err: 'Timeout' }}); }}, 8000);
    }});
    
    if (apiResult.ok) {{
      result.success = true;
      result.message = '管理员设置成功';
    }} else {{
      result.error = apiResult.err;
      result.message = apiResult.err;
    }}
  }} catch(e) {{
    result.error = e.message;
  }}
  return JSON.stringify(result);
}})();";

                var resp = await ExecuteScriptWithResultAsync(script, true);
                var ok = !string.IsNullOrEmpty(resp) && resp.Contains("\"success\":true");
                return ok ? (true, "管理员设置成功") : (false, ExtractJsonField(resp, "error") ?? resp);
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }

        /// <summary>
        /// Remove team managers using removeTeamManagers API.
        /// 取消群管理员
        /// </summary>
        public async Task<(bool Success, string Message)> RemoveTeamManagersAsync(string teamId, List<string> accounts)
        {
            if (!IsConnected || Mode != ConnectionMode.CDP)
                return (false, "未连接或非CDP模式");
            if (string.IsNullOrWhiteSpace(teamId) || accounts == null || accounts.Count == 0)
                return (false, "teamId/accounts为空");

            try
            {
                var teamJson = ToJsonString(teamId.Trim());
                var validAccounts = accounts.Where(a => !string.IsNullOrWhiteSpace(a)).Select(a => a.Trim()).ToList();
                if (validAccounts.Count == 0)
                    return (false, "有效账号列表为空");
                var accountsJson = "[" + string.Join(",", validAccounts.Select(a => ToJsonString(a))) + "]";

                var script = $@"
(async function() {{
  var result = {{ success:false, message:'', error:null }};
  try {{
    if (!window.nim || typeof window.nim.removeTeamManagers !== 'function') {{
      result.error = 'nim.removeTeamManagers not available';
      return JSON.stringify(result);
    }}
    
    var apiResult = await new Promise(function(resolve) {{
      window.nim.removeTeamManagers({{
        teamId: {teamJson},
        accounts: {accountsJson},
        done: function(err, obj) {{
          if (err) resolve({{ ok: false, err: err.message || String(err) }});
          else resolve({{ ok: true }});
        }}
      }});
      setTimeout(function() {{ resolve({{ ok: false, err: 'Timeout' }}); }}, 8000);
    }});
    
    if (apiResult.ok) {{
      result.success = true;
      result.message = '管理员已取消';
    }} else {{
      result.error = apiResult.err;
      result.message = apiResult.err;
    }}
  }} catch(e) {{
    result.error = e.message;
  }}
  return JSON.stringify(result);
}})();";

                var resp = await ExecuteScriptWithResultAsync(script, true);
                var ok = !string.IsNullOrEmpty(resp) && resp.Contains("\"success\":true");
                return ok ? (true, "管理员已取消") : (false, ExtractJsonField(resp, "error") ?? resp);
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }

        /// <summary>
        /// Get history messages using getHistoryMsgs API.
        /// 获取历史消息
        /// </summary>
        public async Task<(bool Success, List<ChatMessage> Messages, string Message)> GetHistoryMsgsAsync(string scene, string to, int limit = 50)
        {
            if (!IsConnected || Mode != ConnectionMode.CDP)
                return (false, null, "未连接或非CDP模式");
            if (string.IsNullOrWhiteSpace(to))
                return (false, null, "to为空");

            try
            {
                var sceneJson = ToJsonString(scene ?? "team");
                var toJson = ToJsonString(to.Trim());

                var script = $@"
(async function() {{
  var result = {{ success:false, msgs:[], message:'', error:null }};
  try {{
    if (!window.nim || typeof window.nim.getHistoryMsgs !== 'function') {{
      result.error = 'nim.getHistoryMsgs not available';
      return JSON.stringify(result);
    }}
    
    var apiResult = await new Promise(function(resolve) {{
      window.nim.getHistoryMsgs({{
        scene: {sceneJson},
        to: {toJson},
        limit: {limit},
        done: function(err, data) {{
          if (err) resolve({{ ok: false, err: err.message || String(err) }});
          else resolve({{ ok: true, msgs: data.msgs || [] }});
        }}
      }});
      setTimeout(function() {{ resolve({{ ok: false, err: 'Timeout' }}); }}, 15000);
    }});
    
    if (apiResult.ok) {{
      result.success = true;
      result.msgs = (apiResult.msgs || []).map(function(m) {{
        return {{
          idClient: m.idClient,
          idServer: m.idServer,
          type: m.type,
          from: m.from,
          to: m.to,
          text: m.text,
          time: m.time,
          scene: m.scene
        }};
      }});
      result.message = '获取成功';
    }} else {{
      result.error = apiResult.err;
      result.message = apiResult.err;
    }}
  }} catch(e) {{
    result.error = e.message;
  }}
  return JSON.stringify(result);
}})();";

                var resp = await ExecuteScriptWithResultAsync(script, true);
                var ok = !string.IsNullOrEmpty(resp) && resp.Contains("\"success\":true");
                
                var messages = new List<ChatMessage>();
                if (ok)
                {
                    // Parse messages from JSON response
                    try
                    {
                        var msgsMatch = System.Text.RegularExpressions.Regex.Match(resp, @"""msgs""\s*:\s*\[(.*?)\](?=\s*,\s*""message"")");
                        if (msgsMatch.Success)
                        {
                            // Simple parsing - in production use proper JSON parser
                            var msgsJson = msgsMatch.Groups[1].Value;
                            var msgMatches = System.Text.RegularExpressions.Regex.Matches(msgsJson, @"\{[^{}]+\}");
                            foreach (System.Text.RegularExpressions.Match m in msgMatches)
                            {
                                var msgText = ExtractJsonFieldFromStr(m.Value, "text");
                                var msgFrom = ExtractJsonFieldFromStr(m.Value, "from");
                                var msgTime = ExtractJsonFieldFromStr(m.Value, "time");
                                var msgIdClient = ExtractJsonFieldFromStr(m.Value, "idClient");
                                
                                if (!string.IsNullOrEmpty(msgFrom))
                                {
                                    messages.Add(new ChatMessage
                                    {
                                        Content = msgText ?? "",
                                        SenderId = msgFrom,
                                        IdClient = msgIdClient,
                                        GroupId = scene == "team" ? to : null,
                                        IsGroupMessage = scene == "team",
                                        Time = !string.IsNullOrEmpty(msgTime) && long.TryParse(msgTime, out var ts) 
                                            ? DateTimeOffset.FromUnixTimeMilliseconds(ts).LocalDateTime 
                                            : DateTime.Now
                                    });
                                }
                            }
                        }
                    }
                    catch { /* ignore parse errors */ }
                }
                
                return ok ? (true, messages, $"获取{messages.Count}条消息") : (false, null, ExtractJsonField(resp, "error") ?? resp);
            }
            catch (Exception ex)
            {
                return (false, null, ex.Message);
            }
        }
        
        // Helper to extract JSON field from a substring
        private string ExtractJsonFieldFromStr(string json, string field)
        {
            var match = System.Text.RegularExpressions.Regex.Match(json, $@"""{field}""\s*:\s*""([^""]*)""");
            if (match.Success) return match.Groups[1].Value;
            match = System.Text.RegularExpressions.Regex.Match(json, $@"""{field}""\s*:\s*([0-9]+)");
            if (match.Success) return match.Groups[1].Value;
            return null;
        }

        /// <summary>
        /// Update info in team (mute notifications) using updateInfoInTeam API.
        /// 消息免打扰设置
        /// </summary>
        public async Task<(bool Success, string Message)> UpdateInfoInTeamAsync(string teamId, bool muteTeam, int muteNotiType = 0)
        {
            if (!IsConnected || Mode != ConnectionMode.CDP)
                return (false, "未连接或非CDP模式");
            if (string.IsNullOrWhiteSpace(teamId))
                return (false, "teamId为空");

            try
            {
                var teamJson = ToJsonString(teamId.Trim());
                var muteJs = muteTeam ? "true" : "false";

                var script = $@"
(async function() {{
  var result = {{ success:false, message:'', error:null }};
  try {{
    if (!window.nim || typeof window.nim.updateInfoInTeam !== 'function') {{
      result.error = 'nim.updateInfoInTeam not available';
      return JSON.stringify(result);
    }}
    
    var apiResult = await new Promise(function(resolve) {{
      window.nim.updateInfoInTeam({{
        teamId: {teamJson},
        muteTeam: {muteJs},
        muteNotiType: {muteNotiType},
        done: function(err, obj) {{
          if (err) resolve({{ ok: false, err: err.message || String(err) }});
          else resolve({{ ok: true }});
        }}
      }});
      setTimeout(function() {{ resolve({{ ok: false, err: 'Timeout' }}); }}, 8000);
    }});
    
    if (apiResult.ok) {{
      result.success = true;
      result.message = {muteJs} ? '已开启免打扰' : '已关闭免打扰';
    }} else {{
      result.error = apiResult.err;
      result.message = apiResult.err;
    }}
  }} catch(e) {{
    result.error = e.message;
  }}
  return JSON.stringify(result);
}})();";

                var resp = await ExecuteScriptWithResultAsync(script, true);
                var ok = !string.IsNullOrEmpty(resp) && resp.Contains("\"success\":true");
                return ok ? (true, muteTeam ? "已开启免打扰" : "已关闭免打扰") : (false, ExtractJsonField(resp, "error") ?? resp);
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }

        /// <summary>
        /// Forward a message using forwardMsg API.
        /// 转发消息
        /// </summary>
        public async Task<(bool Success, string Message)> ForwardMsgAsync(ChatMessage msg, string toScene, string toId)
        {
            if (!IsConnected || Mode != ConnectionMode.CDP)
                return (false, "未连接或非CDP模式");
            if (msg == null || string.IsNullOrWhiteSpace(msg.IdClient))
                return (false, "消息为空");
            if (string.IsNullOrWhiteSpace(toId))
                return (false, "目标为空");

            try
            {
                var idClientJson = ToJsonString(msg.IdClient);
                var fromSceneJson = ToJsonString(msg.IsGroupMessage ? "team" : "p2p");
                var fromToJson = ToJsonString(msg.IsGroupMessage ? msg.GroupId : msg.SenderId);
                var toSceneJson = ToJsonString(toScene ?? "team");
                var toIdJson = ToJsonString(toId.Trim());

                var script = $@"
(async function() {{
  var result = {{ success:false, message:'', error:null }};
  try {{
    if (!window.nim || typeof window.nim.forwardMsg !== 'function') {{
      result.error = 'nim.forwardMsg not available';
      return JSON.stringify(result);
    }}
    
    var msgObj = {{ idClient: {idClientJson}, scene: {fromSceneJson}, to: {fromToJson} }};
    
    var apiResult = await new Promise(function(resolve) {{
      window.nim.forwardMsg({{
        msg: msgObj,
        scene: {toSceneJson},
        to: {toIdJson},
        done: function(err, obj) {{
          if (err) resolve({{ ok: false, err: err.message || String(err) }});
          else resolve({{ ok: true }});
        }}
      }});
      setTimeout(function() {{ resolve({{ ok: false, err: 'Timeout' }}); }}, 8000);
    }});
    
    if (apiResult.ok) {{
      result.success = true;
      result.message = '转发成功';
    }} else {{
      result.error = apiResult.err;
      result.message = apiResult.err;
    }}
  }} catch(e) {{
    result.error = e.message;
  }}
  return JSON.stringify(result);
}})();";

                var resp = await ExecuteScriptWithResultAsync(script, true);
                var ok = !string.IsNullOrEmpty(resp) && resp.Contains("\"success\":true");
                return ok ? (true, "转发成功") : (false, ExtractJsonField(resp, "error") ?? resp);
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }

        /// <summary>
        /// Add user to blacklist using addToBlacklist API.
        /// 添加到黑名单
        /// </summary>
        public async Task<(bool Success, string Message)> AddToBlacklistAsync(string account)
        {
            if (!IsConnected || Mode != ConnectionMode.CDP)
                return (false, "未连接或非CDP模式");
            if (string.IsNullOrWhiteSpace(account))
                return (false, "account为空");

            try
            {
                var accJson = ToJsonString(account.Trim());

                var script = $@"
(async function() {{
  var result = {{ success:false, message:'', error:null }};
  try {{
    if (!window.nim || typeof window.nim.addToBlacklist !== 'function') {{
      result.error = 'nim.addToBlacklist not available';
      return JSON.stringify(result);
    }}
    
    var apiResult = await new Promise(function(resolve) {{
      window.nim.addToBlacklist({{
        account: {accJson},
        done: function(err, obj) {{
          if (err) resolve({{ ok: false, err: err.message || String(err) }});
          else resolve({{ ok: true }});
        }}
      }});
      setTimeout(function() {{ resolve({{ ok: false, err: 'Timeout' }}); }}, 8000);
    }});
    
    if (apiResult.ok) {{
      result.success = true;
      result.message = '已加入黑名单';
    }} else {{
      result.error = apiResult.err;
      result.message = apiResult.err;
    }}
  }} catch(e) {{
    result.error = e.message;
  }}
  return JSON.stringify(result);
}})();";

                var resp = await ExecuteScriptWithResultAsync(script, true);
                var ok = !string.IsNullOrEmpty(resp) && resp.Contains("\"success\":true");
                return ok ? (true, "已加入黑名单") : (false, ExtractJsonField(resp, "error") ?? resp);
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }

        /// <summary>
        /// Remove user from blacklist using removeFromBlacklist API.
        /// 从黑名单移除
        /// </summary>
        public async Task<(bool Success, string Message)> RemoveFromBlacklistAsync(string account)
        {
            if (!IsConnected || Mode != ConnectionMode.CDP)
                return (false, "未连接或非CDP模式");
            if (string.IsNullOrWhiteSpace(account))
                return (false, "account为空");

            try
            {
                var accJson = ToJsonString(account.Trim());

                var script = $@"
(async function() {{
  var result = {{ success:false, message:'', error:null }};
  try {{
    if (!window.nim || typeof window.nim.removeFromBlacklist !== 'function') {{
      result.error = 'nim.removeFromBlacklist not available';
      return JSON.stringify(result);
    }}
    
    var apiResult = await new Promise(function(resolve) {{
      window.nim.removeFromBlacklist({{
        account: {accJson},
        done: function(err, obj) {{
          if (err) resolve({{ ok: false, err: err.message || String(err) }});
          else resolve({{ ok: true }});
        }}
      }});
      setTimeout(function() {{ resolve({{ ok: false, err: 'Timeout' }}); }}, 8000);
    }});
    
    if (apiResult.ok) {{
      result.success = true;
      result.message = '已从黑名单移除';
    }} else {{
      result.error = apiResult.err;
      result.message = apiResult.err;
    }}
  }} catch(e) {{
    result.error = e.message;
  }}
  return JSON.stringify(result);
}})();";

                var resp = await ExecuteScriptWithResultAsync(script, true);
                var ok = !string.IsNullOrEmpty(resp) && resp.Contains("\"success\":true");
                return ok ? (true, "已从黑名单移除") : (false, ExtractJsonField(resp, "error") ?? resp);
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }

        /// <summary>
        /// Set notification type for new team messages using notifyForNewTeamMsg API.
        /// 群消息通知设置
        /// </summary>
        /// <param name="teamId">Team ID</param>
        /// <param name="notiType">0=all, 1=manager only, 2=none</param>
        public async Task<(bool Success, string Message)> NotifyForNewTeamMsgAsync(string teamId, int notiType)
        {
            if (!IsConnected || Mode != ConnectionMode.CDP)
                return (false, "未连接或非CDP模式");
            if (string.IsNullOrWhiteSpace(teamId))
                return (false, "teamId为空");

            try
            {
                var teamJson = ToJsonString(teamId.Trim());

                var script = $@"
(async function() {{
  var result = {{ success:false, message:'', error:null }};
  try {{
    if (!window.nim || typeof window.nim.notifyForNewTeamMsg !== 'function') {{
      result.error = 'nim.notifyForNewTeamMsg not available';
      return JSON.stringify(result);
    }}
    
    var apiResult = await new Promise(function(resolve) {{
      window.nim.notifyForNewTeamMsg({{
        teamId: {teamJson},
        state: {notiType},
        done: function(err, obj) {{
          if (err) resolve({{ ok: false, err: err.message || String(err) }});
          else resolve({{ ok: true }});
        }}
      }});
      setTimeout(function() {{ resolve({{ ok: false, err: 'Timeout' }}); }}, 8000);
    }});
    
    if (apiResult.ok) {{
      result.success = true;
      result.message = '通知设置已更新';
    }} else {{
      result.error = apiResult.err;
      result.message = apiResult.err;
    }}
  }} catch(e) {{
    result.error = e.message;
  }}
  return JSON.stringify(result);
}})();";

                var resp = await ExecuteScriptWithResultAsync(script, true);
                var ok = !string.IsNullOrEmpty(resp) && resp.Contains("\"success\":true");
                return ok ? (true, "通知设置已更新") : (false, ExtractJsonField(resp, "error") ?? resp);
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }

        /// <summary>
        /// Get my info using getMyInfo API.
        /// 获取我的信息
        /// </summary>
        public async Task<(bool Success, string Account, string Nick, string Message)> GetMyInfoAsync()
        {
            if (!IsConnected || Mode != ConnectionMode.CDP)
                return (false, null, null, "未连接或非CDP模式");

            try
            {
                var script = @"
(async function() {
  var result = { success:false, account:'', nick:'', message:'', error:null };
  try {
    if (!window.nim || typeof window.nim.getMyInfo !== 'function') {
      result.error = 'nim.getMyInfo not available';
      return JSON.stringify(result);
    }
    
    var apiResult = await new Promise(function(resolve) {
      window.nim.getMyInfo({
        done: function(err, info) {
          if (err) resolve({ ok: false, err: err.message || String(err) });
          else resolve({ ok: true, info: info });
        }
      });
      setTimeout(function() { resolve({ ok: false, err: 'Timeout' }); }, 8000);
    });
    
    if (apiResult.ok && apiResult.info) {
      result.success = true;
      result.account = apiResult.info.account || '';
      result.nick = apiResult.info.nick || '';
      result.message = '获取成功';
    } else {
      result.error = apiResult.err;
      result.message = apiResult.err;
    }
  } catch(e) {
    result.error = e.message;
  }
  return JSON.stringify(result);
})();";

                var resp = await ExecuteScriptWithResultAsync(script, true);
                var ok = !string.IsNullOrEmpty(resp) && resp.Contains("\"success\":true");
                var account = ExtractJsonField(resp, "account");
                var nick = ExtractJsonField(resp, "nick");
                return ok ? (true, account, nick, "获取成功") : (false, null, null, ExtractJsonField(resp, "error") ?? resp);
            }
            catch (Exception ex)
            {
                return (false, null, null, ex.Message);
            }
        }
        
        #endregion

    }
}
