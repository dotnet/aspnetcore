// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Microsoft.AspNetCore.Razor.Language
{
    internal class LargeTextSourceDocument : RazorSourceDocument
    {
        private readonly List<char[]> _chunks;

        private readonly int _chunkMaxLength;

        private readonly RazorSourceLineCollection _lines;

        private readonly int _length;
        private byte[] _checksum;

        public LargeTextSourceDocument(StreamReader reader, int chunkMaxLength, Encoding encoding, string fileName)
        {
            if (reader == null)
            {
                throw new ArgumentNullException(nameof(reader));
            }

            if (encoding == null)
            {
                throw new ArgumentNullException(nameof(encoding));
            }

            _chunkMaxLength = chunkMaxLength;
            Encoding = encoding;
            FileName = fileName;

            ReadChunks(reader, _chunkMaxLength, out _length, out _chunks);
            _lines = new DefaultRazorSourceLineCollection(this);
        }

        public override char this[int position]
        {
            get
            {
                var chunkIndex = position / _chunkMaxLength;
                var insideChunkPosition = position % _chunkMaxLength;

                return _chunks[chunkIndex][insideChunkPosition];
            }
        }

        public override Encoding Encoding { get; }

        public override string FileName { get; }

        public override int Length => _length;

        public override RazorSourceLineCollection Lines => _lines;

        public override void CopyTo(int sourceIndex, char[] destination, int destinationIndex, int count)
        {
            if (destination == null)
            {
                throw new ArgumentNullException(nameof(destination));
            }

            if (sourceIndex < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(sourceIndex));
            }

            if (destinationIndex < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(destinationIndex));
            }

            if (count < 0 || count > Length - sourceIndex || count > destination.Length - destinationIndex)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }

            if (count == 0)
            {
                return;
            }

            var chunkIndex = sourceIndex / _chunkMaxLength;
            var insideChunkPosition = sourceIndex % _chunkMaxLength;
            var remaining = count;
            var currentDestIndex = destinationIndex;

            while (remaining > 0)
            {
                var toCopy = Math.Min(remaining, _chunkMaxLength - insideChunkPosition);
                Array.Copy(_chunks[chunkIndex], insideChunkPosition, destination, currentDestIndex, toCopy);

                remaining -= toCopy;
                currentDestIndex += toCopy;
                chunkIndex++;
                insideChunkPosition = 0;
            }
        }

        public override byte[] GetChecksum()
        {
            if (_checksum == null)
            {
                var charBuffer = new char[Length];
                CopyTo(0, charBuffer, 0, Length);

                var encoder = Encoding.GetEncoder();
                var byteCount = encoder.GetByteCount(charBuffer, 0, charBuffer.Length, flush: true);
                var bytes = new byte[byteCount];
                encoder.GetBytes(charBuffer, 0, charBuffer.Length, bytes, 0, flush: true);

                using (var hashAlgorithm = SHA1.Create())
                {
                    _checksum = hashAlgorithm.ComputeHash(bytes);
                }
            }

            var copiedChecksum = new byte[_checksum.Length];
            _checksum.CopyTo(copiedChecksum, 0);

            return copiedChecksum;
        }

        private static void ReadChunks(StreamReader reader, int chunkMaxLength, out int length, out List<char[]> chunks)
        {
            length = 0;
            chunks = new List<char[]>();

            int read;
            do
            {
                var chunk = new char[chunkMaxLength];
                read = reader.ReadBlock(chunk, 0, chunkMaxLength);

                length += read;

                if (read > 0)
                {
                    chunks.Add(chunk);
                }
            }
            while (read == chunkMaxLength);
        }
    }
}
