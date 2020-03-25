// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.Mvc.Formatters
{
    /// <summary>
    /// A context object for <see cref="IOutputFormatter.CanWriteResult(OutputFormatterCanWriteContext)"/>.
    /// </summary>
    public abstract class OutputFormatterCanWriteContext
    {
        /// <summary>
        /// Creates a new <see cref="OutputFormatterCanWriteContext"/>.
        /// </summary>
        /// <param name="httpContext">The <see cref="HttpContext"/> for the current request.</param>
        protected OutputFormatterCanWriteContext(HttpContext httpContext)
        {
            if (httpContext == null)
            {
                throw new ArgumentNullException(nameof(httpContext));
            }

            HttpContext = httpContext;
        }

        /// <summary>
        /// Gets or sets the <see cref="HttpContext"/> context associated with the current operation.
        /// </summary>
        public virtual HttpContext HttpContext { get; protected set; }

        /// <summary>
        /// Gets or sets the content type to write to the response.
        /// </summary>
        /// <remarks>
        /// An <see cref="IOutputFormatter"/> can set this value when its
        /// <see cref="IOutputFormatter.CanWriteResult(OutputFormatterCanWriteContext)"/> method is called,
        /// and expect to see the same value provided in
        /// <see cref="IOutputFormatter.WriteAsync(OutputFormatterWriteContext)"/>
        /// </remarks>
        public virtual StringSegment ContentType { get; set; }

        /// <summary>
        /// Gets or sets a value to indicate whether the content type was specified by server-side code.
        /// This allows <see cref="IOutputFormatter.CanWriteResult(OutputFormatterCanWriteContext)"/> to
        /// implement stricter filtering on content types that, for example, are being considered purely
        /// because of an incoming Accept header.
        /// </summary>
        public virtual bool ContentTypeIsServerDefined { get; set; }

        /// <summary>
        /// Gets or sets the object to write to the response.
        /// </summary>
        public virtual object Object { get; protected set; }

        /// <summary>
        /// Gets or sets the <see cref="Type"/> of the object to write to the response.
        /// </summary>
        public virtual Type ObjectType { get; protected set; }
    }
}
