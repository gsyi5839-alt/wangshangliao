using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace WangShangLiaoBot.Services
{
    /// <summary>
    /// Minimal JSON HTTP client for calling bocail.com backend.
    /// Uses JavaScriptSerializer to avoid external dependencies.
    /// </summary>
    internal sealed class HttpJsonClient
    {
        private readonly string _baseUrl;
        private readonly JavaScriptSerializer _json = new JavaScriptSerializer();

        public HttpJsonClient(string baseUrl)
        {
            _baseUrl = (baseUrl ?? "").TrimEnd('/');
        }

        public async Task<T> GetDataAsync<T>(string path, string bearerToken = null)
        {
            using (var http = new HttpClient())
            {
                http.Timeout = TimeSpan.FromSeconds(15);
                if (!string.IsNullOrWhiteSpace(bearerToken))
                    http.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", bearerToken);

                var url = $"{_baseUrl}/{path.TrimStart('/')}";
                var text = await http.GetStringAsync(url).ConfigureAwait(false);
                return ParseEnvelope<T>(text);
            }
        }

        public async Task<T> PostDataAsync<T>(string path, object body, string bearerToken = null)
        {
            using (var http = new HttpClient())
            {
                http.Timeout = TimeSpan.FromSeconds(15);
                if (!string.IsNullOrWhiteSpace(bearerToken))
                    http.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", bearerToken);

                var url = $"{_baseUrl}/{path.TrimStart('/')}";
                var payload = _json.Serialize(body ?? new { });
                var resp = await http.PostAsync(url, new StringContent(payload, Encoding.UTF8, "application/json")).ConfigureAwait(false);
                var text = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
                if (!resp.IsSuccessStatusCode)
                    throw new Exception($"HTTP {(int)resp.StatusCode}: {ExtractMessage(text)}");

                return ParseEnvelope<T>(text);
            }
        }

        private T ParseEnvelope<T>(string jsonText)
        {
            if (string.IsNullOrWhiteSpace(jsonText))
                throw new Exception("Empty response");

            // Expect: { ok: true, data: ... } or { ok: false, error: { message } }
            var dict = _json.DeserializeObject(jsonText) as System.Collections.Generic.Dictionary<string, object>;
            if (dict == null)
                throw new Exception("Invalid response");

            var ok = dict.ContainsKey("ok") && dict["ok"] is bool b && b;
            if (!ok)
                throw new Exception(ExtractMessage(jsonText));

            if (!dict.ContainsKey("data"))
                return default(T);

            // Re-serialize the data node then deserialize into T.
            var dataJson = _json.Serialize(dict["data"]);
            return _json.Deserialize<T>(dataJson);
        }

        private string ExtractMessage(string jsonText)
        {
            try
            {
                var dict = _json.DeserializeObject(jsonText) as System.Collections.Generic.Dictionary<string, object>;
                if (dict == null) return "Request failed";
                if (dict.ContainsKey("error") && dict["error"] is System.Collections.Generic.Dictionary<string, object> err)
                {
                    if (err.ContainsKey("message")) return Convert.ToString(err["message"]);
                }
                return "Request failed";
            }
            catch
            {
                return "Request failed";
            }
        }
    }
}


