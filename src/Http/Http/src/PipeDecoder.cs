using System;
using System.Buffers;
using System.Diagnostics;
using System.IO.Pipelines;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.WebUtilities
{
    // TODO add pipe tests once the api is figured out.
    public class PipeDecoder : IDisposable
    {
        private PipeReader _reader;
        private Decoder _decoder;

        private readonly ArrayPool<char> _charPool;

        private BufferSegment<char> _readHead;
        private int _readIndex;
        private BufferSegment<char> _readTail;

        private long _bufferedCharacters;
        private bool _examinedEverything;

        private const int _rentedCharPoolLength = 8192;
        private const int _minimumReadThreshold = 2048;

        private bool _isCompleted;
        // Let's make this keep track of Memory

        public PipeDecoder(PipeReader reader, Encoding encoding)
        {
            _reader = reader;
            _decoder = encoding.GetDecoder();
            _charPool = ArrayPool<char>.Shared; // This will be the only allocations.
        }

        public async ValueTask<ReadOnlySequence<char>> ReadAsync(CancellationToken token = default)
        {
            if (_bufferedCharacters > 0 && (!_examinedEverything || _isCompleted))
            {
                return GetCurrentReadOnlySequence();
            }

            ReadResult readResult;
            if (!_reader.TryRead(out readResult))
            {
                readResult = await _reader.ReadAsync(token).ConfigureAwait(false);
            }

            if (readResult.IsCompleted)
            {
                _isCompleted = true;
            }

            AllocateReadTail();

            WriteCharactersToOutput(ref readResult);

            return GetCurrentReadOnlySequence();
        }

        public ReadOnlySequence<char> GetCurrentReadOnlySequence()
        {
            return new ReadOnlySequence<char>(_readHead, _readIndex, _readTail, _readTail.End);
        }

        public void AdvanceTo(SequencePosition consumed)
        {
            AdvanceTo(consumed, consumed);
        }

        public void AdvanceTo(SequencePosition consumed, SequencePosition examined)
        {
            if (_readHead == null || _readTail == null)
            {
                ThrowHelper.ThrowInvalidOperationException_NoDataRead();
            }

            AdvanceTo((BufferSegment<char>)consumed.GetObject(), consumed.GetInteger(), (BufferSegment<char>)examined.GetObject(), examined.GetInteger());
        }

        // Let's think about how to 
        private void AdvanceTo(BufferSegment<char> consumedSegment, int consumedIndex, BufferSegment<char> examinedSegment, int examinedIndex)
        {
            if (consumedSegment == null)
            {
                return;
            }

            var returnStart = _readHead;
            var returnEnd = consumedSegment;

            var consumedBytes = new ReadOnlySequence<char>(returnStart, _readIndex, consumedSegment, consumedIndex).Length;

            _bufferedCharacters -= consumedBytes;

            Debug.Assert(_bufferedCharacters >= 0);

            _examinedEverything = false;

            if (examinedSegment == _readTail)
            {
                // If we examined everything, we force ReadAsync to actually read from the underlying stream
                // instead of returning a ReadResult from TryRead.
                _examinedEverything = examinedIndex == _readTail.End;
            }

            // Three cases here:
            // 1. All data is consumed. If so, we reset _readHead and _readTail to _readTail's original memory owner
            //  SetMemory on a IMemoryOwner will reset the internal Memory<byte> to be an empty segment
            // 2. A segment is entirely consumed but there is still more data in nextSegments
            //  We are allowed to remove an extra segment. by setting returnEnd to be the next block.
            // 3. We are in the middle of a segment.
            //  Move _readHead and _readIndex to consumedSegment and index
            if (_bufferedCharacters == 0)
            {
                _readTail.SetMemory(_readTail.MemoryOwner);
                _readHead = _readTail;
                returnEnd = _readTail;
                _readIndex = 0;
            }
            else if (consumedIndex == returnEnd.Length)
            {
                var nextBlock = returnEnd.NextSegment;
                _readHead = nextBlock;
                _readIndex = 0;
                returnEnd = nextBlock;
            }
            else
            {
                _readHead = consumedSegment;
                _readIndex = consumedIndex;
            }

            // Remove all blocks that are freed (except the last one)
            while (returnStart != returnEnd)
            {
                returnStart.ResetMemory();
                returnStart = returnStart.NextSegment;
            }
        }

        private int WriteCharactersToOutput(ref ReadResult readResult)
        {
            var charactersUsed = 0;
            var bytesUsed = 0;
            var span = _readTail.AvailableMemory.Span;
            foreach (var item in readResult.Buffer)
            {
                _decoder.Convert(item.Span, span, true, out bytesUsed, out charactersUsed, out var completed);
                if (_readTail.AvailableMemory.Length == charactersUsed)
                {
                    break;
                }
                span = span.Slice(charactersUsed);
            }

            _readTail.End += charactersUsed;
            _bufferedCharacters += charactersUsed;
            _reader.AdvanceTo(readResult.Buffer.End);

            return charactersUsed;
        }

        private void AllocateReadTail()
        {
            if (_readHead == null)
            {
                Debug.Assert(_readTail == null);
                _readHead = AllocateSegment();
                _readTail = _readHead;
            }
            else if (_readTail.WritableBytes < _minimumReadThreshold)
            {
                CreateNewTailSegment();
            }
        }

        private void CreateNewTailSegment()
        {
            BufferSegment<char> nextSegment = AllocateSegment();
            _readTail.SetNext(nextSegment);
            _readTail = nextSegment;
        }

        private BufferSegment<char> AllocateSegment()
        {
            var nextSegment = new BufferSegment<char>();

            nextSegment.SetMemory(_charPool.Rent(_rentedCharPoolLength));
            return nextSegment;
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}
