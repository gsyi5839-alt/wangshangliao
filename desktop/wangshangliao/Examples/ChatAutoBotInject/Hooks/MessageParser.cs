// MessageParser.cs - 消息解析器
// 用于解析目标软件的消息协议

using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using ChatAutoBotInterface;

namespace ChatAutoBotInject.Hooks
{
    /// <summary>
    /// 消息解析器基类
    /// 针对不同的聊天软件，继承此类并实现具体的解析逻辑
    /// </summary>
    public abstract class MessageParserBase
    {
        /// <summary>
        /// 尝试解析原始数据为聊天消息
        /// </summary>
        /// <param name="rawData">原始字节数据</param>
        /// <returns>解析后的消息，无法解析返回null</returns>
        public abstract ChatMessage TryParse(byte[] rawData);

        /// <summary>
        /// 判断数据是否可能是聊天消息
        /// </summary>
        /// <param name="rawData">原始数据</param>
        /// <returns>是否可能是聊天消息</returns>
        public abstract bool IsLikelyMessage(byte[] rawData);
    }

    /// <summary>
    /// JSON格式消息解析器
    /// 适用于使用JSON协议的聊天软件
    /// </summary>
    public class JsonMessageParser : MessageParserBase
    {
        // 常见的消息字段名模式
        private static readonly string[] ContentFields = { "content", "msg", "text", "message", "body" };
        private static readonly string[] SenderFields = { "from", "sender", "from_user", "sender_id", "fromId" };
        private static readonly string[] ReceiverFields = { "to", "receiver", "to_user", "receiver_id", "toId" };
        private static readonly string[] TypeFields = { "type", "msg_type", "msgType", "message_type" };

        public override bool IsLikelyMessage(byte[] rawData)
        {
            try
            {
                string text = Encoding.UTF8.GetString(rawData);
                
                // 检查是否像JSON
                if (!text.TrimStart().StartsWith("{") && !text.TrimStart().StartsWith("["))
                    return false;

                // 检查是否包含消息相关字段
                foreach (var field in ContentFields)
                {
                    if (text.Contains($"\"{field}\""))
                        return true;
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        public override ChatMessage TryParse(byte[] rawData)
        {
            try
            {
                string json = Encoding.UTF8.GetString(rawData);
                
                // 简单的JSON解析（不依赖外部库）
                var msg = new ChatMessage
                {
                    MessageId = Guid.NewGuid().ToString(),
                    Timestamp = DateTime.Now,
                    RawData = rawData
                };

                // 提取内容
                foreach (var field in ContentFields)
                {
                    string value = ExtractJsonValue(json, field);
                    if (!string.IsNullOrEmpty(value))
                    {
                        msg.Content = value;
                        break;
                    }
                }

                // 提取发送者
                foreach (var field in SenderFields)
                {
                    string value = ExtractJsonValue(json, field);
                    if (!string.IsNullOrEmpty(value))
                    {
                        msg.SenderId = value;
                        msg.SenderName = value;
                        break;
                    }
                }

                // 提取接收者
                foreach (var field in ReceiverFields)
                {
                    string value = ExtractJsonValue(json, field);
                    if (!string.IsNullOrEmpty(value))
                    {
                        msg.ReceiverId = value;
                        msg.ReceiverName = value;
                        break;
                    }
                }

                // 提取类型
                foreach (var field in TypeFields)
                {
                    string value = ExtractJsonValue(json, field);
                    if (!string.IsNullOrEmpty(value))
                    {
                        msg.Type = ParseMessageType(value);
                        break;
                    }
                }

                // 如果没有解析到内容，返回null
                if (string.IsNullOrEmpty(msg.Content))
                    return null;

                return msg;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// 简单的JSON值提取（不依赖JSON库）
        /// </summary>
        private string ExtractJsonValue(string json, string key)
        {
            // 匹配 "key": "value" 或 "key": value 格式
            var pattern = $"\"{key}\"\\s*:\\s*\"?([^\"\\,\\}}\\]]+)\"?";
            var match = Regex.Match(json, pattern, RegexOptions.IgnoreCase);
            
            if (match.Success && match.Groups.Count > 1)
            {
                return match.Groups[1].Value.Trim();
            }
            
            return null;
        }

        /// <summary>
        /// 解析消息类型
        /// </summary>
        private MessageType ParseMessageType(string typeStr)
        {
            typeStr = typeStr.ToLower();
            
            if (typeStr.Contains("text") || typeStr == "1" || typeStr == "0")
                return MessageType.Text;
            if (typeStr.Contains("image") || typeStr.Contains("img") || typeStr == "2")
                return MessageType.Image;
            if (typeStr.Contains("voice") || typeStr.Contains("audio") || typeStr == "3")
                return MessageType.Voice;
            if (typeStr.Contains("file") || typeStr == "4")
                return MessageType.File;
            if (typeStr.Contains("system") || typeStr.Contains("notify"))
                return MessageType.System;
            
            return MessageType.Unknown;
        }
    }

    /// <summary>
    /// Protobuf格式消息解析器（需要根据具体软件的proto定义实现）
    /// </summary>
    public class ProtobufMessageParser : MessageParserBase
    {
        public override bool IsLikelyMessage(byte[] rawData)
        {
            // Protobuf消息通常以特定的varint字段开头
            // 需要根据具体软件分析
            if (rawData.Length < 5) return false;
            
            // 检查是否有Protobuf的典型模式
            // 字段1，wire type 2 (length-delimited) = 0x0A
            // 字段1，wire type 0 (varint) = 0x08
            byte firstByte = rawData[0];
            return (firstByte == 0x08 || firstByte == 0x0A || firstByte == 0x10 || firstByte == 0x12);
        }

        public override ChatMessage TryParse(byte[] rawData)
        {
            // TODO: 实现Protobuf解析
            // 需要根据目标软件的proto定义来实现
            // 可以使用protobuf-net库，或者手动解析
            
            return null;
        }
    }

    /// <summary>
    /// 自定义二进制格式解析器模板
    /// </summary>
    public class BinaryMessageParser : MessageParserBase
    {
        // 包头魔数（需要通过抓包分析确定）
        private byte[] _magicHeader = null;
        
        // 消息结构偏移量（需要通过逆向分析确定）
        private int _contentOffset = 0;
        private int _senderOffset = 0;
        private int _receiverOffset = 0;

        public void SetMagicHeader(byte[] magic)
        {
            _magicHeader = magic;
        }

        public void SetOffsets(int contentOffset, int senderOffset, int receiverOffset)
        {
            _contentOffset = contentOffset;
            _senderOffset = senderOffset;
            _receiverOffset = receiverOffset;
        }

        public override bool IsLikelyMessage(byte[] rawData)
        {
            if (_magicHeader == null || rawData.Length < _magicHeader.Length)
                return false;

            for (int i = 0; i < _magicHeader.Length; i++)
            {
                if (rawData[i] != _magicHeader[i])
                    return false;
            }

            return true;
        }

        public override ChatMessage TryParse(byte[] rawData)
        {
            if (!IsLikelyMessage(rawData))
                return null;

            // TODO: 根据分析的协议结构解析
            // 这里只是模板，需要根据实际协议实现

            return null;
        }
    }

    /// <summary>
    /// 消息解析管理器
    /// 自动尝试多种解析器
    /// </summary>
    public class MessageParserManager
    {
        private List<MessageParserBase> _parsers = new List<MessageParserBase>();
        
        public MessageParserManager()
        {
            // 添加默认解析器
            _parsers.Add(new JsonMessageParser());
            _parsers.Add(new ProtobufMessageParser());
        }

        public void AddParser(MessageParserBase parser)
        {
            _parsers.Insert(0, parser); // 自定义解析器优先
        }

        public ChatMessage TryParse(byte[] rawData)
        {
            foreach (var parser in _parsers)
            {
                if (parser.IsLikelyMessage(rawData))
                {
                    var msg = parser.TryParse(rawData);
                    if (msg != null)
                        return msg;
                }
            }
            return null;
        }
    }

    /// <summary>
    /// 数据包分析辅助类
    /// 用于在开发阶段分析未知协议
    /// </summary>
    public static class PacketAnalyzer
    {
        /// <summary>
        /// 生成数据包的十六进制和ASCII混合显示
        /// </summary>
        public static string HexDump(byte[] data, int maxLength = 512)
        {
            var sb = new StringBuilder();
            int length = Math.Min(data.Length, maxLength);
            
            for (int i = 0; i < length; i += 16)
            {
                // 地址
                sb.Append($"{i:X8}  ");
                
                // 十六进制
                for (int j = 0; j < 16; j++)
                {
                    if (i + j < length)
                        sb.Append($"{data[i + j]:X2} ");
                    else
                        sb.Append("   ");
                    
                    if (j == 7) sb.Append(" ");
                }
                
                sb.Append(" |");
                
                // ASCII
                for (int j = 0; j < 16 && i + j < length; j++)
                {
                    char c = (char)data[i + j];
                    sb.Append(c >= 32 && c < 127 ? c : '.');
                }
                
                sb.AppendLine("|");
            }

            if (data.Length > maxLength)
            {
                sb.AppendLine($"... ({data.Length - maxLength} more bytes)");
            }

            return sb.ToString();
        }

        /// <summary>
        /// 尝试识别数据格式
        /// </summary>
        public static string IdentifyFormat(byte[] data)
        {
            if (data.Length < 2) return "Too short";

            string text = null;
            try { text = Encoding.UTF8.GetString(data); } catch { }

            // JSON
            if (text != null && (text.TrimStart().StartsWith("{") || text.TrimStart().StartsWith("[")))
                return "JSON";

            // XML
            if (text != null && text.TrimStart().StartsWith("<"))
                return "XML";

            // HTTP
            if (text != null && (text.StartsWith("GET ") || text.StartsWith("POST ") || text.StartsWith("HTTP/")))
                return "HTTP";

            // Protobuf (猜测)
            if (data[0] == 0x08 || data[0] == 0x0A)
                return "Protobuf (guess)";

            // GZIP
            if (data[0] == 0x1F && data[1] == 0x8B)
                return "GZIP";

            // PNG
            if (data.Length > 8 && data[0] == 0x89 && data[1] == 0x50 && data[2] == 0x4E && data[3] == 0x47)
                return "PNG Image";

            // JPEG
            if (data.Length > 2 && data[0] == 0xFF && data[1] == 0xD8)
                return "JPEG Image";

            return "Unknown Binary";
        }

        /// <summary>
        /// 提取可打印的ASCII字符串
        /// </summary>
        public static List<string> ExtractStrings(byte[] data, int minLength = 4)
        {
            var result = new List<string>();
            var current = new StringBuilder();

            foreach (byte b in data)
            {
                if (b >= 32 && b < 127)
                {
                    current.Append((char)b);
                }
                else
                {
                    if (current.Length >= minLength)
                    {
                        result.Add(current.ToString());
                    }
                    current.Clear();
                }
            }

            if (current.Length >= minLength)
            {
                result.Add(current.ToString());
            }

            return result;
        }
    }
}

