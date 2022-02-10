// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace Microsoft.Extensions.Diagnostics.HealthChecks;

/// <summary>
/// Options for the default implementation of <see cref="HealthCheckService"/>
/// </summary>
public sealed class HealthCheckServiceOptions
{
    /// <summary>
    /// Gets the health check registrations.
    /// </summary>
    public ICollection<HealthCheckRegistration> Registrations { get; } = new List<HealthCheckRegistration>();
}
