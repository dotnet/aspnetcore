// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Microsoft.AspNetCore.Antiforgery.Internal
{
    public class AntiforgerySerializationContext
    {
        // Avoid allocating 256 bytes (the default) and using 18 (the AntiforgeryToken minimum). 64 bytes is enough for
        // a short username or claim UID and some additional data. MemoryStream bumps capacity to 256 if exceeded.
        private const int InitialStreamSize = 64;

        // Don't let the MemoryStream grow beyond 1 MB.
        private const int MaximumStreamSize = 0x100000;

        // Start _chars off with length 256 (18 bytes is protected into 116 bytes then encoded into 156 characters).
        // Double length from there if necessary.
        private const int InitialCharsLength = 256;

        // Don't let _chars grow beyond 512k characters.
        private const int MaximumCharsLength = 0x80000;

        private char[] _chars;
        private MemoryStream _stream;
        private BinaryReader _reader;
        private BinaryWriter _writer;
        private SHA256 _sha256;

        public MemoryStream Stream
        {
            get
            {
                if (_stream == null)
                {
                    _stream = new MemoryStream(InitialStreamSize);
                }

                return _stream;
            }
            private set
            {
                _stream = value;
            }
        }

        public BinaryReader Reader
        {
            get
            {
                if (_reader == null)
                {
                    // Leave open to clean up correctly even if only one of the reader or writer has been created.
                    _reader = new BinaryReader(Stream, Encoding.UTF8, leaveOpen: true);
                }

                return _reader;
            }
            private set
            {
                _reader = value;
            }
        }

        public BinaryWriter Writer
        {
            get
            {
                if (_writer == null)
                {
                    // Leave open to clean up correctly even if only one of the reader or writer has been created.
                    _writer = new BinaryWriter(Stream, Encoding.UTF8, leaveOpen: true);
                }

                return _writer;
            }
            private set
            {
                _writer = value;
            }
        }

        public SHA256 Sha256
        {
            get
            {
                if (_sha256 == null)
                {
                    _sha256 = CryptographyAlgorithms.CreateSHA256();
                }

                return _sha256;
            }
            private set
            {
                _sha256 = value;
            }
        }

        public char[] GetChars(int count)
        {
            if (_chars == null || _chars.Length < count)
            {
                var newLength = _chars == null ? InitialCharsLength : checked(_chars.Length * 2);
                while (newLength < count)
                {
                    newLength = checked(newLength * 2);
                }

                _chars = new char[newLength];
            }

            return _chars;
        }

        public void Reset()
        {
            if (_chars != null && _chars.Length > MaximumCharsLength)
            {
                _chars = null;
            }

            if (_stream != null)
            {
                if (Stream.Capacity > MaximumStreamSize)
                {
                    Stream = null;
                    Reader = null;
                    Writer = null;
                }
                else
                {
                    Stream.Position = 0L;
                    Stream.SetLength(0L);
                }
            }
        }
    }
}