// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Builder;

namespace Microsoft.AspNetCore.Hosting.Fakes;

public class StartupNoServices : Hosting.StartupBase
{
    public StartupNoServices()
    {
    }

    public override void Configure(IApplicationBuilder builder)
    {
    }
}
