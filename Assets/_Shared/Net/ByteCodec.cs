using System;
using System.Text;

namespace CarSim.Shared
{
    public static class ByteCodec
    {
        // Header: MsgType(1) + Seq(2) + TimestampMs(4) + Length(2) = 9 bytes
        public const int HEADER_SIZE = 9;

        #region Primitives - Little Endian

        public static void WriteByte(byte[] buffer, ref int offset, byte value)
        {
            buffer[offset++] = value;
        }

        public static byte ReadByte(byte[] buffer, ref int offset)
        {
            return buffer[offset++];
        }

        public static void WriteSByte(byte[] buffer, ref int offset, sbyte value)
        {
            buffer[offset++] = (byte)value;
        }

        public static sbyte ReadSByte(byte[] buffer, ref int offset)
        {
            return (sbyte)buffer[offset++];
        }

        public static void WriteUShort(byte[] buffer, ref int offset, ushort value)
        {
            buffer[offset++] = (byte)(value & 0xFF);
            buffer[offset++] = (byte)((value >> 8) & 0xFF);
        }

        public static ushort ReadUShort(byte[] buffer, ref int offset)
        {
            ushort val = (ushort)(buffer[offset] | (buffer[offset + 1] << 8));
            offset += 2;
            return val;
        }

        public static void WriteUInt(byte[] buffer, ref int offset, uint value)
        {
            buffer[offset++] = (byte)(value & 0xFF);
            buffer[offset++] = (byte)((value >> 8) & 0xFF);
            buffer[offset++] = (byte)((value >> 16) & 0xFF);
            buffer[offset++] = (byte)((value >> 24) & 0xFF);
        }

        public static uint ReadUInt(byte[] buffer, ref int offset)
        {
            uint val = (uint)(buffer[offset] | (buffer[offset + 1] << 8) | (buffer[offset + 2] << 16) | (buffer[offset + 3] << 24));
            offset += 4;
            return val;
        }

        public static void WriteFloat(byte[] buffer, ref int offset, float value)
        {
            byte[] bytes = BitConverter.GetBytes(value);
            if (!BitConverter.IsLittleEndian)
            {
                Array.Reverse(bytes);
            }
            Buffer.BlockCopy(bytes, 0, buffer, offset, 4);
            offset += 4;
        }

        public static float ReadFloat(byte[] buffer, ref int offset)
        {
            float val = BitConverter.ToSingle(buffer, offset);
            offset += 4;
            if (!BitConverter.IsLittleEndian)
            {
                byte[] temp = new byte[4];
                Buffer.BlockCopy(buffer, offset - 4, temp, 0, 4);
                Array.Reverse(temp);
                val = BitConverter.ToSingle(temp, 0);
            }
            return val;
        }

        public static void WriteVector3(byte[] buffer, ref int offset, UnityEngine.Vector3 value)
        {
            WriteFloat(buffer, ref offset, value.x);
            WriteFloat(buffer, ref offset, value.y);
            WriteFloat(buffer, ref offset, value.z);
        }

        public static UnityEngine.Vector3 ReadVector3(byte[] buffer, ref int offset)
        {
            float x = ReadFloat(buffer, ref offset);
            float y = ReadFloat(buffer, ref offset);
            float z = ReadFloat(buffer, ref offset);
            return new UnityEngine.Vector3(x, y, z);
        }

        public static void WriteQuaternion(byte[] buffer, ref int offset, UnityEngine.Quaternion value)
        {
            WriteFloat(buffer, ref offset, value.x);
            WriteFloat(buffer, ref offset, value.y);
            WriteFloat(buffer, ref offset, value.z);
            WriteFloat(buffer, ref offset, value.w);
        }

        public static UnityEngine.Quaternion ReadQuaternion(byte[] buffer, ref int offset)
        {
            float x = ReadFloat(buffer, ref offset);
            float y = ReadFloat(buffer, ref offset);
            float z = ReadFloat(buffer, ref offset);
            float w = ReadFloat(buffer, ref offset);
            return new UnityEngine.Quaternion(x, y, z, w);
        }

        public static void WriteString(byte[] buffer, ref int offset, string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                WriteUShort(buffer, ref offset, 0);
                return;
            }
            byte[] bytes = Encoding.UTF8.GetBytes(value);
            ushort len = (ushort)bytes.Length;
            WriteUShort(buffer, ref offset, len);
            Buffer.BlockCopy(bytes, 0, buffer, offset, len);
            offset += len;
        }

        public static string ReadString(byte[] buffer, ref int offset)
        {
            ushort len = ReadUShort(buffer, ref offset);
            if (len == 0) return string.Empty;
            string val = Encoding.UTF8.GetString(buffer, offset, len);
            offset += len;
            return val;
        }

        public static void WriteFixedBytes(byte[] buffer, ref int offset, byte[] data, int length)
        {
            Buffer.BlockCopy(data, 0, buffer, offset, length);
            offset += length;
        }

        public static void ReadFixedBytes(byte[] buffer, ref int offset, byte[] dest, int length)
        {
            Buffer.BlockCopy(buffer, offset, dest, 0, length);
            offset += length;
        }

        #endregion

        #region Header

        public static void WriteHeader(byte[] buffer, ref int offset, MsgType msgType, ushort seq, uint timestampMs, ushort payloadLength)
        {
            WriteByte(buffer, ref offset, (byte)msgType);
            WriteUShort(buffer, ref offset, seq);
            WriteUInt(buffer, ref offset, timestampMs);
            WriteUShort(buffer, ref offset, payloadLength);
        }

        public static void ReadHeader(byte[] buffer, ref int offset, out MsgType msgType, out ushort seq, out uint timestampMs, out ushort payloadLength)
        {
            msgType = (MsgType)ReadByte(buffer, ref offset);
            seq = ReadUShort(buffer, ref offset);
            timestampMs = ReadUInt(buffer, ref offset);
            payloadLength = ReadUShort(buffer, ref offset);
        }

        #endregion
    }
}
