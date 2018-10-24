// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Razor.Compilation;
using Microsoft.AspNetCore.Mvc.Razor.Internal;
using Microsoft.AspNetCore.Mvc.Razor.TagHelpers;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.Razor.Test.DependencyInjection
{
    public class MvcRazorMvcCoreBuilderExtensionsTest
    {
        [Fact]
        public void AddMvcCore_OnServiceCollectionWithoutIHostingEnvironmentInstance_DoesNotDiscoverApplicationParts()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            var builder = services
                .AddMvcCore();

            // Assert
            Assert.Empty(builder.PartManager.ApplicationParts);
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        public void AddMvcCore_OnServiceCollectionWithIHostingEnvironmentInstanceWithInvalidApplicationName_DoesNotDiscoverApplicationParts(string applicationName)
        {
            // Arrange
            var services = new ServiceCollection();

            var hostingEnvironment = new Mock<IHostingEnvironment>();
            hostingEnvironment
                .Setup(h => h.ApplicationName)
                .Returns(applicationName);

            services.AddSingleton(hostingEnvironment.Object);

            // Act
            var builder = services
                .AddMvcCore();

            // Assert
            Assert.Empty(builder.PartManager.ApplicationParts);
        }

        [Fact]
        public void AddRazorViewEngine_AddsMetadataReferenceFeatureProvider()
        {
            // Arrange
            var services = new ServiceCollection();
            var builder = services.AddMvcCore();

            // Act
            builder.AddRazorViewEngine();

            // Assert
#pragma warning disable CS0618 // Type or member is obsolete
            Assert.Single(builder.PartManager.FeatureProviders.OfType<MetadataReferenceFeatureProvider>());
#pragma warning restore CS0618 // Type or member is obsolete
        }

        [Fact]
        public void AddRazorViewEngine_DoesNotAddMultipleMetadataReferenceFeatureProvider_OnMultipleInvocations()
        {
            // Arrange
            var services = new ServiceCollection();
            var builder = services.AddMvcCore();

            // Act - 1
            builder.AddRazorViewEngine();

            // Act - 2
            builder.AddRazorViewEngine();

            // Assert
#pragma warning disable CS0618 // Type or member is obsolete
            Assert.Single(builder.PartManager.FeatureProviders.OfType<MetadataReferenceFeatureProvider>());
#pragma warning restore CS0618 // Type or member is obsolete
        }

        [Fact]
        public void AddRazorViewEngine_DoesNotReplaceExistingMetadataReferenceFeatureProvider()
        {
            // Arrange
            var services = new ServiceCollection();
            var builder = services.AddMvcCore();
#pragma warning disable CS0618 // Type or member is obsolete
            var metadataReferenceFeatureProvider = new MetadataReferenceFeatureProvider();
#pragma warning restore CS0618 // Type or member is obsolete
            builder.PartManager.FeatureProviders.Add(metadataReferenceFeatureProvider);

            // Act
            builder.AddRazorViewEngine();

            // Assert
            var actual = Assert.Single(
#pragma warning disable CS0618 // Type or member is obsolete
                collection: builder.PartManager.FeatureProviders.OfType<MetadataReferenceFeatureProvider>());
#pragma warning restore CS0618 // Type or member is obsolete
            Assert.Same(metadataReferenceFeatureProvider, actual);
        }

        [Fact]
        public void AddTagHelpersAsServices_ReplacesTagHelperActivatorAndTagHelperTypeResolver()
        {
            // Arrange
            var services = new ServiceCollection();
            var builder = services
                .AddMvcCore()
                .ConfigureApplicationPartManager(manager =>
                {
                    manager.ApplicationParts.Add(new TestApplicationPart());
                });

            // Act
            builder.AddTagHelpersAsServices();

            // Assert
            var activatorDescriptor = Assert.Single(services.ToList(), d => d.ServiceType == typeof(ITagHelperActivator));
            Assert.Equal(typeof(ServiceBasedTagHelperActivator), activatorDescriptor.ImplementationType);
        }

        [Fact]
        public void AddTagHelpersAsServices_RegistersDiscoveredTagHelpers()
        {
            // Arrange
            var services = new ServiceCollection();

            var manager = new ApplicationPartManager();
            manager.ApplicationParts.Add(new TestApplicationPart(
                typeof(TestTagHelperOne),
                typeof(TestTagHelperTwo)));

            manager.FeatureProviders.Add(new TestFeatureProvider());

            var builder = new MvcCoreBuilder(services, manager);

            // Act
            builder.AddTagHelpersAsServices();

            // Assert
            var collection = services.ToList();
            Assert.Equal(3, collection.Count);

            var tagHelperOne = Assert.Single(collection, t => t.ServiceType == typeof(TestTagHelperOne));
            Assert.Equal(typeof(TestTagHelperOne), tagHelperOne.ImplementationType);
            Assert.Equal(ServiceLifetime.Transient, tagHelperOne.Lifetime);

            var tagHelperTwo = Assert.Single(collection, t => t.ServiceType == typeof(TestTagHelperTwo));
            Assert.Equal(typeof(TestTagHelperTwo), tagHelperTwo.ImplementationType);
            Assert.Equal(ServiceLifetime.Transient, tagHelperTwo.Lifetime);

            var activator = Assert.Single(collection, t => t.ServiceType == typeof(ITagHelperActivator));
            Assert.Equal(typeof(ServiceBasedTagHelperActivator), activator.ImplementationType);
            Assert.Equal(ServiceLifetime.Transient, activator.Lifetime);
        }

        private class TestTagHelperOne : TagHelper
        {
        }

        private class TestTagHelperTwo : TagHelper
        {
        }

        private class TestFeatureProvider : IApplicationFeatureProvider<TagHelperFeature>
        {
            public void PopulateFeature(IEnumerable<ApplicationPart> parts, TagHelperFeature feature)
            {
                foreach (var type in parts.OfType<IApplicationPartTypeProvider>().SelectMany(tp => tp.Types))
                {
                    feature.TagHelpers.Add(type);
                }
            }
        }
    }
}
