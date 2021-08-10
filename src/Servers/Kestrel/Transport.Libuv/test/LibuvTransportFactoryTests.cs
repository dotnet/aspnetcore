// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
