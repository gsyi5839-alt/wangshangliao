using System;

namespace WangShangLiaoBot.Models
{
    /// <summary>
    /// Team/Group member model - compatible with WangShangLiao NIM SDK getTeamMembers response
    /// </summary>
    [Serializable]
    public class TeamMember
    {
        /// <summary>Unique member ID (teamId-account format)</summary>
        public string Id { get; set; }
        
        /// <summary>Team/Group ID this member belongs to</summary>
        public string TeamId { get; set; }
        
        /// <summary>Member account ID - primary identifier for OnlyMemberBet</summary>
        public string Account { get; set; }
        
        /// <summary>Nickname within the team (may be MD5 hashed)</summary>
        public string NickInTeam { get; set; }
        
        /// <summary>Member type: normal, owner, manager</summary>
        public string Type { get; set; }
        
        /// <summary>Join time (Unix timestamp in milliseconds)</summary>
        public long JoinTime { get; set; }
        
        /// <summary>Last update time (Unix timestamp in milliseconds)</summary>
        public long UpdateTime { get; set; }
        
        /// <summary>Is member active</summary>
        public bool Active { get; set; }
        
        /// <summary>Is member valid</summary>
        public bool Valid { get; set; }
        
        /// <summary>Is member muted</summary>
        public bool Mute { get; set; }
        
        /// <summary>Invitor account ID</summary>
        public string InvitorAccid { get; set; }
        
        /// <summary>Custom data JSON (contains groupId and nicknameCiphertext)</summary>
        public string Custom { get; set; }
        
        /// <summary>Check if member is owner</summary>
        public bool IsOwner => string.Equals(Type, "owner", StringComparison.OrdinalIgnoreCase);
        
        /// <summary>Check if member is manager</summary>
        public bool IsManager => string.Equals(Type, "manager", StringComparison.OrdinalIgnoreCase);
        
        /// <summary>Get join datetime</summary>
        public DateTime JoinDateTime => JoinTime > 0 
            ? DateTimeOffset.FromUnixTimeMilliseconds(JoinTime).LocalDateTime 
            : DateTime.MinValue;
        
        /// <summary>Get update datetime</summary>
        public DateTime UpdateDateTime => UpdateTime > 0 
            ? DateTimeOffset.FromUnixTimeMilliseconds(UpdateTime).LocalDateTime 
            : DateTime.MinValue;
    }
    
    /// <summary>
    /// Team member type constants
    /// </summary>
    public static class TeamMemberType
    {
        public const string Normal = "normal";
        public const string Owner = "owner";
        public const string Manager = "manager";
    }
}

