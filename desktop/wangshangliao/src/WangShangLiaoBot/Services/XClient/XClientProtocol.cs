using System;
using System.Net.Sockets;
using System.Text;
using System.Web.Script.Serialization;
using System.Threading.Tasks;

namespace WangShangLiaoBot.Services.XClient
{
    /// <summary>
    /// xclient协议实现
    /// 基于逆向分析的21303端口协议
    /// </summary>
    public class XClientProtocol
    {
        private const int XCLIENT_PORT = 21303;
        private const string XCLIENT_HOST = "127.0.0.1";
        
        /// <summary>
        /// 消息类型
        /// </summary>
        public enum MessageType : byte
        {
            Request = 1,
            Error = 5
        }
        
        /// <summary>
        /// API端点
        /// </summary>
        public static class ApiUrl
        {
            public const string Login = "/v1/user/login";
            public const string Logout = "/v1/user/logout";
            public const string GetFriendList = "/v1/friend/get-friend-list";
            public const string UpdateSession = "/v1/user/update-session";
            public const string RefreshToken = "/v1/user/RefreshToken";
            public const string SetAvatar = "/v1/settings/avatar";
            public const string SetNickName = "/v1/settings/self-nick-name";
            public const string QueryAppSetting = "/v1/settings/query-app-settings";
            public const string GetAutoReplyState = "/v1/user/get-auto-replies-online-state";
            public const string SetAutoReply = "/v1/settings/set-auto-reply";
            public const string GetSystemSetting = "/v1/settings/get-system-setting";
            public const string SetNotifyState = "/v1/settings/set-notify-state";
            public const string SetSessionTop = "/v1/settings/set-session-top";
        }
        
        /// <summary>
        /// 构建协议消息
        /// 格式: [Type:1] [Flags:1] [Length:4BE] [JSON]
        /// </summary>
        public static byte[] BuildMessage(object payload, byte type = 1, byte flags = 0)
        {
            var json = new JavaScriptSerializer().Serialize(payload);
            var jsonBytes = Encoding.UTF8.GetBytes(json);
            
            var message = new byte[6 + jsonBytes.Length];
            message[0] = type;
            message[1] = flags;
            
            // 大端序长度
            var lenBytes = BitConverter.GetBytes(jsonBytes.Length);
            if (BitConverter.IsLittleEndian)
                Array.Reverse(lenBytes);
            Array.Copy(lenBytes, 0, message, 2, 4);
            
            Array.Copy(jsonBytes, 0, message, 6, jsonBytes.Length);
            
            return message;
        }
        
        /// <summary>
        /// 解析响应
        /// </summary>
        public static (byte type, byte flags, int length, string payload) ParseResponse(byte[] data)
        {
            if (data.Length < 6)
                return (0, 0, 0, "响应太短");
            
            var type = data[0];
            var flags = data[1];
            
            var lenBytes = new byte[4];
            Array.Copy(data, 2, lenBytes, 0, 4);
            if (BitConverter.IsLittleEndian)
                Array.Reverse(lenBytes);
            var length = BitConverter.ToInt32(lenBytes, 0);
            
            var payload = data.Length > 6 
                ? Encoding.UTF8.GetString(data, 6, data.Length - 6)
                : "";
            
            return (type, flags, length, payload);
        }
        
        /// <summary>
        /// 发送请求到xclient
        /// </summary>
        public static async Task<(bool success, string response)> SendRequestAsync(
            string url, 
            object parameters = null,
            int executeType = 0)
        {
            var requestId = Guid.NewGuid().ToString();
            var payload = new
            {
                type = "request",
                requestId = requestId,
                url = url,
                excuteType = executeType,
                @params = new JavaScriptSerializer().Serialize(parameters ?? new {}),
                key = "request"
            };
            
            return await SendMessageAsync(payload);
        }
        
        /// <summary>
        /// 发送buildin命令
        /// </summary>
        public static async Task<(bool success, string response)> SendBuildinAsync()
        {
            var payload = new { action = "buildin" };
            return await SendMessageAsync(payload);
        }
        
        /// <summary>
        /// 发送原始消息
        /// </summary>
        public static async Task<(bool success, string response)> SendMessageAsync(
            object payload, 
            byte type = 1, 
            byte flags = 0,
            int timeoutMs = 3000)
        {
            try
            {
                using var client = new TcpClient();
                await client.ConnectAsync(XCLIENT_HOST, XCLIENT_PORT);
                
                var stream = client.GetStream();
                stream.ReadTimeout = timeoutMs;
                stream.WriteTimeout = timeoutMs;
                
                var message = BuildMessage(payload, type, flags);
                await stream.WriteAsync(message, 0, message.Length);
                await stream.FlushAsync();
                
                // 等待响应
                await Task.Delay(500);
                
                if (client.Available > 0)
                {
                    var buffer = new byte[4096];
                    var read = await stream.ReadAsync(buffer, 0, buffer.Length);
                    
                    var (respType, respFlags, respLen, respPayload) = ParseResponse(buffer.Take(read).ToArray());
                    
                    if (respType == (byte)MessageType.Error)
                    {
                        return (false, $"错误响应: Type={respType}, Payload={respPayload}");
                    }
                    
                    return (true, respPayload);
                }
                
                return (false, "无响应");
            }
            catch (Exception ex)
            {
                return (false, $"异常: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 检查xclient是否运行
        /// </summary>
        public static bool IsXClientRunning()
        {
            try
            {
                using var client = new TcpClient();
                client.Connect(XCLIENT_HOST, XCLIENT_PORT);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
