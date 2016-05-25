// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;

namespace Microsoft.AspNetCore.Http.Features
{
    /// <summary>
    /// Contains the details of a given request. These properties should all be mutable.
    /// None of these properties should ever be set to null.
    /// </summary>
    public interface IHttpRequestFeature
    {
        /// <summary>
        /// The HTTP-version as defined in RFC 7230. E.g. "HTTP/1.1"
        /// </summary>
        string Protocol { get; set; }

        /// <summary>
        /// The request uri scheme. E.g. "http" or "https". Note this value is not included
        /// in the original request, it is inferred by checking if the transport used a TLS
        /// connection or not.
        /// </summary>
        string Scheme { get; set; }

        /// <summary>
        /// The request method as defined in RFC 7230. E.g. "GET", "HEAD", "POST", etc..
        /// </summary>
        string Method { get; set; }

        /// <summary>
        /// The first portion of the request path associated with application root. The value
        /// is un-escaped. The value may be string.Empty.
        /// </summary>
        string PathBase { get; set; }

        /// <summary>
        /// The portion of the request path that identifies the requested resource. The value
        /// is un-escaped. The value may be string.Empty if <see cref="PathBase"/> contains the
        /// full path.
        /// </summary>
        string Path { get; set; }

        /// <summary>
        /// The query portion of the request-target as defined in RFC 7230. The value
        /// may be string.Empty. If not empty then the leading '?' will be included. The value
        /// is in its original form, without un-escaping.
        /// </summary>
        string QueryString { get; set; }

        /// <summary>
        /// The request target as it was sent in the HTTP request. This property contains the
        /// raw path and full query, as well as other request targets such as * for OPTIONS
        /// requests (https://tools.ietf.org/html/rfc7230#section-5.3).
        /// </summary>
        /// <remarks>
        /// This property is not used internally for routing or authorization decisions. It has not
        /// been UrlDecoded and care should be taken in its use.
        /// </remarks>
        string RawTarget { get; set; }

        /// <summary>
        /// Headers included in the request, aggregated by header name. The values are not split
        /// or merged across header lines. E.g. The following headers:
        /// HeaderA: value1, value2
        /// HeaderA: value3
        /// Result in Headers["HeaderA"] = { "value1, value2", "value3" }
        /// </summary>
        IHeaderDictionary Headers { get; set; }

        /// <summary>
        /// A <see cref="Stream"/> representing the request body, if any. Stream.Null may be used
        /// to represent an empty request body.
        /// </summary>
        Stream Body { get; set; }
    }
}
