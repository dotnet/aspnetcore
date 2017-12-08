// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Blazor.E2ETest.Infrastructure;
using Microsoft.Blazor.E2ETest.Infrastructure.ServerFixtures;
using Xunit;

namespace Microsoft.Blazor.E2ETest.Tests
{
    public class StandaloneAppTest
        : ServerTestBase<DevHostServerFixture<StandaloneApp.Program>>
    {
        public StandaloneAppTest(BrowserFixture browserFixture, DevHostServerFixture<StandaloneApp.Program> serverFixture)
            : base(browserFixture, serverFixture)
        {
        }

        [Fact]
        public void HasTitle()
        {
            Navigate("/");
            Assert.Equal("Blazor standalone", Browser.Title);
        }
    }
}
