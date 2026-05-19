// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc.Abstractions;

namespace Microsoft.AspNetCore.Mvc.ApiExplorer;

/// <summary>
/// A context object for <see cref="ApiDescription"/> providers.
/// </summary>
public class ApiDescriptionProviderContext
{
    /// <summary>
    /// Creates a new instance of <see cref="ApiDescriptionProviderContext"/>.
    /// </summary>
    /// <param name="actions">The list of actions.</param>
    public ApiDescriptionProviderContext(IReadOnlyList<ActionDescriptor> actions)
    {
        ArgumentNullException.ThrowIfNull(actions);

        Actions = actions;

        Results = new List<ApiDescription>();
    }

    /// <summary>
    /// The list of actions.
    /// </summary>
    public IReadOnlyList<ActionDescriptor> Actions { get; }

    /// <summary>
    /// The list of resulting <see cref="ApiDescription"/>.
    /// </summary>
    public IList<ApiDescription> Results { get; }
}
