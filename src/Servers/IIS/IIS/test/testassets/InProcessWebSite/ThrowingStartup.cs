// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace TestSite;

public class ThrowingStartup
{
    public void Configure(IApplicationBuilder app)
    {
        throw new InvalidOperationException("From Configure");
    }
}
