// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.HeaderPropagation
{
    /// <summary>
    /// Define the configuration of a header for the <see cref="HeaderPropagationMiddleware"/>.
    /// </summary>
    public class HeaderPropagationEntry
    {
        /// <summary>
        /// Gets or sets the name of the header to be used by the <see cref="HeaderPropagationMessageHandler"/> for the
        /// outbound http requests.
        /// </summary>
        /// <remarks>
        /// If <see cref="ValueFactory"/> is present, the value of the header in the outbound calls will be the one
        /// returned by the factory or, if the factory returns an empty value, the header will be omitted.
        /// Otherwise, it will be the value of the header in the incoming request named as the key of this entry in
        /// <see cref="HeaderPropagationOptions.Headers"/> or, if missing or empty, the value specified in
        /// <see cref="DefaultValue"/> or, if the <see cref="DefaultValue"/> is empty, it will not be
        /// added to the outbound calls.
        /// </remarks>
        public string OutboundHeaderName { get; set; }

        /// <summary>
        /// Gets or sets the default value to be used when the header in the incoming request is missing or empty.
        /// </summary>
        /// <remarks>
        /// This value is ignored when <see cref="ValueFactory"/> is set.
        /// When it is <see cref="StringValues.Empty"/> it has no effect and, if the header is missing or empty in the
        /// incoming request, it will not be added to outbound calls.
        /// </remarks>
        public StringValues DefaultValue { get; set; }

        /// <summary>
        /// Gets or sets the value factory to be used.
        /// It gets as input the inbound header name for this entry as defined in
        /// <see cref="HeaderPropagationOptions.Headers"/> and the <see cref="HttpContext"/> of the current request.
        /// </summary>
        /// <remarks>
        /// When present, the factory is the only method used to set the value.
        /// The factory should return <see cref="StringValues.Empty"/> to not add the header.
        /// When not present, the value will be taken from the header in the incoming request named as the key of this
        /// entry in <see cref="HeaderPropagationOptions.Headers"/> or, if missing or empty, it will be the values
        /// specified in <see cref="DefaultValue"/> or, if the <see cref="DefaultValue"/> is empty, the header will not
        /// be added to the outbound calls.
        /// Please note the factory is called only once per incoming request and the same value will be used by all the
        /// outbound calls.
        /// </remarks>
        public Func<string, HttpContext, StringValues> ValueFactory { get; set; }
    }
}
