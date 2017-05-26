// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Authentication
{
    /// <summary>
    /// Base class used by other context classes.
    /// </summary>
    public abstract class BaseContext
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="context">The request context.</param>
        protected BaseContext(HttpContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            HttpContext = context;
        }

        /// <summary>
        /// The context.
        /// </summary>
        public HttpContext HttpContext { get; }

        /// <summary>
        /// The request.
        /// </summary>
        public HttpRequest Request
        {
            get { return HttpContext.Request; }
        }

        /// <summary>
        /// The response.
        /// </summary>
        public HttpResponse Response
        {
            get { return HttpContext.Response; }
        }
    }
}
