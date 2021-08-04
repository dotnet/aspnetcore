// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Microsoft.AspNetCore.Builder
{
    /// <summary>
    /// Options for configuing the behavior for <see cref="WebApplication.CreateBuilder(WebApplicationBuilderOptions)"/>.
    /// </summary>
    public class WebApplicationBuilderOptions
    {
        /// <summary>
        /// The command line arguments that contain configuration values.
        /// </summary>
        /// <remarks>
        /// The environment name, application name and content root specified as args will be overridden by
        /// the specified properties.
        /// </remarks>
        public string[]? Args { get; init; }

        /// <summary>
        /// The environment name.
        /// </summary>
        public string? EnvironmentName { get; init; }

        /// <summary>
        /// The application name.
        /// </summary>
        public string? ApplicationName { get; init; }

        /// <summary>
        /// The content root path.
        /// </summary>
        public string? ContentRootPath { get; init; }

        internal void ApplyHostConfiguration(IConfigurationBuilder builder)
        {
            Dictionary<string, string>? config = null;

            if (EnvironmentName is not null)
            {
                config = new();
                config[HostDefaults.EnvironmentKey] = EnvironmentName;
            }

            if (ApplicationName is not null)
            {
                config ??= new();
                config[HostDefaults.ApplicationKey] = ApplicationName;
            }

            if (ContentRootPath is not null)
            {
                config ??= new();
                config[HostDefaults.ContentRootKey] = ContentRootPath;
            }

            if (config is not null)
            {
                builder.AddInMemoryCollection(config);
            }
        }
    }
}
