// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Mvc.ModelBinding.Internal;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.ModelBinding
{
    public class ModelBinderFactoryTest
    {
        // No providers => can't create a binder
        [Fact]
        public void CreateBinder_Throws_WhenBinderNotCreated()
        {
            // Arrange 
            var metadataProvider = new TestModelMetadataProvider();
            var options = new TestOptionsManager<MvcOptions>();
            var factory = new ModelBinderFactory(metadataProvider, options);

            var context = new ModelBinderFactoryContext()
            {
                Metadata = metadataProvider.GetMetadataForType(typeof(string)),
            };

            // Act
            var exception = Assert.Throws<InvalidOperationException>(() => factory.CreateBinder(context));

            // Assert
            Assert.Equal(
                $"Could not create a model binder for model object of type '{typeof(string).FullName}'.",
                exception.Message);
        }

        [Fact]
        public void CreateBinder_CreatesNoOpBinder_WhenPropertyDoesntHaveABinder()
        {
            // Arrange 
            var metadataProvider = new TestModelMetadataProvider();

            // There isn't a provider that can handle WidgetId.
            var options = new TestOptionsManager<MvcOptions>();
            options.Value.ModelBinderProviders.Add(new TestModelBinderProvider(c =>
            {
                if (c.Metadata.ModelType == typeof(Widget))
                {
                    Assert.NotNull(c.CreateBinder(c.Metadata.Properties[nameof(Widget.Id)]));
                    return Mock.Of<IModelBinder>();
                }

                return null;
            }));

            var factory = new ModelBinderFactory(metadataProvider, options);

            var context = new ModelBinderFactoryContext()
            {
                Metadata = metadataProvider.GetMetadataForType(typeof(Widget)),
            };

            // Act
            var result = factory.CreateBinder(context);

            // Assert
            Assert.NotNull(result);
        }

        [Fact]
        public void CreateBinder_NestedProperties()
        {
            // Arrange 
            var metadataProvider = new TestModelMetadataProvider();

            var options = new TestOptionsManager<MvcOptions>();
            options.Value.ModelBinderProviders.Add(new TestModelBinderProvider(c =>
            {
                if (c.Metadata.ModelType == typeof(Widget))
                {
                    Assert.NotNull(c.CreateBinder(c.Metadata.Properties[nameof(Widget.Id)]));
                    return Mock.Of<IModelBinder>();
                }
                else if (c.Metadata.ModelType == typeof(WidgetId))
                {
                    return Mock.Of<IModelBinder>();
                }

                return null;
            }));

            var factory = new ModelBinderFactory(metadataProvider, options);

            var context = new ModelBinderFactoryContext()
            {
                Metadata = metadataProvider.GetMetadataForType(typeof(Widget)),
            };

            // Act
            var result = factory.CreateBinder(context);

            // Assert
            Assert.NotNull(result);
        }

        [Fact]
        public void CreateBinder_BreaksCycles()
        {
            // Arrange 
            var metadataProvider = new TestModelMetadataProvider();

            var callCount = 0;

            var options = new TestOptionsManager<MvcOptions>();
            options.Value.ModelBinderProviders.Add(new TestModelBinderProvider(c =>
            {
                var currentCallCount = ++callCount;
                Assert.Equal(typeof(Employee), c.Metadata.ModelType);
                var binder = c.CreateBinder(c.Metadata.Properties[nameof(Employee.Manager)]);

                if (currentCallCount == 2)
                {
                    Assert.IsType<PlaceholderBinder>(binder);
                }

                return Mock.Of<IModelBinder>();
            }));

            var factory = new ModelBinderFactory(metadataProvider, options);

            var context = new ModelBinderFactoryContext()
            {
                Metadata = metadataProvider.GetMetadataForType(typeof(Employee)),
            };

            // Act
            var result = factory.CreateBinder(context);

            // Assert
            Assert.NotNull(result);
        }

        [Fact]
        public void CreateBinder_DoesNotCache_WhenTokenIsNull()
        {
            // Arrange 
            var metadataProvider = new TestModelMetadataProvider();

            var options = new TestOptionsManager<MvcOptions>();
            options.Value.ModelBinderProviders.Add(new TestModelBinderProvider(c =>
            {
                Assert.Equal(typeof(Employee), c.Metadata.ModelType);
                return Mock.Of<IModelBinder>();
            }));

            var factory = new ModelBinderFactory(metadataProvider, options);

            var context = new ModelBinderFactoryContext()
            {
                Metadata = metadataProvider.GetMetadataForType(typeof(Employee)),
            };

            // Act
            var result1 = factory.CreateBinder(context);
            var result2 = factory.CreateBinder(context);

            // Assert
            Assert.NotSame(result1, result2);
        }

        [Fact]
        public void CreateBinder_Caches_WhenTokenIsNotNull()
        {
            // Arrange 
            var metadataProvider = new TestModelMetadataProvider();

            var options = new TestOptionsManager<MvcOptions>();
            options.Value.ModelBinderProviders.Add(new TestModelBinderProvider(c =>
            {
                Assert.Equal(typeof(Employee), c.Metadata.ModelType);
                return Mock.Of<IModelBinder>();
            }));

            var factory = new ModelBinderFactory(metadataProvider, options);

            var context = new ModelBinderFactoryContext()
            {
                Metadata = metadataProvider.GetMetadataForType(typeof(Employee)),
                CacheToken = new object(),
            };

            // Act
            var result1 = factory.CreateBinder(context);
            var result2 = factory.CreateBinder(context);

            // Assert
            Assert.Same(result1, result2);
        }

        private class Widget
        {
            public WidgetId Id { get; set; }
        }

        private class WidgetId
        {
        }

        private class Employee
        {
            public Employee Manager { get; set; }
        }

        private class TestModelBinderProvider : IModelBinderProvider
        {
            private readonly Func<ModelBinderProviderContext, IModelBinder> _factory;

            public TestModelBinderProvider(Func<ModelBinderProviderContext, IModelBinder> factory)
            {
                _factory = factory;
            }

            public IModelBinder GetBinder(ModelBinderProviderContext context)
            {
                return _factory(context);
            }
        }
    }
}
