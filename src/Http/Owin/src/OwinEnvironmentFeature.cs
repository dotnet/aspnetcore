// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Owin;

/// <summary>
/// Default implementation of <see cref="IOwinEnvironmentFeature"/>.
/// </summary>
public class OwinEnvironmentFeature : IOwinEnvironmentFeature
{
    /// <inheritdoc />
    public IDictionary<string, object> Environment { get; set; }
}
