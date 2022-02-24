// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Http.Metadata;

/// <summary>
/// Defines a contract used to specify a collection of tags in <see cref="Endpoint.Metadata"/>.
/// </summary>
public interface ITagsMetadata
{
    /// <summary>
    /// Gets the collection of tags associated with the endpoint.
    /// </summary>
    IReadOnlyList<string> Tags { get; }
}
