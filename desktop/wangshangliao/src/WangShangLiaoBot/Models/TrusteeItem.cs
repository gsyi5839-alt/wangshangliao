using System;

namespace WangShangLiaoBot.Models
{
    /// <summary>
    /// 托管项数据模型
    /// </summary>
    public class TrusteeItem
    {
        /// <summary>序号</summary>
        public int Index { get; set; }
        
        /// <summary>旺旺号</summary>
        public string WangWangId { get; set; }
        
        /// <summary>姓名/昵称</summary>
        public string NickName { get; set; }
        
        /// <summary>托管内容（下注内容）</summary>
        public string Content { get; set; }
        
        /// <summary>创建时间</summary>
        public DateTime CreateTime { get; set; }
        
        /// <summary>是否有效</summary>
        public bool IsActive { get; set; }

        public TrusteeItem()
        {
            CreateTime = DateTime.Now;
            IsActive = true;
        }

        public TrusteeItem(string wangWangId, string content) : this()
        {
            WangWangId = wangWangId;
            Content = content;
        }

        /// <summary>
        /// 转换为聊天格式（用于导出）
        /// </summary>
        public string ToChatFormat()
        {
            return $"{NickName ?? WangWangId} {Content}";
        }

        public override string ToString()
        {
            return $"{WangWangId}\t{NickName}\t{Content}";
        }

        /// <summary>
        /// 从字符串解析
        /// </summary>
        public static TrusteeItem Parse(string line)
        {
            if (string.IsNullOrWhiteSpace(line)) return null;
            var parts = line.Split('\t');
            if (parts.Length < 3) return null;
            return new TrusteeItem
            {
                WangWangId = parts[0],
                NickName = parts[1],
                Content = parts[2],
                IsActive = true
            };
        }
    }
}

