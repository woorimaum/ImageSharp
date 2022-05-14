// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Numerics;

namespace SixLabors.ImageSharp.Formats.Jpeg.Components
{
    internal abstract partial class JpegColorConverterBase
    {
        /// <summary>
        /// <see cref="JpegColorConverterBase"/> abstract base for implementations
        /// based on <see cref="Vector"/> API.
        /// </summary>
        /// <remarks>
        /// Converters of this family can work with data of any size.
        /// Even though real life data is guaranteed to be of size
        /// divisible by 8 newer SIMD instructions like AVX512 won't work with
        /// such data out of the box. These converters have fallback code
        /// for 'remainder' data.
        /// </remarks>
        internal abstract class JpegColorConverterVector : JpegColorConverterBase
        {
            protected JpegColorConverterVector(JpegColorSpace colorSpace, int precision)
                : base(colorSpace, precision)
            {
            }

            public sealed override bool IsAvailable => Vector.IsHardwareAccelerated && Vector<float>.Count % 4 == 0;

            public override void ConvertToRgbInplace(in ComponentValues values)
            {
                DebugGuard.IsTrue(this.IsAvailable, $"{this.GetType().Name} converter is not supported on current hardware.");

                int length = values.Component0.Length;
                int remainder = (int)((uint)length % (uint)Vector<float>.Count);

                int simdCount = length - remainder;
                if (simdCount > 0)
                {
                    this.ConvertCoreVectorizedInplaceToRgb(values.Slice(0, simdCount));
                }

                // Jpeg images width is always divisible by 8 without a remainder
                // so it's safe to say SSE/AVX1/AVX2 implementations would never have
                // 'remainder' pixels
                // But some exotic simd implementations e.g. AVX-512 can have
                // remainder pixels
                if (remainder > 0)
                {
                    this.ConvertCoreInplaceToRgb(values.Slice(simdCount, remainder));
                }
            }

            public override void ConvertFromRgbInplace(in ComponentValues values, Span<float> r, Span<float> g, Span<float> b)
            {
                DebugGuard.IsTrue(this.IsAvailable, $"{this.GetType().Name} converter is not supported on current hardware.");

                int length = values.Component0.Length;
                int remainder = (int)((uint)length % (uint)Vector<float>.Count);

                int simdCount = length - remainder;
                if (simdCount > 0)
                {
                    this.ConvertCoreVectorizedInplaceFromRgb(
                        values.Slice(0, simdCount),
                        r.Slice(0, simdCount),
                        g.Slice(0, simdCount),
                        b.Slice(0, simdCount));
                }

                // Jpeg images width is always divisible by 8 without a remainder
                // so it's safe to say SSE/AVX1/AVX2 implementations would never have
                // 'remainder' pixels
                // But some exotic simd implementations e.g. AVX-512 can have
                // remainder pixels
                if (remainder > 0)
                {
                    this.ConvertCoreInplaceFromRgb(
                        values.Slice(simdCount, remainder),
                        r.Slice(simdCount, remainder),
                        g.Slice(simdCount, remainder),
                        b.Slice(simdCount, remainder));
                }
            }

            protected abstract void ConvertCoreVectorizedInplaceToRgb(in ComponentValues values);

            protected abstract void ConvertCoreInplaceToRgb(in ComponentValues values);

            protected abstract void ConvertCoreVectorizedInplaceFromRgb(in ComponentValues values, Span<float> r, Span<float> g, Span<float> b);

            protected abstract void ConvertCoreInplaceFromRgb(in ComponentValues values, Span<float> r, Span<float> g, Span<float> b);
        }
    }
}
