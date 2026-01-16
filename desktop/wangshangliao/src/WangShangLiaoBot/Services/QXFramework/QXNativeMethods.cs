using System;
using System.Runtime.InteropServices;
using System.Text;

namespace WangShangLiaoBot.Services.QXFramework
{
    /// <summary>
    /// QX (千寻) 框架原生API封装
    /// 基于招财狗(ZCG) Module.dll 逆向分析
    /// </summary>
    public static class QXNativeMethods
    {
        private const string ModuleDll = "Module.dll";

        #region 好友操作

        /// <summary>
        /// 发送私聊消息
        /// </summary>
        [DllImport(ModuleDll, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern int QX_Friend_SendMsg(string robotQQ, string friendId, string message);

        /// <summary>
        /// 发送私聊图片
        /// </summary>
        [DllImport(ModuleDll, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern int QX_Friend_SendMsgPhoto(string robotQQ, string friendId, string imagePath);

        /// <summary>
        /// 发送私聊红包
        /// </summary>
        [DllImport(ModuleDll, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern int QX_Friend_SendRedEnvelope(string robotQQ, string friendId, int amount, int count, string message);

        /// <summary>
        /// 获取好友列表
        /// </summary>
        [DllImport(ModuleDll, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern IntPtr QX_Friend_GetList(string robotQQ);

        /// <summary>
        /// 添加好友
        /// </summary>
        [DllImport(ModuleDll, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern int QX_Friend_Add(string robotQQ, string userId, string message);

        /// <summary>
        /// 删除好友
        /// </summary>
        [DllImport(ModuleDll, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern int QX_Friend_Del(string robotQQ, string friendId);

        /// <summary>
        /// 判断是否好友
        /// </summary>
        [DllImport(ModuleDll, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern bool QX_Friend_Or_Not(string robotQQ, string userId);

        /// <summary>
        /// 处理好友申请
        /// </summary>
        [DllImport(ModuleDll, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern int QX_Friend_SetApply(string robotQQ, string requestId, int accept);

        #endregion

        #region 群操作

        /// <summary>
        /// 发送群消息
        /// </summary>
        [DllImport(ModuleDll, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern int QX_Group_SendMsg(string robotQQ, string groupId, string message);

        /// <summary>
        /// 发送群图片
        /// </summary>
        [DllImport(ModuleDll, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern int QX_Group_SendMsgPhoto(string robotQQ, string groupId, string imagePath);

        /// <summary>
        /// 发送群红包
        /// </summary>
        [DllImport(ModuleDll, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern int QX_Group_SendRedEnvelope(string robotQQ, string groupId, int amount, int count, string message);

        /// <summary>
        /// 获取群成员列表
        /// </summary>
        [DllImport(ModuleDll, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern IntPtr QX_Group_GetUserList(string robotQQ, string groupId);

        /// <summary>
        /// 获取群成员信息
        /// </summary>
        [DllImport(ModuleDll, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern IntPtr QX_Group_GetUserInfo(string robotQQ, string groupId, string userId);

        /// <summary>
        /// 获取群信息
        /// </summary>
        [DllImport(ModuleDll, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern IntPtr QX_Group_GetInfo(string robotQQ, string groupId);

        /// <summary>
        /// 获取群列表
        /// </summary>
        [DllImport(ModuleDll, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern IntPtr QX_Group_Getlist(string robotQQ);

        /// <summary>
        /// 设置成员禁言
        /// </summary>
        [DllImport(ModuleDll, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern int QX_Group_UserSayState(string robotQQ, string groupId, string userId, int minutes);

        /// <summary>
        /// 设置群全员禁言
        /// </summary>
        [DllImport(ModuleDll, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern int QX_Group_SayState(string robotQQ, string groupId, int enable);

        /// <summary>
        /// 踢出群成员
        /// </summary>
        [DllImport(ModuleDll, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern int QX_Group_DelteUser(string robotQQ, string groupId, string userId);

        /// <summary>
        /// 设置群名片
        /// </summary>
        [DllImport(ModuleDll, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern int QX_Group_UserSetCardName(string robotQQ, string groupId, string userId, string cardName);

        /// <summary>
        /// 处理入群申请
        /// </summary>
        [DllImport(ModuleDll, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern int QX_Group_UserSetApply(string robotQQ, string requestId, int accept);

        /// <summary>
        /// 撤回消息
        /// </summary>
        [DllImport(ModuleDll, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern int QX_Group_WithdrawMessage(string robotQQ, string groupId, string messageId);

        /// <summary>
        /// 查询邀请人
        /// </summary>
        [DllImport(ModuleDll, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern IntPtr QX_Group_InquiryInviter(string robotQQ, string groupId, string userId);

        /// <summary>
        /// 查询被动邀请
        /// </summary>
        [DllImport(ModuleDll, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern IntPtr QX_Group_InquiryPassiveInviter(string robotQQ, string groupId, string userId);

        #endregion

        #region 红包/转账

        /// <summary>
        /// 抢红包
        /// </summary>
        [DllImport(ModuleDll, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern int QX_RobRedEnvelope(string robotQQ, string redEnvelopeId);

        /// <summary>
        /// 红包详情
        /// </summary>
        [DllImport(ModuleDll, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern IntPtr QX_RedEnvelope_Particulars(string robotQQ, string redEnvelopeId);

        /// <summary>
        /// 发起转账
        /// </summary>
        [DllImport(ModuleDll, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern int QX_Transfer_Send(string robotQQ, string userId, int amount, string message);

        /// <summary>
        /// 领取转账
        /// </summary>
        [DllImport(ModuleDll, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern int QX_Transfer_Grab(string robotQQ, string transferId);

        /// <summary>
        /// 转账详情
        /// </summary>
        [DllImport(ModuleDll, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern IntPtr QX_Transfer_Detail(string robotQQ, string transferId);

        #endregion

        #region 系统操作

        /// <summary>
        /// 获取账号信息
        /// </summary>
        [DllImport(ModuleDll, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern IntPtr QX_GetInfo(string robotQQ);

        /// <summary>
        /// 获取用户ID列表
        /// </summary>
        [DllImport(ModuleDll, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern IntPtr QX_Get_UserIdList(string robotQQ);

        /// <summary>
        /// 获取在线NIM ID
        /// </summary>
        [DllImport(ModuleDll, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern IntPtr QX_Get_nimIdOnline(string robotQQ);

        /// <summary>
        /// 获取财务信息
        /// </summary>
        [DllImport(ModuleDll, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern IntPtr QX_Get_Finance(string robotQQ);

        /// <summary>
        /// 获取签名Token
        /// </summary>
        [DllImport(ModuleDll, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern IntPtr QX_Get_sig_token(string robotQQ);

        /// <summary>
        /// 获取应用目录
        /// </summary>
        [DllImport(ModuleDll, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern IntPtr QX_Get_Directory_App();

        /// <summary>
        /// 获取客户端目录
        /// </summary>
        [DllImport(ModuleDll, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern IntPtr QX_Get_Directory_Clinent();

        /// <summary>
        /// 上传图片
        /// </summary>
        [DllImport(ModuleDll, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern IntPtr QX_Upload_photo(string robotQQ, string imagePath);

        #endregion

        #region 辅助方法

        /// <summary>
        /// 将IntPtr转换为字符串
        /// </summary>
        public static string PtrToString(IntPtr ptr)
        {
            if (ptr == IntPtr.Zero) return null;
            return Marshal.PtrToStringAnsi(ptr);
        }

        /// <summary>
        /// 将IntPtr转换为UTF8字符串
        /// </summary>
        public static string PtrToStringUtf8(IntPtr ptr)
        {
            if (ptr == IntPtr.Zero) return null;

            int length = 0;
            while (Marshal.ReadByte(ptr, length) != 0) length++;

            byte[] buffer = new byte[length];
            Marshal.Copy(ptr, buffer, 0, length);
            return Encoding.UTF8.GetString(buffer);
        }

        #endregion
    }
}
