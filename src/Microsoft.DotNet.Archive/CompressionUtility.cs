// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using SevenZip;
using System;
using System.IO;

namespace Microsoft.DotNet.Archive
{
    internal static class CompressionUtility
    {
        enum MeasureBy
        {
            Input,
            Output
        }

        private class LzmaProgress : ICodeProgress
        {
            private IProgress<ProgressReport> progress;
            private long totalSize;
            private string phase;
            private MeasureBy measureBy;

            public LzmaProgress(IProgress<ProgressReport> progress, string phase, long totalSize, MeasureBy measureBy)
            {
                this.progress = progress;
                this.totalSize = totalSize;
                this.phase = phase;
                this.measureBy = measureBy;
            }

            public void SetProgress(long inSize, long outSize)
            {
                progress.Report(phase, measureBy == MeasureBy.Input ? inSize : outSize, totalSize);
            }
        }

        public static void Compress(Stream inStream, Stream outStream, IProgress<ProgressReport> progress)
        {
            SevenZip.Compression.LZMA.Encoder encoder = new SevenZip.Compression.LZMA.Encoder();

            CoderPropID[] propIDs =
            {
                    CoderPropID.DictionarySize,
                    CoderPropID.PosStateBits,
                    CoderPropID.LitContextBits,
                    CoderPropID.LitPosBits,
                    CoderPropID.Algorithm,
                    CoderPropID.NumFastBytes,
                    CoderPropID.MatchFinder,
                    CoderPropID.EndMarker
            };
            object[] properties =
            {
                    (Int32)(1 << 26),
                    (Int32)(1),
                    (Int32)(8),
                    (Int32)(0),
                    (Int32)(2),
                    (Int32)(96),
                    "bt4",
                    false
             };

            encoder.SetCoderProperties(propIDs, properties);
            encoder.WriteCoderProperties(outStream);

            Int64 inSize = inStream.Length;
            for (int i = 0; i < 8; i++)
            {
                outStream.WriteByte((Byte)(inSize >> (8 * i)));
            }

            var lzmaProgress = new LzmaProgress(progress, "Compressing", inSize, MeasureBy.Input);
            lzmaProgress.SetProgress(0, 0);
            encoder.Code(inStream, outStream, -1, -1, lzmaProgress);
            lzmaProgress.SetProgress(inSize, outStream.Length);
        }

        public static void Decompress(Stream inStream, Stream outStream, IProgress<ProgressReport> progress)
        {
            byte[] properties = new byte[5];

            if (inStream.Read(properties, 0, 5) != 5)
                throw (new Exception("input .lzma is too short"));

            SevenZip.Compression.LZMA.Decoder decoder = new SevenZip.Compression.LZMA.Decoder();
            decoder.SetDecoderProperties(properties);

            long outSize = 0;
            for (int i = 0; i < 8; i++)
            {
                int v = inStream.ReadByte();
                if (v < 0)
                    throw (new Exception("Can't Read 1"));
                outSize |= ((long)(byte)v) << (8 * i);
            }

            long compressedSize = inStream.Length - inStream.Position;
            var lzmaProgress = new LzmaProgress(progress, "Decompressing", outSize, MeasureBy.Output);
            lzmaProgress.SetProgress(0, 0);
            decoder.Code(inStream, outStream, compressedSize, outSize, lzmaProgress);
            lzmaProgress.SetProgress(inStream.Length, outSize);
        }
    }
}
