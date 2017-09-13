// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Xunit;

namespace Microsoft.AspNetCore.SignalR.Client.Tests
{
    public class HubConnectionBuilderTests
    {
        [Fact]
        public void HubConnectionBuiderThrowsIfConnectionFactoryNotConfigured()
        {
            var ex = Assert.Throws<InvalidOperationException>(() => new HubConnectionBuilder().Build());
            Assert.Equal("Cannot create IConnection instance. The connection factory was not configured.", ex.Message);
        }

        [Fact]
        public void WithUrlThrowsForNullUrls()
        {
            Assert.Equal("url",
                Assert.Throws<ArgumentNullException>(() => new HubConnectionBuilder().WithUrl((string)null)).ParamName);
            Assert.Equal("url",
                Assert.Throws<ArgumentNullException>(() => new HubConnectionBuilder().WithUrl((Uri)null)).ParamName);
        }

        [Fact]
        public void WithLoggerFactoryThrowsForNullLoggerFactory()
        {
            Assert.Equal("loggerFactory",
                Assert.Throws<ArgumentNullException>(() => new HubConnectionBuilder().WithLoggerFactory(null)).ParamName);
        }
    }
}
