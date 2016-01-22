// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
#if NET451
using System.Runtime.Remoting.Messaging;
using System.Runtime.Remoting;
#elif DOTNET5_4
using System.Threading;
#endif

namespace Microsoft.AspNet.Http.Internal
{
    public class HttpContextAccessor : IHttpContextAccessor
    {
#if NET451
        private const string LogicalDataKey = "__HttpContext_Current__";

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

#elif DOTNET5_4
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
