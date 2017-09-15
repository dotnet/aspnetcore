// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;

namespace Microsoft.AspNetCore.Testing
{
    public class TestHttp1Connection<TContext> : Http1Connection<TContext>
    {
        public TestHttp1Connection(IHttpApplication<TContext> application, Http1ConnectionContext context)
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
