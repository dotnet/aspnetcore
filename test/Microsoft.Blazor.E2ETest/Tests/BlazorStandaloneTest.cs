// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Blazor.E2ETest.Infrastructure;
using Microsoft.Blazor.E2ETest.Infrastructure.ServerFixtures;
using Xunit;

namespace Microsoft.Blazor.E2ETest.Tests
{
    public class BlazorStandaloneTest
        : ServerTestBase<DevHostServerFixture<BlazorStandalone.Program>>
    {
        public BlazorStandaloneTest(BrowserFixture browserFixture, DevHostServerFixture<BlazorStandalone.Program> serverFixture)
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
