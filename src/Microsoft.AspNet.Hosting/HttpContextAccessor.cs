// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
#if ASPNET50
using System.Runtime.Remoting.Messaging;
using System.Runtime.Remoting;
#elif ASPNETCORE50
using System.Threading;
#endif
using Microsoft.AspNet.Http;

namespace Microsoft.AspNet.Hosting
{
    public class HttpContextAccessor : IHttpContextAccessor
    {
#if ASPNET50
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

#elif ASPNETCORE50
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
