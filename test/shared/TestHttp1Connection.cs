// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;

namespace Microsoft.AspNetCore.Testing
{
    public class TestHttp1Connection : Http1Connection
    {
        public TestHttp1Connection(Http1ConnectionContext context)
            : base(context)
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
