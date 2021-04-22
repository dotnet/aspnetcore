// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Components.Server
{
    internal class CircuitOptionsJSInteropDetailedErrorsConfiguration : IConfigureOptions<CircuitOptions>
    {
        public CircuitOptionsJSInteropDetailedErrorsConfiguration(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void Configure(CircuitOptions options)
        {
            var value = Configuration[WebHostDefaults.DetailedErrorsKey];
            options.DetailedErrors = string.Equals(value, "true", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(value, "1", StringComparison.OrdinalIgnoreCase);
        }
    }
}
