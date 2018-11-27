// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Internal;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Mvc
{
    /// <summary>
    /// Sets the specified limits to the <see cref="HttpRequest.Form"/>.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class RequestFormLimitsAttribute : Attribute, IFilterFactory, IOrderedFilter
    {
        /// <summary>
        /// Gets the order value for determining the order of execution of filters. Filters execute in
        /// ascending numeric value of the <see cref="Order"/> property.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Filters are executed in an ordering determined by an ascending sort of the <see cref="Order"/> property.
        /// </para>
        /// <para>
        /// The default Order for this attribute is 900 because it must run before ValidateAntiForgeryTokenAttribute and
        /// after any filter which does authentication or login in order to allow them to behave as expected (ie Unauthenticated or Redirect instead of 400).
        /// </para>
        /// <para>
        /// Look at <see cref="IOrderedFilter.Order"/> for more detailed info.
        /// </para>
        /// </remarks>
        public int Order { get; set; } = 900;

        /// <inheritdoc />
        public bool IsReusable => true;

        // Internal for unit testing
        internal FormOptions FormOptions { get; } = new FormOptions();

        /// <summary>
        /// Enables full request body buffering. Use this if multiple components need to read the raw stream.
        /// The default value is false.
        /// </summary>
        public bool BufferBody
        {
            get => FormOptions.BufferBody;
            set => FormOptions.BufferBody = value;
        }

        /// <summary>
        /// If <see cref="BufferBody"/> is enabled, this many bytes of the body will be buffered in memory.
        /// If this threshold is exceeded then the buffer will be moved to a temp file on disk instead.
        /// This also applies when buffering individual multipart section bodies.
        /// </summary>
        public int MemoryBufferThreshold
        {
            get => FormOptions.MemoryBufferThreshold;
            set => FormOptions.MemoryBufferThreshold = value;
        }

        /// <summary>
        /// If <see cref="BufferBody"/> is enabled, this is the limit for the total number of bytes that will
        /// be buffered. Forms that exceed this limit will throw an <see cref="InvalidDataException"/> when parsed.
        /// </summary>
        public long BufferBodyLengthLimit
        {
            get => FormOptions.BufferBodyLengthLimit;
            set => FormOptions.BufferBodyLengthLimit = value;
        }

        /// <summary>
        /// A limit for the number of form entries to allow.
        /// Forms that exceed this limit will throw an <see cref="InvalidDataException"/> when parsed.
        /// </summary>
        public int ValueCountLimit
        {
            get => FormOptions.ValueCountLimit;
            set => FormOptions.ValueCountLimit = value;
        }

        /// <summary>
        /// A limit on the length of individual keys. Forms containing keys that exceed this limit will
        /// throw an <see cref="InvalidDataException"/> when parsed.
        /// </summary>
        public int KeyLengthLimit
        {
            get => FormOptions.KeyLengthLimit;
            set => FormOptions.KeyLengthLimit = value;
        }

        /// <summary>
        /// A limit on the length of individual form values. Forms containing values that exceed this
        /// limit will throw an <see cref="InvalidDataException"/> when parsed.
        /// </summary>
        public int ValueLengthLimit
        {
            get => FormOptions.ValueLengthLimit;
            set => FormOptions.ValueLengthLimit = value;
        }

        /// <summary>
        /// A limit for the length of the boundary identifier. Forms with boundaries that exceed this
        /// limit will throw an <see cref="InvalidDataException"/> when parsed.
        /// </summary>
        public int MultipartBoundaryLengthLimit
        {
            get => FormOptions.MultipartBoundaryLengthLimit;
            set => FormOptions.MultipartBoundaryLengthLimit = value;
        }

        /// <summary>
        /// A limit for the number of headers to allow in each multipart section. Headers with the same name will
        /// be combined. Form sections that exceed this limit will throw an <see cref="InvalidDataException"/>
        /// when parsed.
        /// </summary>
        public int MultipartHeadersCountLimit
        {
            get => FormOptions.MultipartHeadersCountLimit;
            set => FormOptions.MultipartHeadersCountLimit = value;
        }

        /// <summary>
        /// A limit for the total length of the header keys and values in each multipart section.
        /// Form sections that exceed this limit will throw an <see cref="InvalidDataException"/> when parsed.
        /// </summary>
        public int MultipartHeadersLengthLimit
        {
            get => FormOptions.MultipartHeadersLengthLimit;
            set => FormOptions.MultipartHeadersLengthLimit = value;
        }

        /// <summary>
        /// A limit for the length of each multipart body. Forms sections that exceed this limit will throw an
        /// <see cref="InvalidDataException"/> when parsed.
        /// </summary>
        public long MultipartBodyLengthLimit
        {
            get => FormOptions.MultipartBodyLengthLimit;
            set => FormOptions.MultipartBodyLengthLimit = value;
        }

        /// <inheritdoc />
        public IFilterMetadata CreateInstance(IServiceProvider serviceProvider)
        {
            var filter = serviceProvider.GetRequiredService<RequestFormLimitsFilter>();
            filter.FormOptions = FormOptions;
            return filter;
        }
    }
}
