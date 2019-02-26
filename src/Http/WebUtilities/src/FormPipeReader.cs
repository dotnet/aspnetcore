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

        private static ReadOnlySpan<byte> UTF8EqualEncoded => new byte[] {(byte)'='};
        private static ReadOnlySpan<byte> UTF8AndEncoded => new byte[] {(byte)'&'};


        public FormPipeReader(PipeReader pipeReader)
            : this(pipeReader, Encoding.UTF8)
        {
        }

        public FormPipeReader(PipeReader pipeReader, Encoding encoding)
        {
            _pipeReader = pipeReader;
            _encoding = encoding;
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
                    TryParseFormValues(ref buffer, ref accumulator, readResult.IsCompleted, _encoding, KeyLengthLimit, ValueLengthLimit, ValueCountLimit);
                }

                if (readResult.IsCompleted)
                {
                    break;
                }

                _pipeReader.AdvanceTo(buffer.Start, buffer.End);
            }

            return accumulator.GetResults();
        }

        internal static void TryParseFormValues(ref ReadOnlySequence<byte> buffer, ref KeyValueAccumulator accumulator, bool isFinalBlock, Encoding encoding, int keyLengthLimit, int valueLengthLimit, int valueCountLimit)
        {
            if (buffer.IsSingleSegment)
            {
                TryParseFormValuesFast(buffer.First.Span,
                    ref accumulator,
                    isFinalBlock,
                    out var consumed,
                    GetEqualsForEncoding(encoding),
                    GetAndForEncoding(encoding),
                    encoding,
                    keyLengthLimit,
                    valueLengthLimit,
                    valueCountLimit);

                buffer = buffer.Slice(consumed);
                return;
            }

            TryParseValuesSlow(ref buffer,
                ref accumulator,
                isFinalBlock,
                GetEqualsForEncoding(encoding),
                GetAndForEncoding(encoding),
                encoding,
                keyLengthLimit,
                valueLengthLimit,
                valueCountLimit);
        }

        private static void TryParseValuesSlow(ref ReadOnlySequence<byte> buffer,
            ref KeyValueAccumulator accumulator,
            bool isFinalBlock,
            ReadOnlySpan<byte> equalsEncoded,
            ReadOnlySpan<byte> andEncoded,
            Encoding encoding,
            int keyLengthLimit,
            int valueLengthLimit,
            int valueCountLimit)
        {
            var sequenceReader = new SequenceReader<byte>(buffer);
            var consumed = sequenceReader.Position;

            while (!sequenceReader.End)
            {
                if (sequenceReader.TryReadTo(out ReadOnlySequence<byte> key, equalsEncoded))
                {
                    if (!sequenceReader.TryReadTo(out ReadOnlySequence<byte> value, andEncoded))
                    {
                        if (!isFinalBlock)
                        {
                            break;
                        }

                        value = buffer.Slice(sequenceReader.Position);

                        sequenceReader.Advance(value.Length);
                    }

                    var decodedKey = GetDecodedString((key.IsSingleSegment ? key.First : key.ToArray()).Span, encoding);
                    var decodedValue = GetDecodedString((value.IsSingleSegment ? value.First : value.ToArray()).Span, encoding);

                    Append(ref accumulator, keyLengthLimit, valueLengthLimit, valueCountLimit, decodedKey, decodedValue);

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

        private static KeyValueAccumulator Append(ref KeyValueAccumulator accumulator, int keyLengthLimit, int valueLengthLimit, int valueCountLimit, string decodedKey, string decodedValue)
        {
            if (decodedKey.Length > keyLengthLimit)
            {
                throw new InvalidDataException($"Form key or value length limit {keyLengthLimit} exceeded.");
            }

            if (decodedValue.Length > valueLengthLimit)
            {
                throw new InvalidDataException($"Form key or value length limit {valueLengthLimit} exceeded.");
            }

            accumulator.Append(decodedKey, decodedValue);

            if (accumulator.ValueCount > valueCountLimit)
            {
                throw new InvalidDataException($"Form value count limit {valueCountLimit} exceeded.");
            }

            return accumulator;
        }

        private static void TryParseFormValuesFast(ReadOnlySpan<byte> span,
            ref KeyValueAccumulator accumulator,
            bool isFinalBlock,
            out int consumed,
            ReadOnlySpan<byte> equalsEncoded,
            ReadOnlySpan<byte> andEncoded,
            Encoding encoding,
            int keyLengthLimit,
            int valueLengthLimit,
            int valueCountLimit)
        {
            ReadOnlySpan<byte> key = default;
            ReadOnlySpan<byte> value = default;
            consumed = 0;

            while (span.Length > 0)
            {
                var equals = span.IndexOf(equalsEncoded);

                if (equals == -1)
                {
                    break;
                }

                key = span.Slice(0, equals);

                span = span.Slice(key.Length + equalsEncoded.Length);

                var ampersand = span.IndexOf(andEncoded);

                value = span;

                if (ampersand == -1)
                {
                    if (!isFinalBlock)
                    {
                        // We can't that what is currently read is the end of the form value, that's only the case if this is the final block
                        // If we're not in the final block, the consume nothing
                        break;
                    }

                    span = Span<byte>.Empty;
                }
                else
                {
                    value = span.Slice(0, ampersand);

                    span = span.Slice(ampersand + andEncoded.Length);
                }

                var decodedKey = GetDecodedString(key, encoding);
                var decodedValue = GetDecodedString(value, encoding);

                Append(ref accumulator, keyLengthLimit, valueLengthLimit, valueCountLimit, decodedKey, decodedValue);

                consumed += key.Length + value.Length + (ampersand == -1 ? equalsEncoded.Length : equalsEncoded.Length + andEncoded.Length);
            }
        }

        private static string GetDecodedString(ReadOnlySpan<byte> readOnlySpan, Encoding encoding)
        {
            if (readOnlySpan.Length == 0)
            {
                return "";
            }
            else if (encoding == Encoding.UTF8 || encoding == Encoding.ASCII)
            {
                var span = MemoryMarshal.CreateSpan(ref Unsafe.AsRef(readOnlySpan[0]), readOnlySpan.Length);
                var bytes = UrlDecoder.DecodeInPlace(span);
                span = span.Slice(0, bytes);

                int index;
                while ((index = span.IndexOf((byte)'+')) != -1)
                {
                    span[index] = (byte)' ';
                }

                return encoding.GetString(span);
            }
            else
            {
                var decodedString = encoding.GetString(readOnlySpan);
                decodedString = decodedString.Replace('+', ' ');
                return Uri.UnescapeDataString(decodedString);
            }
        }

        private static ReadOnlySpan<byte> GetAndForEncoding(Encoding encoding)
        {
            if (encoding == Encoding.UTF8 || encoding == Encoding.ASCII)
            {
                return UTF8AndEncoded;
            }
            else
            {
                return encoding.GetBytes("&");
            }
        }

        private static ReadOnlySpan<byte> GetEqualsForEncoding(Encoding encoding)
        {
            if (encoding == Encoding.UTF8 || encoding == Encoding.ASCII)
            {
                return UTF8EqualEncoded;
            }
            else
            {
                return encoding.GetBytes("=");
            }
        }
    }
}
