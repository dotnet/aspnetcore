// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Microsoft.AspNetCore.Blazor.DevServer.Server
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
        public static IHost BuildWebHost(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureHostConfiguration(cb => {
                    var applicationPath = args.SkipWhile(a => a != "--applicationpath").Skip(1).FirstOrDefault();
                    var name = Path.ChangeExtension(applicationPath,".StaticWebAssets.xml");

                    if (name != null)
                    {
                        cb.AddInMemoryCollection(new Dictionary<string, string>
                        {
                            [WebHostDefaults.StaticWebAssetsKey] = name
                        });
                    }
                })
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStaticWebAssets();
                    webBuilder.UseStartup<Startup>();
                }).Build();
    }
}
