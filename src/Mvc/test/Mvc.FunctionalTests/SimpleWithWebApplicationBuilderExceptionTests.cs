// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.FunctionalTests
{
    public class SimpleWithWebApplicationBuilderExceptionTests : IClassFixture<MvcTestFixture<SimpleWebSiteWithWebApplicationBuilderException.FakeStartup>>
    {
        private readonly MvcTestFixture<SimpleWebSiteWithWebApplicationBuilderException.FakeStartup> _fixture;

        public SimpleWithWebApplicationBuilderExceptionTests(MvcTestFixture<SimpleWebSiteWithWebApplicationBuilderException.FakeStartup> fixture)
        {
            _fixture = fixture;
        }

        [Fact]
        public void ExceptionThrownFromApplicationCanBeObserved()
        {
            var ex = Assert.Throws<InvalidOperationException>(() => _fixture.CreateClient());
            Assert.Equal("This application failed to start", ex.Message);
        }
    }
}
