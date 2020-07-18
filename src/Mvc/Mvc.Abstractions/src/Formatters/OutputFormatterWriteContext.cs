// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.IO.Pipelines;
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
        public OutputFormatterWriteContext(HttpContext httpContext, Func<PipeWriter, Encoding, TextWriter> writerFactory, Type objectType, object @object)
            : base(httpContext)
        {
            if (writerFactory == null)
            {
                throw new ArgumentNullException(nameof(writerFactory));
            }

            WriterFactory = writerFactory;
            ObjectType = objectType;
            Object = @object;
        }

        /// <summary>
        /// <para>
        /// Gets or sets a delegate used to create a <see cref="TextWriter"/> for writing text to the response.
        /// </para>
        /// <para>
        /// Write to <see cref="HttpResponse.BodyWriter"/> directly to write binary data to the response.
        /// </para>
        /// </summary>
        /// <remarks>
        /// <para>
        /// The <see cref="TextWriter"/> created by this delegate will encode text and write to the
        /// <see cref="HttpResponse.BodyWriter"/> pipe. Call this delegate to create a <see cref="TextWriter"/>
        /// for writing text output to the response pipe.
        /// </para>
        /// <para>
        /// To implement a formatter that writes binary data to the response stream, do not use the
        /// <see cref="WriterFactory"/> delegate, and use <see cref="HttpResponse.BodyWriter"/> instead.
        /// </para>
        /// </remarks>
        public virtual Func<PipeWriter, Encoding, TextWriter> WriterFactory { get; protected set; }
    }
}
