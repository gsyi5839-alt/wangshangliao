using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WangShangLiaoBot.Models;

namespace WangShangLiaoBot.Services
{
    /// <summary>
    /// Client portal API for the desktop bot:
    /// - Login / Register / Recharge / Change password
    /// - Announcement / Versions / Settings
    /// </summary>
    internal sealed class ClientPortalService
    {
        private static ClientPortalService _instance;
        public static ClientPortalService Instance => _instance ?? (_instance = new ClientPortalService());

        private HttpJsonClient CreateClient()
        {
            var cfg = ConfigService.Instance.Config;
            var baseUrl = (cfg?.ApiBaseUrl ?? "https://bocail.com/api").Trim();
            return new HttpJsonClient(baseUrl);
        }

        public async Task<ClientLoginResult> LoginAsync(string username, string password)
        {
            var client = CreateClient();
            return await client.PostDataAsync<ClientLoginResult>("client/login", new { username, password }).ConfigureAwait(false);
        }

        public async Task<ClientLoginResult> RegisterAsync(string username, string password, string superPassword, string cardCode, string boundInfo = null, string promoterUsername = null)
        {
            var client = CreateClient();
            // 新API: cardCode 直接是18位纯数字卡密
            return await client.PostDataAsync<ClientLoginResult>("client/register", new
            {
                username,
                password,
                superPassword,
                cardCode = (cardCode ?? "").Trim(),
                boundInfo,
                promoterUsername
            }).ConfigureAwait(false);
        }

        public async Task<RechargeResult> RechargeAsync(string username, string cardCode)
        {
            var client = CreateClient();
            // 新API: cardCode 直接是18位纯数字卡密
            return await client.PostDataAsync<RechargeResult>(
                "client/recharge",
                new { username, cardCode = (cardCode ?? "").Trim() }
            ).ConfigureAwait(false);
        }

        public async Task ChangePasswordAsync(string username, string superPassword, string newPassword)
        {
            var client = CreateClient();
            await client.PostDataAsync<object>("client/change-password", new { username, superPassword, newPassword }).ConfigureAwait(false);
        }

        public async Task<Announcement> GetAnnouncementAsync()
        {
            var client = CreateClient();
            return await client.GetDataAsync<Announcement>("client/announcement").ConfigureAwait(false);
        }

        public async Task<List<AppVersion>> GetVersionsAsync(int limit = 50)
        {
            var client = CreateClient();
            return await client.GetDataAsync<List<AppVersion>>($"client/versions?limit={limit}").ConfigureAwait(false);
        }

        public async Task<Dictionary<string, string>> GetPublicSettingsAsync()
        {
            var client = CreateClient();
            return await client.GetDataAsync<Dictionary<string, string>>("client/settings").ConfigureAwait(false);
        }

        public async Task<Dictionary<string, string>> GetPrivateSettingsAsync(string clientToken)
        {
            var client = CreateClient();
            return await client.GetDataAsync<Dictionary<string, string>>("client/settings-private", clientToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Get enabled lottery API configurations from server
        /// </summary>
        public async Task<List<LotteryApiConfig>> GetLotteryApisAsync()
        {
            var client = CreateClient();
            return await client.GetDataAsync<List<LotteryApiConfig>>("client/lottery-apis").ConfigureAwait(false);
        }

    }

    /// <summary>
    /// Lottery API configuration from server
    /// </summary>
    internal sealed class LotteryApiConfig
    {
        public long id { get; set; }
        public string name { get; set; }
        public string code { get; set; }
        public string token { get; set; }
        public string api_url { get; set; }
        public string backup_url { get; set; }
        public string format_type { get; set; }
        public string callback_name { get; set; }
        public int rows_count { get; set; }
        public int request_interval { get; set; }
        public int max_requests_per_30s { get; set; }
    }

    internal sealed class ClientLoginResult
    {
        public string token { get; set; }
        public string expireAt { get; set; }
        public int? daysLeft { get; set; }
    }

    internal sealed class RechargeResult
    {
        public string expireAt { get; set; }
        public int? daysLeft { get; set; }
    }

    internal sealed class Announcement
    {
        public long id { get; set; }
        public string title { get; set; }
        public string content { get; set; }
        public string created_at { get; set; }
    }

    internal sealed class AppVersion
    {
        public string version { get; set; }
        public string content { get; set; }
        public string created_at { get; set; }
    }
}


