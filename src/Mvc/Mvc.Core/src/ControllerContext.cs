// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Core;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Routing;

namespace Microsoft.AspNetCore.Mvc;

/// <summary>
/// The context associated with the current request for a controller.
/// </summary>
[DebuggerDisplay("{DebuggerToString(),nq}")]
public class ControllerContext : ActionContext
{
    private IList<IValueProviderFactory>? _valueProviderFactories;

    /// <summary>
    /// Creates a new <see cref="ControllerContext"/>.
    /// </summary>
    /// <remarks>
    /// The default constructor is provided for unit test purposes only.
    /// </remarks>
    public ControllerContext()
    {
    }

    /// <summary>
    /// Creates a new <see cref="ControllerContext"/>.
    /// </summary>
    /// <param name="context">The <see cref="ActionContext"/> associated with the current request.</param>
    public ControllerContext(ActionContext context)
        : base(context)
    {
        if (context.ActionDescriptor is not ControllerActionDescriptor)
        {
            throw new ArgumentException(Resources.FormatActionDescriptorMustBeBasedOnControllerAction(
                typeof(ControllerActionDescriptor)),
                nameof(context));
        }
    }

    /// <summary>
    /// Creates a new <see cref="ControllerContext"/>.
    /// </summary>
    /// <param name="httpContext">The <see cref="HttpContext"/> for the current request.</param>
    /// <param name="routeData">The <see cref="RouteData"/> for the current request.</param>
    /// <param name="actionDescriptor">The <see cref="ControllerActionDescriptor"/> for the selected action.</param>
    internal ControllerContext(
        HttpContext httpContext,
        RouteData routeData,
        ControllerActionDescriptor actionDescriptor)
        : base(httpContext, routeData, actionDescriptor)
    {
    }

    /// <summary>
    /// Gets or sets the <see cref="ControllerActionDescriptor"/> associated with the current request.
    /// </summary>
    public new ControllerActionDescriptor ActionDescriptor
    {
        get { return (ControllerActionDescriptor)base.ActionDescriptor; }
        set { base.ActionDescriptor = value; }
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

    private string DebuggerToString() => ActionDescriptor?.DisplayName ?? $"{{{GetType().FullName}}}";
}
