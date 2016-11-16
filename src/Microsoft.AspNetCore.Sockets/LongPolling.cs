// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO.Pipelines;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Sockets
{
    public class LongPolling : IHttpTransport
    {
        private readonly HttpConnection _channel;
        private readonly Connection _connection;

        public LongPolling(Connection connection)
        {
            _connection = connection;
            _channel = (HttpConnection)connection.Channel;
        }

        public async Task ProcessRequestAsync(HttpContext context)
        {
            var result = await _channel.Output.ReadAsync();
            var buffer = result.Buffer;

            if (buffer.IsEmpty && result.IsCompleted)
            {
                // Client should stop if it receives a 204
                context.Response.StatusCode = 204;
                return;
            }

            if (!buffer.IsEmpty)
            {
                try
                {
                    context.Response.ContentLength = buffer.Length;
                    await buffer.CopyToAsync(context.Response.Body);
                }
                finally
                {
                    _channel.Output.Advance(buffer.End);
                }
            }
        }
    }
}
