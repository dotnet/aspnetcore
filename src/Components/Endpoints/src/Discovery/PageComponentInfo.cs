// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using static Microsoft.AspNetCore.Internal.LinkerFlags;

namespace Microsoft.AspNetCore.Components.Discovery;

/// <summary>
/// The definition for a page, including the type and the associated routes.
/// </summary>
[DebuggerDisplay($"{{{nameof(GetDebuggerDisplay)}(),nq}}")]
internal class PageComponentInfo
{
    /// <summary>
    /// Initializes a new instance of <see cref="PageComponentInfo"/>.
    /// </summary>
    /// <param name="displayName">The name for the page. Used for logging and debug purposes across the system.</param>
    /// <param name="type">The page <see cref="System.Type"/>.</param>
    /// <param name="route">The see list of routes for the page.</param>
    /// <param name="metadata">The page metadata.</param>
    internal PageComponentInfo(
        string displayName,
        [DynamicallyAccessedMembers(Component)] Type type,
        string route,
        IReadOnlyList<object> metadata)
    {
        DisplayName = displayName;
        Type = type;
        Route = route;
        Metadata = metadata;
    }

    /// <summary>
    /// Gets the page display name.
    /// </summary>
    public string DisplayName { get; }

    /// <summary>
    /// Gets the page type.
    /// </summary>
    [DynamicallyAccessedMembers(Component)]
    public Type Type { get; }

    /// <summary>
    /// Gets the routes for the page.
    /// </summary>
    public string Route { get; }

    /// <summary>
    /// Gets the metadata for the page.
    /// </summary>
    public IReadOnlyList<object> Metadata { get; }

    private string GetDebuggerDisplay()
    {
        return $"Type = {Type.FullName}, DisplayName = {DisplayName}";
    }
}
