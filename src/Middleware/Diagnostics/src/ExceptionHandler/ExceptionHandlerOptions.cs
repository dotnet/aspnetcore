// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.Tracing;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

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
    /// Gets or sets a delegate used to map an exception to an HTTP status code.
    /// </summary>
    /// <remarks>
    /// If <see cref="StatusCodeSelector"/> is <c>null</c>, the default exception status code 500 is used.
    /// </remarks>
    public Func<Exception, int>? StatusCodeSelector { get; set; }

    /// <summary>
    /// Gets or sets a callback that can return <see langword="true" /> to suppress diagnostics in <see cref="ExceptionHandlerMiddleware" />.
    /// <para>
    /// If <see cref="SuppressDiagnosticsCallback"/> is <c>null</c>, the default behavior is to suppress diagnostics if the exception was handled by
    /// an <see cref="IExceptionHandler"/> service instance registered in the DI container.
    /// To always record diagnostics for handled exceptions, set a callback that returns <see langword="false" />.
    /// </para>
    /// <para>
    /// This callback is only run if the exception was handled by the middleware.
    /// Unhandled exceptions and exceptions thrown after the response has started are always logged.
    /// </para>
    /// <para>
    /// Suppressed diagnostics include:
    /// </para>
    /// <list type="bullet">
    ///   <item>
    ///     <description>Logging <c>UnhandledException</c> to <see cref="ILogger"/>.</description>
    ///   </item>
    ///   <item>
    ///     <description>Writing the <c>Microsoft.AspNetCore.Diagnostics.HandledException</c> event to <see cref="EventSource" />.</description>
    ///   </item>
    ///   <item>
    ///     <description>Adding the <c>error.type</c> tag to the <c>http.server.request.duration</c> metric.</description>
    ///   </item>
    /// </list>
    /// </summary>
    public Func<ExceptionHandlerSuppressDiagnosticsContext, bool>? SuppressDiagnosticsCallback { get; set; }
}
