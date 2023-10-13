// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Shared;

namespace Microsoft.AspNetCore.Http;

/// <summary>
/// Specifies a collection of tags in <see cref="Endpoint.Metadata"/>.
/// </summary>
/// <remarks>
/// The OpenAPI specification supports a tags classification to categorize operations
/// into related groups. These tags are typically included in the generated specification
/// and are typically used to group operations by tags in the UI.
/// </remarks>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Delegate | AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
[DebuggerDisplay("{ToString(),nq}")]
public sealed class TagsAttribute : Attribute, ITagsMetadata
{
    /// <summary>
    /// Initializes an instance of the <see cref="TagsAttribute"/>.
    /// </summary>
    /// <param name="tags">The tags associated with the endpoint.</param>
    public TagsAttribute(params string[] tags)
    {
        Tags = new List<string>(tags);
    }

    /// <summary>
    /// Gets the collection of tags associated with the endpoint.
    /// </summary>
    public IReadOnlyList<string> Tags { get; }

    /// <inheritdoc/>
    public override string ToString()
    {
        return DebuggerHelpers.GetDebugText(nameof(Tags), Tags);
    }
}
