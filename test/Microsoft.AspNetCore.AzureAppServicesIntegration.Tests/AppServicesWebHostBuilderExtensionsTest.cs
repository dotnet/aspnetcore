// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Hosting;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Hosting.Azure.AppServices.Tests
{
    public class AppServicesWebHostBuilderExtensionsTest
    {
        [Fact]
        public void UseAzureAppServices_RegisterLogger()
        {
            var mock = new Mock<IWebHostBuilder>();

            mock.Object.UseAzureAppServices();

            mock.Verify(builder => builder.ConfigureLogging(It.IsNotNull<Action<ILoggerFactory>>()), Times.Once);
        }
    }
}
