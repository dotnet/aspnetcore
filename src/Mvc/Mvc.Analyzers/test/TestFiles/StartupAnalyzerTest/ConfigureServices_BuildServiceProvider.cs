// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Analyzers.TestFiles.StartupAnalyzerTest
{
    public class ConfigureServices_BuildServiceProvider
    {
        public void ConfigureServices(IServiceCollection services)
        {
            /*MM1*/services.BuildServiceProvider();
        }

        public void Configure(IApplicationBuilder app)
        {
        }
    }
}
