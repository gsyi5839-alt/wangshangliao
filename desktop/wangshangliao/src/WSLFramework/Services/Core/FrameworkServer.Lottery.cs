using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HPSocket;
using HPSocket.Tcp;
using WSLFramework.Models;
using WSLFramework.Protocol;
using WSLFramework.Utils;
using WSLFramework.Services.EventDriven;

namespace WSLFramework.Services
{
    /// <summary>
    /// 框架服务端 - 使用 HPSocket PACK 模式实现
    /// 完全匹配招财狗ZCG协议，增强异常处理和线程安全
    /// </summary>
    public partial class FrameworkServer : IDisposable
    {
        #region 开奖相关处理

        /// <summary>
        /// 处理开奖结果通知
        /// </summary>
        private FrameworkMessage HandleLotteryResult(FrameworkMessage message)
        {
            try
            {
                // 解析开奖数据: period|num1,num2,num3|sum|countdown
                var content = message.Content;
                var parts = content?.Split('|') ?? new string[0];
                
                if (parts.Length >= 4)
                {
                    var period = parts[0];
                    var numbers = parts[1].Split(',');
                    int.TryParse(parts[2], out int sum);
                    int.TryParse(parts[3], out int countdown);
                    
                    int num1 = 0, num2 = 0, num3 = 0;
                    if (numbers.Length >= 3)
                    {
                        int.TryParse(numbers[0], out num1);
                        int.TryParse(numbers[1], out num2);
                        int.TryParse(numbers[2], out num3);
                    }
                    
                    Log($"[Lottery] 收到开奖结果: 期号={period}, 号码={num1}+{num2}+{num3}={sum}");
                    
                    // 广播开奖事件 (供UI和其他服务使用)
                    OnApiCall?.Invoke("开奖结果", new[] { period, $"{num1}+{num2}+{num3}", sum.ToString() }, "成功");
                    
                    return FrameworkMessage.CreateSuccess(message.Id, FrameworkMessageType.LotteryResult, "开奖结果已处理");
                }
                
                return FrameworkMessage.CreateError(message.Id, "开奖数据格式错误");
            }
            catch (Exception ex)
            {
                Log($"[Lottery] 处理开奖结果异常: {ex.Message}");
                return FrameworkMessage.CreateError(message.Id, ex.Message);
            }
        }

        /// <summary>
        /// 处理封盘通知
        /// </summary>
        private FrameworkMessage HandleSealingNotify(FrameworkMessage message)
        {
            try
            {
                var content = message.Content;
                var period = message.Extra;
                
                Log($"[Sealing] 收到封盘通知: 期号={period}");
                
                // 发送封盘消息到活跃群
                if (!string.IsNullOrEmpty(_activeGroupId) && !string.IsNullOrEmpty(content))
                {
                    _ = SendGroupMessageViaCDPAsync(_activeGroupId, content);
                }
                
                // 广播封盘事件 (未来可扩展)
                OnApiCall?.Invoke("封盘通知", new[] { period, _activeGroupId }, "成功");
                
                return FrameworkMessage.CreateSuccess(message.Id, FrameworkMessageType.SealingNotify, "封盘通知已处理");
            }
            catch (Exception ex)
            {
                Log($"[Sealing] 处理封盘通知异常: {ex.Message}");
                return FrameworkMessage.CreateError(message.Id, ex.Message);
            }
        }

        /// <summary>
        /// 处理封盘提醒通知
        /// </summary>
        private FrameworkMessage HandleReminderNotify(FrameworkMessage message)
        {
            try
            {
                var content = message.Content;
                Log($"[Reminder] 收到封盘提醒");
                
                // 发送提醒消息到活跃群
                if (!string.IsNullOrEmpty(_activeGroupId) && !string.IsNullOrEmpty(content))
                {
                    _ = SendGroupMessageViaCDPAsync(_activeGroupId, content);
                }
                
                return FrameworkMessage.CreateSuccess(message.Id, FrameworkMessageType.ReminderNotify, "封盘提醒已处理");
            }
            catch (Exception ex)
            {
                Log($"[Reminder] 处理封盘提醒异常: {ex.Message}");
                return FrameworkMessage.CreateError(message.Id, ex.Message);
            }
        }

        /// <summary>
        /// 处理期号更新
        /// </summary>
        private FrameworkMessage HandlePeriodUpdate(FrameworkMessage message)
        {
            try
            {
                // 解析: currentPeriod|nextPeriod|countdown
                var content = message.Content;
                var parts = content?.Split('|') ?? new string[0];
                
                if (parts.Length >= 3)
                {
                    var currentPeriod = parts[0];
                    var nextPeriod = parts[1];
                    int.TryParse(parts[2], out int countdown);
                    
                    Log($"[Period] 期号更新: {currentPeriod} -> {nextPeriod}, 倒计时={countdown}s");
                    
                    // 广播期号更新事件 (供UI和其他服务使用)
                    OnApiCall?.Invoke("期号更新", new[] { currentPeriod, nextPeriod, countdown.ToString() }, "成功");
                    
                    return FrameworkMessage.CreateSuccess(message.Id, FrameworkMessageType.PeriodUpdate, "期号已更新");
                }
                
                return FrameworkMessage.CreateError(message.Id, "期号数据格式错误");
            }
            catch (Exception ex)
            {
                Log($"[Period] 处理期号更新异常: {ex.Message}");
                return FrameworkMessage.CreateError(message.Id, ex.Message);
            }
        }

        #endregion

    }
}
