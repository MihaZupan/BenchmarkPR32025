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
                    UnsafeCore(ref vsb, pInput + i);
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
                    SpanCore(ref vsb, pInput + i);
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
                    SpanSliceCore(ref vsb, pInput + i);
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
                    RuneCore(ref vsb, pInput + i);
                }
            }

            return vsb.Length;
        }

        private static unsafe void UnsafeCore(ref ValueStringBuilder dest, char* pInput)
        {
            bool surrogatePair = char.IsSurrogatePair(*pInput, *(pInput + 1));
            int encodedBytesBuffer;
            byte* pEncodedBytes = (byte*)&encodedBytesBuffer;

            int encodedBytesCount = Encoding.UTF8.GetBytes(pInput, surrogatePair ? 2 : 1, pEncodedBytes, MaxNumberOfBytesEncoded);
            Debug.Assert(encodedBytesCount <= MaxNumberOfBytesEncoded, "UTF8 encoder should not exceed specified byteCount");

            for (int count = 0; count < encodedBytesCount; ++count)
            {
                UriHelper.EscapeAsciiChar((char)*(pEncodedBytes + count), ref dest);
            }
        }
        private static unsafe void SpanSliceCore(ref ValueStringBuilder dest, char* pInput)
        {
            bool surrogatePair = char.IsSurrogatePair(*pInput, *(pInput + 1));
            Span<byte> encodedBytes = stackalloc byte[MaxNumberOfBytesEncoded];
            int encodedBytesCount = Encoding.UTF8.GetBytes(new ReadOnlySpan<char>(pInput, surrogatePair ? 2 : 1), encodedBytes);
            encodedBytes = encodedBytes.Slice(0, encodedBytesCount);

            foreach (byte b in encodedBytes)
            {
                UriHelper.EscapeAsciiChar((char)b, ref dest);
            }
        }
        private static unsafe void SpanCore(ref ValueStringBuilder dest, char* pInput)
        {
            bool surrogatePair = char.IsSurrogatePair(*pInput, *(pInput + 1));
            Span<byte> encodedBytes = stackalloc byte[MaxNumberOfBytesEncoded];
            int encodedBytesCount = Encoding.UTF8.GetBytes(new ReadOnlySpan<char>(pInput, surrogatePair ? 2 : 1), encodedBytes);
            for (int i = 0; i < encodedBytesCount; i++)
            {
                UriHelper.EscapeAsciiChar((char)encodedBytes[i], ref dest);
            }
        }

        private static unsafe void RuneCore(ref ValueStringBuilder dest, char* pInput)
        {
            bool surrogatePair = char.IsSurrogatePair(*pInput, *(pInput + 1));
            Span<byte> encodedBytes = stackalloc byte[MaxNumberOfBytesEncoded];
            Rune rune = (surrogatePair) ? new Rune(*pInput, *(pInput + 1)) : new Rune(*pInput);
            int encodedBytesCount = rune.EncodeToUtf8(encodedBytes);
            encodedBytes = encodedBytes.Slice(0, encodedBytesCount);

            foreach (byte b in encodedBytes)
            {
                UriHelper.EscapeAsciiChar((char)b, ref dest);
            }
        }
    }
}
