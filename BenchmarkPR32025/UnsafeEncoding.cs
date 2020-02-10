namespace BenchmarkPR32025
{
    static class UnsafeEncoding
    {
        public static unsafe void EncodeToUtf8(uint value, byte* destination, out int bytesWritten)
        {
            if (value <= 0x7Fu)
            {
                destination[0] = (byte)value;
                bytesWritten = 1;
            }
            else if (value <= 0x7FFu)
            {
                destination[0] = (byte)((value + (0b110u << 11)) >> 6);
                destination[1] = (byte)((value & 0x3Fu) + 0x80u);
                bytesWritten = 2;
            }
            else if (value <= 0xFFFFu)
            {
                destination[0] = (byte)((value + (0b1110 << 16)) >> 12);
                destination[1] = (byte)(((value & (0x3Fu << 6)) >> 6) + 0x80u);
                destination[2] = (byte)((value & 0x3Fu) + 0x80u);
                bytesWritten = 3;
            }
            else
            {
                destination[0] = (byte)((value + (0b11110 << 21)) >> 18);
                destination[1] = (byte)(((value & (0x3Fu << 12)) >> 12) + 0x80u);
                destination[2] = (byte)(((value & (0x3Fu << 6)) >> 6) + 0x80u);
                destination[3] = (byte)((value & 0x3Fu) + 0x80u);
                bytesWritten = 4;
            }
        }
    }
}
