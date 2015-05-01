// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
#if DNX451
using System.Runtime.Remoting.Messaging;
using System.Runtime.Remoting;
#elif DNXCORE50
using System.Threading;
#endif
using Microsoft.AspNet.Http;

namespace Microsoft.AspNet.Hosting
{
    public class HttpContextAccessor : IHttpContextAccessor
    {
#if DNX451
        private const string LogicalDataKey = "__HttpContext_Current__";

        public HttpContext HttpContext
        {
            get
            {
                var handle = CallContext.LogicalGetData(LogicalDataKey) as ObjectHandle;
                return handle != null ? handle.Unwrap() as HttpContext : null;
            }
            set
            {
                CallContext.LogicalSetData(LogicalDataKey, new ObjectHandle(value));
            }
        }

#elif DNXCORE50
        private AsyncLocal<HttpContext> _httpContextCurrent = new AsyncLocal<HttpContext>();
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
#endif
    }
}
