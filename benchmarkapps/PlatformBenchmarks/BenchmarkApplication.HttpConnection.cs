// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.IO.Pipelines;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;

namespace PlatformBenchmarks
{
    public partial class BenchmarkApplication : IHttpConnection
    {
        private State _state;

        public PipeReader Reader { get; set; }
        public PipeWriter Writer { get; set; }

        private HttpParser<ParsingAdapter> Parser { get; } = new HttpParser<ParsingAdapter>();

        public async Task ExecuteAsync()
        {
            try
            {
                await ProcessRequestsAsync();

                Reader.Complete();
            }
            catch (Exception ex)
            {
                Reader.Complete(ex);
            }
            finally
            {
                Writer.Complete();
            }
        }

        private async Task ProcessRequestsAsync()
        {
            while (true)
            {
                var task = Reader.ReadAsync();

                if (!task.IsCompleted)
                {
                    // No more data in the input
                    await OnReadCompletedAsync();
                }

                var result = await task;
                if (!ParseHttpRequest(ref result))
                {
                    break;
                }

                if (_state == State.Body)
                {
                    await ProcessRequestAsync();

                    _state = State.StartLine;
                }
            }
        }

        // Should be `in` but ReadResult isn't readonly struct
        private bool ParseHttpRequest(ref ReadResult result)
        {
            var buffer = result.Buffer;
            var consumed = buffer.Start;
            var examined = buffer.End;
            var state = _state;

            if (!buffer.IsEmpty)
            {
                var parsingStartLine = state == State.StartLine;
                if (parsingStartLine)
                {
                    if (Parser.ParseRequestLine(new ParsingAdapter(this), buffer, out consumed, out examined))
                    {
                        state = State.Headers;
                    }
                }

                if (state == State.Headers)
                {
                    if (parsingStartLine)
                    {
                        buffer = buffer.Slice(consumed);
                    }

                    if (Parser.ParseHeaders(new ParsingAdapter(this), buffer, out consumed, out examined, out int consumedBytes))
                    {
                        state = State.Body;
                    }
                }

                if (state != State.Body && result.IsCompleted)
                {
                    ThrowUnexpectedEndOfData();
                }
            }
            else if (result.IsCompleted)
            {
                return false;
            }

            _state = state;
            Reader.AdvanceTo(consumed, examined);
            return true;
        }

        public void OnHeader(Span<byte> name, Span<byte> value)
        {
        }

        public async ValueTask OnReadCompletedAsync()
        {
            await Writer.FlushAsync();
        }

        private static void ThrowUnexpectedEndOfData()
        {
            throw new InvalidOperationException("Unexpected end of data!");
        }

        private enum State
        {
            StartLine,
            Headers,
            Body
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static BufferWriter<WriterAdapter> GetWriter(PipeWriter pipeWriter)
            => new BufferWriter<WriterAdapter>(new WriterAdapter(pipeWriter));

        private struct WriterAdapter : IBufferWriter<byte>
        {
            public PipeWriter Writer;

            public WriterAdapter(PipeWriter writer)
                => Writer = writer;

            public void Advance(int count)
                => Writer.Advance(count);

            public Memory<byte> GetMemory(int sizeHint = 0)
                => Writer.GetMemory(sizeHint);

            public Span<byte> GetSpan(int sizeHint = 0)
                => Writer.GetSpan(sizeHint);
        }

        private struct ParsingAdapter : IHttpRequestLineHandler, IHttpHeadersHandler
        {
            public BenchmarkApplication RequestHandler;

            public ParsingAdapter(BenchmarkApplication requestHandler)
                => RequestHandler = requestHandler;

            public void OnHeader(Span<byte> name, Span<byte> value)
                => RequestHandler.OnHeader(name, value);

            public void OnStartLine(HttpMethod method, HttpVersion version, Span<byte> target, Span<byte> path, Span<byte> query, Span<byte> customMethod, bool pathEncoded)
                => RequestHandler.OnStartLine(method, version, target, path, query, customMethod, pathEncoded);
        }
    }

}
