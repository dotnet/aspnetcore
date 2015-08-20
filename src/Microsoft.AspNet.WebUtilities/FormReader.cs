// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Framework.Internal;
using Microsoft.Framework.Primitives;

namespace Microsoft.AspNet.WebUtilities
{
    /// <summary>
    /// Used to read an 'application/x-www-form-urlencoded' form.
    /// </summary>
    public class FormReader
    {
        private readonly TextReader _reader;
        private readonly char[] _buffer = new char[1024];
        private readonly StringBuilder _builder = new StringBuilder();
        private int _bufferOffset;
        private int _bufferCount;

        public FormReader([NotNull] string data)
        {
            _reader = new StringReader(data);
        }

        public FormReader([NotNull] Stream stream, [NotNull] Encoding encoding)
        {
            _reader = new StreamReader(stream, encoding, detectEncodingFromByteOrderMarks: true, bufferSize: 1024 * 2, leaveOpen: true);
        }

        // Format: key1=value1&key2=value2
        /// <summary>
        /// Reads the next key value pair from the form.
        /// For unbuffered data use the async overload instead.
        /// </summary>
        /// <returns>The next key value pair, or null when the end of the form is reached.</returns>
        public KeyValuePair<string, string>? ReadNextPair()
        {
            var key = ReadWord('=');
            if (string.IsNullOrEmpty(key) && _bufferCount == 0)
            {
                return null;
            }
            var value = ReadWord('&');
            return new KeyValuePair<string, string>(key, value);
        }

        // Format: key1=value1&key2=value2
        /// <summary>
        /// Asynchronously reads the next key value pair from the form.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns>The next key value pair, or null when the end of the form is reached.</returns>
        public async Task<KeyValuePair<string, string>?> ReadNextPairAsync(CancellationToken cancellationToken)
        {
            var key = await ReadWordAsync('=', cancellationToken);
            if (string.IsNullOrEmpty(key) && _bufferCount == 0)
            {
                return null;
            }
            var value = await ReadWordAsync('&', cancellationToken);
            return new KeyValuePair<string, string>(key, value);
        }

        private string ReadWord(char seperator)
        {
            // TODO: Configurable value size limit
            while (true)
            {
                // Empty
                if (_bufferCount == 0)
                {
                    Buffer();
                }

                // End
                if (_bufferCount == 0)
                {
                    return BuildWord();
                }

                var c = _buffer[_bufferOffset++];
                _bufferCount--;

                if (c == seperator)
                {
                    return BuildWord();
                }
                _builder.Append(c);
            }
        }

        private async Task<string> ReadWordAsync(char seperator, CancellationToken cancellationToken)
        {
            // TODO: Configurable value size limit
            while (true)
            {
                // Empty
                if (_bufferCount == 0)
                {
                    await BufferAsync(cancellationToken);
                }

                // End
                if (_bufferCount == 0)
                {
                    return BuildWord();
                }

                var c = _buffer[_bufferOffset++];
                _bufferCount--;

                if (c == seperator)
                {
                    return BuildWord();
                }
                _builder.Append(c);
            }
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
        /// <param name="text">The HTTP form body to parse.</param>
        /// <returns>The collection containing the parsed HTTP form body.</returns>
        public static IDictionary<string, StringValues> ReadForm(string text)
        {
            var reader = new FormReader(text);

            var accumulator = new KeyValueAccumulator();
            var pair = reader.ReadNextPair();
            while (pair.HasValue)
            {
                accumulator.Append(pair.Value.Key, pair.Value.Value);
                pair =  reader.ReadNextPair();
            }

            return accumulator.GetResults();
        }

        /// <summary>
        /// Parses an HTTP form body.
        /// </summary>
        /// <param name="stream">The HTTP form body to parse.</param>
        /// <returns>The collection containing the parsed HTTP form body.</returns>
        public static Task<IDictionary<string, StringValues>> ReadFormAsync(Stream stream, CancellationToken cancellationToken = new CancellationToken())
        {
            return ReadFormAsync(stream, Encoding.UTF8, cancellationToken);
        }

        /// <summary>
        /// Parses an HTTP form body.
        /// </summary>
        /// <param name="stream">The HTTP form body to parse.</param>
        /// <returns>The collection containing the parsed HTTP form body.</returns>
        public static async Task<IDictionary<string, StringValues>> ReadFormAsync(Stream stream, Encoding encoding, CancellationToken cancellationToken = new CancellationToken())
        {
            var reader = new FormReader(stream, encoding);

            var accumulator = new KeyValueAccumulator();
            var pair = await reader.ReadNextPairAsync(cancellationToken);
            while (pair.HasValue)
            {
                accumulator.Append(pair.Value.Key, pair.Value.Value);
                pair = await reader.ReadNextPairAsync(cancellationToken);
            }

            return accumulator.GetResults();
        }
    }
}