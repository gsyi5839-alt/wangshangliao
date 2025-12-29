using System;

namespace WangShangLiaoBot.Models.Spam
{
    /// <summary>
    /// Keyword rule for spam detection.
    /// If message contains Keyword, execute Action (mute/kick).
    /// </summary>
    [Serializable]
    public sealed class SpamKeywordRule
    {
        public string Keyword { get; set; } = "";
        public SpamAction Action { get; set; } = SpamAction.Mute;
    }

    public enum SpamAction
    {
        Mute = 0,
        Kick = 1
    }
}


