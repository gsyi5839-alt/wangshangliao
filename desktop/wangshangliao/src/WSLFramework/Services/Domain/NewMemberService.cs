using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using WSLFramework.Protocol;
using WSLFramework.Utils;

namespace WSLFramework.Services
{
    /// <summary>
    /// 新成员处理服务 - 完全匹配ZCG的新成员处理功能
    /// 包括自动修改群名片、发送欢迎消息等
    /// 基于日志分析: ww_修改群片, NOTIFY_TYPE_USER_UPDATE_NAME
    /// </summary>
    public class NewMemberService
    {
        private static readonly Lazy<NewMemberService> _lazy = 
            new Lazy<NewMemberService>(() => new NewMemberService());
        public static NewMemberService Instance => _lazy.Value;
        
        // CDP桥接
        private CDPBridge _cdpBridge;
        
        // 群管理服务
        private GroupManagementService _groupManager;
        
        // 已处理成员缓存（防止重复处理）
        private readonly ConcurrentDictionary<string, DateTime> _processedMembers;
        
        // 成员群名片缓存
        private readonly ConcurrentDictionary<string, MemberCardInfo> _memberCards;
        
        // 配置
        public bool AutoModifyCard { get; set; } = true;
        public string CardFormat { get; set; } = "{nickname}";  // 群名片格式
        public bool SendGroupWelcome { get; set; } = true;
        public string GroupWelcomeTemplate { get; set; } = "{nickname}号({shortId})群名片自动修改为:{newCard}";
        
        // 事件
        public event EventHandler<NewMemberEventArgs> OnNewMember;
        public event EventHandler<CardChangeEventArgs> OnCardChanged;
        
        private NewMemberService()
        {
            _processedMembers = new ConcurrentDictionary<string, DateTime>();
            _memberCards = new ConcurrentDictionary<string, MemberCardInfo>();
        }
        
        /// <summary>
        /// 初始化服务
        /// </summary>
        public void Initialize(CDPBridge cdpBridge, GroupManagementService groupManager)
        {
            _cdpBridge = cdpBridge;
            _groupManager = groupManager;
            Logger.Info("新成员处理服务已初始化");
        }
        
        #region 新成员处理
        
        /// <summary>
        /// 处理新成员加入群
        /// 基于日志: 新成员加入时自动修改群名片
        /// </summary>
        public async Task HandleNewMemberAsync(string groupId, string memberId, string nickname)
        {
            try
            {
                // 检查是否已处理
                var key = $"{groupId}_{memberId}";
                if (_processedMembers.TryGetValue(key, out var processedTime))
                {
                    // 5分钟内不重复处理
                    if ((DateTime.Now - processedTime).TotalMinutes < 5)
                    {
                        return;
                    }
                }
                
                Logger.Info($"处理新成员: groupId={groupId}, memberId={memberId}, nickname={nickname}");
                
                // 标记为已处理
                _processedMembers[key] = DateTime.Now;
                
                // 触发事件
                OnNewMember?.Invoke(this, new NewMemberEventArgs
                {
                    GroupId = groupId,
                    MemberId = memberId,
                    Nickname = nickname
                });
                
                // 自动修改群名片
                if (AutoModifyCard)
                {
                    await ModifyMemberCardAsync(groupId, memberId, nickname);
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"处理新成员失败: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 修改成员群名片
        /// API: ww_修改群片|机器人ID|群ID|成员ID|新名片
        /// </summary>
        public async Task ModifyMemberCardAsync(string groupId, string memberId, string nickname)
        {
            try
            {
                if (_cdpBridge == null)
                {
                    Logger.Warning("CDP未连接，无法修改群名片");
                    return;
                }
                
                var shortId = ZCGFullApiSpec.GetShortId(memberId);
                
                // 格式化新群名片
                var newCard = CardFormat
                    .Replace("{nickname}", nickname ?? "")
                    .Replace("{shortId}", shortId)
                    .Replace("{memberId}", memberId);
                
                // 调用CDP修改群名片
                await _cdpBridge.ModifyMemberCardAsync(groupId, memberId, newCard);
                
                // 缓存群名片信息
                var cardInfo = new MemberCardInfo
                {
                    GroupId = groupId,
                    MemberId = memberId,
                    OriginalNickname = nickname,
                    NewCard = newCard,
                    ModifyTime = DateTime.Now
                };
                
                var key = $"{groupId}_{memberId}";
                _memberCards[key] = cardInfo;
                
                Logger.Info($"已修改群名片: {nickname} -> {newCard}");
                
                // 触发事件
                OnCardChanged?.Invoke(this, new CardChangeEventArgs
                {
                    GroupId = groupId,
                    MemberId = memberId,
                    OldCard = nickname,
                    NewCard = newCard
                });
                
                // 发送群通知
                if (SendGroupWelcome)
                {
                    var notice = GroupWelcomeTemplate
                        .Replace("{nickname}", nickname ?? "")
                        .Replace("{shortId}", shortId)
                        .Replace("{newCard}", newCard);
                    
                    await _cdpBridge.SendGroupMessageAsync(groupId, notice);
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"修改群名片失败: {ex.Message}");
            }
        }
        
        #endregion
        
        #region 群名片变更通知处理
        
        /// <summary>
        /// 处理群名片变更通知
        /// 基于日志: NOTIFY_TYPE_USER_UPDATE_NAME
        /// JSON格式: {"groupId":1176721,"afterNickname":"娜一"}
        /// </summary>
        public async Task HandleCardChangeNotifyAsync(string groupId, string memberId, string newNickname)
        {
            try
            {
                Logger.Info($"收到名片变更通知: groupId={groupId}, memberId={memberId}, newNickname={newNickname}");
                
                var key = $"{groupId}_{memberId}";
                
                // 检查是否是我们自己修改的
                if (_memberCards.TryGetValue(key, out var cachedInfo))
                {
                    if (cachedInfo.NewCard == newNickname)
                    {
                        // 是我们自己修改的，忽略
                        return;
                    }
                }
                
                // 其他人修改的群名片
                Logger.Info($"群名片被其他人修改: {memberId} -> {newNickname}");
            }
            catch (Exception ex)
            {
                Logger.Error($"处理名片变更通知失败: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 解析群名片变更JSON
        /// 格式: {"groupId":1176721,"afterNickname":"娜一"}
        /// 或者: {"groupId":1176721,"nicknameCiphertext":"JkyNPxugMtHc1g5rJgQcBA=="}
        /// </summary>
        public CardChangeData ParseCardChangeJson(string json)
        {
            try
            {
                if (string.IsNullOrEmpty(json))
                    return null;
                
                var result = new CardChangeData();
                
                // 解析groupId
                var groupIdMatch = Regex.Match(json, @"""groupId""\s*:\s*(\d+)");
                if (groupIdMatch.Success)
                {
                    result.GroupId = groupIdMatch.Groups[1].Value;
                }
                
                // 解析afterNickname
                var nicknameMatch = Regex.Match(json, @"""afterNickname""\s*:\s*""([^""]+)""");
                if (nicknameMatch.Success)
                {
                    result.AfterNickname = nicknameMatch.Groups[1].Value;
                }
                
                // 解析nicknameCiphertext（加密的昵称）
                var ciphertextMatch = Regex.Match(json, @"""nicknameCiphertext""\s*:\s*""([^""]+)""");
                if (ciphertextMatch.Success)
                {
                    result.NicknameCiphertext = ciphertextMatch.Groups[1].Value;
                    // 需要解密 - 使用AES解密
                    result.AfterNickname = DecryptNickname(result.NicknameCiphertext);
                }
                
                return result;
            }
            catch (Exception ex)
            {
                Logger.Error($"解析名片变更JSON失败: {ex.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// 解密群名片昵称
        /// 基于日志: nickname_ciphertext 使用AES加密
        /// </summary>
        private string DecryptNickname(string ciphertext)
        {
            try
            {
                if (string.IsNullOrEmpty(ciphertext))
                    return "";
                
                // Base64解码
                var encryptedBytes = Convert.FromBase64String(ciphertext);
                
                // AES解密 - 使用固定密钥（需要根据实际分析确定）
                // 这里只是占位，实际密钥需要逆向分析获取
                // 暂时返回Base64解码后的字符串（如果是UTF8文本）
                try
                {
                    return System.Text.Encoding.UTF8.GetString(encryptedBytes);
                }
                catch
                {
                    return ciphertext; // 解密失败返回原文
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"解密昵称失败: {ex.Message}");
                return ciphertext;
            }
        }
        
        #endregion
        
        #region 成员信息查询
        
        /// <summary>
        /// 获取成员群名片
        /// </summary>
        public MemberCardInfo GetMemberCard(string groupId, string memberId)
        {
            var key = $"{groupId}_{memberId}";
            _memberCards.TryGetValue(key, out var cardInfo);
            return cardInfo;
        }
        
        /// <summary>
        /// 获取群所有成员名片
        /// </summary>
        public List<MemberCardInfo> GetGroupMemberCards(string groupId)
        {
            var result = new List<MemberCardInfo>();
            
            foreach (var kvp in _memberCards)
            {
                if (kvp.Value.GroupId == groupId)
                {
                    result.Add(kvp.Value);
                }
            }
            
            return result;
        }
        
        /// <summary>
        /// 同步群成员信息
        /// API: ww_获取群成员|机器人ID|群ID
        /// </summary>
        public async Task SyncGroupMembersAsync(string groupId)
        {
            try
            {
                if (_cdpBridge == null)
                    return;
                
                var members = await _cdpBridge.GetGroupMembersAsync(groupId);
                if (members == null)
                    return;
                
                foreach (var member in members)
                {
                    var key = $"{groupId}_{member.memberId}";
                    var cardInfo = new MemberCardInfo
                    {
                        GroupId = groupId,
                        MemberId = member.memberId,
                        OriginalNickname = member.nickname,
                        NewCard = member.card ?? member.nickname,
                        ModifyTime = DateTime.Now
                    };
                    
                    _memberCards[key] = cardInfo;
                }
                
                Logger.Info($"已同步群成员: groupId={groupId}, count={members.Length}");
            }
            catch (Exception ex)
            {
                Logger.Error($"同步群成员失败: {ex.Message}");
            }
        }
        
        #endregion
        
        #region 清理
        
        /// <summary>
        /// 清理过期缓存
        /// </summary>
        public void CleanupCache()
        {
            var now = DateTime.Now;
            var keysToRemove = new List<string>();
            
            // 清理已处理成员缓存（超过1小时）
            foreach (var kvp in _processedMembers)
            {
                if ((now - kvp.Value).TotalHours > 1)
                {
                    keysToRemove.Add(kvp.Key);
                }
            }
            
            foreach (var key in keysToRemove)
            {
                _processedMembers.TryRemove(key, out _);
            }
            
            Logger.Info($"已清理过期缓存: {keysToRemove.Count} 条");
        }
        
        #endregion
    }
    
    #region 数据模型
    
    /// <summary>
    /// 成员群名片信息
    /// </summary>
    public class MemberCardInfo
    {
        public string GroupId { get; set; }
        public string MemberId { get; set; }
        public string OriginalNickname { get; set; }
        public string NewCard { get; set; }
        public DateTime ModifyTime { get; set; }
    }
    
    /// <summary>
    /// 群名片变更数据
    /// </summary>
    public class CardChangeData
    {
        public string GroupId { get; set; }
        public string AfterNickname { get; set; }
        public string NicknameCiphertext { get; set; }
    }
    
    /// <summary>
    /// 新成员事件参数
    /// </summary>
    public class NewMemberEventArgs : EventArgs
    {
        public string GroupId { get; set; }
        public string MemberId { get; set; }
        public string Nickname { get; set; }
    }
    
    /// <summary>
    /// 群名片变更事件参数
    /// </summary>
    public class CardChangeEventArgs : EventArgs
    {
        public string GroupId { get; set; }
        public string MemberId { get; set; }
        public string OldCard { get; set; }
        public string NewCard { get; set; }
    }
    
    #endregion
}
