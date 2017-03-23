// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Server.Kestrel.Internal.Http;

namespace Microsoft.AspNetCore.Testing
{
    public class TestFrame<TContext> : Frame<TContext>
    {
        public TestFrame(IHttpApplication<TContext> application, ConnectionContext context)
        : base(application, context)
        {
        }

        public HttpVersion HttpVersionEnum
        {
            get => _httpVersion;
            set => _httpVersion = value;
        }

        public bool KeepAlive
        {
            get => _keepAlive;
            set => _keepAlive = value;
        }

        public Task ProduceEndAsync()
        {
            return ProduceEnd();
        }
    }
}