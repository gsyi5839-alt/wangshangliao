using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Net.Http;

class CDPTest
{
    static async Task Main()
    {
        try
        {
            Console.WriteLine("=== 开始测试 CDP 连接 ===\n");
            
            // 1. Get WebSocket URL from debug port
            using (var http = new HttpClient())
            {
                var json = await http.GetStringAsync("http://127.0.0.1:9222/json");
                Console.WriteLine("调试端口响应:\n" + json.Substring(0, Math.Min(500, json.Length)) + "\n");
                
                // Extract WebSocket URL
                var wsMatch = System.Text.RegularExpressions.Regex.Match(json, @"""webSocketDebuggerUrl""\s*:\s*""([^""]+)""");
                if (!wsMatch.Success)
                {
                    Console.WriteLine("ERROR: Cannot find WebSocket URL");
                    return;
                }
                
                var wsUrl = wsMatch.Groups[1].Value;
                Console.WriteLine("WebSocket URL: " + wsUrl + "\n");
                
                // 2. Connect to WebSocket
                using (var ws = new ClientWebSocket())
                {
                    var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
                    await ws.ConnectAsync(new Uri(wsUrl), cts.Token);
                    Console.WriteLine("WebSocket 已连接!\n");
                    
                    // 3. Execute JavaScript to get account info
                    var script = @"(function() {
    var result = { url: location.href, title: document.title, account: '', debug: [] };
    
    // Try to find account from page text
    var allText = document.body.innerText || '';
    var wsMatch = allText.match(/旺商号[\s:：]*(\d{6,15})/);
    if (wsMatch) {
        result.account = wsMatch[1];
        result.debug.push('Found from page: ' + wsMatch[1]);
    }
    
    // Try localStorage
    try {
        var keys = Object.keys(localStorage);
        result.debug.push('localStorage keys: ' + keys.length);
        keys.forEach(function(k) {
            if (k.includes('account') || k.includes('user') || k.includes('nim')) {
                var v = localStorage.getItem(k);
                if (v && v.length < 200) {
                    result.debug.push(k + ': ' + v);
                    var m = v.match(/(\d{8,15})/);
                    if (m && !result.account) {
                        result.account = m[1];
                    }
                }
            }
        });
    } catch(e) {}
    
    // Try URL sessionId
    var urlMatch = location.href.match(/sessionId=p2p-(\d+)/);
    if (urlMatch) {
        result.debug.push('Session partner: ' + urlMatch[1]);
    }
    
    return JSON.stringify(result);
})();".Replace("\n", " ").Replace("\r", "");
                    
                    var escapedScript = script.Replace("\\", "\\\\").Replace("\"", "\\\"");
                    var message = "{\"id\":1,\"method\":\"Runtime.evaluate\",\"params\":{\"expression\":\"" + escapedScript + "\",\"returnByValue\":true}}";
                    
                    Console.WriteLine("发送脚本命令...\n");
                    var sendBuffer = Encoding.UTF8.GetBytes(message);
                    await ws.SendAsync(new ArraySegment<byte>(sendBuffer), WebSocketMessageType.Text, true, cts.Token);
                    
                    // 4. Receive response
                    var receiveBuffer = new byte[65536];
                    var response = new StringBuilder();
                    
                    while (true)
                    {
                        var result = await ws.ReceiveAsync(new ArraySegment<byte>(receiveBuffer), cts.Token);
                        response.Append(Encoding.UTF8.GetString(receiveBuffer, 0, result.Count));
                        
                        if (result.EndOfMessage) break;
                    }
                    
                    Console.WriteLine("=== CDP 响应 ===");
                    var responseStr = response.ToString();
                    Console.WriteLine(responseStr.Length > 2000 ? responseStr.Substring(0, 2000) + "..." : responseStr);
                    Console.WriteLine("\n=== 测试完成 ===");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("ERROR: " + ex.Message);
            Console.WriteLine(ex.StackTrace);
        }
    }
}
