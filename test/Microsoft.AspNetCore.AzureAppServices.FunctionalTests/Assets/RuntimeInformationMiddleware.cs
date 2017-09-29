// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;

namespace Microsoft.AspNetCore.AzureAppServices.FunctionalTests
{
    public class ModuleInfo
    {
        public string FileName { get; set; }
        public string ModuleName { get; set; }
        public string Version { get; set; }
        public string InformationalVersion { get; set; }
    }

    public class RuntimeInfo
    {
        public IDictionary Environment { get; set; }
        public IList<ModuleInfo> Modules { get; set; }
    }

    class RuntimeInformationMiddleware
    {
        private readonly RequestDelegate _next;

        public RuntimeInformationMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            if (context.Request.Path == "/runtimeInfo")
            {
                await context.Response.WriteAsync(
                    JsonConvert.SerializeObject(
                        new RuntimeInfo
                        {
                            Environment = Environment.GetEnvironmentVariables(),
                            Modules = Process.GetCurrentProcess().Modules.OfType<ProcessModule>().Select(m =>
                            {
                                Assembly assembly = null;
                                try
                                {
                                    assembly = Assembly.Load(new AssemblyName(Path.GetFileNameWithoutExtension(m.ModuleName)));
                                }
                                catch { }

                                return new ModuleInfo
                                {
                                    FileName = m.FileName,
                                    ModuleName = m.ModuleName,
                                    Version = assembly?.GetName().Version.ToString(),
                                    InformationalVersion = assembly?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
                                };
                            }).ToList()
                        }, Formatting.Indented));
                return;
            }
            // Call the next delegate/middleware in the pipeline
            await _next(context);
        }
    }
}
