// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;

namespace Microsoft.AspNetCore.Http
{
    /// <summary>
    /// Provides an implementation of <see cref="IHttpContextAccessor" /> based on the current execution context. 
    /// </summary>
    public class HttpContextAccessor : IHttpContextAccessor
    {
        private static AsyncLocal<HttpContextHolder> _httpContextCurrent = new AsyncLocal<HttpContextHolder>();

        /// <inheritdoc/>
        public HttpContext? HttpContext
        {
            get
            {
                return  _httpContextCurrent.Value?.Context;
            }
            set
            {
                var holder = _httpContextCurrent.Value;
                if (holder != null)
                {
                    // Clear current HttpContext trapped in the AsyncLocals, as its done.
                    holder.Context = null;
                }

                if (value != null)
                {
                    // Use an object indirection to hold the HttpContext in the AsyncLocal,
                    // so it can be cleared in all ExecutionContexts when its cleared.
                    _httpContextCurrent.Value = new HttpContextHolder { Context = value };
                }
            }
        }

        private class HttpContextHolder
        {
            public HttpContext? Context;
        }
    }
}
