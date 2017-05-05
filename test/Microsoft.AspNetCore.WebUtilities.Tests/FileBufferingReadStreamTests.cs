// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.AspNetCore.WebUtilities
{
    public class FileBufferingReadStreamTests
    {
        private Stream MakeStream(int size)
        {
            // TODO: Fill with random data? Make readonly?
            return new MemoryStream(new byte[size]);
        }

        [Fact]
        public void FileBufferingReadStream_Properties_ExpectedValues()
        {
            var inner = MakeStream(1024 * 2);
            using (var stream = new FileBufferingReadStream(inner, 1024, null, Directory.GetCurrentDirectory()))
            {
                Assert.True(stream.CanRead);
                Assert.True(stream.CanSeek);
                Assert.False(stream.CanWrite);
                Assert.Equal(0, stream.Length); // Nothing buffered yet
                Assert.Equal(0, stream.Position);
                Assert.True(stream.InMemory);
                Assert.Null(stream.TempFileName);
            }
        }

        [Fact]
        public void FileBufferingReadStream_SyncReadUnderThreshold_DoesntCreateFile()
        {
            var inner = MakeStream(1024 * 2);
            using (var stream = new FileBufferingReadStream(inner, 1024 * 3, null, Directory.GetCurrentDirectory()))
            {
                var bytes = new byte[1000];
                var read0 = stream.Read(bytes, 0, bytes.Length);
                Assert.Equal(bytes.Length, read0);
                Assert.Equal(read0, stream.Length);
                Assert.Equal(read0, stream.Position);
                Assert.True(stream.InMemory);
                Assert.Null(stream.TempFileName);

                var read1 = stream.Read(bytes, 0, bytes.Length);
                Assert.Equal(bytes.Length, read1);
                Assert.Equal(read0 + read1, stream.Length);
                Assert.Equal(read0 + read1, stream.Position);
                Assert.True(stream.InMemory);
                Assert.Null(stream.TempFileName);

                var read2 = stream.Read(bytes, 0, bytes.Length);
                Assert.Equal(inner.Length - read0 - read1, read2);
                Assert.Equal(read0 + read1 + read2, stream.Length);
                Assert.Equal(read0 + read1 + read2, stream.Position);
                Assert.True(stream.InMemory);
                Assert.Null(stream.TempFileName);

                var read3 = stream.Read(bytes, 0, bytes.Length);
                Assert.Equal(0, read3);
            }
        }

        [Fact]
        public void FileBufferingReadStream_SyncReadOverThreshold_CreatesFile()
        {
            var inner = MakeStream(1024 * 2);
            string tempFileName;
            using (var stream = new FileBufferingReadStream(inner, 1024, null, GetCurrentDirectory()))
            {
                var bytes = new byte[1000];
                var read0 = stream.Read(bytes, 0, bytes.Length);
                Assert.Equal(bytes.Length, read0);
                Assert.Equal(read0, stream.Length);
                Assert.Equal(read0, stream.Position);
                Assert.True(stream.InMemory);
                Assert.Null(stream.TempFileName);

                var read1 = stream.Read(bytes, 0, bytes.Length);
                Assert.Equal(bytes.Length, read1);
                Assert.Equal(read0 + read1, stream.Length);
                Assert.Equal(read0 + read1, stream.Position);
                Assert.False(stream.InMemory);
                Assert.NotNull(stream.TempFileName);
                tempFileName = stream.TempFileName;
                Assert.True(File.Exists(tempFileName));

                var read2 = stream.Read(bytes, 0, bytes.Length);
                Assert.Equal(inner.Length - read0 - read1, read2);
                Assert.Equal(read0 + read1 + read2, stream.Length);
                Assert.Equal(read0 + read1 + read2, stream.Position);
                Assert.False(stream.InMemory);
                Assert.NotNull(stream.TempFileName);
                Assert.True(File.Exists(tempFileName));

                var read3 = stream.Read(bytes, 0, bytes.Length);
                Assert.Equal(0, read3);
            }

            Assert.False(File.Exists(tempFileName));
        }

        [Fact]
        public void FileBufferingReadStream_SyncReadWithInMemoryLimit_EnforcesLimit()
        {
            var inner = MakeStream(1024 * 2);
            using (var stream = new FileBufferingReadStream(inner, 1024, 900, Directory.GetCurrentDirectory()))
            {
                var bytes = new byte[500];
                var read0 = stream.Read(bytes, 0, bytes.Length);
                Assert.Equal(bytes.Length, read0);
                Assert.Equal(read0, stream.Length);
                Assert.Equal(read0, stream.Position);
                Assert.True(stream.InMemory);
                Assert.Null(stream.TempFileName);

                var exception = Assert.Throws<IOException>(() => stream.Read(bytes, 0, bytes.Length));
                Assert.Equal("Buffer limit exceeded.", exception.Message);
                Assert.True(stream.InMemory);
                Assert.Null(stream.TempFileName);
                Assert.False(File.Exists(stream.TempFileName));
            }
        }

        [Fact]
        public void FileBufferingReadStream_SyncReadWithOnDiskLimit_EnforcesLimit()
        {
            var inner = MakeStream(1024 * 2);
            string tempFileName;
            using (var stream = new FileBufferingReadStream(inner, 512, 1024, GetCurrentDirectory()))
            {
                var bytes = new byte[500];
                var read0 = stream.Read(bytes, 0, bytes.Length);
                Assert.Equal(bytes.Length, read0);
                Assert.Equal(read0, stream.Length);
                Assert.Equal(read0, stream.Position);
                Assert.True(stream.InMemory);
                Assert.Null(stream.TempFileName);

                var read1 = stream.Read(bytes, 0, bytes.Length);
                Assert.Equal(bytes.Length, read1);
                Assert.Equal(read0 + read1, stream.Length);
                Assert.Equal(read0 + read1, stream.Position);
                Assert.False(stream.InMemory);
                Assert.NotNull(stream.TempFileName);
                tempFileName = stream.TempFileName;
                Assert.True(File.Exists(tempFileName));

                var exception = Assert.Throws<IOException>(() => stream.Read(bytes, 0, bytes.Length));
                Assert.Equal("Buffer limit exceeded.", exception.Message);
                Assert.False(stream.InMemory);
                Assert.NotNull(stream.TempFileName);
                Assert.False(File.Exists(tempFileName));
            }

            Assert.False(File.Exists(tempFileName));
        }

        ///////////////////

        [Fact]
        public async Task FileBufferingReadStream_AsyncReadUnderThreshold_DoesntCreateFile()
        {
            var inner = MakeStream(1024 * 2);
            using (var stream = new FileBufferingReadStream(inner, 1024 * 3, null, Directory.GetCurrentDirectory()))
            {
                var bytes = new byte[1000];
                var read0 = await stream.ReadAsync(bytes, 0, bytes.Length);
                Assert.Equal(bytes.Length, read0);
                Assert.Equal(read0, stream.Length);
                Assert.Equal(read0, stream.Position);
                Assert.True(stream.InMemory);
                Assert.Null(stream.TempFileName);

                var read1 = await stream.ReadAsync(bytes, 0, bytes.Length);
                Assert.Equal(bytes.Length, read1);
                Assert.Equal(read0 + read1, stream.Length);
                Assert.Equal(read0 + read1, stream.Position);
                Assert.True(stream.InMemory);
                Assert.Null(stream.TempFileName);

                var read2 = await stream.ReadAsync(bytes, 0, bytes.Length);
                Assert.Equal(inner.Length - read0 - read1, read2);
                Assert.Equal(read0 + read1 + read2, stream.Length);
                Assert.Equal(read0 + read1 + read2, stream.Position);
                Assert.True(stream.InMemory);
                Assert.Null(stream.TempFileName);

                var read3 = await stream.ReadAsync(bytes, 0, bytes.Length);
                Assert.Equal(0, read3);
            }
        }

        [Fact]
        public async Task FileBufferingReadStream_AsyncReadOverThreshold_CreatesFile()
        {
            var inner = MakeStream(1024 * 2);
            string tempFileName;
            using (var stream = new FileBufferingReadStream(inner, 1024, null, GetCurrentDirectory()))
            {
                var bytes = new byte[1000];
                var read0 = await stream.ReadAsync(bytes, 0, bytes.Length);
                Assert.Equal(bytes.Length, read0);
                Assert.Equal(read0, stream.Length);
                Assert.Equal(read0, stream.Position);
                Assert.True(stream.InMemory);
                Assert.Null(stream.TempFileName);

                var read1 = await stream.ReadAsync(bytes, 0, bytes.Length);
                Assert.Equal(bytes.Length, read1);
                Assert.Equal(read0 + read1, stream.Length);
                Assert.Equal(read0 + read1, stream.Position);
                Assert.False(stream.InMemory);
                Assert.NotNull(stream.TempFileName);
                tempFileName = stream.TempFileName;
                Assert.True(File.Exists(tempFileName));

                var read2 = await stream.ReadAsync(bytes, 0, bytes.Length);
                Assert.Equal(inner.Length - read0 - read1, read2);
                Assert.Equal(read0 + read1 + read2, stream.Length);
                Assert.Equal(read0 + read1 + read2, stream.Position);
                Assert.False(stream.InMemory);
                Assert.NotNull(stream.TempFileName);
                Assert.True(File.Exists(tempFileName));

                var read3 = await stream.ReadAsync(bytes, 0, bytes.Length);
                Assert.Equal(0, read3);
            }

            Assert.False(File.Exists(tempFileName));
        }

        [Fact]
        public async Task FileBufferingReadStream_AsyncReadWithInMemoryLimit_EnforcesLimit()
        {
            var inner = MakeStream(1024 * 2);
            using (var stream = new FileBufferingReadStream(inner, 1024, 900, Directory.GetCurrentDirectory()))
            {
                var bytes = new byte[500];
                var read0 = await stream.ReadAsync(bytes, 0, bytes.Length);
                Assert.Equal(bytes.Length, read0);
                Assert.Equal(read0, stream.Length);
                Assert.Equal(read0, stream.Position);
                Assert.True(stream.InMemory);
                Assert.Null(stream.TempFileName);

                var exception = await Assert.ThrowsAsync<IOException>(() => stream.ReadAsync(bytes, 0, bytes.Length));
                Assert.Equal("Buffer limit exceeded.", exception.Message);
                Assert.True(stream.InMemory);
                Assert.Null(stream.TempFileName);
                Assert.False(File.Exists(stream.TempFileName));
            }
        }

        [Fact]
        public async Task FileBufferingReadStream_AsyncReadWithOnDiskLimit_EnforcesLimit()
        {
            var inner = MakeStream(1024 * 2);
            string tempFileName;
            using (var stream = new FileBufferingReadStream(inner, 512, 1024, GetCurrentDirectory()))
            {
                var bytes = new byte[500];
                var read0 = await stream.ReadAsync(bytes, 0, bytes.Length);
                Assert.Equal(bytes.Length, read0);
                Assert.Equal(read0, stream.Length);
                Assert.Equal(read0, stream.Position);
                Assert.True(stream.InMemory);
                Assert.Null(stream.TempFileName);

                var read1 = await stream.ReadAsync(bytes, 0, bytes.Length);
                Assert.Equal(bytes.Length, read1);
                Assert.Equal(read0 + read1, stream.Length);
                Assert.Equal(read0 + read1, stream.Position);
                Assert.False(stream.InMemory);
                Assert.NotNull(stream.TempFileName);
                tempFileName = stream.TempFileName;
                Assert.True(File.Exists(tempFileName));

                var exception = await Assert.ThrowsAsync<IOException>(() => stream.ReadAsync(bytes, 0, bytes.Length));
                Assert.Equal("Buffer limit exceeded.", exception.Message);
                Assert.False(stream.InMemory);
                Assert.NotNull(stream.TempFileName);
                Assert.False(File.Exists(tempFileName));
            }

            Assert.False(File.Exists(tempFileName));
        }

        private static string GetCurrentDirectory()
        {
            return AppContext.BaseDirectory;
        }
    }
}