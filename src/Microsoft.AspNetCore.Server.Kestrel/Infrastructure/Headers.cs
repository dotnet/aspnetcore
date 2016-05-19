// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Text;
using Microsoft.AspNetCore.Server.Kestrel.Http;

namespace Microsoft.AspNetCore.Server.Kestrel.Infrastructure
{
    class Headers
    {
        public static readonly byte[] BytesServer = Encoding.ASCII.GetBytes("\r\nServer: Kestrel");

        private readonly KestrelServerOptions _options;

        public Headers(KestrelServerOptions options)
        {
            _options = options;
        }

        public void Initialize(DateHeaderValueManager dateValueManager)
        {
            var dateHeaderValues = dateValueManager.GetDateHeaderValues();
            ResponseHeaders.SetRawDate(dateHeaderValues.String, dateHeaderValues.Bytes);

            if (_options.AddServerHeader)
            {
                ResponseHeaders.SetRawServer("Kestrel", BytesServer);
            }
        }

        public FrameRequestHeaders RequestHeaders { get; } = new FrameRequestHeaders();
        public FrameResponseHeaders ResponseHeaders { get; } = new FrameResponseHeaders();

        public void Reset()
        {
            RequestHeaders.Reset();
            ResponseHeaders.Reset();
        }
    }
}
