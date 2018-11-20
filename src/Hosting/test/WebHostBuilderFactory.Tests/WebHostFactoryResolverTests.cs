// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Xunit;

namespace Microsoft.AspNetCore.Hosting.WebHostBuilderFactory.Tests
{
    public class WebHostFactoryResolverTests
    {
        [Fact]
        public void CanFindWebHostBuilder_CreateWebHostBuilderPattern()
        {
            // Arrange & Act
            var resolverResult = WebHostFactoryResolver.ResolveWebHostBuilderFactory<IWebHost, IWebHostBuilder>(typeof(IStartupInjectionAssemblyName.Startup).Assembly);

            // Assert
            Assert.Equal(FactoryResolutionResultKind.Success, resolverResult.ResultKind);
            Assert.NotNull(resolverResult.WebHostBuilderFactory);
            Assert.NotNull(resolverResult.WebHostFactory);
            Assert.IsAssignableFrom<IWebHostBuilder>(resolverResult.WebHostBuilderFactory(Array.Empty<string>()));
        }

        [Fact]
        public void CanFindWebHost_CreateWebHostBuilderPattern()
        {
            // Arrange & Act
            var resolverResult = WebHostFactoryResolver.ResolveWebHostFactory<IWebHost, IWebHostBuilder>(typeof(IStartupInjectionAssemblyName.Startup).Assembly);

            // Assert
            Assert.Equal(FactoryResolutionResultKind.Success, resolverResult.ResultKind);
            Assert.NotNull(resolverResult.WebHostBuilderFactory);
            Assert.NotNull(resolverResult.WebHostFactory);
        }

        [Fact]
        public void CanNotFindWebHostBuilder_BuildWebHostPattern()
        {
            // Arrange & Act
            var resolverResult = WebHostFactoryResolver.ResolveWebHostBuilderFactory<IWebHost, IWebHostBuilder>(typeof(BuildWebHostPatternTestSite.Startup).Assembly);

            // Assert
            Assert.Equal(FactoryResolutionResultKind.NoCreateWebHostBuilder, resolverResult.ResultKind);
            Assert.Null(resolverResult.WebHostBuilderFactory);
            Assert.Null(resolverResult.WebHostFactory);
        }

        [Fact]
        public void CanNotFindWebHostBuilder_CreateWebHostBuilderIncorrectSignature()
        {
            // Arrange & Act
            var resolverResult = WebHostFactoryResolver.ResolveWebHostBuilderFactory<IWebHost, IWebHostBuilder>(typeof(CreateWebHostBuilderInvalidSignature.Startup).Assembly);

            // Assert
            Assert.Equal(FactoryResolutionResultKind.NoCreateWebHostBuilder, resolverResult.ResultKind);
            Assert.Null(resolverResult.WebHostBuilderFactory);
            Assert.Null(resolverResult.WebHostFactory);
        }

        [Fact]
        public void CanNotFindWebHost_BuildWebHostIncorrectSignature()
        {
            // Arrange & Act
            var resolverResult = WebHostFactoryResolver.ResolveWebHostFactory<IWebHost, IWebHostBuilder>(typeof(BuildWebHostInvalidSignature.Startup).Assembly);

            // Assert
            Assert.Equal(FactoryResolutionResultKind.NoBuildWebHost, resolverResult.ResultKind);
            Assert.Null(resolverResult.WebHostBuilderFactory);
            Assert.Null(resolverResult.WebHostFactory);
        }

        [Fact]
        public void CanFindWebHost_BuildWebHostPattern()
        {
            // Arrange & Act
            var resolverResult = WebHostFactoryResolver.ResolveWebHostFactory<IWebHost, IWebHostBuilder>(typeof(BuildWebHostPatternTestSite.Startup).Assembly);

            // Assert
            Assert.Equal(FactoryResolutionResultKind.Success, resolverResult.ResultKind);
            Assert.Null(resolverResult.WebHostBuilderFactory);
            Assert.NotNull(resolverResult.WebHostFactory);
        }
    }
}
