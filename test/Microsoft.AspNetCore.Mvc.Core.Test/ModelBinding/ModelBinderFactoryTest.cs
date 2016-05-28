// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Mvc.Internal;
using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;
using Microsoft.AspNetCore.Mvc.ModelBinding.Internal;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
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
        public void CreateBinder_CreatesNoOpBinder_WhenPropertyBindingIsNotAllowed()
        {
            // Arrange
            var metadataProvider = new TestModelMetadataProvider();
            metadataProvider
                .ForProperty<Widget>(nameof(Widget.Id))
                .BindingDetails(m => m.IsBindingAllowed = false);

            var modelBinder = new ByteArrayModelBinder();

            var options = new TestOptionsManager<MvcOptions>();
            options.Value.ModelBinderProviders.Add(new TestModelBinderProvider(c =>
            {
                if (c.Metadata.ModelType == typeof(WidgetId))
                {
                    return modelBinder;
                }

                return null;
            }));

            var factory = new ModelBinderFactory(metadataProvider, options);

            var context = new ModelBinderFactoryContext()
            {
                Metadata = metadataProvider.GetMetadataForProperty(typeof(Widget), nameof(Widget.Id)),
            };

            // Act
            var result = factory.CreateBinder(context);

            // Assert
            Assert.NotNull(result);
            Assert.IsType<NoOpBinder>(result);
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

        public static TheoryData BindingInfoData
        {
            get
            {
                var propertyFilterProvider = Mock.Of<IPropertyFilterProvider>();

                var emptyBindingInfo = new BindingInfo();
                var halfBindingInfo = new BindingInfo
                {
                    BinderModelName = "expected name",
                    BinderType = typeof(Widget),
                };
                var fullBindingInfo = new BindingInfo
                {
                    BinderModelName = "expected name",
                    BinderType = typeof(Widget),
                    BindingSource = BindingSource.Services,
                    PropertyFilterProvider = propertyFilterProvider,
                };

                var emptyBindingMetadata = new BindingMetadata();
                var differentBindingMetadata = new BindingMetadata
                {
                    BinderModelName = "not the expected name",
                    BinderType = typeof(WidgetId),
                    BindingSource = BindingSource.ModelBinding,
                    PropertyFilterProvider = Mock.Of<IPropertyFilterProvider>(),
                };
                var secondHalfBindingMetadata = new BindingMetadata
                {
                    BindingSource = BindingSource.Services,
                    PropertyFilterProvider = propertyFilterProvider,
                };
                var fullBindingMetadata = new BindingMetadata
                {
                    BinderModelName = "expected name",
                    BinderType = typeof(Widget),
                    BindingSource = BindingSource.Services,
                    PropertyFilterProvider = propertyFilterProvider,
                };

                // parameterBindingInfo, bindingMetadata, expectedInfo
                return new TheoryData<BindingInfo, BindingMetadata, BindingInfo>
                {
                    { emptyBindingInfo, emptyBindingMetadata, emptyBindingInfo },
                    { fullBindingInfo, emptyBindingMetadata, fullBindingInfo },
                    { emptyBindingInfo, fullBindingMetadata, fullBindingInfo },
                    // Resulting BindingInfo combines two inputs
                    { halfBindingInfo, secondHalfBindingMetadata, fullBindingInfo },
                    // Parameter information has precedence over type metadata
                    { fullBindingInfo, differentBindingMetadata, fullBindingInfo },
                };
            }
        }

        [Theory]
        [MemberData(nameof(BindingInfoData))]
        public void CreateBinder_PassesExpectedBindingInfo(
            BindingInfo parameterBindingInfo,
            BindingMetadata bindingMetadata,
            BindingInfo expectedInfo)
        {
            // Arrange
            var metadataProvider = new TestModelMetadataProvider();
            metadataProvider.ForType<Employee>().BindingDetails(binding =>
            {
                binding.BinderModelName = bindingMetadata.BinderModelName;
                binding.BinderType = bindingMetadata.BinderType;
                binding.BindingSource = bindingMetadata.BindingSource;
                if (bindingMetadata.PropertyFilterProvider != null)
                {
                    binding.PropertyFilterProvider = bindingMetadata.PropertyFilterProvider;
                }
            });

            var modelBinder = Mock.Of<IModelBinder>();
            var modelBinderProvider = new TestModelBinderProvider(context =>
            {
                Assert.Equal(typeof(Employee), context.Metadata.ModelType);

                Assert.NotNull(context.BindingInfo);
                Assert.Equal(expectedInfo.BinderModelName, context.BindingInfo.BinderModelName, StringComparer.Ordinal);
                Assert.Equal(expectedInfo.BinderType, context.BindingInfo.BinderType);
                Assert.Equal(expectedInfo.BindingSource, context.BindingInfo.BindingSource);
                Assert.Same(expectedInfo.PropertyFilterProvider, context.BindingInfo.PropertyFilterProvider);

                return modelBinder;
            });

            var options = new TestOptionsManager<MvcOptions>();
            options.Value.ModelBinderProviders.Insert(0, modelBinderProvider);

            var factory = new ModelBinderFactory(metadataProvider, options);
            var factoryContext = new ModelBinderFactoryContext
            {
                BindingInfo = parameterBindingInfo,
                Metadata = metadataProvider.GetMetadataForType(typeof(Employee)),
            };

            // Act & Assert
            var result = factory.CreateBinder(factoryContext);

            // Confirm our IModelBinderProvider was called.
            Assert.Same(modelBinder, result);
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
