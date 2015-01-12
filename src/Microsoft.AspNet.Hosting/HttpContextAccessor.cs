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
        private HttpContext _value;

        public bool IsRootContext { get; set; }

        public HttpContext Value
        {
            get
            {
                return IsRootContext ? AccessRootHttpContext() : _value;
            }
        }

        public HttpContext SetValue(HttpContext value)
        {
            if (IsRootContext)
            {
                return ExchangeRootHttpContext(value);
            }
            var prior = _value;
            _value = value;
            return prior;
        }

#if ASPNET50
        private const string LogicalDataKey = "__HttpContext_Current__";
#elif ASPNETCORE50
        private static AsyncLocal<HttpContext> _httpContextCurrent = new AsyncLocal<HttpContext>();
#endif

        private static HttpContext AccessRootHttpContext()
        {
#if ASPNET50
            var handle = CallContext.LogicalGetData(LogicalDataKey) as ObjectHandle;
            return handle != null ? handle.Unwrap() as HttpContext : null;
#elif ASPNETCORE50
            return _httpContextCurrent.Value;
#else
            throw new Exception("TODO: CallContext not available");
#endif
        }

        private static HttpContext ExchangeRootHttpContext(HttpContext httpContext)
        {
#if ASPNET50
            var prior = CallContext.LogicalGetData(LogicalDataKey) as ObjectHandle;
            CallContext.LogicalSetData(LogicalDataKey, new ObjectHandle(httpContext));
            return prior != null ? prior.Unwrap() as HttpContext : null;
#elif ASPNETCORE50
            var prior = _httpContextCurrent.Value;
            _httpContextCurrent.Value = httpContext;
            return prior;
#else
            return null;
#endif
        }
    }
}