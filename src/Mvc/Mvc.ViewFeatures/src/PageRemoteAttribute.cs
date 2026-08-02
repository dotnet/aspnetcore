// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.DependencyInjection;
using Resources = Microsoft.AspNetCore.Mvc.ViewFeatures.Resources;

namespace Microsoft.AspNetCore.Mvc;

/// <summary>
/// A <see cref="RemoteAttributeBase"/> for razor page handler which configures Unobtrusive validation
/// to send an Ajax request to the web site. The invoked handler should return JSON indicating
/// whether the value is valid.
/// </summary>
/// <remarks>Does no server-side validation of the final form submission.</remarks>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public class PageRemoteAttribute : RemoteAttributeBase
{
    /// <summary>
    /// The handler name used when generating the URL where client should send a validation request.
    /// </summary>
    /// <remarks>
    /// If not set the ambient value will be used when generating the URL.
    /// </remarks>
    public string? PageHandler { get; set; }

    /// <summary>
    /// The page name used when generating the URL where client should send a validation request.
    /// </summary>
    /// <remarks>
    /// If not set the ambient value will be used when generating the URL.
    /// </remarks>
    public string? PageName { get; set; }

    /// <inheritdoc />
    protected override string GetUrl(ClientModelValidationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var services = context.ActionContext.HttpContext.RequestServices;
        var factory = services.GetRequiredService<IUrlHelperFactory>();
        var urlHelper = factory.GetUrlHelper(context.ActionContext);

        var url = urlHelper.Page(PageName, PageHandler, RouteData);

        if (url == null)
        {
            throw new InvalidOperationException(Resources.RemoteAttribute_NoUrlFound);
        }

        return url;
    }
}
