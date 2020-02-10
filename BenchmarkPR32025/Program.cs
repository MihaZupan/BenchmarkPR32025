using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using System;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace BenchmarkPR32025
{
    public class Program
    {
        static void Main() => BenchmarkRunner.Run<ToUnsafeOrNotToUnsafe>();
    }

    public class ToUnsafeOrNotToUnsafe
    {
        private readonly string TestUri = string.Concat(Enumerable.Repeat(new string(new char[] { '\ud83f', '\udffe' }), 1000));
        private static readonly char[] TargetBuffer = new char[12000];

        const int MaxNumberOfBytesEncoded = 4;

        [Benchmark]
        public unsafe int Unsafe()
        {
            ValueStringBuilder vsb = new ValueStringBuilder(TargetBuffer);

            fixed (char* pInput = TestUri)
            {
                for (int i = 0; i < TestUri.Length; i += 2)
                {
                    bool surrogatePair = char.IsSurrogatePair(pInput[i], pInput[i + 1]);
                    UnsafeCore(ref vsb, pInput + i, surrogatePair);
                }
            }

            return vsb.Length;
        }

        [Benchmark]
        public unsafe int Span()
        {
            ValueStringBuilder vsb = new ValueStringBuilder(TargetBuffer);

            fixed (char* pInput = TestUri)
            {
                for (int i = 0; i < TestUri.Length; i += 2)
                {
                    bool surrogatePair = char.IsSurrogatePair(pInput[i], pInput[i + 1]);
                    SpanCore(ref vsb, pInput + i, surrogatePair);
                }
            }

            Debug.Assert(vsb.Length == TargetBuffer.Length);

            return vsb.Length;
        }

        [Benchmark]
        public unsafe int SpanSlice()
        {
            ValueStringBuilder vsb = new ValueStringBuilder(TargetBuffer);

            fixed (char* pInput = TestUri)
            {
                for (int i = 0; i < TestUri.Length; i += 2)
                {
                    bool surrogatePair = char.IsSurrogatePair(pInput[i], pInput[i + 1]);
                    SpanSliceCore(ref vsb, pInput + i, surrogatePair);
                }
            }

            return vsb.Length;
        }

        [Benchmark]
        public unsafe int Rune()
        {
            ValueStringBuilder vsb = new ValueStringBuilder(TargetBuffer);

            fixed (char* pInput = TestUri)
            {
                for (int i = 0; i < TestUri.Length; i += 2)
                {
                    bool surrogatePair = char.IsSurrogatePair(pInput[i], pInput[i + 1]);
                    RuneCore(ref vsb, pInput + i, surrogatePair);
                }
            }

            return vsb.Length;
        }

        [Benchmark]
        public unsafe int UnsafeUnsafe()
        {
            ValueStringBuilder vsb = new ValueStringBuilder(TargetBuffer);

            fixed (char* pInput = TestUri)
            {
                for (int i = 0; i < TestUri.Length; i += 2)
                {
                    bool surrogatePair = char.IsSurrogatePair(pInput[i], pInput[i + 1]);
                    UnsafeUnsafeCore(ref vsb, pInput + i, surrogatePair);
                }
            }

            return vsb.Length;
        }

        private static unsafe void UnsafeCore(ref ValueStringBuilder dest, char* pInput, bool surrogatePair)
        {
            int encodedBytesBuffer;
            byte* pEncodedBytes = (byte*)&encodedBytesBuffer;

            int encodedBytesCount = Encoding.UTF8.GetBytes(pInput, surrogatePair ? 2 : 1, pEncodedBytes, MaxNumberOfBytesEncoded);
            Debug.Assert(encodedBytesCount <= MaxNumberOfBytesEncoded, "UTF8 encoder should not exceed specified byteCount");

            for (int count = 0; count < encodedBytesCount; ++count)
            {
                UriHelper.EscapeAsciiChar((char)*(pEncodedBytes + count), ref dest);
            }
        }
        private static unsafe void SpanSliceCore(ref ValueStringBuilder dest, char* pInput, bool surrogatePair)
        {
            Span<byte> encodedBytes = stackalloc byte[MaxNumberOfBytesEncoded];
            int encodedBytesCount = Encoding.UTF8.GetBytes(new ReadOnlySpan<char>(pInput, surrogatePair ? 2 : 1), encodedBytes);
            encodedBytes = encodedBytes.Slice(0, encodedBytesCount);

            foreach (byte b in encodedBytes)
            {
                UriHelper.EscapeAsciiChar((char)b, ref dest);
            }
        }
        private static unsafe void SpanCore(ref ValueStringBuilder dest, char* pInput, bool surrogatePair)
        {
            Span<byte> encodedBytes = stackalloc byte[MaxNumberOfBytesEncoded];
            int encodedBytesCount = Encoding.UTF8.GetBytes(new ReadOnlySpan<char>(pInput, surrogatePair ? 2 : 1), encodedBytes);
            for (int i = 0; i < encodedBytesCount; i++)
            {
                UriHelper.EscapeAsciiChar((char)encodedBytes[i], ref dest);
            }
        }

        private static unsafe void RuneCore(ref ValueStringBuilder dest, char* pInput, bool surrogatePair)
        {
            Span<byte> encodedBytes = stackalloc byte[MaxNumberOfBytesEncoded];
            Rune rune = (surrogatePair) ? new Rune(*pInput, *(pInput + 1)) : new Rune(*pInput);
            int encodedBytesCount = rune.EncodeToUtf8(encodedBytes);
            encodedBytes = encodedBytes.Slice(0, encodedBytesCount);

            foreach (byte b in encodedBytes)
            {
                UriHelper.EscapeAsciiChar((char)b, ref dest);
            }
        }

        private static unsafe void UnsafeUnsafeCore(ref ValueStringBuilder dest, char* pInput, bool surrogatePair)
        {
            int encodedBytesBuffer;
            byte* pEncodedBytes = (byte*)&encodedBytesBuffer;

            uint value = surrogatePair ? (uint)char.ConvertToUtf32(*pInput, *(pInput + 1)) : (uint)*pInput;
            UnsafeEncoding.EncodeToUtf8(value, pEncodedBytes, out int bytesWritten);

            for (int i = 0; i < bytesWritten; i++)
            {
                UriHelper.EscapeAsciiChar((char)*(pEncodedBytes + i), ref dest);
            }
        }
    }
}
