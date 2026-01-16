using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Timers;
using WangShangLiaoBot.Models;
using WangShangLiaoBot.Services;
using WangShangLiaoBot.Services.HPSocket;
using WangShangLiaoBot.Forms.Settings;
using WangShangLiaoBot.Controls;
using WangShangLiaoBot.Controls.BetProcess;
using WangShangLiaoBot.Controls.Odds;

namespace WangShangLiaoBot.Forms
{
    public partial class MainForm : Form
    {
        private void LoadPlayerData()
        {
            // 【优化】从绑定群的成员缓存加载玩家
            _players = LoadPlayersFromBoundGroup();
            Logger.Info($"[MainForm] LoadPlayerData: Loaded {_players?.Count ?? 0} players");
        }

        private List<Player> LoadPlayersFromBoundGroup()
        {
            var players = new List<Player>();
            
            try
            {
                // 获取绑定的群号
                var config = ConfigService.Instance.Config;
                var groupId = config?.GroupId;
                
                // 如果没有配置群号，尝试从已有的群成员缓存中获取第一个
                if (string.IsNullOrEmpty(groupId))
                {
                    var cachedGroups = DataService.Instance.GetCachedGroupIds();
                    if (cachedGroups != null && cachedGroups.Count > 0)
                    {
                        groupId = cachedGroups[0];
                        Logger.Info($"[MainForm] 未配置绑定群号，使用缓存中的群: {groupId}");
                    }
                    else
                    {
                        Logger.Info("[MainForm] 未配置绑定群号且无缓存，玩家列表为空");
                        return players;
                    }
                }
                
                // 从群成员缓存加载
                var members = DataService.Instance.LoadGroupMembersCache(groupId);
                
                // 如果群成员缓存为空，尝试加载有余额的玩家作为后备
                if (members == null || members.Count == 0)
                {
                    Logger.Info($"[MainForm] 群 {groupId} 缓存为空，加载有余额的玩家");
                    return LoadPlayersWithBalance();
                }
                
                Logger.Info($"[MainForm] 从群 {groupId} 缓存加载了 {members.Count} 个成员");
                
                // 转换为 Player 对象，并获取余额信息
                foreach (var member in members)
                {
                    if (string.IsNullOrEmpty(member.WangShangId)) continue;
                    
                    // 获取玩家余额
                    var score = ScoreService.Instance.GetPlayerScore(member.WangShangId);
                    
                    // 尝试从玩家数据文件获取更多信息
                    var existingPlayer = DataService.Instance.GetPlayer(member.WangShangId);
                    
                    var player = new Player
                    {
                        WangWangId = member.WangShangId,
                        Nickname = existingPlayer?.Nickname ?? member.Name ?? "",
                        Score = score?.Balance ?? 0,
                        ReservedScore = existingPlayer?.ReservedScore ?? 0,
                        IsTuo = existingPlayer?.IsTuo ?? false,
                        Remark = existingPlayer?.Remark ?? "",
                        LastActiveTime = existingPlayer?.LastActiveTime ?? DateTime.MinValue
                    };
                    
                    players.Add(player);
                }
                
                // 按余额排序（有余额的在前）
                players = players.OrderByDescending(p => p.Score).ToList();
            }
            catch (Exception ex)
            {
                Logger.Error($"[MainForm] 加载绑定群玩家失败: {ex.Message}");
            }
            
            return players;
        }

        private List<Player> LoadPlayersWithBalance()
        {
            var players = new List<Player>();
            
            try
            {
                var scores = ScoreService.Instance.GetAllPlayersWithBalance();
                
                foreach (var score in scores.OrderByDescending(s => s.Balance).Take(MAX_DISPLAY_PLAYERS))
                {
                    var existingPlayer = DataService.Instance.GetPlayer(score.PlayerId);
                    
                    players.Add(new Player
                    {
                        WangWangId = score.PlayerId,
                        Nickname = existingPlayer?.Nickname ?? score.PlayerNick ?? "",
                        Score = score.Balance,
                        ReservedScore = existingPlayer?.ReservedScore ?? 0,
                        IsTuo = existingPlayer?.IsTuo ?? false
                    });
                }
                
                Logger.Info($"[MainForm] 后备加载了 {players.Count} 个有余额的玩家");
            }
            catch (Exception ex)
            {
                Logger.Error($"[MainForm] 后备加载玩家失败: {ex.Message}");
            }
            
            return players;
        }

        private void RefreshPlayerList(bool reloadFromDatabase = false)
        {
            // 【优化】使用 BeginUpdate/EndUpdate 批量更新，避免逐个重绘
            listPlayers.BeginUpdate();
            
            try
            {
                // Reload data from database if requested
                if (reloadFromDatabase)
                {
                    // 【优化】从绑定群加载玩家
                    _players = LoadPlayersFromBoundGroup();
                    Logger.Info($"[MainForm] After reload: _players.Count={_players?.Count ?? 0}");
                }
                
                listPlayers.Items.Clear();
                
                // 【BUG修复】检查 _players 是否为 null
                if (_players == null)
                {
                    _players = new List<Player>();
                }
                
                var displayPlayers = chkShowTuoPlayer.Checked 
                    ? _players 
                    : _players.Where(p => !p.IsTuo).ToList();
                
                _totalPlayerCount = displayPlayers.Count;
                
                // 【优化】限制显示数量，避免 UI 卡顿
                var playersToShow = displayPlayers.Take(MAX_DISPLAY_PLAYERS).ToList();
                
                // 【优化】批量创建 ListViewItem
                var items = new ListViewItem[playersToShow.Count];
                for (int i = 0; i < playersToShow.Count; i++)
                {
                    var player = playersToShow[i];
                    var item = new ListViewItem(player.WangWangId);
                    var displayNickname = (string.IsNullOrEmpty(player.Nickname) || IsMd5Hash(player.Nickname))
                        ? (player.WangWangId.Length >= 4 ? player.WangWangId.Substring(player.WangWangId.Length - 4) : player.WangWangId)
                        : player.Nickname;
                    item.SubItems.Add(displayNickname);
                    item.SubItems.Add(player.Score.ToString());
                    item.SubItems.Add(player.ReservedScore.ToString());
                    item.SubItems.Add(player.Remark ?? "");
                    item.SubItems.Add(player.LastActiveTime.ToString("HH:mm"));
                    item.Tag = player;
                    items[i] = item;
                }
                
                // 【优化】使用 AddRange 批量添加
                listPlayers.Items.AddRange(items);
                
                // 显示状态
                if (_totalPlayerCount > MAX_DISPLAY_PLAYERS)
                {
                    lblStatus.Text = $"显示前 {MAX_DISPLAY_PLAYERS} 个玩家 (共 {_totalPlayerCount} 个)";
                }
                else
                {
                    lblStatus.Text = $"共 {_totalPlayerCount} 个玩家";
                }
            }
            finally
            {
                listPlayers.EndUpdate();
            }
        }

        private async void btnViewAccount_Click(object sender, EventArgs e)
        {
            var frameworkClient = Services.HPSocket.FrameworkClient.Instance;
            
            // 检查副框架连接
            if (!frameworkClient.IsConnected)
            {
                MessageBox.Show("请先连接副框架！\n\n副框架（招财狗框架）需要先启动并登录旺商聊", 
                    "未连接", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            
            try
            {
                // 从副框架获取账号信息
                var accountInfo = await frameworkClient.GetAccountInfoAsync();
                
                if (accountInfo != null)
                {
                    var message = $"═══════ 账号信息 ═══════\n\n" +
                                  $"账号ID：{accountInfo.AccountId ?? "未知"}\n" +
                                  $"旺旺号：{accountInfo.Wwid ?? "未知"}\n" +
                                  $"昵　称：{accountInfo.Nickname ?? "未知"}\n" +
                                  $"群　号：{accountInfo.GroupId ?? "未设置"}\n" +
                                  $"群名称：{accountInfo.GroupName ?? "未知"}\n" +
                                  $"NIM ID：{accountInfo.NimId ?? "未知"}\n" +
                                  $"状　态：{(accountInfo.IsLoggedIn ? "已登录" : "未登录")}\n\n" +
                                  $"═══════════════════════";
                    
                    MessageBox.Show(message, "查看账号信息", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    
                    // 同步到本地配置
                    var config = ConfigService.Instance.Config;
                    if (!string.IsNullOrEmpty(accountInfo.GroupId))
                    {
                        config.GroupId = accountInfo.GroupId;
                        config.GroupName = accountInfo.GroupName;
                        ConfigService.Instance.SaveConfig();
                    }
                    
                    Logger.Info($"[MainForm] 玩家管理初始化: 管理旺旺号={accountInfo.Wwid}, 绑定群={accountInfo.GroupId}");
                }
                else
                {
                    MessageBox.Show("无法获取账号信息，请确认副框架已登录旺商聊", 
                        "获取失败", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"获取账号信息失败: {ex.Message}");
                MessageBox.Show($"获取账号信息失败：{ex.Message}", 
                    "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnModifyInfo_Click(object sender, EventArgs e)
        {
            var wangwangId = txtWangWangId.Text.Trim();
            var nickname = txtNickname.Text.Trim();
            var scoreText = txtScore.Text.Trim();
            
            if (string.IsNullOrEmpty(wangwangId))
            {
                MessageBox.Show("请输入旺旺号", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            
            decimal score;
            if (!decimal.TryParse(scoreText, out score))
            {
                score = 0;
            }
            
            // Find or create player
            var player = _players.FirstOrDefault(p => p.WangWangId == wangwangId);
            if (player == null)
            {
                player = new Player 
                { 
                    WangWangId = wangwangId,
                    LastActiveTime = DateTime.Now
                };
                _players.Add(player);
            }
            
            player.Nickname = nickname;
            player.Score = score;
            player.LastActiveTime = DateTime.Now;
            
            // Save to file
            DataService.Instance.SavePlayer(player);
            RefreshPlayerList();
            
            MessageBox.Show("玩家信息已更新", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void btnSearchPlayer_Click(object sender, EventArgs e)
        {
            var searchText = txtWangWangId.Text.Trim();
            if (string.IsNullOrEmpty(searchText))
            {
                RefreshPlayerList();
                return;
            }
            
            var found = _players.Where(p => 
                p.WangWangId.Contains(searchText) || 
                (p.Nickname != null && p.Nickname.Contains(searchText))).ToList();
            
            listPlayers.Items.Clear();
            foreach (var player in found)
            {
                var item = new ListViewItem(player.WangWangId);
                // Show last 4 digits of WangWangId if nickname is empty or is MD5 hash
                var displayNickname = (string.IsNullOrEmpty(player.Nickname) || IsMd5Hash(player.Nickname))
                    ? (player.WangWangId.Length >= 4 ? player.WangWangId.Substring(player.WangWangId.Length - 4) : player.WangWangId)
                    : player.Nickname;
                item.SubItems.Add(displayNickname);
                item.SubItems.Add(player.Score.ToString());
                item.SubItems.Add(player.ReservedScore.ToString());
                item.SubItems.Add(player.Remark ?? "");
                item.SubItems.Add(player.LastActiveTime.ToString("HH:mm"));
                item.Tag = player;
                listPlayers.Items.Add(item);
            }
            
            lblStatus.Text = string.Format("搜索到 {0} 个玩家", filteredPlayers.Count);
        }

        private void listPlayers_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listPlayers.SelectedItems.Count > 0)
            {
                var player = listPlayers.SelectedItems[0].Tag as Player;
                if (player != null)
                {
                    txtWangWangId.Text = player.WangWangId;
                    txtNickname.Text = player.Nickname;
                    txtScore.Text = player.Score.ToString();
                }
            }
        }

        private async void btnSyncNicknames_Click(object sender, EventArgs e)
        {
            try
            {
                if (!ChatService.Instance.IsConnected)
                {
                    MessageBox.Show("请先连接到旺商聊", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                
                lblStatus.Text = "正在获取群列表...";
                btnSyncNicknames.Enabled = false;
                
                // Get all teams
                var teams = await ChatService.Instance.GetAllTeamsAsync();
                Logger.Info($"[MainForm] Found {teams?.Count ?? 0} teams to sync");
                
                if (teams == null || teams.Count == 0)
                {
                    // Fallback to current session
                    await SyncMemberNicknamesAsync();
                }
                else
                {
                    int totalCreated = 0;
                    int totalUpdated = 0;
                    
                    // Sync each team
                    for (int i = 0; i < teams.Count; i++)
                    {
                        var team = teams[i];
                        lblStatus.Text = $"正在同步群 {i+1}/{teams.Count}: {team.Name}...";
                        Logger.Info($"[MainForm] Syncing team {team.TeamId}: {team.Name} ({team.MemberNum} members)");
                        
                        var nicknames = await ChatService.Instance.GetMemberPlaintextNicknamesAsync(team.TeamId);
                        if (nicknames != null && nicknames.Count > 0)
                        {
                            Logger.Info($"[MainForm] Got {nicknames.Count} nicknames from team {team.TeamId}");
                            
                            foreach (var kvp in nicknames)
                            {
                                var account = kvp.Key;
                                var nickname = kvp.Value;
                                
                                if (string.IsNullOrWhiteSpace(nickname) || IsMd5Hash(nickname))
                                    continue;
                                
                                var existingPlayer = DataService.Instance.GetPlayer(account);
                                
                                if (existingPlayer == null)
                                {
                                    var newPlayer = new Player
                                    {
                                        WangWangId = account,
                                        Nickname = nickname,
                                        Score = 0
                                    };
                                    DataService.Instance.SavePlayer(newPlayer);
                                    totalCreated++;
                                }
                                else if (string.IsNullOrEmpty(existingPlayer.Nickname) || IsMd5Hash(existingPlayer.Nickname))
                                {
                                    existingPlayer.Nickname = nickname;
                                    DataService.Instance.SavePlayer(existingPlayer);
                                    totalUpdated++;
                                }
                            }
                        }
                    }
                    
                    Logger.Info($"[MainForm] Sync all teams complete: created={totalCreated}, updated={totalUpdated}");
                    
                    // Reload and refresh
                    _players = DataService.Instance.GetAllPlayers();
                    RefreshPlayerList();
                }
                
                lblStatus.Text = $"同步完成，共 {_players.Count} 个玩家";
            }
            catch (Exception ex)
            {
                Logger.Error($"[MainForm] Failed to sync nicknames: {ex.Message}");
                lblStatus.Text = "同步失败";
                MessageBox.Show($"同步昵称失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnSyncNicknames.Enabled = true;
            }
        }

        private async Task SyncMemberNicknamesAsync()
        {
            try
            {
                // Get current connected group ID
                var currentSession = await ChatService.Instance.GetCurrentSessionIdAsync();
                if (string.IsNullOrWhiteSpace(currentSession))
                {
                    Logger.Info("[MainForm] No current session, skipping nickname sync");
                    return;
                }
                
                // Extract team ID from session (format: team-XXXXXXXXX)
                var teamId = currentSession.StartsWith("team-") 
                    ? currentSession.Substring(5) 
                    : currentSession;
                
                Logger.Info($"[MainForm] Syncing member nicknames for team: {teamId}");
                
                // Get plaintext nicknames from WangShangLiao
                var nicknames = await ChatService.Instance.GetMemberPlaintextNicknamesAsync(teamId);
                
                if (nicknames == null || nicknames.Count == 0)
                {
                    Logger.Info("[MainForm] No plaintext nicknames found");
                    return;
                }
                
                Logger.Info($"[MainForm] Got {nicknames.Count} plaintext nicknames");
                
                // Update player data with plaintext nicknames
                int created = 0;
                int updated = 0;
                int skippedEmpty = 0;
                int skippedHash = 0;
                
                // Log first 5 nicknames for debugging
                int debugCount = 0;
                foreach (var kvp in nicknames)
                {
                    if (debugCount < 5)
                    {
                        Logger.Info($"[MainForm] DEBUG nickname: {kvp.Key} -> '{kvp.Value}' (len={kvp.Value?.Length ?? 0}, isMd5={IsMd5Hash(kvp.Value)})");
                        debugCount++;
                    }
                }
                
                foreach (var kvp in nicknames)
                {
                    var account = kvp.Key;
                    var nickname = kvp.Value;
                    
                    // Skip empty or whitespace nicknames
                    if (string.IsNullOrWhiteSpace(nickname))
                    {
                        skippedEmpty++;
                        continue;
                    }
                    
                    // Skip if nickname is MD5 hash (shouldn't happen, but double check)
                    if (IsMd5Hash(nickname))
                    {
                        skippedHash++;
                        continue;
                    }
                    
                    // Check if player exists
                    var existingPlayer = DataService.Instance.GetPlayer(account);
                    
                    if (existingPlayer == null)
                    {
                        // Create new player with decrypted nickname
                        var newPlayer = new Player
                        {
                            WangWangId = account,
                            Nickname = nickname,
                            Score = 0
                        };
                        DataService.Instance.SavePlayer(newPlayer);
                        created++;
                        Logger.Info($"[MainForm] Created player: {account} -> {nickname}");
                    }
                    else if (string.IsNullOrEmpty(existingPlayer.Nickname) || IsMd5Hash(existingPlayer.Nickname))
                    {
                        // Update existing player's nickname if current is empty or MD5 hash
                        existingPlayer.Nickname = nickname;
                        DataService.Instance.SavePlayer(existingPlayer);
                        updated++;
                        Logger.Info($"[MainForm] Updated nickname: {account} -> {nickname}");
                    }
                    else if (existingPlayer.Nickname != nickname)
                    {
                        // Also update if nickname changed (e.g., user changed their name)
                        var oldNick = existingPlayer.Nickname;
                        existingPlayer.Nickname = nickname;
                        DataService.Instance.SavePlayer(existingPlayer);
                        updated++;
                        Logger.Info($"[MainForm] Changed nickname: {account} [{oldNick}] -> [{nickname}]");
                    }
                }
                
                Logger.Info($"[MainForm] Created {created} players, updated {updated} nicknames (skippedEmpty={skippedEmpty}, skippedHash={skippedHash})");
                
                // Refresh player list on UI thread
                if (InvokeRequired)
                {
                    Invoke(new Action(() => 
                    {
                        _players = DataService.Instance.GetAllPlayers();
                        RefreshPlayerList();
                    }));
                }
                else
                {
                    _players = DataService.Instance.GetAllPlayers();
                    RefreshPlayerList();
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"[MainForm] Failed to sync member nicknames: {ex.Message}");
            }
        }

        private static bool IsMd5Hash(string value)
        {
            if (string.IsNullOrEmpty(value) || value.Length != 32)
                return false;
            
            foreach (char c in value)
            {
                if (!((c >= '0' && c <= '9') || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F')))
                    return false;
            }
            return true;
        }

    }
}
