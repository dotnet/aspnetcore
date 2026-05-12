// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;

public class OOPRendererServerFixture<TStartup> : BasicTestAppServerSiteFixture<TStartup> where TStartup : class
{
    public OOPRendererServerFixture()
    {
        AdditionalArguments.Add("--EnableOOPRenderer=true");
    }
}
