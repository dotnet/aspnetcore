// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;

namespace Microsoft.AspNetCore.Hosting.Tests.Fakes;

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
