// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Authorization
{
    public class AuthorizationMiddlewareContext
    {
        public HttpContext Context { get; set; }
        public Endpoint Endpoint { get; set; }
    }
}
