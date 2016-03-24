// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Internal;
using Microsoft.AspNetCore.Mvc.ViewComponents;
using Xunit;

namespace Microsoft.Extensions.DependencyInjection
{
    public class MvcViewFeaturesMvcBuilderExtensionsTest
    {
        [Fact]
        public void AddViewComponentsAsServices_ReplacesViewComponentActivator()
        {
            // Arrange
            var services = new ServiceCollection();
            var builder = services
                .AddMvc()
                .ConfigureApplicationPartManager(manager =>
                {
                    manager.ApplicationParts.Add(new TestApplicationPart());
                    manager.FeatureProviders.Add(new ViewComponentFeatureProvider());
                });

            // Act
            builder.AddViewComponentsAsServices();

            // Assert
            var descriptor = Assert.Single(services.ToList(), d => d.ServiceType == typeof(IViewComponentActivator));
            Assert.Equal(typeof(ServiceBasedViewComponentActivator), descriptor.ImplementationType);
        }

        [Fact]
        public void AddViewComponentsAsServices_RegistersDiscoveredViewComponents()
        {
            // Arrange
            var services = new ServiceCollection();

            var manager = new ApplicationPartManager();
            manager.ApplicationParts.Add(new TestApplicationPart(
                typeof(ConventionsViewComponent),
                typeof(AttributeViewComponent)));

            manager.FeatureProviders.Add(new TestProvider());

            var builder = new MvcBuilder(services, manager);

            // Act
            builder.AddViewComponentsAsServices();

            // Assert
            var collection = services.ToList();
            Assert.Equal(3, collection.Count);

            Assert.Equal(typeof(ConventionsViewComponent), collection[0].ServiceType);
            Assert.Equal(typeof(ConventionsViewComponent), collection[0].ImplementationType);
            Assert.Equal(ServiceLifetime.Transient, collection[0].Lifetime);

            Assert.Equal(typeof(AttributeViewComponent), collection[1].ServiceType);
            Assert.Equal(typeof(AttributeViewComponent), collection[1].ImplementationType);
            Assert.Equal(ServiceLifetime.Transient, collection[1].Lifetime);

            Assert.Equal(typeof(IViewComponentActivator), collection[2].ServiceType);
            Assert.Equal(typeof(ServiceBasedViewComponentActivator), collection[2].ImplementationType);
            Assert.Equal(ServiceLifetime.Singleton, collection[2].Lifetime);
        }

        public class ConventionsViewComponent
        {
            public string Invoke() => "Hello world";
        }

        [ViewComponent(Name = "AttributesAreGreat")]
        public class AttributeViewComponent
        {
            public Task<string> InvokeAsync() => Task.FromResult("Hello world");
        }

        private class TestProvider : IApplicationFeatureProvider<ViewComponentFeature>
        {
            public void PopulateFeature(IEnumerable<ApplicationPart> parts, ViewComponentFeature feature)
            {
                foreach (var type in parts.OfType<IApplicationPartTypeProvider>().SelectMany(p => p.Types))
                {
                    feature.ViewComponents.Add(type);
                }
            }
        }
    }
}
