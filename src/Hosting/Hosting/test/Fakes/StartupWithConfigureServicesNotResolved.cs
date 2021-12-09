// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Builder;

namespace Microsoft.AspNetCore.Hosting.Fakes;

public class StartupWithConfigureServicesNotResolved
{
    public StartupWithConfigureServicesNotResolved()
    {
    }

    public void Configure(IApplicationBuilder builder, int notAService)
    {
    }
}
