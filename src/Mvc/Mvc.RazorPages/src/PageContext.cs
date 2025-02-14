// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;

namespace Microsoft.AspNetCore.Mvc.RazorPages;

/// <summary>
/// The context associated with the current request for a Razor page.
/// </summary>
[DebuggerDisplay("{DebuggerToString(),nq}")]
public class PageContext : ActionContext
{
    private CompiledPageActionDescriptor? _actionDescriptor;
    private IList<IValueProviderFactory>? _valueProviderFactories;
    private ViewDataDictionary? _viewData;
    private IList<Func<IRazorPage>>? _viewStartFactories;

    /// <summary>
    /// Creates an empty <see cref="PageContext"/>.
    /// </summary>
    /// <remarks>
    /// The default constructor is provided for unit test purposes only.
    /// </remarks>
    public PageContext()
    {
    }

    /// <summary>
    /// Initializes a new instance of <see cref="PageContext"/>.
    /// </summary>
    /// <param name="actionContext">The <see cref="ActionContext"/>.</param>
    public PageContext(ActionContext actionContext)
        : base(actionContext)
    {
    }

    /// <summary>
    /// Initializes a new instance of <see cref="PageContext"/>.
    /// </summary>
    /// <param name="httpContext">The <see cref="HttpContext"/> for the current request.</param>
    /// <param name="routeData">The <see cref="RouteData"/> for the current request.</param>
    /// <param name="actionDescriptor">The <see cref="CompiledPageActionDescriptor"/> for the selected action.</param>
    internal PageContext(
        HttpContext httpContext,
        RouteData routeData,
        CompiledPageActionDescriptor actionDescriptor)
        : base(httpContext, routeData, actionDescriptor)
    {
        _actionDescriptor = actionDescriptor;
    }

    /// <summary>
    /// Gets or sets the <see cref="PageActionDescriptor"/>.
    /// </summary>
    public new virtual CompiledPageActionDescriptor ActionDescriptor
    {
        get => _actionDescriptor!;
        set
        {
            ArgumentNullException.ThrowIfNull(value);

            _actionDescriptor = value;
            base.ActionDescriptor = value;
        }
    }

    /// <summary>
    /// Gets or sets the list of <see cref="IValueProviderFactory"/> instances for the current request.
    /// </summary>
    public virtual IList<IValueProviderFactory> ValueProviderFactories
    {
        get
        {
            if (_valueProviderFactories == null)
            {
                _valueProviderFactories = new List<IValueProviderFactory>();
            }

            return _valueProviderFactories;
        }
        set
        {
            ArgumentNullException.ThrowIfNull(value);

            _valueProviderFactories = value;
        }
    }

    /// <summary>
    /// Gets or sets <see cref="ViewDataDictionary"/>.
    /// </summary>
    public virtual ViewDataDictionary ViewData
    {
        get => _viewData!;
        set
        {
            ArgumentNullException.ThrowIfNull(value);

            _viewData = value;
        }
    }

    /// <summary>
    /// Gets or sets the applicable _ViewStart instances.
    /// </summary>
    public virtual IList<Func<IRazorPage>> ViewStartFactories
    {
        get => _viewStartFactories!;
        set
        {
            ArgumentNullException.ThrowIfNull(value);

            _viewStartFactories = value;
        }
    }

    private string DebuggerToString() => ActionDescriptor?.DisplayName ?? $"{{{GetType().FullName}}}";
}
