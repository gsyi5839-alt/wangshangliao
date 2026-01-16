using System;

namespace WangShangLiaoBot.Models
{
    /// <summary>
    /// Team/Group info model - compatible with WangShangLiao NIM SDK getTeam response
    /// </summary>
    [Serializable]
    public class TeamInfo
    {
        /// <summary>Team/Group ID (NIM SDK teamId = groupCloudId)</summary>
        public string TeamId { get; set; }
        
        /// <summary>Internal group ID (used for WangShangLiao backend API)</summary>
        public int GroupId { get; set; }
        
        /// <summary>Group cloud ID (same as TeamId, used for NIM SDK)</summary>
        public string GroupCloudId { get; set; }
        
        /// <summary>Group account/number (external visible group number)</summary>
        public string GroupAccount { get; set; }
        
        /// <summary>Team name (may be encrypted)</summary>
        public string TeamName { get; set; }
        
        /// <summary>Team name (decrypted)</summary>
        public string Name { get; set; }
        
        /// <summary>Team type: advanced or normal</summary>
        public string Type { get; set; }
        
        /// <summary>Owner account ID</summary>
        public string Owner { get; set; }
        
        /// <summary>Team level</summary>
        public int Level { get; set; }
        
        /// <summary>Is team valid</summary>
        public bool Valid { get; set; }
        
        /// <summary>Is team valid to current user</summary>
        public bool ValidToCurrentUser { get; set; }
        
        /// <summary>Member count</summary>
        public int MemberNum { get; set; }
        
        /// <summary>Member update time (Unix timestamp in milliseconds)</summary>
        public long MemberUpdateTime { get; set; }
        
        /// <summary>Create time (Unix timestamp in milliseconds)</summary>
        public long CreateTime { get; set; }
        
        /// <summary>Update time (Unix timestamp in milliseconds)</summary>
        public long UpdateTime { get; set; }
        
        /// <summary>Team avatar URL</summary>
        public string Avatar { get; set; }
        
        /// <summary>Team introduction</summary>
        public string Intro { get; set; }
        
        /// <summary>Team announcement</summary>
        public string Announcement { get; set; }
        
        /// <summary>Join mode: noVerify, needVerify, rejectAll</summary>
        public string JoinMode { get; set; }
        
        /// <summary>Be invite mode</summary>
        public string BeInviteMode { get; set; }
        
        /// <summary>Invite mode</summary>
        public string InviteMode { get; set; }
        
        /// <summary>Update team mode</summary>
        public string UpdateTeamMode { get; set; }
        
        /// <summary>Update custom mode</summary>
        public string UpdateCustomMode { get; set; }
        
        /// <summary>Is team muted</summary>
        public bool Mute { get; set; }
        
        /// <summary>Mute type</summary>
        public string MuteType { get; set; }
        
        /// <summary>Server custom data</summary>
        public string ServerCustom { get; set; }
        
        /// <summary>Custom data</summary>
        public string Custom { get; set; }
        
        /// <summary>Check if team is advanced type</summary>
        public bool IsAdvanced => string.Equals(Type, "advanced", StringComparison.OrdinalIgnoreCase);
        
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
    /// Team type constants
    /// </summary>
    public static class TeamType
    {
        public const string Normal = "normal";
        public const string Advanced = "advanced";
    }
    
    /// <summary>
    /// Team join mode constants
    /// </summary>
    public static class TeamJoinMode
    {
        public const string NoVerify = "noVerify";
        public const string NeedVerify = "needVerify";
        public const string RejectAll = "rejectAll";
    }
}

