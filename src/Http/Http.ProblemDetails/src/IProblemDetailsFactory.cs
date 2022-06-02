// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Http;

using Microsoft.AspNetCore.Mvc;

/// <summary>
/// 
/// </summary>
internal interface IHttpProblemDetailsFactory
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="context"></param>
    /// <param name="problemDetails"></param>
    /// <returns></returns>
    Task WriteAsync(
        HttpContext context,
        ProblemDetails problemDetails);

    /// <summary>
    /// Creates a <see cref="ProblemDetails" /> instance that configures defaults based on values specified in <see cref="ProblemDetailsOptions" />.
    /// </summary>
    /// <param name="httpContext">The <see cref="HttpContext" />.</param>
    /// <param name="options"></param>
    /// <param name="statusCode">The value for <see cref="ProblemDetails.Status"/>.</param>
    /// <param name="title">The value for <see cref="ProblemDetails.Title" />.</param>
    /// <param name="type">The value for <see cref="ProblemDetails.Type" />.</param>
    /// <param name="detail">The value for <see cref="ProblemDetails.Detail" />.</param>
    /// <param name="instance">The value for <see cref="ProblemDetails.Instance" />.</param>
    /// <param name="extensions">The value for <see cref="ProblemDetails.Extensions" />.</param>
    /// <returns>The <see cref="ProblemDetails"/> instance.</returns>
    ProblemDetails CreateProblemDetails(
        HttpContext httpContext,
        int? statusCode = null,
        string? title = null,
        string? type = null,
        string? detail = null,
        string? instance = null,
        IDictionary<string, object?>? extensions = null);
}
