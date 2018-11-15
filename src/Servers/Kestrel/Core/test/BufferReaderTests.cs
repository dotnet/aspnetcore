// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Buffers.Tests
{
    public abstract class ReadableBufferReaderFacts
    {
        public class Array : SingleSegment
        {
            public Array() : base(ReadOnlySequenceFactory.ArrayFactory) { }
            internal Array(ReadOnlySequenceFactory factory) : base(factory) { }
        }

        public class OwnedMemory : SingleSegment
        {
            public OwnedMemory() : base(ReadOnlySequenceFactory.OwnedMemoryFactory) { }
        }
        public class Memory : SingleSegment
        {
            public Memory() : base(ReadOnlySequenceFactory.MemoryFactory) { }
        }

        public class SingleSegment : SegmentPerByte
        {
            public SingleSegment() : base(ReadOnlySequenceFactory.SingleSegmentFactory) { }
            internal SingleSegment(ReadOnlySequenceFactory factory) : base(factory) { }

            [Fact]
            public void AdvanceSingleBufferSkipsBytes()
            {
                var reader = new BufferReader(Factory.CreateWithContent(new byte[] { 1, 2, 3, 4, 5 }));
                reader.Advance(2);
                Assert.Equal(2, reader.CurrentSegmentIndex);
                Assert.Equal(3, reader.CurrentSegment[reader.CurrentSegmentIndex]);
                Assert.Equal(3, reader.Peek());
                reader.Advance(2);
                Assert.Equal(5, reader.Peek());
                Assert.Equal(4, reader.CurrentSegmentIndex);
                Assert.Equal(5, reader.CurrentSegment[reader.CurrentSegmentIndex]);
            }

            [Fact]
            public void TakeReturnsByteAndMoves()
            {
                var reader = new BufferReader(Factory.CreateWithContent(new byte[] { 1, 2 }));
                Assert.Equal(0, reader.CurrentSegmentIndex);
                Assert.Equal(1, reader.CurrentSegment[reader.CurrentSegmentIndex]);
                Assert.Equal(1, reader.Read());
                Assert.Equal(1, reader.CurrentSegmentIndex);
                Assert.Equal(2, reader.CurrentSegment[reader.CurrentSegmentIndex]);
                Assert.Equal(2, reader.Read());
                Assert.Equal(-1, reader.Read());
            }
        }

        public class SegmentPerByte : ReadableBufferReaderFacts
        {
            public SegmentPerByte() : base(ReadOnlySequenceFactory.SegmentPerByteFactory) { }
            internal SegmentPerByte(ReadOnlySequenceFactory factory) : base(factory) { }
        }

        internal ReadOnlySequenceFactory Factory { get; }

        internal ReadableBufferReaderFacts(ReadOnlySequenceFactory factory)
        {
            Factory = factory;
        }

        [Fact]
        public void PeekReturnsByteWithoutMoving()
        {
            var reader = new BufferReader(Factory.CreateWithContent(new byte[] { 1, 2 }));
            Assert.Equal(1, reader.Peek());
            Assert.Equal(1, reader.Peek());
        }

        [Fact]
        public void CursorIsCorrectAtEnd()
        {
            var reader = new BufferReader(Factory.CreateWithContent(new byte[] { 1, 2 }));
            reader.Read();
            reader.Read();
            Assert.True(reader.End);
        }

        [Fact]
        public void CursorIsCorrectWithEmptyLastBlock()
        {
            var first = new BufferSegment(new byte[] { 1, 2 });
            var last = first.Append(new byte[4]);

            var reader = new BufferReader(new ReadOnlySequence<byte>(first, 0, last, 0));
            reader.Read();
            reader.Read();
            reader.Read();
            Assert.Same(last, reader.Position.GetObject());
            Assert.Equal(0, reader.Position.GetInteger());
            Assert.True(reader.End);
        }

        [Fact]
        public void PeekReturnsMinusOneByteInTheEnd()
        {
            var reader = new BufferReader(Factory.CreateWithContent(new byte[] { 1, 2 }));
            Assert.Equal(1, reader.Read());
            Assert.Equal(2, reader.Read());
            Assert.Equal(-1, reader.Peek());
        }

        [Fact]
        public void AdvanceToEndThenPeekReturnsMinusOne()
        {
            var reader = new BufferReader(Factory.CreateWithContent(new byte[] { 1, 2, 3, 4, 5 }));
            reader.Advance(5);
            Assert.True(reader.End);
            Assert.Equal(-1, reader.Peek());
        }

        [Fact]
        public void AdvancingPastLengthThrows()
        {
            var reader = new BufferReader(Factory.CreateWithContent(new byte[] { 1, 2, 3, 4, 5 }));
            try
            {
                reader.Advance(6);
                Assert.True(false);
            }
            catch (Exception ex)
            {
                Assert.True(ex is ArgumentOutOfRangeException);
            }
        }

        [Fact]
        public void CtorFindsFirstNonEmptySegment()
        {
            var buffer = Factory.CreateWithContent(new byte[] { 1 });
            var reader = new BufferReader(buffer);

            Assert.Equal(1, reader.Peek());
        }

        [Fact]
        public void EmptySegmentsAreSkippedOnMoveNext()
        {
            var buffer = Factory.CreateWithContent(new byte[] { 1, 2 });
            var reader = new BufferReader(buffer);

            Assert.Equal(1, reader.Peek());
            reader.Advance(1);
            Assert.Equal(2, reader.Peek());
        }

        [Fact]
        public void PeekGoesToEndIfAllEmptySegments()
        {
            var buffer = Factory.CreateOfSize(0);
            var reader = new BufferReader(buffer);

            Assert.Equal(-1, reader.Peek());
            Assert.True(reader.End);
        }

        [Fact]
        public void AdvanceTraversesSegments()
        {
            var buffer = Factory.CreateWithContent(new byte[] { 1, 2, 3 });
            var reader = new BufferReader(buffer);

            reader.Advance(2);
            Assert.Equal(3, reader.CurrentSegment[reader.CurrentSegmentIndex]);
            Assert.Equal(3, reader.Read());
        }

        [Fact]
        public void AdvanceThrowsPastLengthMultipleSegments()
        {
            var buffer = Factory.CreateWithContent(new byte[] { 1, 2, 3 });
            var reader = new BufferReader(buffer);

            try
            {
                reader.Advance(4);
                Assert.True(false);
            }
            catch (Exception ex)
            {
                Assert.True(ex is ArgumentOutOfRangeException);
            }
        }

        [Fact]
        public void TakeTraversesSegments()
        {
            var buffer = Factory.CreateWithContent(new byte[] { 1, 2, 3 });
            var reader = new BufferReader(buffer);

            Assert.Equal(1, reader.Read());
            Assert.Equal(2, reader.Read());
            Assert.Equal(3, reader.Read());
            Assert.Equal(-1, reader.Read());
        }

        [Fact]
        public void PeekTraversesSegments()
        {
            var buffer = Factory.CreateWithContent(new byte[] { 1, 2 });
            var reader = new BufferReader(buffer);

            Assert.Equal(1, reader.CurrentSegment[reader.CurrentSegmentIndex]);
            Assert.Equal(1, reader.Read());

            Assert.Equal(2, reader.CurrentSegment[reader.CurrentSegmentIndex]);
            Assert.Equal(2, reader.Peek());
            Assert.Equal(2, reader.Read());
            Assert.Equal(-1, reader.Peek());
            Assert.Equal(-1, reader.Read());
        }

        [Fact]
        public void PeekWorksWithEmptySegments()
        {
            var buffer = Factory.CreateWithContent(new byte[] { 1 });
            var reader = new BufferReader(buffer);

            Assert.Equal(0, reader.CurrentSegmentIndex);
            Assert.Equal(1, reader.CurrentSegment.Length);
            Assert.Equal(1, reader.Peek());
            Assert.Equal(1, reader.Read());
            Assert.Equal(-1, reader.Peek());
            Assert.Equal(-1, reader.Read());
        }

        [Fact]
        public void WorksWithEmptyBuffer()
        {
            var reader = new BufferReader(Factory.CreateWithContent(new byte[] { }));

            Assert.Equal(0, reader.CurrentSegmentIndex);
            Assert.Equal(0, reader.CurrentSegment.Length);
            Assert.Equal(-1, reader.Peek());
            Assert.Equal(-1, reader.Read());
        }

        [Theory]
        [InlineData(0, false)]
        [InlineData(5, false)]
        [InlineData(10, false)]
        [InlineData(11, true)]
        [InlineData(12, true)]
        [InlineData(15, true)]
        public void ReturnsCorrectCursor(int takes, bool end)
        {
            var readableBuffer = Factory.CreateWithContent(new byte[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 });
            var reader = new BufferReader(readableBuffer);
            for (int i = 0; i < takes; i++)
            {
                reader.Read();
            }

            var expected = end ? new byte[] { } : readableBuffer.Slice((long)takes).ToArray();
            Assert.Equal(expected, readableBuffer.Slice(reader.Position).ToArray());
        }

        [Fact]
        public void SlicingBufferReturnsCorrectCursor()
        {
            var buffer = Factory.CreateWithContent(new byte[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 });
            var sliced = buffer.Slice(2L);

            var reader = new BufferReader(sliced);
            Assert.Equal(sliced.ToArray(), buffer.Slice(reader.Position).ToArray());
            Assert.Equal(2, reader.Peek());
            Assert.Equal(0, reader.CurrentSegmentIndex);
        }

        [Fact]
        public void ReaderIndexIsCorrect()
        {
            var buffer = Factory.CreateWithContent(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 });
            var reader = new BufferReader(buffer);

            var counter = 1;
            while (!reader.End)
            {
                var span = reader.CurrentSegment;
                for (int i = reader.CurrentSegmentIndex; i < span.Length; i++)
                {
                    Assert.Equal(counter++, reader.CurrentSegment[i]);
                }
                reader.Advance(span.Length);
            }
            Assert.Equal(buffer.Length, reader.ConsumedBytes);
        }
    }

}
