// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.WebUtilities
{
    /// <summary>
    /// Used to read an 'application/x-www-form-urlencoded' form.
    /// </summary>
    public class FormReader : IDisposable
    {
        public const int DefaultKeyCountLimit = 1024;
        public const int DefaultKeyLengthLimit = 1024 * 2;
        public const int DefaultValueLengthLimit = 1024 * 1024 * 4;

        private const int _rentedCharPoolLength = 8192;
        private readonly TextReader _reader;
        private readonly char[] _buffer;
        private readonly ArrayPool<char> _charPool;
        private readonly StringBuilder _builder = new StringBuilder();
        private int _bufferOffset;
        private int _bufferCount;
        private bool _disposed;

        public FormReader(string data)
            : this(data, ArrayPool<char>.Shared)
        {
        }

        public FormReader(string data, ArrayPool<char> charPool)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            _buffer = charPool.Rent(_rentedCharPoolLength);
            _charPool = charPool;
            _reader = new StringReader(data);
        }

        public FormReader(Stream stream)
            : this(stream, Encoding.UTF8, ArrayPool<char>.Shared)
        {
        }

        public FormReader(Stream stream, Encoding encoding)
            : this(stream, encoding, ArrayPool<char>.Shared)
        {
        }

        public FormReader(Stream stream, Encoding encoding, ArrayPool<char> charPool)
        {
            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            if (encoding == null)
            {
                throw new ArgumentNullException(nameof(encoding));
            }

            _buffer = charPool.Rent(_rentedCharPoolLength);
            _charPool = charPool;
            _reader = new StreamReader(stream, encoding, detectEncodingFromByteOrderMarks: true, bufferSize: 1024 * 2, leaveOpen: true);
        }

        /// <summary>
        /// The limit on the number of form keys to allow in ReadForm or ReadFormAsync.
        /// </summary>
        public int KeyCountLimit { get; set; } = DefaultKeyCountLimit;

        /// <summary>
        /// The limit on the length of form keys.
        /// </summary>
        public int KeyLengthLimit { get; set; } = DefaultKeyLengthLimit;

        /// <summary>
        /// The limit on the length of form values.
        /// </summary>
        public int ValueLengthLimit { get; set; } = DefaultValueLengthLimit;

        // Format: key1=value1&key2=value2
        /// <summary>
        /// Reads the next key value pair from the form.
        /// For unbuffered data use the async overload instead.
        /// </summary>
        /// <returns>The next key value pair, or null when the end of the form is reached.</returns>
        public KeyValuePair<string, string>? ReadNextPair()
        {
            var key = ReadWord('=', KeyLengthLimit);
            if (string.IsNullOrEmpty(key) && _bufferCount == 0)
            {
                return null;
            }
            var value = ReadWord('&', ValueLengthLimit);
            return new KeyValuePair<string, string>(key, value);
        }

        // Format: key1=value1&key2=value2
        /// <summary>
        /// Asynchronously reads the next key value pair from the form.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns>The next key value pair, or null when the end of the form is reached.</returns>
        public async Task<KeyValuePair<string, string>?> ReadNextPairAsync(CancellationToken cancellationToken = new CancellationToken())
        {
            var key = await ReadWordAsync('=', KeyLengthLimit, cancellationToken);
            if (string.IsNullOrEmpty(key) && _bufferCount == 0)
            {
                return null;
            }
            var value = await ReadWordAsync('&', ValueLengthLimit, cancellationToken);
            return new KeyValuePair<string, string>(key, value);
        }

        private string ReadWord(char seperator, int limit)
        {
            while (true)
            {
                // Empty
                if (_bufferCount == 0)
                {
                    Buffer();
                }

                string word;
                if (ReadChar(seperator, limit, out word))
                {
                    return word;
                }
            }
        }

        private async Task<string> ReadWordAsync(char seperator, int limit, CancellationToken cancellationToken)
        {
            while (true)
            {
                // Empty
                if (_bufferCount == 0)
                {
                    await BufferAsync(cancellationToken);
                }

                string word;
                if (ReadChar(seperator, limit, out word))
                {
                    return word;
                }
            }
        }

        private bool ReadChar(char seperator, int limit, out string word)
        {
            // End
            if (_bufferCount == 0)
            {
                word = BuildWord();
                return true;
            }

            var c = _buffer[_bufferOffset++];
            _bufferCount--;

            if (c == seperator)
            {
                word = BuildWord();
                return true;
            }
            if (_builder.Length >= limit)
            {
                throw new InvalidDataException($"Form key or value length limit {limit} exceeded.");
            }
            _builder.Append(c);
            word = null;
            return false;
        }

        // '+' un-escapes to ' ', %HH un-escapes as ASCII (or utf-8?)
        private string BuildWord()
        {
            _builder.Replace('+', ' ');
            var result = _builder.ToString();
            _builder.Clear();
            return Uri.UnescapeDataString(result); // TODO: Replace this, it's not completely accurate.
        }

        private void Buffer()
        {
            _bufferOffset = 0;
            _bufferCount = _reader.Read(_buffer, 0, _buffer.Length);
        }

        private async Task BufferAsync(CancellationToken cancellationToken)
        {
            // TODO: StreamReader doesn't support cancellation?
            cancellationToken.ThrowIfCancellationRequested();
            _bufferOffset = 0;
            _bufferCount = await _reader.ReadAsync(_buffer, 0, _buffer.Length);
        }

        /// <summary>
        /// Parses text from an HTTP form body.
        /// </summary>
        /// <returns>The collection containing the parsed HTTP form body.</returns>
        public Dictionary<string, StringValues> ReadForm()
        {
            var accumulator = new KeyValueAccumulator();
            var pair = ReadNextPair();
            while (pair.HasValue)
            {
                accumulator.Append(pair.Value.Key, pair.Value.Value);
                if (accumulator.Count > KeyCountLimit)
                {
                    throw new InvalidDataException($"Form key count limit {KeyCountLimit} exceeded.");
                }
                pair = ReadNextPair();
            }

            return accumulator.GetResults();
        }

        /// <summary>
        /// Parses an HTTP form body.
        /// </summary>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The collection containing the parsed HTTP form body.</returns>
        public async Task<Dictionary<string, StringValues>> ReadFormAsync(CancellationToken cancellationToken = new CancellationToken())
        {
            var accumulator = new KeyValueAccumulator();
            var pair = await ReadNextPairAsync(cancellationToken);
            while (pair.HasValue)
            {
                accumulator.Append(pair.Value.Key, pair.Value.Value);
                if (accumulator.Count > KeyCountLimit)
                {
                    throw new InvalidDataException($"Form key count limit {KeyCountLimit} exceeded.");
                }
                pair = await ReadNextPairAsync(cancellationToken);
            }

            return accumulator.GetResults();
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                _charPool.Return(_buffer);
            }
        }
    }
}