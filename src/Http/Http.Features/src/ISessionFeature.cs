// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Http.Features;

/// <summary>
/// Provides access to the <see cref="ISession"/> for the current request.
/// </summary>
public interface ISessionFeature
{
    /// <summary>
    /// The <see cref="ISession"/> for the current request.
    /// </summary>
    ISession Session { get; set; }
}
