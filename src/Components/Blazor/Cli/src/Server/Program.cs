// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

namespace Microsoft.AspNetCore.Blazor.Cli.Server
{
    // This project is a CLI tool, so we don't expect anyone to reference it
    // as a runtime library. As such we consider it reasonable to mark the
    // following method as public purely so the E2E tests project can invoke it.

    /// <summary>
    /// Intended for framework test use only.
    /// </summary>
    public class Program
    {
        /// <summary>
        /// Intended for framework test use only.
        /// </summary>
        public static IWebHost BuildWebHost(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .UseConfiguration(new ConfigurationBuilder()
                    .AddCommandLine(args)
                    .Build())
                .UseStartup<Startup>()
                .Build();
    }
}
