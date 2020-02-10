using System;
using System.Text;

namespace BenchmarkPR32025
{
    class UriHelper
    {
        internal static ReadOnlySpan<byte> HexUpperChars => new byte[16]
        {
            (byte)'0', (byte)'1', (byte)'2', (byte)'3', (byte)'4', (byte)'5', (byte)'6', (byte)'7',
            (byte)'8', (byte)'9', (byte)'A', (byte)'B', (byte)'C', (byte)'D', (byte)'E', (byte)'F'
        };

        internal static void EscapeAsciiChar(char ch, ref ValueStringBuilder to)
        {
            to.Append('%');
            to.Append((char)HexUpperChars[(ch & 0xf0) >> 4]);
            to.Append((char)HexUpperChars[ch & 0xf]);
        }
    }
}
