// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Http;

/// <summary>
/// 
/// </summary>
public sealed class ProblemDetailsEndpointProvider
{
    private readonly ProblemDetailsOptions _options;
    private readonly IHttpProblemDetailsFactory _factory;

    internal ProblemDetailsEndpointProvider(
        IOptions<ProblemDetailsOptions> options,
        IHttpProblemDetailsFactory factory)
    {
        _options = options.Value;
        _factory = factory;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="defaultStatusCode"></param>
    /// <param name="configureDetails"></param>
    /// <returns></returns>
    public RequestDelegate CreateRequestDelegate(
        int defaultStatusCode,
        Action<HttpContext, ProblemDetails>? configureDetails = null)
    {
        return (HttpContext context) =>
        {
            context.Response.StatusCode = defaultStatusCode;

            if (CanWrite(defaultStatusCode))
            {
                var details = _factory.CreateProblemDetails(context, statusCode: context.Response.StatusCode);
                configureDetails?.Invoke(context, details);

                return _factory.WriteAsync(context, details);
            }

            return Task.CompletedTask;
        };
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="context"></param>
    /// <param name="statusCode"></param>
    /// <param name="title"></param>
    /// <param name="type"></param>
    /// <param name="detail"></param>
    /// <param name="instance"></param>
    /// <param name="extensions"></param>
    /// <returns></returns>
    public Task WriteResponse(
        HttpContext context,
        int statusCode,
        string? title = null,
        string? type = null,
        string? detail = null,
        string? instance = null,
        IDictionary<string, object?>? extensions = null)
    {
        context.Response.StatusCode = statusCode;
        var details = _factory.CreateProblemDetails(
            context,
            statusCode: context.Response.StatusCode,
            title,
            type,
            detail,
            instance,
            extensions);
        return _factory.WriteAsync(context, details);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="statusCode"></param>
    /// <param name="isRouting"></param>
    /// <returns></returns>
    public bool CanWrite(int statusCode, bool isRouting = false)
        => _options.IsEnabled(statusCode, isRouting);
}
