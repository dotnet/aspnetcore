// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Blazor.E2ETest.Infrastructure;
using Xunit;

namespace Blazor.E2ETest.Tests
{
    public class MonoSanityTest : AspNetSiteTestBase<MonoSanity.Startup>
    {
        public MonoSanityTest(BrowserFixture browserFixture, AspNetServerFixture serverFixture)
            : base(browserFixture, serverFixture)
        {
        }

        [Fact]
        public void HasTitle()
        {
            Navigate("/");
            Assert.Equal("Mono sanity check", Browser.Title);
        }
    }
}
