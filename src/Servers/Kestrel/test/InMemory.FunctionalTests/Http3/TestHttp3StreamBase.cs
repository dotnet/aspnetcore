// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.IO;
using System.IO.Pipelines;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http3;
using Xunit;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Tests
{
    public partial class Http3TestBase
    {
        internal class TestHttp3StreamBase
        {
            protected IDuplexPipe _application;
            protected IDuplexPipe _transport;

            protected Http3TestBase _testBase;
            protected Http3Connection _connection;
            protected long _bytesReceived;

            protected Task SendAsync(ReadOnlySpan<byte> span)
            {
                var writableBuffer = _application.Output;
                writableBuffer.Write(span);
                return FlushAsync(writableBuffer);
            }

            protected static async Task FlushAsync(PipeWriter writableBuffer)
            {
                await writableBuffer.FlushAsync().AsTask().DefaultTimeout();
            }

            internal async Task<Http3FrameWithPayload> ReceiveFrameAsync()
            {
                var frame = new Http3FrameWithPayload();

                while (true)
                {
                    var result = await _application.Input.ReadAsync().AsTask().DefaultTimeout();
                    var buffer = result.Buffer;
                    var consumed = buffer.Start;
                    var examined = buffer.Start;
                    var copyBuffer = buffer;

                    try
                    {
                        Assert.True(buffer.Length > 0);

                        if (Http3FrameReader.TryReadFrame(ref buffer, frame, out var framePayload))
                        {
                            consumed = examined = framePayload.End;
                            frame.Payload = framePayload.ToArray();
                            return frame;
                        }
                        else
                        {
                            examined = buffer.End;
                        }

                        if (result.IsCompleted)
                        {
                            throw new IOException("The reader completed without returning a frame.");
                        }
                    }
                    finally
                    {
                        _bytesReceived += copyBuffer.Slice(copyBuffer.Start, consumed).Length;
                        _application.Input.AdvanceTo(consumed, examined);
                    }
                }
            }
        }
    }
}
