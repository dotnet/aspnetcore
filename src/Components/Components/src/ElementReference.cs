// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;

namespace Microsoft.AspNetCore.Components;

/// <summary>
/// Represents a reference to a rendered element.
/// </summary>
public readonly struct ElementReference
{
    private static long _nextIdForWebAssemblyOnly = 1;

    /// <summary>
    /// Gets a unique identifier for <see cref="ElementReference" />.
    /// </summary>
    /// <remarks>
    /// The Id is unique at least within the scope of a given user/circuit.
    /// This property is public to support Json serialization and should not be used by user code.
    /// </remarks>
    public string Id { get; }

    /// <summary>
    /// Gets the <see cref="ElementReferenceContext"/> instance.
    /// </summary>
    public ElementReferenceContext? Context { get; }

    /// <summary>
    /// Instantiates a new <see cref="ElementReference" />.
    /// </summary>
    /// <param name="id">A unique identifier for this <see cref="ElementReference"/>.</param>
    /// <param name="context">The nullable <see cref="ElementReferenceContext"/> instance.</param>
    public ElementReference(string id, ElementReferenceContext? context)
    {
        Id = id;
        Context = context;
    }

    /// <summary>
    /// Instantiates a new <see cref="ElementReference"/>.
    /// </summary>
    /// <param name="id">A unique identifier for this <see cref="ElementReference"/>.</param>
    public ElementReference(string id) : this(id, null)
    {
    }

    internal static ElementReference CreateWithUniqueId(ElementReferenceContext? context)
        => new ElementReference(CreateUniqueId(), context);

    private static string CreateUniqueId()
    {
        if (OperatingSystem.IsBrowser())
        {
            // On WebAssembly there's only one user, so it's fine to expose the number
            // of IDs that have been assigned, and this is cheaper than creating a GUID.
            // It's unfortunate that this still involves a heap allocation. If that becomes
            // a problem we could extend RenderTreeFrame to have both "string" and "long"
            // fields for ElementRefCaptureId, of which only one would be in use depending
            // on the platform.
            var id = Interlocked.Increment(ref _nextIdForWebAssemblyOnly);
            return id.ToString(CultureInfo.InvariantCulture);
        }
        else
        {
            // For remote rendering, it's important not to disclose any cross-user state,
            // such as the number of IDs that have been assigned.
            return Guid.NewGuid().ToString("D", CultureInfo.InvariantCulture);
        }
    }
}
