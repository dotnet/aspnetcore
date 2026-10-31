// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Http.Metadata;

/// <summary>
/// Defines a contract used to specify a summary in <see cref="Endpoint.Metadata"/>.
/// </summary>
public interface IEndpointSummaryMetadata
{
    /// <summary>
    /// Gets the summary associated with the endpoint.
    /// </summary>
    string Summary { get; }
}
