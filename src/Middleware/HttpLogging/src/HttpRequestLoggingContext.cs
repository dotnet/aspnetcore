// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.HttpLogging
{
    /// <summary>
    /// Context for modifying the HttpRequest log.
    /// </summary>
    public class HttpRequestLoggingContext
    {
        internal HttpRequestLoggingContext(HttpContext context, HttpLoggingOptions options, IHeaderDictionary headers)
        {
            HttpContext = context;
            Options = options;
            Headers = headers;
        }

        /// <summary>
        /// The <see cref="HttpContext"/> for the request.
        /// </summary>
        public HttpContext HttpContext { get; }

        /// <summary>
        /// The <see cref="HttpLoggingOptions"/>.
        /// </summary>
        public HttpLoggingOptions Options { get; }

        /// <summary>
        /// What will be logged for the the <see cref="HttpRequest.Protocol"/>.
        /// </summary>
        public string? Protocol { get; set; }

        /// <summary>
        /// What will be logged for the the <see cref="HttpRequest.Method"/>.
        /// </summary>
        public string? Method { get; set; }

        /// <summary>
        /// What will be logged for the the <see cref="HttpRequest.Scheme"/>.
        /// </summary>
        public string? Scheme { get; set; }

        /// <summary>
        /// What will be logged for the the <see cref="HttpRequest.Path"/>.
        /// </summary>
        public string? Path { get; set; }

        /// <summary>
        /// What will be logged for the the <see cref="HttpRequest.PathBase"/>.
        /// </summary>
        public string? PathBase { get; set; }

        /// <summary>
        /// What will be logged for the the <see cref="HttpRequest.QueryString"/>.
        /// </summary>
        public string? Query { get; set; }

        /// <summary>
        /// What will be logged for the the <see cref="HttpRequest.Headers"/>.
        /// Can be modified without modifying the <see cref="HttpRequest.Headers"/>.
        /// </summary>
        public IHeaderDictionary Headers { get; }

        private List<(string, string)>? _extra;

        /// <summary>
        /// Extra messages that will be logged.
        /// </summary>
        public List<(string, string)> Extra => _extra ??= new List<(string, string)>();
    }
}
