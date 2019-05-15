// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using MockHostTypes;
using System;
using Xunit;

namespace Microsoft.Extensions.Hosting.Tests
{
    public class HostFactoryResolverTests
    {
        [Fact]
        public void BuildWebHostPattern_CanFindWebHost()
        {
            var factory = HostFactoryResolver.ResolveWebHostFactory<IWebHost>(typeof(BuildWebHostPatternTestSite.Program).Assembly);

            Assert.NotNull(factory);
            Assert.IsAssignableFrom<IWebHost>(factory(Array.Empty<string>()));
        }

        [Fact]
        public void BuildWebHostPattern_CanFindServiceProvider()
        {
            var factory = HostFactoryResolver.ResolveServiceProviderFactory(typeof(BuildWebHostPatternTestSite.Program).Assembly);

            Assert.NotNull(factory);
            Assert.IsAssignableFrom<IServiceProvider>(factory(Array.Empty<string>()));
        }

        [Fact]
        public void BuildWebHostPattern__Invalid_CantFindWebHost()
        {
            var factory = HostFactoryResolver.ResolveWebHostFactory<IWebHost>(typeof(BuildWebHostInvalidSignature.Program).Assembly);

            Assert.Null(factory);
        }

        [Fact]
        public void BuildWebHostPattern__Invalid_CantFindServiceProvider()
        {
            var factory = HostFactoryResolver.ResolveServiceProviderFactory(typeof(BuildWebHostInvalidSignature.Program).Assembly);

            Assert.Null(factory);
        }

        [Fact]
        public void CreateWebHostBuilderPattern_CanFindWebHostBuilder()
        {
            var factory = HostFactoryResolver.ResolveWebHostBuilderFactory<IWebHostBuilder>(typeof(CreateWebHostBuilderPatternTestSite.Program).Assembly);

            Assert.NotNull(factory);
            Assert.IsAssignableFrom<IWebHostBuilder>(factory(Array.Empty<string>()));
        }

        [Fact]
        public void CreateWebHostBuilderPattern_CanFindServiceProvider()
        {
            var factory = HostFactoryResolver.ResolveServiceProviderFactory(typeof(CreateWebHostBuilderPatternTestSite.Program).Assembly);

            Assert.NotNull(factory);
            Assert.IsAssignableFrom<IServiceProvider>(factory(Array.Empty<string>()));
        }

        [Fact]
        public void CreateWebHostBuilderPattern__Invalid_CantFindWebHostBuilder()
        {
            var factory = HostFactoryResolver.ResolveWebHostBuilderFactory<IWebHostBuilder>(typeof(CreateWebHostBuilderInvalidSignature.Program).Assembly);

            Assert.Null(factory);
        }

        [Fact]
        public void CreateWebHostBuilderPattern__InvalidReturnType_CanFindServiceProvider()
        {
            var factory = HostFactoryResolver.ResolveServiceProviderFactory(typeof(CreateWebHostBuilderInvalidSignature.Program).Assembly);

            Assert.NotNull(factory);
            Assert.Null(factory(Array.Empty<string>()));

        }

        [Fact]
        public void CreateHostBuilderPattern_CanFindHostBuilder()
        {
            var factory = HostFactoryResolver.ResolveHostBuilderFactory<IHostBuilder>(typeof(CreateHostBuilderPatternTestSite.Program).Assembly);

            Assert.NotNull(factory);
            Assert.IsAssignableFrom<IHostBuilder>(factory(Array.Empty<string>()));
        }

        [Fact]
        public void CreateHostBuilderPattern_CanFindServiceProvider()
        {
            var factory = HostFactoryResolver.ResolveServiceProviderFactory(typeof(CreateHostBuilderPatternTestSite.Program).Assembly);

            Assert.NotNull(factory);
            Assert.IsAssignableFrom<IServiceProvider>(factory(Array.Empty<string>()));
        }

        [Fact]
        public void CreateHostBuilderPattern__Invalid_CantFindHostBuilder()
        {
            var factory = HostFactoryResolver.ResolveHostBuilderFactory<IHostBuilder>(typeof(CreateHostBuilderInvalidSignature.Program).Assembly);

            Assert.Null(factory);
        }

        [Fact]
        public void CreateHostBuilderPattern__Invalid_CantFindServiceProvider()
        {
            var factory = HostFactoryResolver.ResolveServiceProviderFactory(typeof(CreateHostBuilderInvalidSignature.Program).Assembly);

            Assert.Null(factory);
        }
    }
}
