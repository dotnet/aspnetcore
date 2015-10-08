// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNet.Cors.Infrastructure
{
    /// <summary>
    /// CORS-related constants.
    /// </summary>
    public static class CorsConstants
    {
        /// <summary>
        /// The HTTP method for the CORS preflight request.
        /// </summary>
        public static readonly string PreflightHttpMethod = "OPTIONS";

        /// <summary>
        /// The Origin request header.
        /// </summary>
        public static readonly string Origin = "Origin";

        /// <summary>
        /// The value for the Access-Control-Allow-Origin response header to allow all origins.
        /// </summary>
        public static readonly string AnyOrigin = "*";

        /// <summary>
        /// The Access-Control-Request-Method request header.
        /// </summary>
        public static readonly string AccessControlRequestMethod = "Access-Control-Request-Method";

        /// <summary>
        /// The Access-Control-Request-Headers request header.
        /// </summary>
        public static readonly string AccessControlRequestHeaders = "Access-Control-Request-Headers";

        /// <summary>
        /// The Access-Control-Allow-Origin response header.
        /// </summary>
        public static readonly string AccessControlAllowOrigin = "Access-Control-Allow-Origin";

        /// <summary>
        /// The Access-Control-Allow-Headers response header.
        /// </summary>
        public static readonly string AccessControlAllowHeaders = "Access-Control-Allow-Headers";

        /// <summary>
        /// The Access-Control-Expose-Headers response header.
        /// </summary>
        public static readonly string AccessControlExposeHeaders = "Access-Control-Expose-Headers";

        /// <summary>
        /// The Access-Control-Allow-Methods response header.
        /// </summary>
        public static readonly string AccessControlAllowMethods = "Access-Control-Allow-Methods";

        /// <summary>
        /// The Access-Control-Allow-Credentials response header.
        /// </summary>
        public static readonly string AccessControlAllowCredentials = "Access-Control-Allow-Credentials";

        /// <summary>
        /// The Access-Control-Max-Age response header.
        /// </summary>
        public static readonly string AccessControlMaxAge = "Access-Control-Max-Age";

        internal static readonly string[] SimpleRequestHeaders =
        {
            "Origin",
            "Accept",
            "Accept-Language",
            "Content-Language",
        };

        internal static readonly string[] SimpleResponseHeaders =
        {
            "Cache-Control",
            "Content-Language",
            "Content-Type",
            "Expires",
            "Last-Modified",
            "Pragma"
        };

        internal static readonly string[] SimpleMethods =
        {
            "GET",
            "HEAD",
            "POST"
        };
    }
}