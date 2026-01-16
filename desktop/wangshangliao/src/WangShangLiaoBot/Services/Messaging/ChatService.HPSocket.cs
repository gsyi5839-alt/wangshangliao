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
        #region HPSocket Framework Integration
        
        /// <summary>
        /// 通过HPSocket副框架发送消息
        /// </summary>
        private async Task<(bool Success, string Scene, string To, string Message)> SendTextViaFrameworkAsync(string scene, string to, string text)
        {
            try
            {
                var frameworkClient = HPSocket.FrameworkClient.Instance;
                
                // 如果未连接到副框架，尝试连接
                if (!frameworkClient.IsConnected)
                {
                    Log("尝试连接到副框架服务端...");
                    var connected = await frameworkClient.ConnectAsync("127.0.0.1", 14746);
                    if (!connected)
                    {
                        Log("连接副框架失败，无法发送消息");
                        return (false, scene, to, "连接副框架失败");
                    }
                    Log("成功连接到副框架服务端");
                }
                
                // 通过副框架发送消息
                bool success;
                if (scene == "team")
                {
                    success = await frameworkClient.SendGroupMessageAsync(to, text);
                }
                else
                {
                    success = await frameworkClient.SendPrivateMessageAsync(to, text);
                }
                
                if (success)
                {
                    Log($"通过副框架发送消息成功 [{scene}:{to}]");
                    return (true, scene, to, "通过副框架发送成功");
                }
                else
                {
                    Log($"通过副框架发送消息失败 [{scene}:{to}]");
                    return (false, scene, to, "副框架发送失败");
                }
            }
            catch (Exception ex)
            {
                Log($"通过副框架发送消息异常: {ex.Message}");
                return (false, scene, to, ex.Message);
            }
        }
        
        /// <summary>
        /// 检查是否可以通过任一方式发送消息
        /// </summary>
        public bool CanSendMessage
        {
            get
            {
                // CDP连接 或 副框架连接
                return IsConnected || HPSocket.FrameworkClient.Instance.IsConnected;
            }
        }
        
        /// <summary>
        /// 获取当前连接状态描述
        /// </summary>
        public string ConnectionStatusText
        {
            get
            {
                if (IsConnected)
                    return "CDP直连";
                else if (HPSocket.FrameworkClient.Instance.IsConnected)
                    return "副框架";
                else
                    return "未连接";
            }
        }
        
        #endregion

    }
}
