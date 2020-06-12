// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.Extensions.Diagnostics.HealthChecks
{
    public sealed class HealthCheckContext
    {
        /// <summary>
        /// Gets or sets the <see cref="HealthCheckRegistration"/> of the currently executing <see cref="IHealthCheck"/>.
        /// </summary>
        // This allows null values for convenience during unit testing. This is expected to be non-null when within application code.
        public HealthCheckRegistration Registration { get; set; } = default!;
    }
}
