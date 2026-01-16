using System;
using System.Runtime.InteropServices;
using System.Text;

namespace WSLFramework.Native
{
    /// <summary>
    /// 网易云信 nim.dll P/Invoke 封装
    /// 基于招财狗逆向分析的 API 定义
    /// </summary>
    public static class NimApi
    {
        private const string NIM_DLL = "nim.dll";
        
        #region 回调委托定义
        
        /// <summary>
        /// SDK日志回调
        /// </summary>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void nim_sdk_log_cb([MarshalAs(UnmanagedType.LPStr)] string log_content, IntPtr user_data);
        
        /// <summary>
        /// 登录回调
        /// </summary>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void nim_json_transport_cb([MarshalAs(UnmanagedType.LPStr)] string json_result, IntPtr user_data);
        
        /// <summary>
        /// 消息接收回调
        /// </summary>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void nim_talk_receive_cb([MarshalAs(UnmanagedType.LPStr)] string content, [MarshalAs(UnmanagedType.LPStr)] string json_extension, IntPtr user_data);
        
        /// <summary>
        /// 发送消息回调
        /// </summary>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void nim_talk_ack_cb([MarshalAs(UnmanagedType.LPStr)] string result, IntPtr user_data);
        
        /// <summary>
        /// 群事件回调
        /// </summary>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void nim_team_event_cb([MarshalAs(UnmanagedType.LPStr)] string result, [MarshalAs(UnmanagedType.LPStr)] string json_extension, IntPtr user_data);
        
        /// <summary>
        /// 好友关系变化回调
        /// </summary>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void nim_friend_change_cb([MarshalAs(UnmanagedType.LPStr)] string result, [MarshalAs(UnmanagedType.LPStr)] string json_extension, IntPtr user_data);
        
        /// <summary>
        /// 用户名片变化回调
        /// </summary>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void nim_user_name_card_changed_cb([MarshalAs(UnmanagedType.LPStr)] string result, [MarshalAs(UnmanagedType.LPStr)] string json_extension, IntPtr user_data);
        
        /// <summary>
        /// 系统消息回调
        /// </summary>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void nim_sysmsg_cb([MarshalAs(UnmanagedType.LPStr)] string result, [MarshalAs(UnmanagedType.LPStr)] string json_extension, IntPtr user_data);
        
        /// <summary>
        /// 断线回调
        /// </summary>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void nim_disconnect_cb([MarshalAs(UnmanagedType.LPStr)] string json_extension, IntPtr user_data);
        
        /// <summary>
        /// 被踢回调
        /// </summary>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void nim_kickout_cb(int client_type, [MarshalAs(UnmanagedType.LPStr)] string json_extension, IntPtr user_data);
        
        /// <summary>
        /// 多端登录通知回调
        /// </summary>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void nim_multispot_login_cb([MarshalAs(UnmanagedType.LPStr)] string result, IntPtr user_data);
        
        #endregion
        
        #region Client - 客户端基础接口
        
        /// <summary>
        /// 初始化SDK
        /// </summary>
        [DllImport(NIM_DLL, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern bool nim_client_init(
            [MarshalAs(UnmanagedType.LPStr)] string app_data_dir,
            [MarshalAs(UnmanagedType.LPStr)] string app_install_dir,
            [MarshalAs(UnmanagedType.LPStr)] string json_extension);
        
        /// <summary>
        /// 清理SDK
        /// </summary>
        [DllImport(NIM_DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void nim_client_cleanup([MarshalAs(UnmanagedType.LPStr)] string json_extension);
        
        /// <summary>
        /// 登录
        /// </summary>
        [DllImport(NIM_DLL, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern void nim_client_login(
            [MarshalAs(UnmanagedType.LPStr)] string app_key,
            [MarshalAs(UnmanagedType.LPStr)] string account,
            [MarshalAs(UnmanagedType.LPStr)] string token,
            [MarshalAs(UnmanagedType.LPStr)] string json_extension,
            nim_json_transport_cb cb,
            IntPtr user_data);
        
        /// <summary>
        /// 登出
        /// </summary>
        [DllImport(NIM_DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void nim_client_logout(
            int logout_type,
            [MarshalAs(UnmanagedType.LPStr)] string json_extension,
            nim_json_transport_cb cb,
            IntPtr user_data);
        
        /// <summary>
        /// 获取登录状态
        /// </summary>
        [DllImport(NIM_DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern int nim_client_get_login_state([MarshalAs(UnmanagedType.LPStr)] string json_extension);
        
        /// <summary>
        /// 重新登录
        /// </summary>
        [DllImport(NIM_DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void nim_client_relogin([MarshalAs(UnmanagedType.LPStr)] string json_extension);
        
        /// <summary>
        /// 获取当前账号
        /// </summary>
        [DllImport(NIM_DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr nim_client_current_user_account();
        
        /// <summary>
        /// 获取SDK版本
        /// </summary>
        [DllImport(NIM_DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr nim_client_version();
        
        /// <summary>
        /// 注册断线回调
        /// </summary>
        [DllImport(NIM_DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void nim_client_reg_disconnect_cb(
            [MarshalAs(UnmanagedType.LPStr)] string json_extension,
            nim_disconnect_cb cb,
            IntPtr user_data);
        
        /// <summary>
        /// 注册被踢回调
        /// </summary>
        [DllImport(NIM_DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void nim_client_reg_kickout_cb(
            [MarshalAs(UnmanagedType.LPStr)] string json_extension,
            nim_kickout_cb cb,
            IntPtr user_data);
        
        /// <summary>
        /// 注册多端登录通知回调
        /// </summary>
        [DllImport(NIM_DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void nim_client_reg_multispot_login_notify_cb(
            [MarshalAs(UnmanagedType.LPStr)] string json_extension,
            nim_multispot_login_cb cb,
            IntPtr user_data);
        
        /// <summary>
        /// 注册自动重连回调
        /// </summary>
        [DllImport(NIM_DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void nim_client_reg_auto_relogin_cb(
            [MarshalAs(UnmanagedType.LPStr)] string json_extension,
            nim_json_transport_cb cb,
            IntPtr user_data);
        
        #endregion
        
        #region Talk - 聊天消息接口
        
        /// <summary>
        /// 发送消息
        /// </summary>
        [DllImport(NIM_DLL, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern void nim_talk_send_msg(
            [MarshalAs(UnmanagedType.LPStr)] string json_msg,
            [MarshalAs(UnmanagedType.LPStr)] string json_extension,
            nim_talk_ack_cb cb,
            IntPtr user_data);
        
        /// <summary>
        /// 停止发送消息
        /// </summary>
        [DllImport(NIM_DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void nim_talk_stop_send_msg(
            [MarshalAs(UnmanagedType.LPStr)] string client_msg_id,
            int type,
            [MarshalAs(UnmanagedType.LPStr)] string json_extension);
        
        /// <summary>
        /// 注册接收消息回调
        /// </summary>
        [DllImport(NIM_DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void nim_talk_reg_receive_cb(
            [MarshalAs(UnmanagedType.LPStr)] string json_extension,
            nim_talk_receive_cb cb,
            IntPtr user_data);
        
        /// <summary>
        /// 注册批量接收消息回调
        /// </summary>
        [DllImport(NIM_DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void nim_talk_reg_receive_msgs_cb(
            [MarshalAs(UnmanagedType.LPStr)] string json_extension,
            nim_talk_receive_cb cb,
            IntPtr user_data);
        
        /// <summary>
        /// 注册发送消息结果回调
        /// </summary>
        [DllImport(NIM_DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void nim_talk_reg_ack_cb(
            [MarshalAs(UnmanagedType.LPStr)] string json_extension,
            nim_talk_ack_cb cb,
            IntPtr user_data);
        
        /// <summary>
        /// 撤回消息
        /// </summary>
        [DllImport(NIM_DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void nim_talk_recall_msg(
            [MarshalAs(UnmanagedType.LPStr)] string json_msg,
            [MarshalAs(UnmanagedType.LPStr)] string notify,
            [MarshalAs(UnmanagedType.LPStr)] string json_extension,
            nim_talk_ack_cb cb,
            IntPtr user_data);
        
        #endregion
        
        #region Team - 群组接口
        
        /// <summary>
        /// 注册群事件回调
        /// </summary>
        [DllImport(NIM_DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void nim_team_reg_team_event_cb(
            [MarshalAs(UnmanagedType.LPStr)] string json_extension,
            nim_team_event_cb cb,
            IntPtr user_data);
        
        /// <summary>
        /// 查询群信息
        /// </summary>
        [DllImport(NIM_DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void nim_team_query_team_info_online_async(
            [MarshalAs(UnmanagedType.LPStr)] string tid,
            [MarshalAs(UnmanagedType.LPStr)] string json_extension,
            nim_team_event_cb cb,
            IntPtr user_data);
        
        /// <summary>
        /// 查询我的所有群
        /// </summary>
        [DllImport(NIM_DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void nim_team_query_all_my_teams_async(
            [MarshalAs(UnmanagedType.LPStr)] string json_extension,
            nim_team_event_cb cb,
            IntPtr user_data);
        
        /// <summary>
        /// 查询我的所有群信息
        /// </summary>
        [DllImport(NIM_DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void nim_team_query_all_my_teams_info_async(
            [MarshalAs(UnmanagedType.LPStr)] string json_extension,
            nim_team_event_cb cb,
            IntPtr user_data);
        
        /// <summary>
        /// 查询群成员
        /// </summary>
        [DllImport(NIM_DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void nim_team_query_team_members_async(
            [MarshalAs(UnmanagedType.LPStr)] string tid,
            bool include_user_info,
            [MarshalAs(UnmanagedType.LPStr)] string json_extension,
            nim_team_event_cb cb,
            IntPtr user_data);
        
        /// <summary>
        /// 全群禁言
        /// </summary>
        [DllImport(NIM_DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void nim_team_mute_async(
            [MarshalAs(UnmanagedType.LPStr)] string tid,
            bool set_mute,
            [MarshalAs(UnmanagedType.LPStr)] string json_extension,
            nim_team_event_cb cb,
            IntPtr user_data);
        
        /// <summary>
        /// 禁言/解禁群成员
        /// </summary>
        [DllImport(NIM_DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void nim_team_mute_member_async(
            [MarshalAs(UnmanagedType.LPStr)] string tid,
            [MarshalAs(UnmanagedType.LPStr)] string member_id,
            bool set_mute,
            [MarshalAs(UnmanagedType.LPStr)] string json_extension,
            nim_team_event_cb cb,
            IntPtr user_data);
        
        /// <summary>
        /// 更新我的群昵称
        /// </summary>
        [DllImport(NIM_DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void nim_team_update_my_property_async(
            [MarshalAs(UnmanagedType.LPStr)] string json_info,
            [MarshalAs(UnmanagedType.LPStr)] string json_extension,
            nim_team_event_cb cb,
            IntPtr user_data);
        
        /// <summary>
        /// 更新他人群昵称
        /// </summary>
        [DllImport(NIM_DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void nim_team_update_other_nick_async(
            [MarshalAs(UnmanagedType.LPStr)] string json_info,
            [MarshalAs(UnmanagedType.LPStr)] string json_extension,
            nim_team_event_cb cb,
            IntPtr user_data);
        
        /// <summary>
        /// 踢出群成员
        /// </summary>
        [DllImport(NIM_DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void nim_team_kick_async(
            [MarshalAs(UnmanagedType.LPStr)] string tid,
            [MarshalAs(UnmanagedType.LPStr)] string json_ids,
            [MarshalAs(UnmanagedType.LPStr)] string json_extension,
            nim_team_event_cb cb,
            IntPtr user_data);
        
        /// <summary>
        /// 邀请入群
        /// </summary>
        [DllImport(NIM_DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void nim_team_invite_async(
            [MarshalAs(UnmanagedType.LPStr)] string tid,
            [MarshalAs(UnmanagedType.LPStr)] string json_ids,
            [MarshalAs(UnmanagedType.LPStr)] string invitation_postscript,
            [MarshalAs(UnmanagedType.LPStr)] string invitation_attachment,
            [MarshalAs(UnmanagedType.LPStr)] string json_extension,
            nim_team_event_cb cb,
            IntPtr user_data);
        
        /// <summary>
        /// 退出群
        /// </summary>
        [DllImport(NIM_DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void nim_team_leave_async(
            [MarshalAs(UnmanagedType.LPStr)] string tid,
            [MarshalAs(UnmanagedType.LPStr)] string json_extension,
            nim_team_event_cb cb,
            IntPtr user_data);
        
        /// <summary>
        /// 同意入群邀请
        /// </summary>
        [DllImport(NIM_DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void nim_team_accept_invitation_async(
            [MarshalAs(UnmanagedType.LPStr)] string tid,
            [MarshalAs(UnmanagedType.LPStr)] string invitor_id,
            [MarshalAs(UnmanagedType.LPStr)] string json_extension,
            nim_team_event_cb cb,
            IntPtr user_data);
        
        /// <summary>
        /// 拒绝入群邀请
        /// </summary>
        [DllImport(NIM_DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void nim_team_reject_invitation_async(
            [MarshalAs(UnmanagedType.LPStr)] string tid,
            [MarshalAs(UnmanagedType.LPStr)] string invitor_id,
            [MarshalAs(UnmanagedType.LPStr)] string reason,
            [MarshalAs(UnmanagedType.LPStr)] string json_extension,
            nim_team_event_cb cb,
            IntPtr user_data);
        
        #endregion
        
        #region User - 用户接口
        
        /// <summary>
        /// 获取用户名片
        /// </summary>
        [DllImport(NIM_DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void nim_user_get_user_name_card(
            [MarshalAs(UnmanagedType.LPStr)] string json_accids,
            [MarshalAs(UnmanagedType.LPStr)] string json_extension,
            nim_json_transport_cb cb,
            IntPtr user_data);
        
        /// <summary>
        /// 在线获取用户名片
        /// </summary>
        [DllImport(NIM_DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void nim_user_get_user_name_card_online(
            [MarshalAs(UnmanagedType.LPStr)] string json_accids,
            [MarshalAs(UnmanagedType.LPStr)] string json_extension,
            nim_json_transport_cb cb,
            IntPtr user_data);
        
        /// <summary>
        /// 注册用户名片变化回调
        /// </summary>
        [DllImport(NIM_DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void nim_user_reg_user_name_card_changed_cb(
            [MarshalAs(UnmanagedType.LPStr)] string json_extension,
            nim_user_name_card_changed_cb cb,
            IntPtr user_data);
        
        /// <summary>
        /// 更新我的名片
        /// </summary>
        [DllImport(NIM_DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void nim_user_update_my_user_name_card(
            [MarshalAs(UnmanagedType.LPStr)] string json_card,
            [MarshalAs(UnmanagedType.LPStr)] string json_extension,
            nim_json_transport_cb cb,
            IntPtr user_data);
        
        /// <summary>
        /// 设置黑名单
        /// </summary>
        [DllImport(NIM_DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void nim_user_set_black(
            [MarshalAs(UnmanagedType.LPStr)] string accid,
            bool set_black,
            [MarshalAs(UnmanagedType.LPStr)] string json_extension,
            nim_json_transport_cb cb,
            IntPtr user_data);
        
        /// <summary>
        /// 设置静音
        /// </summary>
        [DllImport(NIM_DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void nim_user_set_mute(
            [MarshalAs(UnmanagedType.LPStr)] string accid,
            bool set_mute,
            [MarshalAs(UnmanagedType.LPStr)] string json_extension,
            nim_json_transport_cb cb,
            IntPtr user_data);
        
        #endregion
        
        #region Friend - 好友接口
        
        /// <summary>
        /// 注册好友变化回调
        /// </summary>
        [DllImport(NIM_DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void nim_friend_reg_changed_cb(
            [MarshalAs(UnmanagedType.LPStr)] string json_extension,
            nim_friend_change_cb cb,
            IntPtr user_data);
        
        /// <summary>
        /// 获取好友列表
        /// </summary>
        [DllImport(NIM_DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void nim_friend_get_list(
            [MarshalAs(UnmanagedType.LPStr)] string json_extension,
            nim_json_transport_cb cb,
            IntPtr user_data);
        
        /// <summary>
        /// 添加好友
        /// </summary>
        [DllImport(NIM_DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void nim_friend_request(
            [MarshalAs(UnmanagedType.LPStr)] string accid,
            int verify_type,
            [MarshalAs(UnmanagedType.LPStr)] string msg,
            [MarshalAs(UnmanagedType.LPStr)] string json_extension,
            nim_json_transport_cb cb,
            IntPtr user_data);
        
        /// <summary>
        /// 删除好友
        /// </summary>
        [DllImport(NIM_DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void nim_friend_delete(
            [MarshalAs(UnmanagedType.LPStr)] string accid,
            [MarshalAs(UnmanagedType.LPStr)] string json_extension,
            nim_json_transport_cb cb,
            IntPtr user_data);
        
        #endregion
        
        #region SysMsg - 系统消息接口
        
        /// <summary>
        /// 注册系统消息回调
        /// </summary>
        [DllImport(NIM_DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void nim_sysmsg_reg_sysmsg_cb(
            [MarshalAs(UnmanagedType.LPStr)] string json_extension,
            nim_sysmsg_cb cb,
            IntPtr user_data);
        
        /// <summary>
        /// 查询系统消息
        /// </summary>
        [DllImport(NIM_DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void nim_sysmsg_query_msg_async(
            int limit_count,
            long last_time,
            [MarshalAs(UnmanagedType.LPStr)] string json_extension,
            nim_sysmsg_cb cb,
            IntPtr user_data);
        
        /// <summary>
        /// 发送自定义通知
        /// </summary>
        [DllImport(NIM_DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void nim_sysmsg_send_custom_notification(
            [MarshalAs(UnmanagedType.LPStr)] string json_msg,
            [MarshalAs(UnmanagedType.LPStr)] string json_extension);
        
        #endregion
        
        #region Global - 全局接口
        
        /// <summary>
        /// 释放SDK分配的字符串
        /// </summary>
        [DllImport(NIM_DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void nim_global_free_str_buf(IntPtr str);
        
        /// <summary>
        /// 释放SDK分配的缓冲区
        /// </summary>
        [DllImport(NIM_DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void nim_global_free_buf(IntPtr buf);
        
        /// <summary>
        /// 设置代理
        /// </summary>
        [DllImport(NIM_DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void nim_global_set_proxy(
            int type,
            [MarshalAs(UnmanagedType.LPStr)] string host,
            int port,
            [MarshalAs(UnmanagedType.LPStr)] string user,
            [MarshalAs(UnmanagedType.LPStr)] string password);
        
        /// <summary>
        /// 注册SDK日志回调
        /// </summary>
        [DllImport(NIM_DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void nim_tool_reg_sdk_log_callback(
            nim_sdk_log_cb cb,
            IntPtr user_data);
        
        /// <summary>
        /// 获取SDK缓存目录
        /// </summary>
        [DllImport(NIM_DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr nim_tool_get_user_appdata_dir([MarshalAs(UnmanagedType.LPStr)] string app_account);
        
        /// <summary>
        /// 获取安装目录
        /// </summary>
        [DllImport(NIM_DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr nim_tool_get_cur_module_dir();
        
        /// <summary>
        /// 生成UUID
        /// </summary>
        [DllImport(NIM_DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr nim_tool_get_uuid();
        
        #endregion
        
        #region 辅助方法
        
        /// <summary>
        /// 将IntPtr转为字符串并释放
        /// </summary>
        public static string PtrToStringAndFree(IntPtr ptr)
        {
            if (ptr == IntPtr.Zero) return null;
            string str = Marshal.PtrToStringAnsi(ptr);
            nim_global_free_str_buf(ptr);
            return str;
        }
        
        /// <summary>
        /// 获取当前账号字符串
        /// </summary>
        public static string GetCurrentAccount()
        {
            return PtrToStringAndFree(nim_client_current_user_account());
        }
        
        /// <summary>
        /// 获取SDK版本
        /// </summary>
        public static string GetVersion()
        {
            return PtrToStringAndFree(nim_client_version());
        }
        
        /// <summary>
        /// 生成新的消息ID
        /// </summary>
        public static string GenerateUUID()
        {
            return PtrToStringAndFree(nim_tool_get_uuid());
        }
        
        #endregion
    }
    
    #region 常量定义
    
    /// <summary>
    /// 登录状态
    /// </summary>
    public enum NimLoginState
    {
        Logout = 0,
        Login = 1,
        Logining = 2
    }
    
    /// <summary>
    /// 消息类型
    /// </summary>
    public enum NimMsgType
    {
        Text = 0,
        Image = 1,
        Audio = 2,
        Video = 3,
        Location = 4,
        Notification = 5,
        File = 6,
        Tips = 10,
        Robot = 11,
        G2NetCall = 12,
        Custom = 100,
        Unknown = 1000
    }
    
    /// <summary>
    /// 会话类型
    /// </summary>
    public enum NimSessionType
    {
        P2P = 0,
        Team = 1
    }
    
    /// <summary>
    /// 群类型
    /// </summary>
    public enum NimTeamType
    {
        Normal = 0,
        Advanced = 1
    }
    
    /// <summary>
    /// 群成员类型
    /// </summary>
    public enum NimTeamUserType
    {
        Normal = 0,
        Creator = 1,
        Manager = 2,
        Apply = 3,
        Inviting = 4
    }
    
    /// <summary>
    /// 群通知类型
    /// </summary>
    public enum NimTeamNotifyType
    {
        Invalid = 0,
        Invite = 1,          // 邀请
        Kick = 2,            // 踢出
        Leave = 3,           // 离开
        Update = 4,          // 更新群信息
        Dismiss = 5,         // 解散
        ApplyPass = 6,       // 申请入群通过
        TransferOwner = 7,   // 转让群主
        AddManager = 8,      // 添加管理员
        RemoveManager = 9,   // 移除管理员
        InviteAccept = 10,   // 接受入群邀请
        MuteMember = 11,     // 禁言/解禁成员
        MuteAll = 12         // 全员禁言
    }
    
    /// <summary>
    /// 客户端类型
    /// </summary>
    public enum NimClientType
    {
        Default = 0,
        Android = 1,
        iOS = 2,
        PC = 4,
        Web = 16,
        Mac = 64
    }
    
    #endregion
}
