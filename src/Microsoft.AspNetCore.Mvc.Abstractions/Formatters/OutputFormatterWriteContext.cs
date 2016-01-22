// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Text;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Mvc.Formatters
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
        /// <param name="writerFactory">The delegate used to create a <see cref="TextWriter"/> for writing the response.</param>
        /// <param name="objectType">The <see cref="Type"/> of the object to write to the response.</param>
        /// <param name="object">The object to write to the response.</param>
        public OutputFormatterWriteContext(HttpContext httpContext, Func<Stream, Encoding, TextWriter> writerFactory, Type objectType, object @object)
        {
            if (httpContext == null)
            {
                throw new ArgumentNullException(nameof(httpContext));
            }

            if (writerFactory == null)
            {
                throw new ArgumentNullException(nameof(writerFactory));
            }

            HttpContext = httpContext;
            WriterFactory = writerFactory;
            ObjectType = objectType;
            Object = @object;
        }

        /// <summary>
        /// Gets or sets the <see cref="HttpContext"/> context associated with the current operation.
        /// </summary>
        public virtual HttpContext HttpContext { get; protected set; }

        /// <summary>
        /// Gets or sets a delegate used to create a <see cref="TextWriter"/> for writing the response.
        /// </summary>
        public virtual Func<Stream, Encoding, TextWriter> WriterFactory { get; protected set; }
    }
}
