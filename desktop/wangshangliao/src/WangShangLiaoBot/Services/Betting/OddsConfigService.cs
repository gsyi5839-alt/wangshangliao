using System;
using System.Globalization;
using System.IO;
using System.Text;
using WangShangLiaoBot.Models.Betting;

namespace WangShangLiaoBot.Services.Betting
{
    /// <summary>
    /// Persist odds configs without expanding AppConfig (keeps files small and avoids touching old large files).
    /// Stored as a simple INI-like file in DataService.DatabaseDir.
    /// </summary>
    public sealed class OddsConfigService
    {
        private static OddsConfigService _instance;
        public static OddsConfigService Instance => _instance ?? (_instance = new OddsConfigService());

        private OddsConfigService() { }

        private string ClassicOddsPath
            => Path.Combine(DataService.Instance.DatabaseDir, "odds-classic.ini");

        /// <summary>
        /// Load classic odds. Returns null if not configured.
        /// </summary>
        public ClassicOddsConfig LoadClassicOdds()
        {
            try
            {
                if (!File.Exists(ClassicOddsPath)) return null;
                var lines = File.ReadAllLines(ClassicOddsPath, Encoding.UTF8);
                var cfg = new ClassicOddsConfig();

                foreach (var line in lines)
                {
                    if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#")) continue;
                    var idx = line.IndexOf('=');
                    if (idx <= 0) continue;
                    var k = line.Substring(0, idx).Trim();
                    var v = line.Substring(idx + 1).Trim();
                    if (!decimal.TryParse(v, NumberStyles.Any, CultureInfo.InvariantCulture, out var d))
                        decimal.TryParse(v, out d);

                    if (string.Equals(k, "DxdsOdds", StringComparison.OrdinalIgnoreCase)) cfg.DxdsOdds = d;
                    if (string.Equals(k, "BigOddSmallEvenOdds", StringComparison.OrdinalIgnoreCase)) cfg.BigOddSmallEvenOdds = d;
                    if (string.Equals(k, "BigEvenSmallOddOdds", StringComparison.OrdinalIgnoreCase)) cfg.BigEvenSmallOddOdds = d;
                }

                // If still all zero, treat as not configured
                if (cfg.DxdsOdds == 0m && cfg.BigOddSmallEvenOdds == 0m && cfg.BigEvenSmallOddOdds == 0m)
                    return null;

                return cfg;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Save classic odds.
        /// </summary>
        public void SaveClassicOdds(ClassicOddsConfig cfg)
        {
            if (cfg == null) return;
            try
            {
                Directory.CreateDirectory(DataService.Instance.DatabaseDir);
                var sb = new StringBuilder();
                sb.AppendLine("# Auto generated - classic odds");
                sb.AppendLine("DxdsOdds=" + cfg.DxdsOdds.ToString(CultureInfo.InvariantCulture));
                sb.AppendLine("BigOddSmallEvenOdds=" + cfg.BigOddSmallEvenOdds.ToString(CultureInfo.InvariantCulture));
                sb.AppendLine("BigEvenSmallOddOdds=" + cfg.BigEvenSmallOddOdds.ToString(CultureInfo.InvariantCulture));
                File.WriteAllText(ClassicOddsPath, sb.ToString(), Encoding.UTF8);
            }
            catch
            {
                // ignore
            }
        }
    }
}


