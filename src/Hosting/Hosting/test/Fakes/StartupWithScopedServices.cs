// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Builder;
using static Microsoft.AspNetCore.Hosting.Tests.StartupManagerTests;

namespace Microsoft.AspNetCore.Hosting.Fakes;

public class StartupWithScopedServices
{
    public DisposableService DisposableService { get; set; }

    public void Configure(IApplicationBuilder builder, DisposableService disposable)
    {
        DisposableService = disposable;
    }
}
