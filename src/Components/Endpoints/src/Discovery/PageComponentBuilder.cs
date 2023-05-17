// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Linq;

namespace Microsoft.AspNetCore.Components;

/// <summary>
/// Initializes a new instance of <see cref="PageComponentBuilder"/>.
/// </summary>
[DebuggerDisplay($"{{{nameof(GetDebuggerDisplay)}(),nq}}")]
public class PageComponentBuilder : IEquatable<PageComponentBuilder?>
{
    private IReadOnlyList<string> _routeTemplates = Array.Empty<string>();

    /// <summary>
    /// Gets or sets the source for this page component.
    /// </summary>
    public required string Source { get; set; }

    /// <summary>
    /// Gets or sets the route templates for this page component.
    /// </summary>
    public required IReadOnlyList<string> RouteTemplates
    {
        get => _routeTemplates;
        set
        {
            ArgumentOutOfRangeException.ThrowIfZero(value.Count, nameof(value));
            _routeTemplates = value;
        }
    }

    /// <summary>
    /// Gets or sets the page type.
    /// </summary>
    public required Type PageType { get; set; }

    /// <summary>
    /// Compares the given <paramref name="source"/> against the source for this <see cref="PageComponentBuilder"/>.
    /// </summary>
    /// <param name="source">The source to compare against.</param>
    /// <returns><c>true</c> when it has the same source; false otherwise.</returns>
    public bool HasSource(string source)
    {
        return string.Equals(Source, source, StringComparison.Ordinal);
    }

    /// <inheritdoc/>
    public override bool Equals(object? obj)
    {
        return Equals(obj as PageComponentBuilder);
    }

    /// <inheritdoc/>
    public bool Equals(PageComponentBuilder? other)
    {
        return other is not null &&
               Source == other.Source &&
               RouteTemplates.SequenceEqual(other.RouteTemplates!, StringComparer.OrdinalIgnoreCase) &&
               EqualityComparer<Type>.Default.Equals(PageType, other.PageType);
    }

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(Source);
        if (RouteTemplates != null)
        {
            for (var i = 0; i < RouteTemplates.Count; i++)
            {
                hash.Add(RouteTemplates[i]);
            }
        }
        hash.Add(PageType);
        return hash.ToHashCode();
    }

    private string GetDebuggerDisplay()
    {
        return $"{PageType.FullName}{string.Join(", ", RouteTemplates ?? Enumerable.Empty<string>())}";
    }
}
