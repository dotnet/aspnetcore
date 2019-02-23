// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipelines;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.WebUtilities
{
    class FormReaderPipe : IDisposable
    {
        // This is very inefficient.
        public const int DefaultValueCountLimit = 1024;
        public const int DefaultKeyLengthLimit = 1024 * 2;
        public const int DefaultValueLengthLimit = 1024 * 1024 * 4;
        private PipeDecoder _pipeDecoder;
        private string _key;
        private string _value;
        private bool _endOfPipe;

        public FormReaderPipe(PipeReader pipeReader, Encoding encoding)
        {
            _pipeDecoder = new PipeDecoder(pipeReader, encoding);
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


        // Format: key1=value1&key2=value2
        /// <summary>
        /// Asynchronously reads the next key value pair from the form.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns>The next key value pair, or null when the end of the form is reached.</returns>
        public async Task<KeyValuePair<string, string>?> ReadNextPairAsync(CancellationToken cancellationToken = new CancellationToken())
        {
            StartReadNextPair();

            while (!_endOfPipe)
            {
                if (await TryReadNextPairAsync())
                {
                    return new KeyValuePair<string, string>(_key, _value);
                }
            }

            return null;
        }

        private void StartReadNextPair()
        {
            _key = null;
            _value = null;
        }

        private async Task<ReadOnlySequence<char>> GetNextReadOnlySequence()
        {
            var ros = _pipeDecoder.GetCurrentReadOnlySequence();
            if (ros.IsEmpty)
            {
                ros = await _pipeDecoder.ReadAsync();
                if (ros.IsEmpty)
                {
                    _endOfPipe = true;
                }
            }

            return ros;
        }

        private async Task<bool> TryReadNextPairAsync()
        {
            if (_key == null)
            {
                // TODO this doesn't need to go async here always
                var ros = await GetNextReadOnlySequence();
                if (!TryReadWord(ros, '=', KeyLengthLimit, out _key))
                {
                    return false;
                }
            }

            if (_value == null)
            {
                // TODO this doesn't need to go async here always
                var ros = await GetNextReadOnlySequence();

                if (!TryReadWord(ros, '&', ValueLengthLimit, out _value))
                {
                    return false;
                }
            }

            return true;
        }

        private bool TryReadWord(ReadOnlySequence<char> ros, char delimiter, int limit, out string res)
        {
            // KeyLengthLimit
            var position = ros.PositionOf(delimiter);
            if (position == null)
            {
                _pipeDecoder.AdvanceTo(ros.Start, ros.End);
                res = "";
                return false;
            }

            var result = ros.Slice(0, position.Value);

            // TODOOOOOO encoding.
            if (result.IsSingleSegment)
            {
                res = new string(result.First.Span);
            }
            else
            {
                // Allocation here is super uncommon. 
                res = new string(result.ToArray());
            }

            _pipeDecoder.AdvanceTo(result.End);

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
            while (!_endOfPipe)
            {
                await ReadNextPairAsync(cancellationToken);
                Append(ref accumulator);
            }
            return accumulator.GetResults();
        }

        private void Append(ref KeyValueAccumulator accumulator)
        {
            if (ReadSucceeded())
            {
                accumulator.Append(_key, _value);
                if (accumulator.ValueCount > ValueCountLimit)
                {
                    throw new InvalidDataException($"Form value count limit {ValueCountLimit} exceeded.");
                }
            }
        }
        private bool ReadSucceeded()
        {
            return _key != null && _value != null;
        }
    }
}
