// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.DependencyInjection;
using Resources = Microsoft.AspNetCore.Mvc.ViewFeatures.Resources;

namespace Microsoft.AspNetCore.Mvc;

/// <summary>
/// A <see cref="RemoteAttributeBase"/> for controllers which configures Unobtrusive validation to send an Ajax request to the
/// web site. The invoked action should return JSON indicating whether the value is valid.
/// </summary>
/// <remarks>Does no server-side validation of the final form submission.</remarks>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public class RemoteAttribute : RemoteAttributeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RemoteAttribute"/> class.
    /// </summary>
    /// <remarks>
    /// Intended for subclasses that support URL generation with no route, action, or controller names.
    /// </remarks>
    protected RemoteAttribute()
    { }

    /// <summary>
    /// Initializes a new instance of the <see cref="RemoteAttribute"/> class.
    /// </summary>
    /// <param name="routeName">
    /// The route name used when generating the URL where client should send a validation request.
    /// </param>
    /// <remarks>
    /// Finds the <paramref name="routeName"/> in any area of the application.
    /// </remarks>
    public RemoteAttribute(string routeName)
        : this()
    {
        RouteName = routeName;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RemoteAttribute"/> class.
    /// </summary>
    /// <param name="action">
    /// The action name used when generating the URL where client should send a validation request.
    /// </param>
    /// <param name="controller">
    /// The controller name used when generating the URL where client should send a validation request.
    /// </param>
    /// <remarks>
    /// <para>
    /// If either <paramref name="action"/> or <paramref name="controller"/> is <c>null</c>, uses the corresponding
    /// ambient value.
    /// </para>
    /// <para>Finds the <paramref name="controller"/> in the current area.</para>
    /// </remarks>
    public RemoteAttribute(string action, string controller)
        : this()
    {
        if (action != null)
        {
            RouteData["action"] = action;
        }

        if (controller != null)
        {
            RouteData["controller"] = controller;
        }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RemoteAttribute"/> class.
    /// </summary>
    /// <param name="action">
    /// The action name used when generating the URL where client should send a validation request.
    /// </param>
    /// <param name="controller">
    /// The controller name used when generating the URL where client should send a validation request.
    /// </param>
    /// <param name="areaName">The name of the area containing the <paramref name="controller"/>.</param>
    /// <remarks>
    /// <para>
    /// If either <paramref name="action"/> or <paramref name="controller"/> is <c>null</c>, uses the corresponding
    /// ambient value.
    /// </para>
    /// If <paramref name="areaName"/> is <c>null</c>, finds the <paramref name="controller"/> in the root area.
    /// Use the <see cref="RemoteAttribute(string, string)"/> overload find the <paramref name="controller"/> in
    /// the current area. Or explicitly pass the current area's name as the <paramref name="areaName"/> argument to
    /// this overload.
    /// </remarks>
    public RemoteAttribute(string action, string controller, string areaName)
        : this(action, controller)
    {
        RouteData["area"] = areaName;
    }

    /// <summary>
    /// Gets or sets the route name used when generating the URL where client should send a validation request.
    /// </summary>
    protected string? RouteName { get; set; }

    /// <inheritdoc />
    protected override string GetUrl(ClientModelValidationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var services = context.ActionContext.HttpContext.RequestServices;
        var factory = services.GetRequiredService<IUrlHelperFactory>();
        var urlHelper = factory.GetUrlHelper(context.ActionContext);

        var url = urlHelper.RouteUrl(new UrlRouteContext()
        {
            RouteName = RouteName,
            Values = RouteData,
        });

        if (url == null)
        {
            throw new InvalidOperationException(Resources.RemoteAttribute_NoUrlFound);
        }

        return url;
    }
}
