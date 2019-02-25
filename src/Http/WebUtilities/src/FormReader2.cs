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
                return new KeyValuePair<string, string>(null, null);
            }

            _value = await FindStringInReadOnlySequenceAsync(_andEncoded, ValueLengthLimit);
            if (_value == null)
            {
                return new KeyValuePair<string, string>(null, null);
            }

            return new KeyValuePair<string, string>(_key, _value);
        }

        private async Task<string> FindStringInReadOnlySequenceAsync(Memory<byte> delimiter, int limit)
        {
            while (true)
            {
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

                if (readResult.IsCompleted && readResult.Buffer.IsEmpty)
                {
                    return null;
                }

                var buffer = readResult.Buffer;
                if (TryFindDelimiter(ref buffer, ref delimiter, limit, out var stringRes))
                {
                    return stringRes;
                }
            }
        }

        private bool TryFindDelimiter(ref ReadOnlySequence<byte> buffer, ref Memory<byte> delimiter, int limit, out string res)
        {
            res = null;
            var sequenceReader = new SequenceReader<byte>(buffer);

            // Should always have something in the buffer.
            Debug.Assert(!sequenceReader.End);

            if (!sequenceReader.TryReadToAny(out ReadOnlySpan<byte> result, delimiter.Span, advancePastDelimiter: false)
                || !sequenceReader.IsNext(delimiter.Span, advancePast: true))
            {
                // need more memory no matter what.
                _pipeReader.AdvanceTo(buffer.Start, buffer.End);
                return false;
            }

            res = _encoding.GetString(result);
            if (res.Length > limit)
            {
                throw new InvalidDataException($"Form key or value length limit {limit} exceeded.");
            }

            _pipeReader.AdvanceTo(sequenceReader.Position);

            return true;
        }

        /// <summary>
        /// Parses an HTTP form body.
        /// </summary>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The collection containing the parsed HTTP form body.</returns>
        public async Task<Dictionary<string, StringValues>> ReadFormAsync(CancellationToken cancellationToken = default)
        {
            var accumulator = new KeyValueAccumulator();
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
