// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.Extensions.Diagnostics.HealthChecks
{
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
}
