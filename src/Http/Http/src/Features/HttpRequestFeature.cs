// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;

namespace Microsoft.AspNetCore.Http.Features
{
    /// <summary>
    /// Default implementation for <see cref="IHttpRequestFeature"/>.
    /// </summary>
    public class HttpRequestFeature : IHttpRequestFeature
    {
        /// <summary>
        /// Initiaizes a new instance of <see cref="HttpRequestFeature"/>.
        /// </summary>
        public HttpRequestFeature()
        {
            Headers = new HeaderDictionary();
            Body = Stream.Null;
            Protocol = string.Empty;
            Scheme = string.Empty;
            Method = string.Empty;
            PathBase = string.Empty;
            Path = string.Empty;
            QueryString = string.Empty;
            RawTarget = string.Empty;
        }

        /// <inheritdoc />
        public string Protocol { get; set; }

        /// <inheritdoc />
        public string Scheme { get; set; }

        /// <inheritdoc />
        public string Method { get; set; }

        /// <inheritdoc />
        public string PathBase { get; set; }

        /// <inheritdoc />
        public string Path { get; set; }

        /// <inheritdoc />
        public string QueryString { get; set; }

        /// <inheritdoc />
        public string RawTarget { get; set; }

        /// <inheritdoc />
        public IHeaderDictionary Headers { get; set; }

        /// <inheritdoc />
        public Stream Body { get; set; }
    }
}
