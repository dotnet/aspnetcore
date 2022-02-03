// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Builder;

/// <summary>
/// Options for configuring the <see cref="ExceptionHandlerMiddleware"/>.
/// </summary>
public class ExceptionHandlerOptions
{
    /// <summary>
    /// The path to the exception handling endpoint. This path will be used when executing
    /// the <see cref="ExceptionHandler"/>.
    /// </summary>
    public PathString ExceptionHandlingPath { get; set; }

    /// <summary>
    /// The <see cref="RequestDelegate" /> that will handle the exception. If this is not
    /// explicitly provided, the subsequent middleware pipeline will be used by default.
    /// </summary>
    public RequestDelegate? ExceptionHandler { get; set; }

    /// <summary>
    /// This value controls whether the <see cref="ExceptionHandlerMiddleware" /> should
    /// consider a response with a 404 status code to be a valid result of executing the
    /// <see cref="ExceptionHandler"/>. The default value is false and the middleware will
    /// consider 404 status codes to be an error on the server and will therefore rethrow
    /// the original exception.
    /// </summary>
    public bool AllowStatusCode404Response { get; set; }
}
