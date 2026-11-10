// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.WebUtilities
{
    public class PooledMemoryStreamTests
    {
        [Fact]
        public void Constructor_DoesNotRentUntilCapacityIsNeeded()
        {
            var pool = new Mock<ArrayPool<byte>>(MockBehavior.Strict);
            using (var stream = new PooledMemoryStream(pool.Object, 16))
            {
                Assert.Equal(0, stream.Length);
                Assert.Equal(0, stream.Position);
                pool.Verify(p => p.Rent(It.IsAny<int>()), Times.Never);
            }
        }

        [Fact]
        public void Constructor_ValidatesArguments()
        {
            Assert.Throws<ArgumentNullException>(() => new PooledMemoryStream(null, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => new PooledMemoryStream(ArrayPool<byte>.Shared, -1));
        }

        [Fact]
        public void Write_RentsExactInitialSizeAndGrowsGeometricallyUpToMaximum()
        {
            var poolState = CreateExactSizePool();
            using (var stream = new PooledMemoryStream(poolState.Pool.Object, 10))
            {
                stream.Write(new byte[] { 1, 2, 3 }, 0, 3);
                stream.WriteByte(4);
                stream.Write(new byte[] { 5, 6, 7 }, 0, 3);

                Assert.Equal(new[] { 3, 6, 10 }, poolState.RentedSizes);
                Assert.Equal(2, poolState.ReturnedBuffers.Count);
                Assert.Equal(new byte[] { 1, 2, 3, 4, 5, 6, 7 }, GetWrittenBytes(stream));
            }
        }

        [Fact]
        public void Write_WhenRequiredSizeIsMoreThanDouble_GrowsToExactRequiredSize()
        {
            var poolState = CreateExactSizePool();
            using (var stream = new PooledMemoryStream(poolState.Pool.Object, 20))
            {
                stream.Write(new byte[] { 1, 2 }, 0, 2);
                stream.SetLength(7);

                Assert.Equal(new[] { 2, 7 }, poolState.RentedSizes);
                Assert.Equal(new byte[] { 1, 2, 0, 0, 0, 0, 0 }, GetWrittenBytes(stream));
            }
        }

        [Fact]
        public void Growth_PreservesWrittenDataAndCurrentPosition()
        {
            var poolState = CreateExactSizePool();
            using (var stream = new PooledMemoryStream(poolState.Pool.Object, 16))
            {
                stream.Write(new byte[] { 1, 2, 3, 4 }, 0, 4);
                stream.Position = 2;
                stream.Write(new byte[] { 9, 8, 7, 6 }, 0, 4);

                Assert.Equal(6, stream.Position);
                Assert.Equal(6, stream.Length);
                Assert.Equal(new byte[] { 1, 2, 9, 8, 7, 6 }, GetWrittenBytes(stream));
            }
        }

        [Fact]
        public void Read_SupportsArrayOverload()
        {
            using (var stream = CreateStreamWithData(new byte[] { 1, 2, 3, 4, 5 }))
            {
                stream.Position = 0;
                var array = new byte[5];

                var firstRead = stream.Read(array, 1, 2);
                var secondRead = stream.Read(array, 3, 2);

                Assert.Equal(2, firstRead);
                Assert.Equal(2, secondRead);
                Assert.Equal(new byte[] { 0, 1, 2, 3, 4 }, array);
                Assert.Equal(4, stream.Position);
            }
        }

        [Fact]
        public async Task ReadAsync_SupportsArrayOverload()
        {
            using (var stream = CreateStreamWithData(new byte[] { 1, 2, 3, 4, 5 }))
            {
                stream.Position = 0;
                var array = new byte[5];

                var firstRead = await stream.ReadAsync(array, 1, 2, CancellationToken.None);
                var secondRead = await stream.ReadAsync(array, 3, 2, CancellationToken.None);

                Assert.Equal(2, firstRead);
                Assert.Equal(2, secondRead);
                Assert.Equal(new byte[] { 0, 1, 2, 3, 4 }, array);
                Assert.Equal(4, stream.Position);
            }
        }

        [Fact]
        public async Task WriteAsync_SupportsArrayOverload()
        {
            using (var stream = new PooledMemoryStream(ArrayPool<byte>.Shared, 8))
            {
                var data = new byte[] { 0, 1, 2, 3, 4 };

                await stream.WriteAsync(data, 1, 2, CancellationToken.None);
                await stream.WriteAsync(data, 3, 2, CancellationToken.None);

                Assert.Equal(new byte[] { 1, 2, 3, 4 }, GetWrittenBytes(stream));
                Assert.Equal(4, stream.Position);
            }
        }

        [Theory]
        [InlineData(SeekOrigin.Begin, 2, 2)]
        [InlineData(SeekOrigin.Current, -1, 1)]
        [InlineData(SeekOrigin.End, -2, 3)]
        public void Seek_ValidOffset_UpdatesPosition(SeekOrigin origin, long offset, long expected)
        {
            using (var stream = CreateStreamWithData(new byte[] { 1, 2, 3, 4, 5 }))
            {
                stream.Position = 2;

                var position = stream.Seek(offset, origin);

                Assert.Equal(expected, position);
                Assert.Equal(expected, stream.Position);
                Assert.Equal(5, stream.Length);
            }
        }

        [Fact]
        public void Position_CanMovePastEndWithoutChangingLength()
        {
            using (var stream = CreateStreamWithData(new byte[] { 1, 2, 3 }))
            {
                stream.Position = 10;

                Assert.Equal(10, stream.Position);
                Assert.Equal(3, stream.Length);
                Assert.Equal(-1, stream.ReadByte());
                Assert.Equal(10, stream.Position);
            }
        }

        [Fact]
        public void PositionAndSeek_ValidateBoundaries()
        {
            using (var stream = new PooledMemoryStream(ArrayPool<byte>.Shared, 8))
            {
                Assert.Throws<ArgumentOutOfRangeException>(() => stream.Position = -1);
                Assert.Throws<ArgumentOutOfRangeException>(() => stream.Position = (long)int.MaxValue + 1);
                Assert.Throws<IOException>(() => stream.Seek(-1, SeekOrigin.Begin));
                Assert.Throws<ArgumentOutOfRangeException>(() => stream.Seek(0, (SeekOrigin)int.MaxValue));
            }
        }

        [Fact]
        public void SparseWrite_ZeroFillsGap()
        {
            using (var stream = CreateStreamWithData(new byte[] { 1, 2 }))
            {
                stream.Position = 5;

                stream.WriteByte(9);

                Assert.Equal(6, stream.Length);
                Assert.Equal(new byte[] { 1, 2, 0, 0, 0, 9 }, GetWrittenBytes(stream));
            }
        }

        [Fact]
        public void SetLength_GrowsWithZerosAndShrinksPosition()
        {
            using (var stream = CreateStreamWithData(new byte[] { 1, 2, 3 }))
            {
                stream.SetLength(6);
                Assert.Equal(new byte[] { 1, 2, 3, 0, 0, 0 }, GetWrittenBytes(stream));
                Assert.Equal(3, stream.Position);

                stream.Position = 5;
                stream.SetLength(2);

                Assert.Equal(2, stream.Length);
                Assert.Equal(2, stream.Position);
                Assert.Equal(new byte[] { 1, 2 }, GetWrittenBytes(stream));
            }
        }

        [Fact]
        public void SetLength_ValidatesBoundaries()
        {
            using (var stream = new PooledMemoryStream(ArrayPool<byte>.Shared, 8))
            {
                Assert.Throws<ArgumentOutOfRangeException>(() => stream.SetLength(-1));
                Assert.Throws<ArgumentOutOfRangeException>(() => stream.SetLength(9));
            }
        }

        [Fact]
        public void Write_ExceedingMaximumCapacityThrowsWithoutChangingContent()
        {
            var poolState = CreateExactSizePool();
            using (var stream = new PooledMemoryStream(poolState.Pool.Object, 4))
            {
                stream.Write(new byte[] { 1, 2, 3, 4 }, 0, 4);

                var exception = Assert.Throws<IOException>(() => stream.WriteByte(5));

                Assert.Equal("Memory threshold exceeded.", exception.Message);
                Assert.Equal(4, stream.Position);
                Assert.Equal(4, stream.Length);
                Assert.Equal(new byte[] { 1, 2, 3, 4 }, GetWrittenBytes(stream));
                Assert.Equal(new[] { 4 }, poolState.RentedSizes);
            }
        }

        [Fact]
        public void Write_WhenPositionPlusCountOverflowsThrowsWithoutRenting()
        {
            var pool = new Mock<ArrayPool<byte>>(MockBehavior.Strict);
            using (var stream = new PooledMemoryStream(pool.Object, int.MaxValue))
            {
                stream.Position = int.MaxValue;

                Assert.Throws<OverflowException>(() => stream.WriteByte(1));
                Assert.Equal(int.MaxValue, stream.Position);
                Assert.Equal(0, stream.Length);
            }
        }

        [Fact]
        public async Task ZeroByteOperations_DoNotRentOrChangeState()
        {
            var pool = new Mock<ArrayPool<byte>>(MockBehavior.Strict);
            using (var stream = new PooledMemoryStream(pool.Object, 8))
            {
                var buffer = new byte[0];

                stream.Write(buffer, 0, 0);
                Assert.Equal(0, stream.Read(buffer, 0, 0));
                await stream.WriteAsync(buffer, 0, 0, CancellationToken.None);
                Assert.Equal(0, await stream.ReadAsync(buffer, 0, 0, CancellationToken.None));
                stream.SetLength(0);
                stream.Flush();
                await stream.FlushAsync();

                Assert.Equal(0, stream.Position);
                Assert.Equal(0, stream.Length);
                pool.Verify(p => p.Rent(It.IsAny<int>()), Times.Never);
            }
        }

        [Fact]
        public void AsyncOperations_WithCanceledTokenThrowWithoutChangingState()
        {
            using (var stream = CreateStreamWithData(new byte[] { 1, 2, 3 }))
            {
                stream.Position = 1;
                var canceledToken = new CancellationToken(true);

                Assert.Throws<OperationCanceledException>(() =>
                {
                    stream.ReadAsync(new byte[1], 0, 1, canceledToken);
                });
                Assert.Throws<OperationCanceledException>(() =>
                {
                    stream.WriteAsync(new byte[] { 9 }, 0, 1, canceledToken);
                });

                Assert.Equal(1, stream.Position);
                Assert.Equal(new byte[] { 1, 2, 3 }, GetWrittenBytes(stream));
            }
        }

        [Fact]
        public void Buffer_ContainsLogicalContentAndReflectsOverwrites()
        {
            using (var stream = new PooledMemoryStream(ArrayPool<byte>.Shared, 16))
            {
                stream.Write(new byte[] { 1, 2, 3, 4 }, 0, 4);
                stream.Position = 1;
                stream.Write(new byte[] { 8, 9 }, 0, 2);

                Assert.Equal(4, stream.Length);
                Assert.Equal(new byte[] { 1, 8, 9, 4 }, GetWrittenBytes(stream));
            }
        }

        [Fact]
        public void Buffer_CanBeCopiedWithoutChangingPositionForSpill()
        {
            using (var stream = new PooledMemoryStream(ArrayPool<byte>.Shared, 16))
            {
                stream.Write(new byte[] { 1, 2 }, 0, 2);
                stream.Write(new byte[] { 3, 4, 5 }, 0, 3);
                stream.Position = 2;

                using (var destination = new MemoryStream())
                {
                    destination.Write(stream.Buffer, 0, (int)stream.Length);

                    Assert.Equal(new byte[] { 1, 2, 3, 4, 5 }, destination.ToArray());
                    Assert.Equal(2, stream.Position);
                }
            }
        }

        [Fact]
        public void Dispose_ReturnsEveryRentedBufferExactlyOnce()
        {
            var poolState = CreateExactSizePool();
            var stream = new PooledMemoryStream(poolState.Pool.Object, 8);
            stream.Write(new byte[] { 1, 2 }, 0, 2);
            stream.Write(new byte[] { 3, 4, 5 }, 0, 3);

            stream.Dispose();
            stream.Dispose();

            Assert.Equal(2, poolState.ReturnedBuffers.Count);
            Assert.Equal(2, poolState.ReturnedBuffers.Distinct().Count());
            poolState.Pool.Verify(p => p.Return(It.IsAny<byte[]>(), false), Times.Exactly(2));
        }

        [Fact]
        public void DisposedStream_ReportsClosedAndOperationsThrow()
        {
            var stream = CreateStreamWithData(new byte[] { 1, 2, 3 });
            stream.Dispose();

            Assert.False(stream.CanRead);
            Assert.False(stream.CanSeek);
            Assert.False(stream.CanWrite);
            Assert.Throws<ObjectDisposedException>(() => stream.Flush());
            Assert.Throws<ObjectDisposedException>(() => GC.KeepAlive(stream.Length));
            Assert.Throws<ObjectDisposedException>(() => GC.KeepAlive(stream.Position));
            Assert.Throws<ObjectDisposedException>(() => stream.Position = 0);
            Assert.Throws<ObjectDisposedException>(() => GC.KeepAlive(stream.Buffer));
            Assert.Throws<ObjectDisposedException>(() => stream.Read(new byte[1], 0, 1));
            Assert.Throws<ObjectDisposedException>(() => stream.Write(new byte[1], 0, 1));
            Assert.Throws<ObjectDisposedException>(() => stream.Seek(0, SeekOrigin.Begin));
            Assert.Throws<ObjectDisposedException>(() => stream.SetLength(0));
            Assert.Throws<ObjectDisposedException>(() =>
            {
                stream.ReadAsync(new byte[1], 0, 1, CancellationToken.None);
            });
            Assert.Throws<ObjectDisposedException>(() =>
            {
                stream.WriteAsync(new byte[1], 0, 1, CancellationToken.None);
            });
        }

        private static PooledMemoryStream CreateStreamWithData(byte[] data)
        {
            var stream = new PooledMemoryStream(ArrayPool<byte>.Shared, 32);
            stream.Write(data, 0, data.Length);

            return stream;
        }

        private static byte[] GetWrittenBytes(PooledMemoryStream stream)
        {
            var data = new byte[(int)stream.Length];
            if (data.Length > 0)
            {
                System.Buffer.BlockCopy(stream.Buffer, 0, data, 0, data.Length);
            }

            return data;
        }

        private static PoolState CreateExactSizePool()
        {
            var poolState = new PoolState();
            poolState.Pool.Setup(p => p.Rent(It.IsAny<int>()))
                .Returns((int size) =>
                {
                    poolState.RentedSizes.Add(size);
                    return new byte[size];
                });
            poolState.Pool.Setup(p => p.Return(It.IsAny<byte[]>(), It.IsAny<bool>()))
                .Callback((byte[] buffer, bool clear) => poolState.ReturnedBuffers.Add(buffer));

            return poolState;
        }

        private sealed class PoolState
        {
            public Mock<ArrayPool<byte>> Pool { get; } = new Mock<ArrayPool<byte>>();

            public List<int> RentedSizes { get; } = new List<int>();

            public List<byte[]> ReturnedBuffers { get; } = new List<byte[]>();
        }
    }
}
