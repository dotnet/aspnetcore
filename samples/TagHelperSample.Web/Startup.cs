// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Mvc;
using Microsoft.Framework.DependencyInjection;

namespace TagHelperSample.Web
{
    public class Startup
    {
        public void Configure(IApplicationBuilder app)
        {
            app.UseServices(services =>
            {
                services.AddMvc();

                // Setup services with a test AssemblyProvider so that only the sample's assemblies are loaded. This
                // prevents loading controllers from other assemblies when the sample is used in functional tests.
                services.AddTransient<IAssemblyProvider, TestAssemblyProvider<Startup>>();
            });
            app.UseMvc();
        }
    }
}
