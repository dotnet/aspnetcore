// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.Cors.Infrastructure;

/// <summary>
/// CORS-related constants.
/// </summary>
public static class CorsConstants
{
    /// <summary>
    /// The HTTP method for the CORS preflight request.
    /// </summary>
    public static readonly string PreflightHttpMethod = HttpMethods.Options;

    /// <summary>
    /// The Origin request header.
    /// </summary>
    public static readonly string Origin = HeaderNames.Origin;

    /// <summary>
    /// The value for the Access-Control-Allow-Origin response header to allow all origins.
    /// </summary>
    public static readonly string AnyOrigin = "*";

    /// <summary>
    /// The value for the Access-Control-Allow-Headers response header to allow all headers.
    /// </summary>
    public static readonly string AnyHeader = "*";

    /// <summary>
    /// The value for the Access-Control-Allow-Methods response header to allow all methods.
    /// </summary>
    public static readonly string AnyMethod = "*";

    /// <summary>
    /// The Access-Control-Request-Method request header.
    /// </summary>
    public static readonly string AccessControlRequestMethod = HeaderNames.AccessControlRequestMethod;

    /// <summary>
    /// The Access-Control-Request-Headers request header.
    /// </summary>
    public static readonly string AccessControlRequestHeaders = HeaderNames.AccessControlRequestHeaders;

    /// <summary>
    /// The Access-Control-Allow-Origin response header.
    /// </summary>
    public static readonly string AccessControlAllowOrigin = HeaderNames.AccessControlAllowOrigin;

    /// <summary>
    /// The Access-Control-Allow-Headers response header.
    /// </summary>
    public static readonly string AccessControlAllowHeaders = HeaderNames.AccessControlAllowHeaders;

    /// <summary>
    /// The Access-Control-Expose-Headers response header.
    /// </summary>
    public static readonly string AccessControlExposeHeaders = HeaderNames.AccessControlExposeHeaders;

    /// <summary>
    /// The Access-Control-Allow-Methods response header.
    /// </summary>
    public static readonly string AccessControlAllowMethods = HeaderNames.AccessControlAllowMethods;

    /// <summary>
    /// The Access-Control-Allow-Credentials response header.
    /// </summary>
    public static readonly string AccessControlAllowCredentials = HeaderNames.AccessControlAllowCredentials;

    /// <summary>
    /// The Access-Control-Max-Age response header.
    /// </summary>
    public static readonly string AccessControlMaxAge = HeaderNames.AccessControlMaxAge;
}
