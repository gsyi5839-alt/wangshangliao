using System;
using System.Runtime.InteropServices;

namespace WangShangLiaoBot.Services.QXFramework
{
    /// <summary>
    /// QX框架消息回调处理器
    /// 基于xplugin.dll的Message_*回调函数
    /// </summary>
    public static class QXMessageCallback
    {
        #region 回调委托定义

        /// <summary>
        /// 群消息回调
        /// </summary>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int GroupMessageCallback(
            string robotQQ,
            int msgType,
            string msgSubTemp,
            string fromGroup,
            string fromQQ,
            string targetQQ,
            string msgContent,
            string msgNum,
            string msgId,
            string rawMessage);

        /// <summary>
        /// 私聊消息回调
        /// </summary>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int PrivateMessageCallback(
            string robotQQ,
            int msgType,
            string msgSubTemp,
            string fromQQ,
            string msgContent,
            string msgNum,
            string msgId,
            string rawMessage);

        /// <summary>
        /// 群事件回调
        /// </summary>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int GroupEventCallback(
            string robotQQ,
            int eventType,
            int eventSubType,
            string fromGroup,
            string fromQQ,
            string targetQQ,
            string extraData);

        /// <summary>
        /// 好友事件回调
        /// </summary>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int FriendEventCallback(
            string robotQQ,
            int eventType,
            string fromQQ,
            string extraData);

        #endregion

        #region 消息类型常量

        /// <summary>
        /// 群消息类型
        /// </summary>
        public static class GroupMsgType
        {
            public const int Text = 1;           // 文本消息
            public const int Image = 2;          // 图片消息
            public const int Voice = 3;          // 语音消息
            public const int Video = 4;          // 视频消息
            public const int File = 5;           // 文件消息
            public const int Reply = 6;          // 引用回复
            public const int Forward = 7;        // 转发消息
            public const int RedEnvelope = 8;    // 红包消息
            public const int Transfer = 9;       // 转账消息
            public const int At = 10;            // @消息
        }

        /// <summary>
        /// 群事件类型
        /// </summary>
        public static class GroupEventType
        {
            public const int MemberJoin = 1;          // 成员加入
            public const int MemberLeave = 2;         // 成员离开
            public const int MemberKicked = 3;        // 成员被踢
            public const int MemberMuted = 4;         // 成员被禁言
            public const int MemberUnmuted = 5;       // 成员解除禁言
            public const int GroupMuted = 6;          // 全群禁言
            public const int GroupUnmuted = 7;        // 解除全群禁言
            public const int CardChanged = 8;         // 名片修改
            public const int ApplyJoin = 9;           // 申请入群
            public const int InviteJoin = 10;         // 邀请入群
            public const int AdminChanged = 11;       // 管理员变更
            public const int GroupInfoChanged = 12;   // 群信息变更
        }

        /// <summary>
        /// 好友事件类型
        /// </summary>
        public static class FriendEventType
        {
            public const int Apply = 1;          // 好友申请
            public const int Added = 2;          // 已添加好友
            public const int Deleted = 3;        // 好友删除
            public const int Online = 4;         // 好友上线
            public const int Offline = 5;        // 好友下线
        }

        #endregion

        #region 事件处理

        /// <summary>
        /// 群消息接收事件
        /// </summary>
        public static event Action<GroupMessageEventArgs> OnGroupMessage;

        /// <summary>
        /// 私聊消息接收事件
        /// </summary>
        public static event Action<PrivateMessageEventArgs> OnPrivateMessage;

        /// <summary>
        /// 群事件
        /// </summary>
        public static event Action<GroupEventArgs> OnGroupEvent;

        /// <summary>
        /// 好友事件
        /// </summary>
        public static event Action<FriendEventArgs> OnFriendEvent;

        /// <summary>
        /// 处理群消息 (被xplugin.dll调用)
        /// </summary>
        public static int HandleGroupMessage(
            string robotQQ,
            int msgType,
            string msgSubTemp,
            string fromGroup,
            string fromQQ,
            string targetQQ,
            string msgContent,
            string msgNum,
            string msgId,
            string rawMessage)
        {
            try
            {
                var args = new GroupMessageEventArgs
                {
                    RobotQQ = robotQQ,
                    MessageType = msgType,
                    SubType = msgSubTemp,
                    GroupId = fromGroup,
                    SenderId = fromQQ,
                    TargetId = targetQQ,
                    Content = msgContent,
                    MessageNum = msgNum,
                    MessageId = msgId,
                    RawMessage = rawMessage,
                    Timestamp = DateTime.Now
                };

                OnGroupMessage?.Invoke(args);
                return args.Handled ? 1 : 0;
            }
            catch
            {
                return 0;
            }
        }

        /// <summary>
        /// 处理私聊消息 (被xplugin.dll调用)
        /// </summary>
        public static int HandlePrivateMessage(
            string robotQQ,
            int msgType,
            string msgSubTemp,
            string fromQQ,
            string msgContent,
            string msgNum,
            string msgId,
            string rawMessage)
        {
            try
            {
                var args = new PrivateMessageEventArgs
                {
                    RobotQQ = robotQQ,
                    MessageType = msgType,
                    SubType = msgSubTemp,
                    SenderId = fromQQ,
                    Content = msgContent,
                    MessageNum = msgNum,
                    MessageId = msgId,
                    RawMessage = rawMessage,
                    Timestamp = DateTime.Now
                };

                OnPrivateMessage?.Invoke(args);
                return args.Handled ? 1 : 0;
            }
            catch
            {
                return 0;
            }
        }

        /// <summary>
        /// 处理群事件 (被xplugin.dll调用)
        /// </summary>
        public static int HandleGroupEvent(
            string robotQQ,
            int eventType,
            int eventSubType,
            string fromGroup,
            string fromQQ,
            string targetQQ,
            string extraData)
        {
            try
            {
                var args = new GroupEventArgs
                {
                    RobotQQ = robotQQ,
                    EventType = eventType,
                    EventSubType = eventSubType,
                    GroupId = fromGroup,
                    OperatorId = fromQQ,
                    TargetId = targetQQ,
                    ExtraData = extraData,
                    Timestamp = DateTime.Now
                };

                OnGroupEvent?.Invoke(args);
                return args.Handled ? 1 : 0;
            }
            catch
            {
                return 0;
            }
        }

        /// <summary>
        /// 处理好友事件 (被xplugin.dll调用)
        /// </summary>
        public static int HandleFriendEvent(
            string robotQQ,
            int eventType,
            string fromQQ,
            string extraData)
        {
            try
            {
                var args = new FriendEventArgs
                {
                    RobotQQ = robotQQ,
                    EventType = eventType,
                    UserId = fromQQ,
                    ExtraData = extraData,
                    Timestamp = DateTime.Now
                };

                OnFriendEvent?.Invoke(args);
                return args.Handled ? 1 : 0;
            }
            catch
            {
                return 0;
            }
        }

        #endregion
    }

    #region 事件参数类

    /// <summary>
    /// 群消息事件参数
    /// </summary>
    public class GroupMessageEventArgs : EventArgs
    {
        public string RobotQQ { get; set; }
        public int MessageType { get; set; }
        public string SubType { get; set; }
        public string GroupId { get; set; }
        public string SenderId { get; set; }
        public string TargetId { get; set; }
        public string Content { get; set; }
        public string MessageNum { get; set; }
        public string MessageId { get; set; }
        public string RawMessage { get; set; }
        public DateTime Timestamp { get; set; }
        public bool Handled { get; set; }

        /// <summary>
        /// 是否为文本消息
        /// </summary>
        public bool IsText => MessageType == QXMessageCallback.GroupMsgType.Text;

        /// <summary>
        /// 是否为图片消息
        /// </summary>
        public bool IsImage => MessageType == QXMessageCallback.GroupMsgType.Image;

        /// <summary>
        /// 是否为@消息
        /// </summary>
        public bool IsAt => MessageType == QXMessageCallback.GroupMsgType.At;

        /// <summary>
        /// 是否为红包消息
        /// </summary>
        public bool IsRedEnvelope => MessageType == QXMessageCallback.GroupMsgType.RedEnvelope;

        /// <summary>
        /// 是否为转账消息
        /// </summary>
        public bool IsTransfer => MessageType == QXMessageCallback.GroupMsgType.Transfer;
    }

    /// <summary>
    /// 私聊消息事件参数
    /// </summary>
    public class PrivateMessageEventArgs : EventArgs
    {
        public string RobotQQ { get; set; }
        public int MessageType { get; set; }
        public string SubType { get; set; }
        public string SenderId { get; set; }
        public string Content { get; set; }
        public string MessageNum { get; set; }
        public string MessageId { get; set; }
        public string RawMessage { get; set; }
        public DateTime Timestamp { get; set; }
        public bool Handled { get; set; }
    }

    /// <summary>
    /// 群事件参数
    /// </summary>
    public class GroupEventArgs : EventArgs
    {
        public string RobotQQ { get; set; }
        public int EventType { get; set; }
        public int EventSubType { get; set; }
        public string GroupId { get; set; }
        public string OperatorId { get; set; }
        public string TargetId { get; set; }
        public string ExtraData { get; set; }
        public DateTime Timestamp { get; set; }
        public bool Handled { get; set; }

        /// <summary>
        /// 是否为成员加入事件
        /// </summary>
        public bool IsMemberJoin => EventType == QXMessageCallback.GroupEventType.MemberJoin;

        /// <summary>
        /// 是否为成员离开事件
        /// </summary>
        public bool IsMemberLeave => EventType == QXMessageCallback.GroupEventType.MemberLeave;

        /// <summary>
        /// 是否为成员被踢事件
        /// </summary>
        public bool IsMemberKicked => EventType == QXMessageCallback.GroupEventType.MemberKicked;

        /// <summary>
        /// 是否为名片修改事件
        /// </summary>
        public bool IsCardChanged => EventType == QXMessageCallback.GroupEventType.CardChanged;

        /// <summary>
        /// 是否为入群申请事件
        /// </summary>
        public bool IsJoinApply => EventType == QXMessageCallback.GroupEventType.ApplyJoin;
    }

    /// <summary>
    /// 好友事件参数
    /// </summary>
    public class FriendEventArgs : EventArgs
    {
        public string RobotQQ { get; set; }
        public int EventType { get; set; }
        public string UserId { get; set; }
        public string ExtraData { get; set; }
        public DateTime Timestamp { get; set; }
        public bool Handled { get; set; }

        /// <summary>
        /// 是否为好友申请事件
        /// </summary>
        public bool IsFriendApply => EventType == QXMessageCallback.FriendEventType.Apply;
    }

    #endregion
}
