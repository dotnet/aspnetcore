using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipelines;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.WebUtilities
{
    public class FormReader2
    {
        public const int DefaultValueCountLimit = 1024;
        public const int DefaultKeyLengthLimit = 1024 * 2;
        public const int DefaultValueLengthLimit = 1024 * 1024 * 4;
        private PipeReader _pipeReader;
        private Encoding _encoding;

        private Memory<byte> _equalEncoded;
        private Memory<byte> _andEncoded;
        private string _key;
        private string _value;
        private bool _endOfPipe;

        public FormReader2(PipeReader pipeReader)
            : this(pipeReader, Encoding.UTF8)
        {
        }

        public FormReader2(PipeReader pipeReader, Encoding encoding)
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

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        private void StartReadNextPair()
        {
            _key = null;
            _value = null;
        }

        // Format: key1=value1&key2=value2
        /// <summary>
        /// Asynchronously reads the next key value pair from the form.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns>The next key value pair, or null when the end of the form is reached.</returns>
        public async Task<KeyValuePair<string, string>?> ReadNextPairAsync(CancellationToken cancellationToken = new CancellationToken())
        {
            StartReadNextPair();



            // TODO a bit of cleanup here.
            _key = await FindStringInReadOnlySequenceAsync(_equalEncoded, KeyLengthLimit);
            if (_key == null)
            {
                return null;
            }

            _value = await FindStringInReadOnlySequenceAsync(_andEncoded, ValueLengthLimit);
            if (_value == null)
            {
                return null;
            }

            return new KeyValuePair<string, string>(_key, _value);
        }

        private async Task<string> FindStringInReadOnlySequenceAsync(Memory<byte> delimiter, int limit)
        {
            while (true)
            {
                if (_endOfPipe)
                {
                    return null;
                }

                ReadResult readResult;
                if (!_pipeReader.TryRead(out readResult))
                {
                    // TODO return awaited version of this to remove state machine
                    readResult = await _pipeReader.ReadAsync();
                }

                if (readResult.IsCanceled)
                {
                    continue;
                }

                // Need to cover case where we get end of buffer.
                // Original form reader had weird logic for handling it.

                if (TryFindDelimiter(ref readResult, ref delimiter, limit, out var stringRes))
                {
                    return stringRes;
                }
            }
        }

        private bool TryFindDelimiter(ref ReadResult readResult, ref Memory<byte> delimiter, int limit, out string res)
        {
            res = null;

            // Should always have something in the buffer.
            ReadOnlySpan<byte> result;
            SequencePosition position;
            var sequenceReader = new SequenceReader<byte>(readResult.Buffer);

            if (!sequenceReader.TryReadToAny(out result, delimiter.Span, advancePastDelimiter: false)
                || !sequenceReader.IsNext(delimiter.Span, advancePast: true))
            {
                // need more memory no matter what.
                if (readResult.IsCompleted)
                {
                    // take whatever remains in the buffer and put it into the string.
                    // TODO there is cleaner logic here
                    result = sequenceReader.UnreadSpan;
                    position = readResult.Buffer.End;
                    _endOfPipe = true;
                }
                else
                {
                    _pipeReader.AdvanceTo(readResult.Buffer.Start, readResult.Buffer.End);
                    return false;
                }
            }
            else
            {
                position = sequenceReader.Position;
            }

            res = _encoding.GetString(result);

            // TODO check if utf8 here.
            res = res.Replace('+', ' ');
            res = Uri.UnescapeDataString(res);

            if (res.Length > limit)
            {
                throw new InvalidDataException($"Form key or value length limit {limit} exceeded.");
            }

            // return Uri.UnescapeDataString(result); // TODO: Replace this, it's not completely accurate.
            _pipeReader.AdvanceTo(position);

            return true;
        }

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
                await ReadNextPairAsync(cancellationToken);
                if (ReadSucceeded())
                {
                    accumulator.Append(_key, _value);
                    if (accumulator.ValueCount > ValueCountLimit)
                    {
                        throw new InvalidDataException($"Form value count limit {ValueCountLimit} exceeded.");
                    }
                }
                else
                {
                    return accumulator.GetResults();
                }
            }
        }

        private bool ReadSucceeded()
        {
            return _key != null && _value != null;
        }
    }
}
