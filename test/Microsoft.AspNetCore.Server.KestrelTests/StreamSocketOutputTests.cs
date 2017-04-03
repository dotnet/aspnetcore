// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using Microsoft.AspNetCore.Server.Kestrel.Adapter.Internal;
using Microsoft.AspNetCore.Server.Kestrel.Internal.System.IO.Pipelines;
using Xunit;

namespace Microsoft.AspNetCore.Server.KestrelTests
{
    public class StreamSocketOutputTests
    {
        [Fact]
        public void DoesNotThrowForNullBuffers()
        {
            // This test was added because SslStream throws if passed null buffers with (count == 0)
            // Which happens if ProduceEnd is called in Frame without _responseStarted == true
            // As it calls ProduceStart with write immediate == true
            // This happens in WebSocket Upgrade over SSL
            using (var factory = new PipeFactory())
            {
                var socketOutput = new StreamSocketOutput(new ThrowsOnNullWriteStream(), factory.Create());

                // Should not throw
                socketOutput.Write(default(ArraySegment<byte>), true);

                Assert.True(true);

                socketOutput.Dispose();
            }
        }

        private class ThrowsOnNullWriteStream : Stream
        {
            public override bool CanRead
            {
                get
                {
                    throw new NotImplementedException();
                }
            }

            public override bool CanSeek
            {
                get
                {
                    throw new NotImplementedException();
                }
            }

            public override bool CanWrite
            {
                get
                {
                    throw new NotImplementedException();
                }
            }

            public override long Length
            {
                get
                {
                    throw new NotImplementedException();
                }
            }

            public override long Position
            {
                get
                {
                    throw new NotImplementedException();
                }

                set
                {
                    throw new NotImplementedException();
                }
            }

            public override void Flush()
            {
                throw new NotImplementedException();
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                throw new NotImplementedException();
            }

            public override long Seek(long offset, SeekOrigin origin)
            {
                throw new NotImplementedException();
            }

            public override void SetLength(long value)
            {
                throw new NotImplementedException();
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
                if (buffer == null)
                {
                    throw new ArgumentNullException(nameof(buffer));
                }
            }
        }
    }
}
