// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Blazor.E2ETest.Infrastructure;
using Microsoft.AspNetCore.Blazor.E2ETest.Infrastructure.ServerFixtures;
using Xunit;

namespace Microsoft.AspNetCore.Blazor.E2ETest.Tests
{
    public class HostedInAspNetTest : ServerTestBase<AspNetSiteServerFixture>
    {
        public HostedInAspNetTest(BrowserFixture browserFixture, AspNetSiteServerFixture serverFixture)
            : base(browserFixture, serverFixture)
        {
            serverFixture.BuildWebHostMethod = HostedInAspNet.Server.Program.BuildWebHost;
            serverFixture.Environment = AspNetEnvironment.Development;
        }

        [Fact]
        public void HasTitle()
        {
            Navigate("/", noReload: true);
            Assert.Equal("Sample Blazor app", Browser.Title);
        }
    }
}
