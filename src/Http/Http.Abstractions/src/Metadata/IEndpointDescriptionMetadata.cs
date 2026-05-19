// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Http.Metadata;

/// <summary>
/// Defines a contract used to specify a description in <see cref="Endpoint.Metadata"/>.
/// </summary>
public interface IEndpointDescriptionMetadata
{
    /// <summary>
    /// Gets the description associated with the endpoint.
    /// </summary>
    string Description { get; }
}
