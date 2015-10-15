// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Http;

namespace Microsoft.AspNet.Mvc.Formatters
{
    /// <summary>
    /// A context object for <see cref="IOutputFormatter.WriteAsync(OutputFormatterWriteContext)"/>.
    /// </summary>
    public class OutputFormatterWriteContext : OutputFormatterCanWriteContext
    {
        /// <summary>
        /// Creates a new <see cref="OutputFormatterWriteContext"/>.
        /// </summary>
        /// <param name="httpContext">The <see cref="Http.HttpContext"/> for the current request.</param>
        /// <param name="objectType">The <see cref="Type"/> of the object to write to the response.</param>
        /// <param name="@object">The object to write to the response.</param>
        public OutputFormatterWriteContext(HttpContext httpContext, Type objectType, object @object)
        {
            if (httpContext == null)
            {
                throw new ArgumentNullException(nameof(httpContext));
            }

            HttpContext = httpContext;
            ObjectType = objectType;
            Object = @object;
        }

        /// <summary>
        /// Gets or sets the <see cref="HttpContext"/> context associated with the current operation.
        /// </summary>
        public virtual HttpContext HttpContext { get; protected set; }
    }
}
