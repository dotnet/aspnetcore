// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Http.Features;

/// <summary>
/// Provides access to the <see cref="IQueryCollection"/> associated with the HTTP request.
/// </summary>
public interface IQueryFeature
{
    /// <summary>
    /// Gets or sets the <see cref="IQueryCollection"/>.
    /// </summary>
    IQueryCollection Query { get; set; }
}
