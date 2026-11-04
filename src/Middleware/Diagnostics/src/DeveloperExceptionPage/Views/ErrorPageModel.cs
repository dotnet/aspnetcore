// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Primitives;
using Microsoft.Extensions.StackTrace.Sources;

namespace Microsoft.AspNetCore.Diagnostics.RazorViews;

/// <summary>
/// Holds data to be displayed on the error page.
/// </summary>
internal sealed class ErrorPageModel
{
    /// <summary>
    /// Options for what output to display.
    /// </summary>
    public DeveloperExceptionPageOptions Options { get; set; }

    /// <summary>
    /// Detailed information about each exception in the stack.
    /// </summary>
    public IEnumerable<ExceptionDetails> ErrorDetails { get; set; }

    /// <summary>
    /// Parsed query data.
    /// </summary>
    public IQueryCollection Query { get; set; }

    /// <summary>
    /// Request cookies.
    /// </summary>
    public IRequestCookieCollection Cookies { get; set; }

    /// <summary>
    /// Request headers.
    /// </summary>
    public IDictionary<string, StringValues> Headers { get; set; }

    /// <summary>
    /// Request route values.
    /// </summary>
    public RouteValueDictionary RouteValues { get; set; }

    /// <summary>
    /// Request endpoint.
    /// </summary>
    public EndpointModel Endpoint { get; set; }

    /// <summary>
    /// The text be inside the HTML title element.
    /// </summary>
    public string Title { get; set; }
}
