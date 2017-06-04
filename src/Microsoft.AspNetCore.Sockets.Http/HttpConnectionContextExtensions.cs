// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Sockets
{
    public static class HttpConnectionContextExtensions
    {
        public static HttpContext GetHttpContext(this ConnectionContext connection)
        {
            return connection.Metadata.Get<HttpContext>(ConnectionMetadataNames.HttpContext);
        }
    }
}
