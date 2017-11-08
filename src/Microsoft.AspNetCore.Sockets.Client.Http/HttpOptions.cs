// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Net.Http;

namespace Microsoft.AspNetCore.Sockets.Client.Http
{
    public class HttpOptions
    {
        public HttpMessageHandler HttpMessageHandler { get; set; }
        public IReadOnlyCollection<KeyValuePair<string, string>> Headers { get; set; }
        public Func<string> JwtBearerTokenFactory { get; set; }
    }
}
