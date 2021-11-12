// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Http.Abstractions.Metadata;

/// <summary>
/// A marker interface which can be used to identify a resource with Antiforgery validation enabled.
/// </summary>
public interface IValidateAntiforgeryMetadata : IAntiforgeryMetadata
{
    /// <summary>
    /// Gets a value that determines if idempotent HTTP methods (<c>GET</c>, <c>HEAD</c>, <c>OPTIONS</c> and <c>TRACE</c>) are validated.
    /// </summary>
    bool ValidateIdempotentRequests { get; }
}
