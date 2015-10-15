// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Text;
using Microsoft.AspNet.Http;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNet.Mvc.Formatters
{
    /// <summary>
    /// Represents information used by a formatter for participating in
    /// output content negotiation and in writing out the response.
    /// </summary>
    public class OutputFormatterContext
    {
        /// <summary>
        /// The return value of the action method.
        /// </summary>
        public object Object { get; set; }

        /// <summary>
        /// The declared return type of the action.
        /// </summary>
        public Type DeclaredType { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="HttpContext"/> context associated with the current operation.
        /// </summary>
        public HttpContext HttpContext { get; set; }

        /// <summary>
        /// The encoding which is chosen by the selected formatter.
        /// </summary>
        public Encoding SelectedEncoding { get; set; }

        /// <summary>
        /// The content type which is chosen by the selected formatter.
        /// </summary>
        public MediaTypeHeaderValue SelectedContentType { get; set; }


        /// <summary>
        /// Gets or sets a flag to indicate that content-negotiation could not find a formatter based on the 
        /// information on the <see cref="Http.HttpRequest"/>.
        /// </summary>
        public bool? FailedContentNegotiation { get; set; }
    }
}
