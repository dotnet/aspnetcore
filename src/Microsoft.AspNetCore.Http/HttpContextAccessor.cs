// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading;

namespace Microsoft.AspNetCore.Http
{
    public class HttpContextAccessor : IHttpContextAccessor
    {
        private static AsyncLocal<HttpContext> _httpContextCurrent = new AsyncLocal<HttpContext>();

        public HttpContext HttpContext
        {
            get
            {
                return _httpContextCurrent.Value;
            }
            set
            {
                _httpContextCurrent.Value = value;
            }
        }
    }
}
