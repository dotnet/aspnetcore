// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.HttpLogging
{
    /// <summary>
    /// Context for modifying the HttpResponse log.
    /// </summary>
    public class HttpResponseLoggingContext
    {
        internal HttpResponseLoggingContext(HttpContext context, HttpLoggingOptions options, IHeaderDictionary headers)
        {
            HttpContext = context;
            Options = options;
            Headers = headers;
        }

        /// <summary>
        /// The <see cref="HttpContext"/> for the response.
        /// </summary>
        public HttpContext HttpContext { get; }

        /// <summary>
        /// The <see cref="HttpLoggingOptions"/>.
        /// </summary>
        public HttpLoggingOptions Options { get; }

        /// <summary>
        /// What will be logged for the the <see cref="HttpResponse.StatusCode"/>.
        /// </summary>
        public string? StatusCode { get; set; }

        /// <summary>
        /// What will be logged for the the <see cref="HttpResponse.Headers"/>.
        /// Can be modified without modifying the <see cref="HttpResponse.Headers"/>.
        /// </summary>
        public IHeaderDictionary Headers { get; }

        private List<(string, string)>? _extra;

        /// <summary>
        /// Extra messages that will be logged.
        /// </summary>
        public List<(string, string)> Extra
        {
            get
            {
                _extra ??= new List<(string, string)>();
                return _extra;
            }
        }
    }
}
