using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using WSLFramework.Utils;

namespace WSLFramework.Services
{
    /// <summary>
    /// Protobuf 消息构建器 - 根据旺商聊深度连接协议第十六节实现
    /// 用于构建和解析 NIM SDK 使用的 Protobuf 格式消息
    /// </summary>
    public class ProtobufBuilder
    {
        #region 常量 - Wire Types

        /// <summary>Varint (int32, int64, uint32, uint64, sint32, sint64, bool, enum)</summary>
        public const int WIRE_TYPE_VARINT = 0;
        /// <summary>64-bit (fixed64, sfixed64, double)</summary>
        public const int WIRE_TYPE_64BIT = 1;
        /// <summary>Length-delimited (string, bytes, embedded messages, packed repeated fields)</summary>
        public const int WIRE_TYPE_LENGTH_DELIMITED = 2;
        /// <summary>32-bit (fixed32, sfixed32, float)</summary>
        public const int WIRE_TYPE_32BIT = 5;

        #endregion

        #region 常量 - Protobuf 头部

        /// <summary>
        /// 消息魔数 (前4字节)
        /// 根据文档: 09 1A 49 1F
        /// </summary>
        public static readonly byte[] MAGIC_NUMBER = { 0x09, 0x1A, 0x49, 0x1F };

        /// <summary>标准头部大小</summary>
        public const int HEADER_SIZE = 12;

        #endregion

        #region 单例模式

        private static readonly Lazy<ProtobufBuilder> _instance =
            new Lazy<ProtobufBuilder>(() => new ProtobufBuilder());

        public static ProtobufBuilder Instance => _instance.Value;

        #endregion

        #region Varint 编码/解码

        /// <summary>
        /// 编码 Varint
        /// </summary>
        public byte[] EncodeVarint(ulong value)
        {
            var result = new List<byte>();
            while (value > 127)
            {
                result.Add((byte)((value & 0x7F) | 0x80));
                value >>= 7;
            }
            result.Add((byte)value);
            return result.ToArray();
        }

        /// <summary>
        /// 编码有符号整数为 Varint (ZigZag 编码)
        /// </summary>
        public byte[] EncodeSVarint(long value)
        {
            // ZigZag 编码: (n << 1) ^ (n >> 63)
            var zigzag = (ulong)((value << 1) ^ (value >> 63));
            return EncodeVarint(zigzag);
        }

        /// <summary>
        /// 解码 Varint
        /// </summary>
        public ulong DecodeVarint(byte[] data, ref int offset)
        {
            ulong result = 0;
            int shift = 0;
            byte b;
            do
            {
                if (offset >= data.Length)
                    throw new InvalidOperationException("Unexpected end of data");
                b = data[offset++];
                result |= (ulong)(b & 0x7F) << shift;
                shift += 7;
            } while ((b & 0x80) != 0);
            return result;
        }

        #endregion

        #region 固定长度编码

        /// <summary>
        /// 编码 64 位固定长度整数 (little-endian)
        /// </summary>
        public byte[] EncodeFixed64(ulong value)
        {
            return BitConverter.GetBytes(value);
        }

        /// <summary>
        /// 编码 32 位固定长度整数 (little-endian)
        /// </summary>
        public byte[] EncodeFixed32(uint value)
        {
            return BitConverter.GetBytes(value);
        }

        /// <summary>
        /// 解码 64 位固定长度整数
        /// </summary>
        public ulong DecodeFixed64(byte[] data, ref int offset)
        {
            if (offset + 8 > data.Length)
                throw new InvalidOperationException("Unexpected end of data");
            var result = BitConverter.ToUInt64(data, offset);
            offset += 8;
            return result;
        }

        /// <summary>
        /// 解码 32 位固定长度整数
        /// </summary>
        public uint DecodeFixed32(byte[] data, ref int offset)
        {
            if (offset + 4 > data.Length)
                throw new InvalidOperationException("Unexpected end of data");
            var result = BitConverter.ToUInt32(data, offset);
            offset += 4;
            return result;
        }

        #endregion

        #region 字段编码

        /// <summary>
        /// 编码字段标签 (field number + wire type)
        /// </summary>
        public byte[] EncodeTag(int fieldNumber, int wireType)
        {
            var tag = (ulong)((fieldNumber << 3) | wireType);
            return EncodeVarint(tag);
        }

        /// <summary>
        /// 编码长度分隔字段
        /// </summary>
        public byte[] EncodeLengthDelimited(byte[] data)
        {
            using (var ms = new MemoryStream())
            {
                var length = EncodeVarint((ulong)data.Length);
                ms.Write(length, 0, length.Length);
                ms.Write(data, 0, data.Length);
                return ms.ToArray();
            }
        }

        /// <summary>
        /// 编码字符串字段
        /// </summary>
        public byte[] EncodeString(int fieldNumber, string value)
        {
            using (var ms = new MemoryStream())
            {
                var tag = EncodeTag(fieldNumber, WIRE_TYPE_LENGTH_DELIMITED);
                ms.Write(tag, 0, tag.Length);

                var bytes = Encoding.UTF8.GetBytes(value ?? "");
                var encoded = EncodeLengthDelimited(bytes);
                ms.Write(encoded, 0, encoded.Length);

                return ms.ToArray();
            }
        }

        /// <summary>
        /// 编码 64 位固定长度字段
        /// </summary>
        public byte[] EncodeFixed64Field(int fieldNumber, ulong value)
        {
            using (var ms = new MemoryStream())
            {
                var tag = EncodeTag(fieldNumber, WIRE_TYPE_64BIT);
                ms.Write(tag, 0, tag.Length);

                var data = EncodeFixed64(value);
                ms.Write(data, 0, data.Length);

                return ms.ToArray();
            }
        }

        /// <summary>
        /// 编码 Varint 字段
        /// </summary>
        public byte[] EncodeVarintField(int fieldNumber, ulong value)
        {
            using (var ms = new MemoryStream())
            {
                var tag = EncodeTag(fieldNumber, WIRE_TYPE_VARINT);
                ms.Write(tag, 0, tag.Length);

                var data = EncodeVarint(value);
                ms.Write(data, 0, data.Length);

                return ms.ToArray();
            }
        }

        #endregion

        #region 完整消息构建 (根据旺商聊深度连接协议第十六节)

        /// <summary>
        /// 构建完整的 Protobuf 消息
        /// 格式:
        /// Field1 (tag=0x09): 64-bit fixed - 消息ID
        /// Field2 (tag=0x11): 64-bit fixed - 时间戳
        /// Field3 (tag=0x19): 64-bit fixed - 加密种子
        /// Field4 (tag=0x22): length-delimited - 加密消息体
        /// </summary>
        public byte[] BuildMessage(ulong msgId, ulong timestamp, ulong seed, byte[] payload)
        {
            using (var ms = new MemoryStream())
            {
                // Field 1: 消息ID (wire type 1 = 64-bit)
                ms.WriteByte(0x09);  // tag: field=1, type=1
                var msgIdBytes = EncodeFixed64(msgId);
                ms.Write(msgIdBytes, 0, msgIdBytes.Length);

                // Field 2: 时间戳 (wire type 1 = 64-bit)
                ms.WriteByte(0x11);  // tag: field=2, type=1
                var timestampBytes = EncodeFixed64(timestamp);
                ms.Write(timestampBytes, 0, timestampBytes.Length);

                // Field 3: 加密种子 (wire type 1 = 64-bit)
                ms.WriteByte(0x19);  // tag: field=3, type=1
                var seedBytes = EncodeFixed64(seed);
                ms.Write(seedBytes, 0, seedBytes.Length);

                // Field 4: 消息体 (wire type 2 = length-delimited)
                ms.WriteByte(0x22);  // tag: field=4, type=2
                var encoded = EncodeLengthDelimited(payload);
                ms.Write(encoded, 0, encoded.Length);

                return ms.ToArray();
            }
        }

        /// <summary>
        /// 构建带魔数头部的完整消息
        /// </summary>
        public byte[] BuildMessageWithMagic(ulong msgId, ulong timestamp, ulong seed, byte[] payload)
        {
            using (var ms = new MemoryStream())
            {
                // 写入魔数
                ms.Write(MAGIC_NUMBER, 0, MAGIC_NUMBER.Length);

                // 写入版本标识 (4字节)
                var version = new byte[] { 0x01, 0xA0, 0xF8, 0x43 };
                ms.Write(version, 0, version.Length);

                // 写入消息类型前缀 (4字节)
                var prefix = new byte[] { 0x02, 0x00, 0x00, 0x00 };
                ms.Write(prefix, 0, prefix.Length);

                // 写入 Protobuf 消息
                var message = BuildMessage(msgId, timestamp, seed, payload);
                ms.Write(message, 0, message.Length);

                return ms.ToArray();
            }
        }

        #endregion

        #region 消息解析

        /// <summary>
        /// 解析消息头部
        /// </summary>
        public ProtobufHeader ParseHeader(byte[] data)
        {
            if (data == null || data.Length < HEADER_SIZE)
                return null;

            var header = new ProtobufHeader
            {
                RawBytes = new byte[HEADER_SIZE]
            };
            Array.Copy(data, header.RawBytes, HEADER_SIZE);

            // 检查魔数
            header.HasMagic = data[0] == MAGIC_NUMBER[0] &&
                             data[1] == MAGIC_NUMBER[1] &&
                             data[2] == MAGIC_NUMBER[2] &&
                             data[3] == MAGIC_NUMBER[3];

            if (header.HasMagic)
            {
                // 提取版本和类型
                header.Version = BitConverter.ToUInt32(data, 4);
                header.MessageType = BitConverter.ToUInt32(data, 8);
            }

            return header;
        }

        /// <summary>
        /// 解析 Protobuf 消息字段
        /// </summary>
        public List<ProtobufField> ParseFields(byte[] data, int startOffset = 0)
        {
            var fields = new List<ProtobufField>();
            int offset = startOffset;

            while (offset < data.Length)
            {
                try
                {
                    // 读取标签
                    var tag = (int)DecodeVarint(data, ref offset);
                    var fieldNumber = tag >> 3;
                    var wireType = tag & 0x07;

                    var field = new ProtobufField
                    {
                        FieldNumber = fieldNumber,
                        WireType = wireType
                    };

                    // 根据 wire type 读取值
                    switch (wireType)
                    {
                        case WIRE_TYPE_VARINT:
                            field.Value = DecodeVarint(data, ref offset);
                            break;

                        case WIRE_TYPE_64BIT:
                            field.Value = DecodeFixed64(data, ref offset);
                            break;

                        case WIRE_TYPE_LENGTH_DELIMITED:
                            var length = (int)DecodeVarint(data, ref offset);
                            if (offset + length > data.Length)
                                break;
                            var bytes = new byte[length];
                            Array.Copy(data, offset, bytes, 0, length);
                            field.BytesValue = bytes;
                            field.Value = bytes;
                            offset += length;
                            break;

                        case WIRE_TYPE_32BIT:
                            field.Value = DecodeFixed32(data, ref offset);
                            break;

                        default:
                            // 未知类型，停止解析
                            return fields;
                    }

                    fields.Add(field);
                }
                catch
                {
                    // 解析错误，停止
                    break;
                }
            }

            return fields;
        }

        #endregion

        #region Base64 编码/解码 (URL安全)

        /// <summary>
        /// URL安全的 Base64 编码
        /// </summary>
        public string ToUrlSafeBase64(byte[] data)
        {
            var base64 = Convert.ToBase64String(data);
            // 替换字符
            return base64.Replace('+', '-').Replace('/', '_').TrimEnd('=');
        }

        /// <summary>
        /// URL安全的 Base64 解码
        /// </summary>
        public byte[] FromUrlSafeBase64(string base64)
        {
            // 还原字符
            var standard = base64.Replace('-', '+').Replace('_', '/');
            // 补齐 padding
            var padding = 4 - (standard.Length % 4);
            if (padding < 4)
            {
                standard += new string('=', padding);
            }
            return Convert.FromBase64String(standard);
        }

        #endregion

        #region 辅助方法

        /// <summary>
        /// 生成随机消息ID
        /// </summary>
        public ulong GenerateMessageId()
        {
            var random = new Random();
            var bytes = new byte[8];
            random.NextBytes(bytes);
            return BitConverter.ToUInt64(bytes, 0);
        }

        /// <summary>
        /// 获取当前时间戳 (毫秒)
        /// </summary>
        public ulong GetCurrentTimestamp()
        {
            return (ulong)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }

        /// <summary>
        /// 生成随机种子
        /// </summary>
        public ulong GenerateSeed()
        {
            var random = new Random();
            var bytes = new byte[8];
            random.NextBytes(bytes);
            return BitConverter.ToUInt64(bytes, 0);
        }

        /// <summary>
        /// 将字节数组转为十六进制字符串
        /// </summary>
        public string ToHexString(byte[] data)
        {
            if (data == null)
                return "";
            return BitConverter.ToString(data).Replace("-", " ");
        }

        #endregion
    }

    #region 数据模型

    /// <summary>
    /// Protobuf 消息头部
    /// </summary>
    public class ProtobufHeader
    {
        /// <summary>原始字节</summary>
        public byte[] RawBytes { get; set; }
        /// <summary>是否有魔数</summary>
        public bool HasMagic { get; set; }
        /// <summary>版本</summary>
        public uint Version { get; set; }
        /// <summary>消息类型</summary>
        public uint MessageType { get; set; }
    }

    /// <summary>
    /// Protobuf 字段
    /// </summary>
    public class ProtobufField
    {
        /// <summary>字段编号</summary>
        public int FieldNumber { get; set; }
        /// <summary>Wire 类型</summary>
        public int WireType { get; set; }
        /// <summary>值</summary>
        public object Value { get; set; }
        /// <summary>字节值 (用于 length-delimited)</summary>
        public byte[] BytesValue { get; set; }

        /// <summary>
        /// Wire 类型名称
        /// </summary>
        public string WireTypeName
        {
            get
            {
                switch (WireType)
                {
                    case ProtobufBuilder.WIRE_TYPE_VARINT: return "Varint";
                    case ProtobufBuilder.WIRE_TYPE_64BIT: return "64-bit";
                    case ProtobufBuilder.WIRE_TYPE_LENGTH_DELIMITED: return "Length-delimited";
                    case ProtobufBuilder.WIRE_TYPE_32BIT: return "32-bit";
                    default: return $"Unknown({WireType})";
                }
            }
        }

        /// <summary>
        /// 获取字符串值 (用于 length-delimited 字段)
        /// </summary>
        public string GetStringValue()
        {
            if (BytesValue == null)
                return null;
            try
            {
                return Encoding.UTF8.GetString(BytesValue);
            }
            catch
            {
                return null;
            }
        }

        public override string ToString()
        {
            if (BytesValue != null)
            {
                var str = GetStringValue();
                if (!string.IsNullOrEmpty(str))
                    return $"Field{FieldNumber}({WireTypeName}): \"{str}\"";
                return $"Field{FieldNumber}({WireTypeName}): {BytesValue.Length} bytes";
            }
            return $"Field{FieldNumber}({WireTypeName}): {Value}";
        }
    }

    #endregion
}
