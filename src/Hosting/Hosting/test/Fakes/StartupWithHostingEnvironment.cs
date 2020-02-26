// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;

namespace Microsoft.AspNetCore.Hosting.Tests.Fakes
{
    public class StartupWithHostingEnvironment
    {
        public StartupWithHostingEnvironment(IHostEnvironment env)
        {
            env.EnvironmentName = "Changed";
        }

        public void Configure(IApplicationBuilder app)
        {

        }
    }
}
