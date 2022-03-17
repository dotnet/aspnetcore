// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Http.Metadata;

/// <summary>
/// Interface marking attributes that specify a parameter should be bound using the request headers.
/// </summary>
public interface IFromHeaderMetadata
{
    /// <summary>
    /// The request header name.
    /// </summary>
    string? Name { get; }
}
