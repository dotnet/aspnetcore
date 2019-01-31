// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Microsoft.Extensions.Logging.AzureAppServices.Test
{
    public class LoggerBuilderExtensionsTests
    {
        private IWebAppContext _appContext;

        public LoggerBuilderExtensionsTests()
        {
            var contextMock = new Mock<IWebAppContext>();
            contextMock.SetupGet(c => c.IsRunningInAzureWebApp).Returns(true);
            contextMock.SetupGet(c => c.HomeFolder).Returns(".");
            _appContext = contextMock.Object;
        }

        [Fact]
        public void BuilderExtensionAddsSingleSetOfServicesWhenCalledTwice()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddLogging(builder => builder.AddAzureWebAppDiagnostics(_appContext));
            var count = serviceCollection.Count;

            Assert.NotEqual(0, count);

            serviceCollection.AddLogging(builder => builder.AddAzureWebAppDiagnostics(_appContext));

            Assert.Equal(count, serviceCollection.Count);
        }

        [Fact]
        public void BuilderExtensionAddsConfigurationChangeTokenSource()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddLogging(builder => builder.AddConfiguration(new ConfigurationBuilder().Build()));

            // Tracking for main configuration
            Assert.Equal(1, serviceCollection.Count(d => d.ServiceType == typeof(IOptionsChangeTokenSource<LoggerFilterOptions>)));

            serviceCollection.AddLogging(builder => builder.AddAzureWebAppDiagnostics(_appContext));

            // Make sure we add another config change token for azure diagnostic configuration
            Assert.Equal(2, serviceCollection.Count(d => d.ServiceType == typeof(IOptionsChangeTokenSource<LoggerFilterOptions>)));
        }

        [Fact]
        public void BuilderExtensionAddsIConfigureOptions()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddLogging(builder => builder.AddConfiguration(new ConfigurationBuilder().Build()));

            // Tracking for main configuration
            Assert.Equal(2, serviceCollection.Count(d => d.ServiceType == typeof(IConfigureOptions<LoggerFilterOptions>)));

            serviceCollection.AddLogging(builder => builder.AddAzureWebAppDiagnostics(_appContext));

            Assert.Equal(4, serviceCollection.Count(d => d.ServiceType == typeof(IConfigureOptions<LoggerFilterOptions>)));
        }

        [Fact]
        public void LoggerProviderIsResolvable()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddLogging(builder => builder.AddAzureWebAppDiagnostics(_appContext));

            var serviceProvider = serviceCollection.BuildServiceProvider();
            var loggerFactory = serviceProvider.GetService<ILoggerProvider>();
        }
    }
}
