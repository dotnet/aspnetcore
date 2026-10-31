// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;

namespace Microsoft.AspNetCore.Mvc.ApplicationModels;

/// <summary>
/// A context object for <see cref="IApplicationModelProvider"/>.
/// </summary>
public class ApplicationModelProviderContext
{
    /// <summary>
    /// Initializes a new instance of <see cref="ApplicationModelProviderContext"/>.
    /// </summary>
    /// <param name="controllerTypes">The discovered controller <see cref="TypeInfo"/> instances.</param>
    public ApplicationModelProviderContext(IEnumerable<TypeInfo> controllerTypes)
    {
        ArgumentNullException.ThrowIfNull(controllerTypes);

        ControllerTypes = controllerTypes;
    }

    /// <summary>
    /// Gets the discovered controller <see cref="TypeInfo"/> instances.
    /// </summary>
    public IEnumerable<TypeInfo> ControllerTypes { get; }

    /// <summary>
    /// Gets the <see cref="ApplicationModel"/>.
    /// </summary>
    public ApplicationModel Result { get; } = new ApplicationModel();
}
