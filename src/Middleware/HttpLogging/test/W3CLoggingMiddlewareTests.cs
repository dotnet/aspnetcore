// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting.Internal;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.HttpLogging
{
    public class W3CLoggingMiddlewareTests
    {
        [Fact]
        public void Ctor_ThrowsExceptionsWhenNullArgs()
        {
            Assert.Throws<ArgumentNullException>(() => new W3CLoggingMiddleware(
                null,
                CreateOptionsAccessor(),
                new HostingEnvironment()));

            Assert.Throws<ArgumentNullException>(() => new W3CLoggingMiddleware(c =>
            {
                return Task.CompletedTask;
            },
            null,
            new HostingEnvironment()));

            Assert.Throws<ArgumentNullException>(() => new W3CLoggingMiddleware(c =>
            {
                return Task.CompletedTask;
            },
            CreateOptionsAccessor(),
            null));
        }

        private IOptionsMonitor<W3CLoggerOptions> CreateOptionsAccessor()
        {
            var options = new W3CLoggerOptions();
            var optionsAccessor = Mock.Of<IOptionsMonitor<W3CLoggerOptions>>(o => o.CurrentValue == options);
            return optionsAccessor;
        }
    }
}
