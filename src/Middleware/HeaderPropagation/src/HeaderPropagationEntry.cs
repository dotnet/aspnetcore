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
        /// Gets or sets the name of the header to be used by the <see cref="HeaderPropagationMessageHandler"/> fot the outbound http requests.
        /// </summary>
        public string OutboundHeaderName { get; set; }

        /// <summary>
        /// Gets or sets the default values to be used when the header in the incoming request is missing or empty.
        /// </summary>
        public StringValues DefaultValues { get; set; }

        /// <summary>
        /// Gets or sets the value factory to be used.
        /// It gets as input the inbound header name for this entry as defined in the <see cref="HeaderPropagationOptions"/>
        /// and the <see cref="HttpContext"/> of the current request.
        /// </summary>
        /// <remarks>
        /// When present, the factory is the only method used to set the value.
        /// To not add the header, return <see cref="StringValues.Empty"/>.
        /// </remarks>
        public Func<string, HttpContext, StringValues> ValueFactory { get; set; }
    }
}
