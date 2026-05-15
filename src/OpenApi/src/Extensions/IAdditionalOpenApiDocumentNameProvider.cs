// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.OpenApi.Extensions;

/// <summary>
/// An interface that lets users provide additional OpenAPI document names.
/// </summary>
public interface IAdditionalOpenApiDocumentNameProvider
{
    /// <summary>
    /// Gets the additional document names.
    /// </summary>
    public IEnumerable<string> DocumentNames { get; }
}
