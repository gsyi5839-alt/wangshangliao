using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;

namespace WangShangLiaoBot.Services
{
    /// <summary>
    /// Image generator service for bill and lottery image generation
    /// 图片生成服务 - 用于账单和开奖图片生成
    /// </summary>
    public sealed class ImageGeneratorService
    {
        private static ImageGeneratorService _instance;
        public static ImageGeneratorService Instance => _instance ?? (_instance = new ImageGeneratorService());
        
        private ImageGeneratorService() { }
        
        /// <summary>
        /// Generate bill image from text content
        /// 从文本内容生成账单图片
        /// </summary>
        public string GenerateBillImage(string content, string title = "账单")
        {
            try
            {
                var outputPath = GetTempImagePath("bill");
                RenderTextToImage(content, title, outputPath, Color.White, Color.FromArgb(30, 30, 30));
                return outputPath;
            }
            catch (Exception ex)
            {
                Logger.Error($"[ImageGenerator] GenerateBillImage error: {ex.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// Generate bet data image from text content
        /// 从下注数据内容生成图片
        /// </summary>
        public string GenerateBetDataImage(string period, string content)
        {
            try
            {
                var outputPath = GetTempImagePath("betdata");
                var title = $"第 {period} 期下注数据";
                RenderTextToImage(content, title, outputPath, 
                    Color.FromArgb(240, 248, 255), Color.FromArgb(25, 25, 112));
                return outputPath;
            }
            catch (Exception ex)
            {
                Logger.Error($"[ImageGenerator] GenerateBetDataImage error: {ex.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// Generate lottery result image
        /// 生成开奖结果图片
        /// </summary>
        public string GenerateLotteryImage(string period, int num1, int num2, int num3, int sum)
        {
            try
            {
                var outputPath = GetTempImagePath("lottery");
                
                var bs = sum >= 14 ? "大" : "小";
                var ds = sum % 2 == 0 ? "双" : "单";
                
                var content = $"第 {period} 期\n\n" +
                              $"  {num1}  +  {num2}  +  {num3}  =  {sum}\n\n" +
                              $"[ {bs}{ds} ]";
                
                RenderTextToImage(content, "开奖结果", outputPath, 
                    Color.FromArgb(255, 245, 230), Color.FromArgb(139, 69, 19));
                
                return outputPath;
            }
            catch (Exception ex)
            {
                Logger.Error($"[ImageGenerator] GenerateLotteryImage error: {ex.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// Render text content to image file
        /// 将文本内容渲染为图片文件
        /// </summary>
        private void RenderTextToImage(string content, string title, string outputPath, 
            Color bgColor, Color textColor)
        {
            // Calculate image size based on content
            var lines = content.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            var lineCount = Math.Max(lines.Length, 3);
            var maxLineLength = 0;
            foreach (var line in lines)
            {
                if (line.Length > maxLineLength) maxLineLength = line.Length;
            }
            
            var fontFamily = "Microsoft YaHei";
            var fontSize = 14f;
            var titleFontSize = 16f;
            var padding = 20;
            var lineHeight = (int)(fontSize * 1.8);
            var titleHeight = (int)(titleFontSize * 2);
            
            var width = Math.Max(maxLineLength * (int)(fontSize * 0.8), 200) + padding * 2;
            var height = titleHeight + lineCount * lineHeight + padding * 2 + 20;
            
            // Limit size
            width = Math.Min(width, 800);
            height = Math.Min(height, 1200);
            
            using (var bmp = new Bitmap(width, height))
            using (var g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
                
                // Fill background
                g.Clear(bgColor);
                
                // Draw border
                using (var borderPen = new Pen(textColor, 2))
                {
                    g.DrawRectangle(borderPen, 1, 1, width - 3, height - 3);
                }
                
                // Draw title
                using (var titleFont = new Font(fontFamily, titleFontSize, FontStyle.Bold))
                using (var titleBrush = new SolidBrush(textColor))
                {
                    var titleSize = g.MeasureString(title, titleFont);
                    var titleX = (width - titleSize.Width) / 2;
                    g.DrawString(title, titleFont, titleBrush, titleX, padding);
                    
                    // Draw title underline
                    using (var linePen = new Pen(textColor, 1))
                    {
                        g.DrawLine(linePen, padding, padding + titleHeight, width - padding, padding + titleHeight);
                    }
                }
                
                // Draw content
                using (var font = new Font(fontFamily, fontSize))
                using (var brush = new SolidBrush(textColor))
                {
                    var y = padding + titleHeight + 10;
                    foreach (var line in lines)
                    {
                        g.DrawString(line, font, brush, padding, y);
                        y += lineHeight;
                        
                        if (y > height - padding) break;
                    }
                }
                
                // Save image
                var dir = Path.GetDirectoryName(outputPath);
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                
                bmp.Save(outputPath, ImageFormat.Png);
            }
        }
        
        /// <summary>
        /// Get temporary image file path
        /// </summary>
        private string GetTempImagePath(string prefix)
        {
            var tempDir = Path.Combine(DataService.Instance.DatabaseDir, "TempImages");
            Directory.CreateDirectory(tempDir);
            
            // Clean up old temp images (older than 1 hour)
            try
            {
                foreach (var file in Directory.GetFiles(tempDir, "*.png"))
                {
                    var fi = new FileInfo(file);
                    if (fi.LastWriteTime < DateTime.Now.AddHours(-1))
                    {
                        try { fi.Delete(); } catch { }
                    }
                }
            }
            catch { }
            
            return Path.Combine(tempDir, $"{prefix}_{DateTime.Now:yyyyMMdd_HHmmss}_{Guid.NewGuid():N}.png");
        }
        
        /// <summary>
        /// Convert image file to Base64 for potential web display
        /// </summary>
        public string ImageToBase64(string imagePath)
        {
            if (!File.Exists(imagePath)) return null;
            
            try
            {
                var bytes = File.ReadAllBytes(imagePath);
                return Convert.ToBase64String(bytes);
            }
            catch (Exception ex)
            {
                Logger.Error($"[ImageGenerator] ImageToBase64 error: {ex.Message}");
                return null;
            }
        }
    }
}

