// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Microsoft.AspNetCore.Mvc.Diagnostics;

/// <summary>
/// An <see cref="EventData"/> that occurs before a view page.
/// </summary>
public sealed class BeforeViewPageEventData : EventData
{
    /// <summary>
    /// The name of the event.
    /// </summary>
    public const string EventName = EventNamespace +
        "Razor." +
        "BeforeViewPage";

    /// <summary>
    /// Initializes a new instance of <see cref="BeforeViewPageEventData"/>.
    /// </summary>
    /// <param name="page">The page.</param>
    /// <param name="viewContext">The view context.</param>
    /// <param name="actionDescriptor">The action.</param>
    /// <param name="httpContext">The http context.</param>
    public BeforeViewPageEventData(IRazorPage page, ViewContext viewContext, ActionDescriptor actionDescriptor, HttpContext httpContext)
    {
        Page = page;
        ViewContext = viewContext;
        ActionDescriptor = actionDescriptor;
        HttpContext = httpContext;
    }

    /// <summary>
    /// The <see cref="IRazorPage"/>.
    /// </summary>
    public IRazorPage Page { get; }

    /// <summary>
    /// The <see cref="ViewContext"/>.
    /// </summary>
    public ViewContext ViewContext { get; }

    /// <summary>
    /// The <see cref="ActionDescriptor"/>.
    /// </summary>
    public ActionDescriptor ActionDescriptor { get; }

    /// <summary>
    /// The <see cref="HttpContext"/>.
    /// </summary>
    public HttpContext HttpContext { get; }

    /// <inheritdoc/>
    protected override int Count => 4;

    /// <inheritdoc/>
    protected override KeyValuePair<string, object> this[int index] => index switch
    {
        0 => new KeyValuePair<string, object>(nameof(Page), Page),
        1 => new KeyValuePair<string, object>(nameof(ViewContext), ViewContext),
        2 => new KeyValuePair<string, object>(nameof(ActionDescriptor), ActionDescriptor),
        3 => new KeyValuePair<string, object>(nameof(HttpContext), HttpContext),
        _ => throw new ArgumentOutOfRangeException(nameof(index))
    };
}

/// <summary>
/// An <see cref="EventData"/> that occurs after a view page.
/// </summary>
public sealed class AfterViewPageEventData : EventData
{
    /// <summary>
    /// The name of the event.
    /// </summary>
    public const string EventName = EventNamespace +
        "Razor." +
        "AfterViewPage";

    /// <summary>
    /// Initializes a new instance of <see cref="AfterViewPageEventData"/>.
    /// </summary>
    /// <param name="page">The page.</param>
    /// <param name="viewContext">The view context.</param>
    /// <param name="actionDescriptor">The action.</param>
    /// <param name="httpContext">The http context.</param>
    public AfterViewPageEventData(IRazorPage page, ViewContext viewContext, ActionDescriptor actionDescriptor, HttpContext httpContext)
    {
        Page = page;
        ViewContext = viewContext;
        ActionDescriptor = actionDescriptor;
        HttpContext = httpContext;
    }

    /// <summary>
    /// The <see cref="IRazorPage"/>.
    /// </summary>
    public IRazorPage Page { get; }

    /// <summary>
    /// The <see cref="ViewContext"/>.
    /// </summary>
    public ViewContext ViewContext { get; }

    /// <summary>
    /// The <see cref="ActionDescriptor"/>.
    /// </summary>
    public ActionDescriptor ActionDescriptor { get; }

    /// <summary>
    /// The <see cref="HttpContext"/>.
    /// </summary>
    public HttpContext HttpContext { get; }

    /// <inheritdoc/>
    protected override int Count => 4;

    /// <inheritdoc/>
    protected override KeyValuePair<string, object> this[int index] => index switch
    {
        0 => new KeyValuePair<string, object>(nameof(Page), Page),
        1 => new KeyValuePair<string, object>(nameof(ViewContext), ViewContext),
        2 => new KeyValuePair<string, object>(nameof(ActionDescriptor), ActionDescriptor),
        3 => new KeyValuePair<string, object>(nameof(HttpContext), HttpContext),
        _ => throw new ArgumentOutOfRangeException(nameof(index))
    };
}
