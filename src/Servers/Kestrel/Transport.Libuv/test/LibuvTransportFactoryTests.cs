// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal;
using Microsoft.AspNetCore.Testing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Tests
{
    public class LibuvTransportFactoryTests
    {
        [Theory]
        [InlineData(0)]
        [InlineData(-1337)]
        public void StartWithNonPositiveThreadCountThrows(int threadCount)
        {
#pragma warning disable CS0618
            var options = new LibuvTransportOptions { ThreadCount = threadCount };
#pragma warning restore CS0618

            var exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
                new LibuvTransportFactory(Options.Create(options), new LifetimeNotImplemented(), Mock.Of<ILoggerFactory>()));

            Assert.Equal("threadCount", exception.ParamName);
        }

        [Fact]
        public void LoggerCategoryNameIsLibuvTransportNamespace()
        {
            var mockLoggerFactory = new Mock<ILoggerFactory>();
#pragma warning disable CS0618
            new LibuvTransportFactory(Options.Create<LibuvTransportOptions>(new LibuvTransportOptions()), new LifetimeNotImplemented(), mockLoggerFactory.Object);
#pragma warning restore CS0618
            mockLoggerFactory.Verify(factory => factory.CreateLogger("Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv"));
        }
    }
}
