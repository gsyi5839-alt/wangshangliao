using System;

namespace WangShangLiaoBot.Models
{
    /// <summary>
    /// User info model - compatible with WangShangLiao NIM SDK getUser/getUsers response
    /// </summary>
    [Serializable]
    public class UserInfo
    {
        /// <summary>User account ID</summary>
        public string Account { get; set; }
        
        /// <summary>User nickname (may be MD5 hashed)</summary>
        public string Nick { get; set; }
        
        /// <summary>User avatar URL or ID</summary>
        public string Avatar { get; set; }
        
        /// <summary>User gender: unknown, male, female</summary>
        public string Gender { get; set; }
        
        /// <summary>Custom data JSON (contains nickname_ciphertext)</summary>
        public string Custom { get; set; }
        
        /// <summary>Create time (Unix timestamp in milliseconds)</summary>
        public long CreateTime { get; set; }
        
        /// <summary>Update time (Unix timestamp in milliseconds)</summary>
        public long UpdateTime { get; set; }
        
        /// <summary>Email address</summary>
        public string Email { get; set; }
        
        /// <summary>Phone number</summary>
        public string Tel { get; set; }
        
        /// <summary>Birthday</summary>
        public string Birth { get; set; }
        
        /// <summary>Signature</summary>
        public string Sign { get; set; }
        
        /// <summary>Get create datetime</summary>
        public DateTime CreateDateTime => CreateTime > 0 
            ? DateTimeOffset.FromUnixTimeMilliseconds(CreateTime).LocalDateTime 
            : DateTime.MinValue;
        
        /// <summary>Get update datetime</summary>
        public DateTime UpdateDateTime => UpdateTime > 0 
            ? DateTimeOffset.FromUnixTimeMilliseconds(UpdateTime).LocalDateTime 
            : DateTime.MinValue;
    }
    
    /// <summary>
    /// Friend info model - compatible with WangShangLiao NIM SDK getFriends response
    /// </summary>
    [Serializable]
    public class FriendInfo
    {
        /// <summary>Friend account ID</summary>
        public string Account { get; set; }
        
        /// <summary>Friend alias (remark name)</summary>
        public string Alias { get; set; }
        
        /// <summary>Create time (Unix timestamp in milliseconds)</summary>
        public long CreateTime { get; set; }
        
        /// <summary>Update time (Unix timestamp in milliseconds)</summary>
        public long UpdateTime { get; set; }
        
        /// <summary>Is friend valid</summary>
        public bool Valid { get; set; }
        
        /// <summary>Custom data JSON</summary>
        public string Custom { get; set; }
    }
    
    /// <summary>
    /// Session info model - compatible with WangShangLiao NIM SDK getLocalSessions response
    /// </summary>
    [Serializable]
    public class SessionInfo
    {
        /// <summary>Session ID (format: scene-to, e.g. team-40821608989)</summary>
        public string Id { get; set; }
        
        /// <summary>Scene type: p2p, team</summary>
        public string Scene { get; set; }
        
        /// <summary>Target ID (user account for p2p, team ID for team)</summary>
        public string To { get; set; }
        
        /// <summary>Unread message count</summary>
        public int Unread { get; set; }
        
        /// <summary>Update time (Unix timestamp in milliseconds)</summary>
        public long UpdateTime { get; set; }
        
        /// <summary>Is session pinned (stick top)</summary>
        public bool IsTop { get; set; }
        
        /// <summary>Custom data for stick top</summary>
        public string TopCustom { get; set; }
        
        /// <summary>Check if this is a team/group session</summary>
        public bool IsTeam => string.Equals(Scene, "team", StringComparison.OrdinalIgnoreCase);
        
        /// <summary>Check if this is a P2P (private) session</summary>
        public bool IsP2P => string.Equals(Scene, "p2p", StringComparison.OrdinalIgnoreCase);
        
        /// <summary>Get update datetime</summary>
        public DateTime UpdateDateTime => UpdateTime > 0 
            ? DateTimeOffset.FromUnixTimeMilliseconds(UpdateTime).LocalDateTime 
            : DateTime.MinValue;
    }
}
