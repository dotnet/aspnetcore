// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Owin;

/// <summary>
/// A feature interface for an OWIN environment.
/// </summary>
public interface IOwinEnvironmentFeature
{
    /// <summary>
    /// Gets or sets the environment values.
    /// </summary>
    IDictionary<string, object> Environment { get; set; }
}
