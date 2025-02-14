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
    /// Gets or sets whether the handler needs to create a separate <see cref="IServiceProvider"/> scope and
    /// replace it on <see cref="HttpContext.RequestServices"/> when re-executing the request to handle an error.
    /// </summary>
    /// <remarks>The default value is <see langword="false"/>.</remarks>
    public bool CreateScopeForErrors { get; set; }

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

    /// <summary>
    /// Gets or sets a delegate used to map an exception to a http status code.
    /// </summary>
    /// <remarks>
    /// If <see cref="StatusCodeSelector"/> is <c>null</c>, the default exception status code 500 is used.
    /// </remarks>
    public Func<Exception, int>? StatusCodeSelector { get; set; }
}
