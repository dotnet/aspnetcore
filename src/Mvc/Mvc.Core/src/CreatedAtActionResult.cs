// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Core;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Mvc;

/// <summary>
/// An <see cref="ActionResult"/> that returns a Created (201) response with a Location header.
/// </summary>
[DefaultStatusCode(DefaultStatusCode)]
public class CreatedAtActionResult : ObjectResult
{
    private const int DefaultStatusCode = StatusCodes.Status201Created;

    /// <summary>
    /// Initializes a new instance of the <see cref="CreatedAtActionResult"/> with the values
    /// provided.
    /// </summary>
    /// <param name="actionName">The name of the action to use for generating the URL.</param>
    /// <param name="controllerName">The name of the controller to use for generating the URL.</param>
    /// <param name="routeValues">The route data to use for generating the URL.</param>
    /// <param name="value">The value to format in the entity body.</param>
    public CreatedAtActionResult(
        string? actionName,
        string? controllerName,
        object? routeValues,
        [ActionResultObjectValue] object? value)
        : base(value)
    {
        ActionName = actionName;
        ControllerName = controllerName;
        RouteValues = routeValues == null ? null : new RouteValueDictionary(routeValues);
        StatusCode = DefaultStatusCode;
    }

    /// <summary>
    /// Gets or sets the <see cref="IUrlHelper" /> used to generate URLs.
    /// </summary>
    public IUrlHelper? UrlHelper { get; set; }

    /// <summary>
    /// Gets or sets the name of the action to use for generating the URL.
    /// </summary>
    public string? ActionName { get; set; }

    /// <summary>
    /// Gets or sets the name of the controller to use for generating the URL.
    /// </summary>
    public string? ControllerName { get; set; }

    /// <summary>
    /// Gets or sets the route data to use for generating the URL.
    /// </summary>
    public RouteValueDictionary? RouteValues { get; set; }

    /// <inheritdoc />
    public override void OnFormatting(ActionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        base.OnFormatting(context);

        var request = context.HttpContext.Request;

        var urlHelper = UrlHelper;
        if (urlHelper == null)
        {
            var services = context.HttpContext.RequestServices;
            urlHelper = services.GetRequiredService<IUrlHelperFactory>().GetUrlHelper(context);
        }

        var url = urlHelper.Action(
            ActionName,
            ControllerName,
            RouteValues,
            request.Scheme,
            request.Host.ToUriComponent());

        if (string.IsNullOrEmpty(url))
        {
            throw new InvalidOperationException(Resources.NoRoutesMatched);
        }

        context.HttpContext.Response.Headers.Location = url;
    }
}
