// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#if NET451
using System;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Messaging;
#elif NETSTANDARD1_3
using System.Threading;
#endif

namespace Microsoft.AspNetCore.Http
{
    public class HttpContextAccessor : IHttpContextAccessor
    {
#if NET451
        private static readonly string LogicalDataKey = "__HttpContext_Current__" + AppDomain.CurrentDomain.Id;

        public HttpContext HttpContext
        {
            get
            {
                var handle = CallContext.LogicalGetData(LogicalDataKey) as ObjectHandle;
                return handle?.Unwrap() as HttpContext;
            }
            set
            {
                CallContext.LogicalSetData(LogicalDataKey, new ObjectHandle(value));
            }
        }

#elif NETSTANDARD1_3
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
