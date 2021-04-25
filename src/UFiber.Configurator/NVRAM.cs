using System;
using System.Security.Cryptography;
using System.Text;
using static System.BitConverter;

namespace UFiber.Configurator
{
    public class NVRAM
    {
        private const int NvRamCrcOffset = 0x3FC;
        private const int NvRamCrcLength = 4;
        private const uint NvRamOffset = 0x580;
        private const uint NvRamLength = 0x400;
        public uint Checksum { get; private set; }
        public byte[] AfeId { get; }
        public byte[] VoiceBoardId { get; }
        public uint NandPartSizeKb { get; }
        public uint NandPartOfsKb { get; }
        public uint SyslogSize { get; }
        public byte[] WLanParams { get; }
        public byte[] WpsDevPin { get; }
        public string GponPassword { get; private set; }
        public byte[] GponVendorId { get; private set; }
        public byte[] GponVendorSerialNumber { get; private set; }
        public uint OldCheckSum { get; }
        public byte[] BaseMacAddr { get; private set; }
        public uint Version { get; }
        public uint NumMacAddr { get; }
        public uint PsiSize { get; }
        public uint MainThread { get; }
        public byte[] BoardId { get; }
        public byte[] BootLine { get; }
        private readonly byte[] _data;
        private readonly int _offset;
        private readonly int _length;

        public NVRAM(ReadOnlySpan<byte> data, uint offset = NvRamOffset, uint length = NvRamLength)
        {
            _data = new byte[data.Length];
            data.CopyTo(_data.AsSpan());

            _offset = (int)offset;
            _length = (int)length;

            Version = GetUint(0);

            BootLine = _data[OffsetOf(0x4)..OffsetOf(0x104)];
            BoardId = _data[OffsetOf(0x104)..OffsetOf(0x114)];
            MainThread = GetUint(0x114);
            PsiSize = GetUint(0x118);
            NumMacAddr = GetUint(0x11C);
            BaseMacAddr = _data[OffsetOf(0x120)..OffsetOf(0x126)];
            OldCheckSum = GetUint(0x128);
            GponVendorId = _data[OffsetOf(0x12C)..OffsetOf(0x130)];
            GponVendorSerialNumber = _data[OffsetOf(0x130)..OffsetOf(0x139)];
            GponPassword = Encoding.UTF8.GetString(_data[OffsetOf(0x139)..OffsetOf(0x144)]);
            WpsDevPin = _data[OffsetOf(0x144)..OffsetOf(0x14C)];
            WLanParams = _data[OffsetOf(0x14C)..OffsetOf(0x24C)];
            SyslogSize = GetUint(0x24C);
            NandPartOfsKb = GetUint(0x250);
            NandPartSizeKb = GetUint(0x264);
            VoiceBoardId = _data[OffsetOf(0x278)..OffsetOf(0x288)];
            AfeId = _data[OffsetOf(0x288)..OffsetOf(0x290)];

            Checksum = GetUint(NvRamCrcOffset);

            var crcIndex = _offset + NvRamCrcOffset;
            _data[crcIndex++] = 0;
            _data[crcIndex++] = 0;
            _data[crcIndex++] = 0;
            _data[crcIndex] = 0;

            var crc = CRC32.GenerateCrc(_data.AsSpan(_offset, _length));

            if (crc != Checksum)
            {
                throw new InvalidOperationException("Invalid data, CRC doesn't match");
            }
        }

        public void SetBaseMacAddress(string mac)
        {
            var data = AsBytes(mac);

            SetBaseMacAddress(data);
        }

        public void SetBaseMacAddress(ReadOnlySpan<byte> mac)
        {
            const int MacRelativeOffset = 0x120;
            const int MacLength = 6;

            if (mac.Length != MacLength)
            {
                throw new ArgumentOutOfRangeException(nameof(mac));
            }

            BaseMacAddr = SetBytes(mac, MacRelativeOffset, MacLength, false);
        }

        public void SetGponId(string id)
        {
            SetGponId(Encoding.UTF8.GetBytes(id));
        }

        public void SetGponId(ReadOnlySpan<byte> id)
        {
            const int VendorIdRelativeOffset = 0x12C;
            const int VendorIdLength = 4;

            if (id.Length != VendorIdLength)
            {
                throw new ArgumentOutOfRangeException(nameof(id));
            }

            GponVendorId = SetBytes(id, VendorIdRelativeOffset, VendorIdLength);
        }

        public void SetGponSerialNumber(string serialNumber)
        {
            SetGponSerialNumber(Encoding.UTF8.GetBytes(serialNumber));
        }

        public void SetGponSerialNumber(ReadOnlySpan<byte> serialNumber)
        {
            const int VendorSnRelativeOffset = 0x130;
            const int VendorSnLength = 8;

            if (serialNumber.Length != VendorSnLength)
            {
                throw new ArgumentOutOfRangeException(nameof(serialNumber));
            }

            GponVendorSerialNumber = SetBytes(serialNumber, VendorSnRelativeOffset, VendorSnLength);
        }

        public void SetGponPassword(string password)
        {
            const int PasswordOffset = 0x139;
            const int MaxPasswordLength = 10;

            if (password == null)
            {
                throw new ArgumentNullException(nameof(password));
            }

            if (password.Length > MaxPasswordLength)
            {
                throw new ArgumentOutOfRangeException(nameof(password));
            }

            var bits = Encoding.UTF8.GetBytes(password);

            SetBytes(bits.AsSpan(), PasswordOffset, password.Length);

            GponPassword = password;
        }

        public byte[] CompletePatch()
        {
            var bits = _data.AsSpan(_offset, _length);

            Checksum = CRC32.GenerateCrc(bits);

            var bitsIndex = NvRamCrcOffset;
            bits[bitsIndex++] = (byte)(Checksum >> 24);
            bits[bitsIndex++] = (byte)((Checksum >> 16) & 0xFF);
            bits[bitsIndex++] = (byte)((Checksum >> 8) & 0xFF);
            bits[bitsIndex] = (byte)(Checksum & 0xFF);

            return _data;
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendLine("--- NVRAM Information --");
            sb.AppendLine($"- mtdblock3 hash: {ComputeSha256Hash(this._data)}");
            sb.AppendLine($"- NVRAM Version: {this.Version}");
            sb.AppendLine($"- Boot parameters: {UnsafeAsciiBytesToString(this.BootLine)}");
            sb.AppendLine($"- Board Id: {UnsafeAsciiBytesToString(this.BoardId)}");
            sb.AppendLine($"- PSI size: {this.PsiSize}");
            sb.AppendLine($"- Total MAC addresses: {this.NumMacAddr}");
            sb.AppendLine($"- GPON MAC address: { BitConverter.ToString(this.BaseMacAddr).Replace("-", ":")}");
            sb.AppendLine($"- GPON Vendor Id: {Encoding.UTF8.GetString(this.GponVendorId)}");
            sb.AppendLine($"- GPON Serial Number: {UnsafeAsciiBytesToString(this.GponVendorSerialNumber)}");
            sb.AppendLine($"- GPON SLID (password): {this.GponPassword.TrimEnd((char)0)}");
            sb.AppendLine($"- Checksum: {this.Checksum}");
            return sb.ToString();
        }

        public static string UnsafeAsciiBytesToString(byte[] buffer)
        {
            unsafe
            {
                fixed (byte* pAscii = buffer)
                {
                    return new String((sbyte*)pAscii);
                }
            }
        }

        private int OffsetOf(int value) => _offset + value;
        private int OffsetOf(int value1, int value2) => _offset + value1 + value2;

        private byte[] SetBytes(ReadOnlySpan<byte> source, int relativeOffset, int length, bool zeroTerminate = true)
        {
            var dataIndex = _offset + relativeOffset;
            var dataEnd = dataIndex + length;
            var sourceIndex = 0;

            while (dataIndex < dataEnd)
            {
                _data[dataIndex++] = source[sourceIndex++];
            }

            if (zeroTerminate)
            {
                _data[dataIndex] = 0;
            }

            return _data[OffsetOf(relativeOffset)..OffsetOf(relativeOffset, length)];
        }

        private static byte[] AsBytes(string data)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            if (data.Length % 2 != 0)
            {
                throw new ArgumentException("Data must be an even length.", nameof(data));
            }

            data = data.ToLower();

            var len = data.Length / 2;

            var bits = new byte[len];

            var bitsIndex = 0;
            var dataIndex = 0;

            while (dataIndex < data.Length)
            {
                var highNibble = ToHex(data[dataIndex++]);
                var lowNibble = ToHex(data[dataIndex++]);
                bits[bitsIndex++] = (byte)(highNibble << 4 | lowNibble);
            }

            return bits;

            static int ToHex(char c) =>
                c switch
                {
                    >= 'a' and <= 'f' => 10 + (c - 'a'),
                    >= '0' and <= '9' => c - '0',
                    _ => throw new ArgumentOutOfRangeException(nameof(c))
                };
        }

        private static string ComputeSha256Hash(byte[] rawData)
        {
            using (SHA256 sha256Hash = SHA256.Create())
            {
                byte[] bytes = sha256Hash.ComputeHash(rawData);

                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }
                return builder.ToString();
            }
        }

        private uint GetUint(int offset)
        {
            var bits = _data.AsSpan(OffsetOf(offset), 4);

            return (uint)(bits[0] << 24 |
                bits[1] << 16 |
                bits[2] << 8 |
                bits[3]);
        }
    }
}