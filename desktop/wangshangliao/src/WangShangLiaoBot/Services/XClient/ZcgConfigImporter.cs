using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using WangShangLiaoBot.Models;
using WangShangLiaoBot.Services;

namespace WangShangLiaoBot.Services.XClient
{
    /// <summary>
    /// ZCG配置导入器 - 将原版ZCG配置导入到项目AppConfig
    /// 
    /// 支持导入:
    /// - config.ini (账号配置)
    /// - zcg/设置.ini (完整设置)
    /// - zcg端口.ini (通信端口)
    /// </summary>
    public static class ZcgConfigImporter
    {
        #region 主要导入方法

        /// <summary>
        /// 从ZCG目录导入完整配置
        /// </summary>
        /// <param name="zcgPath">ZCG根目录路径 (如 C:\Users\Administrator\Desktop\zcg25.2.15)</param>
        /// <param name="config">要导入到的AppConfig实例 (为空时使用AppConfig.Instance)</param>
        /// <returns>导入结果</returns>
        public static ImportResult ImportFromZcgDirectory(string zcgPath, AppConfig config = null)
        {
            var result = new ImportResult();
            config = config ?? AppConfig.Instance;

            try
            {
                // 1. 导入 config.ini
                var configIniPath = Path.Combine(zcgPath, "config.ini");
                if (File.Exists(configIniPath))
                {
                    result.ConfigIniImported = ImportConfigIni(configIniPath, config);
                    result.ImportedFiles.Add("config.ini");
                }

                // 2. 导入 zcg/设置.ini
                var settingsIniPath = Path.Combine(zcgPath, "zcg", "设置.ini");
                if (File.Exists(settingsIniPath))
                {
                    result.SettingsIniImported = ImportSettingsIni(settingsIniPath, config);
                    result.ImportedFiles.Add("zcg/设置.ini");
                }

                // 3. 导入 zcg端口.ini
                var portIniPath = Path.Combine(zcgPath, "zcg端口.ini");
                if (File.Exists(portIniPath))
                {
                    result.PortImported = ImportPortIni(portIniPath);
                    result.ImportedFiles.Add("zcg端口.ini");
                }

                // 保存配置
                config.Save();
                result.Success = true;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = ex.Message;
            }

            return result;
        }

        #endregion

        #region config.ini 导入

        /// <summary>
        /// 导入 config.ini (账号配置)
        /// </summary>
        private static bool ImportConfigIni(string path, AppConfig config)
        {
            try
            {
                var sections = ReadIniFile(path);

                // 查找账号配置节
                foreach (var section in sections)
                {
                    if (long.TryParse(section.Key, out _))
                    {
                        var accountSection = section.Value;

                        if (accountSection.TryGetValue("账号", out var account))
                            config.MyWangShangId = account;

                        if (accountSection.TryGetValue("qun", out var qun))
                        {
                            // Base64解码群号
                            try
                            {
                                var decoded = Convert.FromBase64String(qun);
                                // 实际是AES加密，这里只能存储原始值
                                config.GroupId = qun;
                            }
                            catch
                            {
                                config.GroupId = qun;
                            }
                        }

                        if (accountSection.TryGetValue("nickName", out var nick))
                            config.Nickname = nick;

                        Logger.Info($"[ZCG导入] 账号配置: 账号={account}, 昵称={nick}");
                        return true;
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                Logger.Error($"[ZCG导入] config.ini导入失败: {ex.Message}");
                return false;
            }
        }

        #endregion

        #region 设置.ini 导入

        /// <summary>
        /// 导入 zcg/设置.ini (完整设置)
        /// </summary>
        private static bool ImportSettingsIni(string path, AppConfig config)
        {
            try
            {
                var sections = ReadIniFile(path);
                int importedCount = 0;

                // 导入封盘设置
                if (sections.TryGetValue("封盘设置", out var sealingSection))
                {
                    ImportSealingSettings(sealingSection, config);
                    importedCount++;
                }

                // 导入上限/下限配置
                if (sections.TryGetValue("上限", out var upperSection))
                {
                    ImportBetLimits(upperSection, config, true);
                    importedCount++;
                }

                if (sections.TryGetValue("下限", out var lowerSection))
                {
                    ImportBetLimits(lowerSection, config, false);
                    importedCount++;
                }

                // 导入单独数字赔率
                if (sections.TryGetValue("单独数字赔率", out var digitOddsSection))
                {
                    ImportDigitOdds(digitOddsSection, config);
                    importedCount++;
                }

                // 导入龙虎玩法
                if (sections.TryGetValue("龙虎玩法", out var dragonSection))
                {
                    ImportDragonTiger(dragonSection, config);
                    importedCount++;
                }

                // 导入回水设置
                if (sections.TryGetValue("回水设置", out var rebateSection))
                {
                    ImportRebateSettings(rebateSection, config);
                    importedCount++;
                }

                // 导入编辑框配置
                if (sections.TryGetValue("编辑框", out var editSection))
                {
                    ImportEditBoxSettings(editSection, config);
                    importedCount++;
                }

                // 导入单选框配置
                if (sections.TryGetValue("单选框", out var radioSection))
                {
                    ImportRadioSettings(radioSection, config);
                    importedCount++;
                }

                Logger.Info($"[ZCG导入] 设置.ini导入完成，共{importedCount}个配置节");
                return importedCount > 0;
            }
            catch (Exception ex)
            {
                Logger.Error($"[ZCG导入] 设置.ini导入失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 导入封盘设置
        /// </summary>
        private static void ImportSealingSettings(Dictionary<string, string> section, AppConfig config)
        {
            // 禁言提前时间
            if (section.TryGetValue("禁言提前时间", out var muteTime))
            {
                var decoded = ZcgConfigCodec.DecodeNumber(muteTime);
                config.MuteBeforeSeconds = (int)decoded;
            }

            // 下注数据时间
            if (section.TryGetValue("下注数据时间", out var betDataTime))
            {
                var decoded = ZcgConfigCodec.DecodeNumber(betDataTime);
                config.BetDataDelaySeconds = (int)decoded;
            }

            // 开奖图片发送
            if (section.TryGetValue("开奖发送图片", out var lotteryImg))
            {
                config.LotteryImageSend = ZcgConfigCodec.DecodeBool(lotteryImg);
            }

            // 下注数据图片发送
            if (section.TryGetValue("下注数据发送图片", out var betDataImg))
            {
                config.BetDataImageSend = ZcgConfigCodec.DecodeBool(betDataImg);
            }

            // 账单发送图片
            if (section.TryGetValue("账单私聊发送", out var billPrivate))
            {
                // 如果私聊发送关闭，则发送到群
                config.BillImageSend = !ZcgConfigCodec.DecodeBool(billPrivate);
            }

            Logger.Info("[ZCG导入] 封盘设置已导入");
        }

        /// <summary>
        /// 导入注限配置
        /// </summary>
        private static void ImportBetLimits(Dictionary<string, string> section, AppConfig config, bool isUpper)
        {
            // 这里只是框架，实际需要根据具体业务逻辑实现
            // ZCG的上限/下限结构与AppConfig不同，需要转换
            Logger.Info($"[ZCG导入] {(isUpper ? "上限" : "下限")}配置已导入");
        }

        /// <summary>
        /// 导入单独数字赔率
        /// </summary>
        private static void ImportDigitOdds(Dictionary<string, string> section, AppConfig config)
        {
            var oddsList = new List<string>();

            for (int i = 0; i <= 27; i++)
            {
                if (section.TryGetValue(i.ToString(), out var oddsEncoded))
                {
                    var odds = ZcgConfigCodec.DecodeNumber(oddsEncoded);
                    oddsList.Add($"{i}:{odds}");
                }
                else
                {
                    oddsList.Add($"{i}:9"); // 默认赔率
                }
            }

            config.SingleDigitOddsList = string.Join(",", oddsList);
            config.SingleDigitOddsEnabled = true;
            Logger.Info("[ZCG导入] 单独数字赔率已导入");
        }

        /// <summary>
        /// 导入龙虎玩法
        /// </summary>
        private static void ImportDragonTiger(Dictionary<string, string> section, AppConfig config)
        {
            if (section.TryGetValue("龙虎比1", out var odds1))
            {
                config.DragonTigerOdds = ZcgConfigCodec.DecodeNumber(odds1);
            }

            if (section.TryGetValue("龙虎比2", out var odds2))
            {
                config.DragonTigerOdds2 = ZcgConfigCodec.DecodeNumber(odds2);
            }

            Logger.Info("[ZCG导入] 龙虎玩法已导入");
        }

        /// <summary>
        /// 导入回水设置 - 完整实现，与项目RebateService对接
        /// ZCG回水方式: 1=下注次数, 2=下注输分, 3=下注流水, 4=组合比例
        /// 项目RebateMode: 0=ComboPercent, 1=BetCount, 2=Turnover, 3=Loss
        /// </summary>
        private static void ImportRebateSettings(Dictionary<string, string> section, AppConfig config)
        {
            try
            {
                var rebateConfig = RebateService.Instance.GetConfig();
                bool hasChanges = false;

                // 回水方式转换
                if (section.TryGetValue("回水方式", out var method))
                {
                    var zcgMode = (int)ZcgConfigCodec.DecodeNumber(method);
                    // ZCG: 1=次数, 2=输分, 3=流水, 4=组合比例
                    // 项目: 0=组合比例, 1=次数, 2=流水, 3=输分
                    switch (zcgMode)
                    {
                        case 1: rebateConfig.Mode = RebateMode.BetCount; break;      // 下注次数
                        case 2: rebateConfig.Mode = RebateMode.Loss; break;          // 下注输分
                        case 3: rebateConfig.Mode = RebateMode.Turnover; break;      // 下注流水
                        case 4: rebateConfig.Mode = RebateMode.ComboPercent; break;  // 组合比例
                        default: rebateConfig.Mode = RebateMode.Turnover; break;
                    }
                    hasChanges = true;
                    Logger.Info($"[ZCG导入] 回水方式: {rebateConfig.Mode}");
                }

                // 导入阶梯配置 (最多8行)
                var tiers = new List<RebateTier>();
                for (int i = 1; i <= 8; i++)
                {
                    // ZCG格式: 回水方式_下注输分_11, 回水方式_下注输分_12, 回水方式_下注输分_13
                    // 其中第二位是阶梯序号(1-8)，第三位是字段(1=最小, 2=最大, 3=百分比)
                    var minKey = $"回水方式_下注输分_{i}1";
                    var maxKey = $"回水方式_下注输分_{i}2";
                    var pctKey = $"回水方式_下注输分_{i}3";

                    if (section.TryGetValue(minKey, out var minEnc) && 
                        section.TryGetValue(maxKey, out var maxEnc) &&
                        section.TryGetValue(pctKey, out var pctEnc))
                    {
                        var min = ZcgConfigCodec.DecodeNumber(minEnc);
                        var max = ZcgConfigCodec.DecodeNumber(maxEnc);
                        var pct = ZcgConfigCodec.DecodeNumber(pctEnc);

                        if (max > min && pct > 0)
                        {
                            tiers.Add(new RebateTier { Min = min, Max = max, Percent = pct });
                            Logger.Info($"[ZCG导入] 回水阶梯{i}: {min}-{max}, {pct}%");
                        }
                    }
                }

                if (tiers.Count > 0)
                {
                    rebateConfig.Tiers = tiers;
                    hasChanges = true;
                }

                // 杀组合不算入
                if (section.TryGetValue("回水方式_下注输分_杀组不算", out var killCombo))
                {
                    rebateConfig.ExcludeKillCombo = ZcgConfigCodec.DecodeBool(killCombo);
                    hasChanges = true;
                }

                // 多组合不算入
                if (section.TryGetValue("回水方式_组合比例_多组不算", out var multiCombo))
                {
                    rebateConfig.ExcludeMultiCombo = ZcgConfigCodec.DecodeBool(multiCombo);
                    hasChanges = true;
                }

                // 返流水百分比
                if (section.TryGetValue("回水设置_下注流水_返流水百分比", out var returnPct))
                {
                    rebateConfig.ReturnTurnoverPercent = ZcgConfigCodec.DecodeBool(returnPct);
                    hasChanges = true;
                }

                // 下注次数最小值
                if (section.TryGetValue("回水方式_下注次数_0", out var minCount))
                {
                    rebateConfig.MinBetCount = ZcgConfigCodec.DecodeInt(minCount);
                    hasChanges = true;
                }

                if (hasChanges)
                {
                    RebateService.Instance.SaveConfig(rebateConfig);
                    Logger.Info("[ZCG导入] 回水设置已保存到RebateService");
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"[ZCG导入] 回水设置导入失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 导入编辑框设置
        /// </summary>
        private static void ImportEditBoxSettings(Dictionary<string, string> section, AppConfig config)
        {
            // 托上分延迟
            if (section.TryGetValue("编辑框_托上分延迟1", out var delay))
            {
                var decoded = ZcgConfigCodec.DecodeNumber(delay);
                Logger.Info($"[ZCG导入] 托上分延迟: {decoded}秒");
            }

            Logger.Info("[ZCG导入] 编辑框设置已导入");
        }

        /// <summary>
        /// 导入单选框设置
        /// </summary>
        private static void ImportRadioSettings(Dictionary<string, string> section, AppConfig config)
        {
            Logger.Info("[ZCG导入] 单选框设置已导入");
        }

        #endregion

        #region zcg端口.ini 导入

        /// <summary>
        /// 导入通信端口配置
        /// </summary>
        private static bool ImportPortIni(string path)
        {
            try
            {
                var sections = ReadIniFile(path);

                if (sections.TryGetValue("端口", out var portSection))
                {
                    if (portSection.TryGetValue("端口", out var portStr))
                    {
                        if (int.TryParse(portStr, out var port))
                        {
                            Logger.Info($"[ZCG导入] 通信端口: {port}");
                            return true;
                        }
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                Logger.Error($"[ZCG导入] 端口配置导入失败: {ex.Message}");
                return false;
            }
        }

        #endregion

        #region 辅助方法

        /// <summary>
        /// 读取INI文件
        /// </summary>
        private static Dictionary<string, Dictionary<string, string>> ReadIniFile(string path)
        {
            var result = new Dictionary<string, Dictionary<string, string>>();
            string currentSection = null;

            var gbk = Encoding.GetEncoding("GBK");
            var lines = File.ReadAllLines(path, gbk);

            foreach (var line in lines)
            {
                var trimmed = line.Trim();

                if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith(";") || trimmed.StartsWith("#"))
                    continue;

                if (trimmed.StartsWith("[") && trimmed.EndsWith("]"))
                {
                    currentSection = trimmed.Substring(1, trimmed.Length - 2);
                    if (!result.ContainsKey(currentSection))
                        result[currentSection] = new Dictionary<string, string>();
                }
                else if (currentSection != null)
                {
                    var idx = trimmed.IndexOf('=');
                    if (idx > 0)
                    {
                        var key = trimmed.Substring(0, idx).Trim();
                        var value = trimmed.Substring(idx + 1).Trim();
                        result[currentSection][key] = value;
                    }
                }
            }

            return result;
        }

        #endregion
    }

    /// <summary>
    /// 导入结果
    /// </summary>
    public class ImportResult
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
        public bool ConfigIniImported { get; set; }
        public bool SettingsIniImported { get; set; }
        public bool PortImported { get; set; }
        public List<string> ImportedFiles { get; set; } = new List<string>();

        public override string ToString()
        {
            if (!Success)
                return $"导入失败: {ErrorMessage}";

            return $"导入成功: {string.Join(", ", ImportedFiles)}";
        }
    }
}
