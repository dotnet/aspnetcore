// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.HttpLogging
{
    /// <summary>
    /// Options for the <see cref="HttpLoggingMiddleware"/>.
    /// </summary>
    public sealed class HttpLoggingOptions
    {
        /// <summary>
        /// Fields to log for the Request and Response. Defaults to logging request and response properties and headers.
        /// </summary>
        public HttpLoggingFields LoggingFields { get; set; } = HttpLoggingFields.RequestPropertiesAndHeaders | HttpLoggingFields.ResponsePropertiesAndHeaders;

        /// <summary>
        /// Request header values that are allowed to be logged.
        /// <p>
        /// If a request header is not present in the <see cref="RequestHeaders"/>,
        /// the header name will be logged with a redacted value.
        /// </p>
        /// </summary>
        public ISet<string> RequestHeaders { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            HeaderNames.Accept,
            HeaderNames.AcceptEncoding,
            HeaderNames.AcceptLanguage,
            HeaderNames.Allow,
            HeaderNames.Connection,
            HeaderNames.ContentLength,
            HeaderNames.ContentType,
            HeaderNames.Host,
            HeaderNames.UserAgent
        };

        /// <summary>
        /// Response header values that are allowed to be logged.
        /// <p>
        /// If a response header is not present in the <see cref="ResponseHeaders"/>,
        /// the header name will be logged with a redacted value.
        /// </p>
        /// </summary>
        public ISet<string> ResponseHeaders { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            HeaderNames.ContentLength,
            HeaderNames.ContentType,
            HeaderNames.TransferEncoding
        };

        /// <summary>
        /// Options for configuring encodings for a specific media type.
        /// <p>
        /// If the request or response do not match the supported media type,
        /// the response body will not be logged.
        /// </p>
        /// </summary>
        public MediaTypeOptions MediaTypeOptions { get; } = MediaTypeOptions.BuildDefaultMediaTypeOptions();

        /// <summary>
        /// Maximum request body size to log (in bytes). Defaults to 32 KB.
        /// </summary>
        public int RequestBodyLogLimit { get; set; } = 32 * 1024;

        /// <summary>
        /// Maximum response body size to log (in bytes). Defaults to 32 KB.
        /// </summary>
        public int ResponseBodyLogLimit { get; set; } = 32 * 1024;
    }
}
