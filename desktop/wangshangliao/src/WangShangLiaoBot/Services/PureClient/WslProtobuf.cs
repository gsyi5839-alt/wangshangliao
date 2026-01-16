using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace WangShangLiaoBot.Services.PureClient
{
    /// <summary>
    /// 旺商聊Protobuf消息编解码
    /// 基于逆向分析的api.common.Message实现
    /// </summary>
    public static class WslProtobuf
    {
        #region Protobuf Wire Types

        private const int WIRE_VARINT = 0;
        private const int WIRE_FIXED64 = 1;
        private const int WIRE_LENGTH_DELIMITED = 2;
        private const int WIRE_FIXED32 = 5;

        #endregion

        #region Message Field Numbers (从逆向分析获取)

        // api.common.Message 字段
        public const int FIELD_SEND_TYPE = 1;
        public const int FIELD_FROM = 2;
        public const int FIELD_IS_OFFLINE = 3;
        public const int FIELD_TTL = 4;
        public const int FIELD_TARGET_OS = 5;
        public const int FIELD_BODY = 6;
        public const int FIELD_ENCRYPT = 7;
        public const int FIELD_LINE_VERSION_PARAM = 8;
        public const int FIELD_NIM_LIST = 9;
        public const int FIELD_CLIENT_CALLBACK = 10;

        // MessageContent 字段
        public const int FIELD_CONTENT_TEXT = 1;
        public const int FIELD_CONTENT_IMAGE = 2;
        public const int FIELD_CONTENT_VOICE = 3;
        public const int FIELD_CONTENT_VIDEO = 4;
        public const int FIELD_CONTENT_FILE = 5;

        #endregion

        /// <summary>
        /// 编码消息
        /// </summary>
        public static byte[] EncodeMessage(WslMessage message)
        {
            using (var ms = new MemoryStream())
            {
                // sendType (field 1, varint)
                WriteTag(ms, FIELD_SEND_TYPE, WIRE_VARINT);
                WriteVarint(ms, message.SendType);

                // from (field 2, varint)
                if (message.From != 0)
                {
                    WriteTag(ms, FIELD_FROM, WIRE_VARINT);
                    WriteVarint(ms, message.From);
                }

                // isOffline (field 3, varint)
                WriteTag(ms, FIELD_IS_OFFLINE, WIRE_VARINT);
                WriteVarint(ms, message.IsOffline ? 1 : 0);

                // ttl (field 4, varint)
                if (message.Ttl > 0)
                {
                    WriteTag(ms, FIELD_TTL, WIRE_VARINT);
                    WriteVarint(ms, message.Ttl);
                }

                // targetOs (field 5, length-delimited)
                if (!string.IsNullOrEmpty(message.TargetOs))
                {
                    WriteTag(ms, FIELD_TARGET_OS, WIRE_LENGTH_DELIMITED);
                    WriteString(ms, message.TargetOs);
                }

                // body (field 6, length-delimited)
                if (message.Body != null && message.Body.Length > 0)
                {
                    WriteTag(ms, FIELD_BODY, WIRE_LENGTH_DELIMITED);
                    WriteBytes(ms, message.Body);
                }

                // encrypt (field 7, varint)
                WriteTag(ms, FIELD_ENCRYPT, WIRE_VARINT);
                WriteVarint(ms, message.Encrypt ? 1 : 0);

                // nimList (field 9, repeated length-delimited)
                if (message.NimList != null)
                {
                    foreach (var nim in message.NimList)
                    {
                        WriteTag(ms, FIELD_NIM_LIST, WIRE_LENGTH_DELIMITED);
                        WriteString(ms, nim);
                    }
                }

                return ms.ToArray();
            }
        }

        /// <summary>
        /// 解码消息
        /// </summary>
        public static WslMessage DecodeMessage(byte[] data)
        {
            var message = new WslMessage();

            using (var ms = new MemoryStream(data))
            {
                while (ms.Position < ms.Length)
                {
                    var tag = ReadVarint(ms);
                    var fieldNumber = (int)(tag >> 3);
                    var wireType = (int)(tag & 0x7);

                    switch (fieldNumber)
                    {
                        case FIELD_SEND_TYPE:
                            message.SendType = (int)ReadVarint(ms);
                            break;
                        case FIELD_FROM:
                            message.From = ReadVarint(ms);
                            break;
                        case FIELD_IS_OFFLINE:
                            message.IsOffline = ReadVarint(ms) != 0;
                            break;
                        case FIELD_TTL:
                            message.Ttl = (int)ReadVarint(ms);
                            break;
                        case FIELD_TARGET_OS:
                            message.TargetOs = ReadString(ms);
                            break;
                        case FIELD_BODY:
                            message.Body = ReadBytes(ms);
                            break;
                        case FIELD_ENCRYPT:
                            message.Encrypt = ReadVarint(ms) != 0;
                            break;
                        case FIELD_NIM_LIST:
                            message.NimList = message.NimList ?? new List<string>();
                            message.NimList.Add(ReadString(ms));
                            break;
                        default:
                            // 跳过未知字段
                            SkipField(ms, wireType);
                            break;
                    }
                }
            }

            return message;
        }

        /// <summary>
        /// 编码文本消息内容
        /// </summary>
        public static byte[] EncodeTextContent(string text, string scene, string to)
        {
            using (var ms = new MemoryStream())
            {
                // scene (field 1)
                WriteTag(ms, 1, WIRE_LENGTH_DELIMITED);
                WriteString(ms, scene);

                // to (field 2)
                WriteTag(ms, 2, WIRE_LENGTH_DELIMITED);
                WriteString(ms, to);

                // text (field 3)
                WriteTag(ms, 3, WIRE_LENGTH_DELIMITED);
                WriteString(ms, text);

                return ms.ToArray();
            }
        }

        /// <summary>
        /// 编码API请求
        /// </summary>
        public static byte[] EncodeRequest(string url, string paramsJson)
        {
            using (var ms = new MemoryStream())
            {
                // url (field 1)
                WriteTag(ms, 1, WIRE_LENGTH_DELIMITED);
                WriteString(ms, url);

                // params (field 2)
                WriteTag(ms, 2, WIRE_LENGTH_DELIMITED);
                WriteString(ms, paramsJson);

                return ms.ToArray();
            }
        }

        #region Protobuf Encoding Helpers

        private static void WriteTag(Stream stream, int fieldNumber, int wireType)
        {
            WriteVarint(stream, (ulong)((fieldNumber << 3) | wireType));
        }

        private static void WriteVarint(Stream stream, ulong value)
        {
            while (value >= 0x80)
            {
                stream.WriteByte((byte)(value | 0x80));
                value >>= 7;
            }
            stream.WriteByte((byte)value);
        }

        private static void WriteVarint(Stream stream, long value)
        {
            WriteVarint(stream, (ulong)value);
        }

        private static void WriteVarint(Stream stream, int value)
        {
            WriteVarint(stream, (ulong)value);
        }

        private static void WriteString(Stream stream, string value)
        {
            var bytes = Encoding.UTF8.GetBytes(value);
            WriteVarint(stream, bytes.Length);
            stream.Write(bytes, 0, bytes.Length);
        }

        private static void WriteBytes(Stream stream, byte[] value)
        {
            WriteVarint(stream, value.Length);
            stream.Write(value, 0, value.Length);
        }

        #endregion

        #region Protobuf Decoding Helpers

        private static ulong ReadVarint(Stream stream)
        {
            ulong result = 0;
            int shift = 0;
            int b;

            while ((b = stream.ReadByte()) >= 0)
            {
                result |= (ulong)(b & 0x7F) << shift;
                if ((b & 0x80) == 0)
                    break;
                shift += 7;
            }

            return result;
        }

        private static string ReadString(Stream stream)
        {
            var length = (int)ReadVarint(stream);
            var bytes = new byte[length];
            stream.Read(bytes, 0, length);
            return Encoding.UTF8.GetString(bytes);
        }

        private static byte[] ReadBytes(Stream stream)
        {
            var length = (int)ReadVarint(stream);
            var bytes = new byte[length];
            stream.Read(bytes, 0, length);
            return bytes;
        }

        private static void SkipField(Stream stream, int wireType)
        {
            switch (wireType)
            {
                case WIRE_VARINT:
                    ReadVarint(stream);
                    break;
                case WIRE_FIXED64:
                    stream.Position += 8;
                    break;
                case WIRE_LENGTH_DELIMITED:
                    var length = (int)ReadVarint(stream);
                    stream.Position += length;
                    break;
                case WIRE_FIXED32:
                    stream.Position += 4;
                    break;
            }
        }

        #endregion
    }

    /// <summary>
    /// 旺商聊消息 (对应api.common.Message)
    /// </summary>
    public class WslMessage
    {
        public int SendType { get; set; }
        public long From { get; set; }
        public bool IsOffline { get; set; }
        public int Ttl { get; set; }
        public string TargetOs { get; set; }
        public byte[] Body { get; set; }
        public bool Encrypt { get; set; }
        public string LineVersionParam { get; set; }
        public List<string> NimList { get; set; }
        public string ClientCallback { get; set; }

        /// <summary>
        /// 创建文本消息
        /// </summary>
        public static WslMessage CreateTextMessage(string scene, string to, string text)
        {
            return new WslMessage
            {
                SendType = 0,
                Body = WslProtobuf.EncodeTextContent(text, scene, to),
                Encrypt = false,
                NimList = new List<string>()
            };
        }
    }
}
