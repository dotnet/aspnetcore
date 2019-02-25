// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipelines;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections.Abstractions.Sources;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.WebUtilities
{
    public class FormPipeReader
    {
        public const int DefaultValueCountLimit = 1024;
        public const int DefaultKeyLengthLimit = 1024 * 2;
        public const int DefaultValueLengthLimit = 1024 * 1024 * 4;
        private PipeReader _pipeReader;
        private Encoding _encoding;

        private Memory<byte> _equalEncoded;
        private Memory<byte> _andEncoded;

        public FormPipeReader(PipeReader pipeReader)
            : this(pipeReader, Encoding.UTF8)
        {
        }

        public FormPipeReader(PipeReader pipeReader, Encoding encoding)
        {
            _pipeReader = pipeReader;
            _encoding = encoding;

            _equalEncoded = encoding.GetBytes("=");
            _andEncoded = encoding.GetBytes("&");
        }

        /// <summary>
        /// The limit on the number of form values to allow in ReadForm or ReadFormAsync.
        /// </summary>
        public int ValueCountLimit { get; set; } = DefaultValueCountLimit;

        /// <summary>
        /// The limit on the length of form keys.
        /// </summary>
        public int KeyLengthLimit { get; set; } = DefaultKeyLengthLimit;

        /// <summary>
        /// The limit on the length of form values.
        /// </summary>
        public int ValueLengthLimit { get; set; } = DefaultValueLengthLimit;

        /// <summary>
        /// Parses an HTTP form body.
        /// </summary>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The collection containing the parsed HTTP form body.</returns>
        public async Task<Dictionary<string, StringValues>> ReadFormAsync(CancellationToken cancellationToken = default)
        {
            KeyValueAccumulator accumulator = default;
            while (true)
            {
                ReadResult readResult;
                if (!_pipeReader.TryRead(out readResult))
                {
                    readResult = await _pipeReader.ReadAsync();
                }
                var buffer = readResult.Buffer;


                if (!buffer.IsEmpty)
                {
                    TryParseFormValues(ref buffer, ref accumulator, readResult.IsCompleted);
                }

                if (accumulator.ValueCount > ValueCountLimit)
                {
                    throw new InvalidDataException($"Form value count limit {ValueCountLimit} exceeded.");
                }

                if (readResult.IsCompleted)
                {
                    break;
                }

                _pipeReader.AdvanceTo(buffer.Start, buffer.End);
            }

            return accumulator.GetResults();
        }

        internal void TryParseFormValues(ref ReadOnlySequence<byte> buffer, ref KeyValueAccumulator accumulator, bool isFinalBlock)
        {
            var sequenceReader = new SequenceReader<byte>(buffer);
            var consumed = sequenceReader.Position;

            while (!sequenceReader.End)
            {
                if (sequenceReader.TryReadToAny(out ReadOnlySpan<byte> key, _equalEncoded.Span))
                {
                    if (!sequenceReader.TryReadToAny(out ReadOnlySpan<byte> value, _andEncoded.Span))
                    {
                        if (!isFinalBlock)
                        {
                            break;
                        }

                        var valueBuffer = buffer.Slice(sequenceReader.Position);

                        value = valueBuffer.IsSingleSegment ? valueBuffer.First.Span : valueBuffer.ToArray();

                        sequenceReader.Advance(valueBuffer.Length);
                    }

                    var decodedKey = GetDecodedString(key);
                    var decodedValue = GetDecodedString(value);

                    // TODO these error messages can be more specific
                    if (decodedKey.Length > KeyLengthLimit)
                    {
                        throw new InvalidDataException($"Form key or value length limit {KeyLengthLimit} exceeded.");
                    }

                    if (decodedValue.Length > ValueLengthLimit)
                    {
                        throw new InvalidDataException($"Form key or value length limit {ValueLengthLimit} exceeded.");
                    }

                    accumulator.Append(decodedKey, decodedValue);

                    consumed = sequenceReader.Position;
                }
                else
                {
                    // TODO this feels tacky
                    break;
                }
            }

            buffer = buffer.Slice(consumed);
        }

        private string GetDecodedString(ReadOnlySpan<byte> readOnlySpan)
        {
            if (readOnlySpan.Length == 0)
            {
                return "";
            }
            else if (_encoding == Encoding.UTF8 || _encoding == Encoding.ASCII)
            {
                var span = MemoryMarshal.CreateSpan(ref Unsafe.AsRef(readOnlySpan[0]), readOnlySpan.Length);
                var bytes = UrlDecoder.DecodeInPlace(span);
                span = span.Slice(0, bytes);

                int index;
                while ((index = span.IndexOf((byte)'+')) != -1)
                {
                    span[index] = (byte)' ';
                }
                return _encoding.GetString(span);
            }
            else
            {
                var decodedString = _encoding.GetString(readOnlySpan);
                decodedString = decodedString.Replace('+', ' ');
                return Uri.UnescapeDataString(decodedString);
            }
        }
    }
}
