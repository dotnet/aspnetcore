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

        private byte[] _buffer;
        private readonly PipeReader _pipeReader;
        private readonly Encoding _encoding;
        private readonly Encoder _encoder;
        private string _currentKey;
        private string _currentValue;
        private bool _endOfStream;
        private bool _disposed;
        private int _bufferOffset;
        private int _bufferCount;
        private readonly StringBuilder _builder = new StringBuilder();

        public FormReaderPipe(PipeReader pipeReader, Encoding encoding)
        {
            _pipeReader = pipeReader;
            _encoding = encoding;
            _encoder = _encoding.GetEncoder();
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

        // Format: key1=value1&key2=value2
        /// <summary>
        /// Asynchronously reads the next key value pair from the form.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns>The next key value pair, or null when the end of the form is reached.</returns>
        public async Task<KeyValuePair<string, string>?> ReadNextPairAsync(CancellationToken cancellationToken = default)
        {
            await ReadNextPairPipeAsync(cancellationToken);
            if (ReadSucceeded())
            {
                return new KeyValuePair<string, string>(_currentKey, _currentValue);
            }
            return null;
        }

        private bool ReadSucceeded()
        {
            return _currentKey != null && _currentValue != null;
        }

        /// Parses an HTTP form body.
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The collection containing the parsed HTTP form body.</returns>
        public async Task<Dictionary<string, StringValues>> ReadFormAsync(CancellationToken cancellationToken = default)
        {
            var accumulator = new KeyValueAccumulator();
            while (!_endOfStream)
            {
                await ReadNextPairPipeAsync(cancellationToken);
                Append(ref accumulator);
            }
            return accumulator.GetResults();
        }

        private async Task ReadNextPairPipeAsync(CancellationToken cancellationToken = default)
        {
            StartReadNextPair();
            while (!_endOfStream)
            {
                // Empty
                if (_bufferCount == 0)
                {
                    // read from the pipe
                    await PipeReadAsync(cancellationToken);
                }
                if (TryReadNextPair())
                {
                    break;
                }
            }
        }

        private async Task PipeReadAsync(CancellationToken cancellationToken)
        {
            _bufferOffset = 0;
            // how do I get T from a read only sequence? No indexer
            var readResult = await _pipeReader.ReadAsync(cancellationToken);
            // Does ToArray allocate? Probably haha
            _buffer = readResult.Buffer.ToArray();
            _endOfStream = readResult.IsCompleted;
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        private void StartReadNextPair()
        {
            _currentKey = null;
            _currentValue = null;
        }

        private bool TryReadNextPair()
        {
            if (_currentKey == null)
            {
                if (!TryReadWord('=', KeyLengthLimit, out _currentKey))
                {
                    return false;
                }

                if (_bufferCount == 0)
                {
                    return false;
                }
            }

            if (_currentValue == null)
            {
                if (!TryReadWord('&', ValueLengthLimit, out _currentValue))
                {
                    return false;
                }
            }
            return true;
        }

        private bool TryReadWord(char separator, int limit, out string value)
        {
            do
            {
                if (ReadChar(separator, limit, out value))
                {
                    return true;
                }
            } while (_bufferCount > 0);
            return false;
        }

        private bool ReadChar(char separator, int limit, out string word)
        {
            // End
            if (_bufferCount == 0)
            {
                word = BuildWord();
                return true;
            }
            
            var c = _buffer[_bufferOffset++];
            _bufferCount--;
            
            if (c == separator)
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

        // So let's do a few things here.
        // Ideally, we should only create the string after everything is done.
        private string BuildWord()
        {
            // need a start and end index
            // TWO string copies here.... Wowzer it's bad
            // need to handle encodoing here. Does that need to be character by character?
            // I think so.
            _builder.Replace('+', ' ');
            // First string alloc
            var result = _builder.ToString();
            _builder.Clear();
            // Yeah let's replace that if we can.
            return Uri.UnescapeDataString(result); // TODO: Replace this, it's not completely accurate.
        }

        private void Append(ref KeyValueAccumulator accumulator)
        {
            if (ReadSucceeded())
            {
                accumulator.Append(_currentKey, _currentValue);
                if (accumulator.ValueCount > ValueCountLimit)
                {
                    throw new InvalidDataException($"Form value count limit {ValueCountLimit} exceeded.");
                }
            }
        }
    }
}
