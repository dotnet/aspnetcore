// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Internal;

namespace Microsoft.AspNetCore.Mvc.Controllers;

/// <summary>
/// A descriptor for an action of a controller.
/// </summary>
[DebuggerDisplay("{DisplayName,nq}")]
public class ControllerActionDescriptor : ActionDescriptor
{
    /// <summary>
    /// The name of the controller.
    /// </summary>
    public string ControllerName { get; set; } = default!;

    /// <summary>
    /// The name of the action.
    /// </summary>
    public virtual string ActionName { get; set; } = default!;

    /// <summary>
    /// The <see cref="MethodInfo"/>.
    /// </summary>
    public MethodInfo MethodInfo { get; set; } = default!;

    /// <summary>
    /// The <see cref="TypeInfo"/> of the controller..
    /// </summary>
    public TypeInfo ControllerTypeInfo { get; set; } = default!;

    internal EndpointFilterDelegate? FilterDelegate { get; set; }

    // Cache entry so we can avoid an external cache
    internal ControllerActionInvokerCacheEntry? CacheEntry { get; set; }

    /// <inheritdoc />
    public override string? DisplayName
    {
        get
        {
            if (base.DisplayName == null && ControllerTypeInfo != null && MethodInfo != null)
            {
                base.DisplayName = string.Format(
                    CultureInfo.InvariantCulture,
                    "{0}.{1} ({2})",
                    TypeNameHelper.GetTypeDisplayName(ControllerTypeInfo),
                    MethodInfo.Name,
                    ControllerTypeInfo.Assembly.GetName().Name);
            }

            return base.DisplayName!;
        }

        set
        {
            ArgumentNullException.ThrowIfNull(value);

            base.DisplayName = value;
        }
    }
}
