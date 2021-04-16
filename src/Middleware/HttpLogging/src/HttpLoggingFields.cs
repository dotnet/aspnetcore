// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.HttpLogging
{
    /// <summary>
    /// Logging enum to enable different request and response logging fields.
    /// </summary>
    [Flags]
    public enum HttpLoggingFields : long
    {
        /// <summary>
        /// None
        /// </summary>
        None = 0x0,

        /// <summary>
        /// Request Path
        /// </summary>
        RequestPath = 0x1,

        /// <summary>
        /// Request Query String
        /// </summary>
        RequestQuery = 0x2,

        /// <summary>
        /// Request Protocol
        /// </summary>
        RequestProtocol = 0x4,

        /// <summary>
        /// Request Method
        /// </summary>
        RequestMethod = 0x8,

        /// <summary>
        /// Request Scheme
        /// </summary>
        RequestScheme = 0x10,

        /// <summary>
        /// Response Status Code
        /// </summary>
        ResponseStatusCode = 0x20,

        /// <summary>
        /// Request Headers
        /// </summary>
        RequestHeaders = 0x40,

        /// <summary>
        /// Response Headers
        /// </summary>
        ResponseHeaders = 0x80,

        /// <summary>
        /// Request Trailers
        /// </summary>
        RequestTrailers = 0x100,

        /// <summary>
        /// Response Trailers
        /// </summary>
        ResponseTrailers = 0x200,

        /// <summary>
        /// Request Body
        /// </summary>
        RequestBody = 0x400,

        /// <summary>
        /// Response Body
        /// </summary>
        ResponseBody = 0x800,

        /// <summary>
        /// Combination of request properties, including Path, Query, Protocol, Method, and Scheme
        /// </summary>
        RequestProperties = RequestPath | RequestQuery | RequestProtocol | RequestMethod | RequestScheme,

        /// <summary>
        /// Combination of Request Properties and Request Headers
        /// </summary>
        RequestPropertiesAndHeaders = RequestProperties | RequestHeaders,

        /// <summary>
        /// Combination of Response Properties and Response Headers
        /// </summary>
        ResponsePropertiesAndHeaders = ResponseStatusCode | ResponseHeaders,

        /// <summary>
        /// Entire Request
        /// </summary>
        Request = RequestPropertiesAndHeaders | RequestBody,

        /// <summary>
        /// Entire Response
        /// </summary>
        Response = ResponseStatusCode | ResponseHeaders | ResponseBody,

        /// <summary>
        /// Entire Request and Response
        /// </summary>
        All = Request | Response
    }
}
