using System;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Net.Http;
using System.Net.Http.QPack;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http3;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Tests
{
    public partial class Http3TestBase
    {
        internal class TestHttp3RequestStream : TestHttp3StreamBase, IHttpHeadersHandler
        {
            internal TestStreamContext StreamContext { get; }

            public bool CanRead => true;
            public bool CanWrite => true;

            public long StreamId => 0;

            internal byte[] HeaderEncodingBuffer { get; set; }

            private QPackEncoder _qpackEncoder = new QPackEncoder();
            private QPackDecoder _qpackDecoder = new QPackDecoder(8192);
            protected readonly Dictionary<string, string> _decodedHeaders = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            public TestHttp3RequestStream(Http3TestBase testBase, Http3Connection connection)
            {
                _testBase = testBase;
                _connection = connection;
                var inputPipeOptions = GetInputPipeOptions(_testBase._serviceContext, _testBase._memoryPool, PipeScheduler.ThreadPool);
                var outputPipeOptions = GetOutputPipeOptions(_testBase._serviceContext, _testBase._memoryPool, PipeScheduler.ThreadPool);

                var pair = DuplexPipe.CreateConnectionPair(inputPipeOptions, outputPipeOptions);
                _transport = pair.Transport;
                _application = pair.Application;
                HeaderEncodingBuffer = new byte[testBase._serviceContext.ServerOptions.Limits.MaxRequestHeadersTotalSize];
                StreamContext = new TestStreamContext(canRead: true, canWrite: true, pair.Transport, pair.Application);
            }

            public async Task<bool> SendHeadersAsync(IEnumerable<KeyValuePair<string, string>> headers, bool endStream = false)
            {
                var outputWriter = _application.Output;
                var frame = new Http3RawFrame();
                frame.PrepareHeaders();
                var buffer = HeaderEncodingBuffer.AsMemory();
                var done = _qpackEncoder.BeginEncode(headers, buffer.Span, out var length);
                frame.Length = length;

                // TODO may want to modify behavior of input frames to mock different client behavior (client can send anything).
                Http3FrameWriter.WriteHeader(frame, outputWriter);
                await SendAsync(buffer.Span.Slice(0, length));

                if (endStream)
                {
                    await _application.Output.CompleteAsync();
                }

                return done;
            }

            internal async Task SendDataAsync(Memory<byte> data, bool endStream = false)
            {
                var outputWriter = _application.Output;
                var frame = new Http3RawFrame();
                frame.PrepareData();
                frame.Length = data.Length;
                Http3FrameWriter.WriteHeader(frame, outputWriter);
                await SendAsync(data.Span);

                if (endStream)
                {
                    await _application.Output.CompleteAsync();
                }
            }

            internal async Task<Dictionary<string, string>> ExpectHeadersAsync()
            {
                var http3WithPayload = await ReceiveFrameAsync();
                _qpackDecoder.Decode(http3WithPayload.PayloadSequence, this);
                return _decodedHeaders;
            }

            internal async Task<Memory<byte>> ExpectDataAsync()
            {
                var http3WithPayload = await ReceiveFrameAsync();
                return http3WithPayload.Payload;
            }

            internal async Task ExpectReceiveEndOfStream()
            {
                var result = await _application.Input.ReadAsync().AsTask().DefaultTimeout();
                Assert.True(result.IsCompleted);
            }

            public void OnHeader(ReadOnlySpan<byte> name, ReadOnlySpan<byte> value)
            {
                _decodedHeaders[name.GetAsciiStringNonNullCharacters()] = value.GetAsciiOrUTF8StringNonNullCharacters();
            }

            public void OnHeadersComplete(bool endHeaders)
            {
            }

            public void OnStaticIndexedHeader(int index)
            {
                var knownHeader = H3StaticTable.Instance[index];
                _decodedHeaders[((Span<byte>)knownHeader.Name).GetAsciiStringNonNullCharacters()] = HttpUtilities.GetAsciiOrUTF8StringNonNullCharacters(knownHeader.Value);
            }

            public void OnStaticIndexedHeader(int index, ReadOnlySpan<byte> value)
            {
                _decodedHeaders[((Span<byte>)H3StaticTable.Instance[index].Name).GetAsciiStringNonNullCharacters()] = value.GetAsciiOrUTF8StringNonNullCharacters();
            }

            internal async Task WaitForStreamErrorAsync(Http3ErrorCode protocolError, string expectedErrorMessage)
            {
                var readResult = await _application.Input.ReadAsync();
                _testBase.Logger.LogTrace("Input is completed");

                Assert.True(readResult.IsCompleted);
                Assert.Equal((long)protocolError, StreamContext.Error);

                if (expectedErrorMessage != null)
                {
                    Assert.Contains(_testBase.TestApplicationErrorLogger.Messages, m => m.Exception?.Message.Contains(expectedErrorMessage) ?? false);
                }
            }
        }
    }
}
