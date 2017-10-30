// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Mvc.Internal;
using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.ModelBinding
{
    public class ModelBinderFactoryTest
    {
        [Fact]
        public void CreateBinder_Throws_WhenNoProviders()
        {
            // Arrange
            var expected = $"'{typeof(MvcOptions).FullName}.{nameof(MvcOptions.ModelBinderProviders)}' must not be " +
                $"empty. At least one '{typeof(IModelBinderProvider).FullName}' is required to model bind.";
            var metadataProvider = new TestModelMetadataProvider();
            var options = Options.Create(new MvcOptions());
            var factory = new ModelBinderFactory(metadataProvider, options);
            var context = new ModelBinderFactoryContext()
            {
                Metadata = metadataProvider.GetMetadataForType(typeof(string)),
            };

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() => factory.CreateBinder(context));
            Assert.Equal(expected, exception.Message);
        }

        [Fact]
        public void CreateBinder_Throws_WhenBinderNotCreated()
        {
            // Arrange
            var metadataProvider = new TestModelMetadataProvider();
            var options = Options.Create(new MvcOptions());
            options.Value.ModelBinderProviders.Add(new TestModelBinderProvider(_ => null));

            var factory = new ModelBinderFactory(metadataProvider, options);
            var context = new ModelBinderFactoryContext()
            {
                Metadata = metadataProvider.GetMetadataForType(typeof(string)),
            };

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() => factory.CreateBinder(context));
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
            var options = Options.Create(new MvcOptions());
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

            var options = Options.Create(new MvcOptions());
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

            var options = Options.Create(new MvcOptions());
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

            var options = Options.Create(new MvcOptions());
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

            var options = Options.Create(new MvcOptions());
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

            var options = Options.Create(new MvcOptions());
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

            var options = Options.Create(new MvcOptions());
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

        [Fact]
        public void CreateBinder_Caches_NonRootNodes()
        {
            // Arrange
            var metadataProvider = new TestModelMetadataProvider();

            var options = Options.Create(new MvcOptions());

            IModelBinder inner = null;

            var widgetProvider = new TestModelBinderProvider(c =>
            {
                if (c.Metadata.ModelType == typeof(Widget))
                {
                    var binder = c.CreateBinder(c.Metadata.Properties[nameof(Widget.Id)]);
                    if (inner == null)
                    {
                        inner = binder;
                    }
                    else
                    {
                        Assert.Same(inner, binder);
                    }

                    return Mock.Of<IModelBinder>();
                }

                return null;
            });

            var widgetIdProvider = new TestModelBinderProvider(c =>
            {
                Assert.Equal(typeof(WidgetId), c.Metadata.ModelType);
                return Mock.Of<IModelBinder>();
            });

            options.Value.ModelBinderProviders.Add(widgetProvider);
            options.Value.ModelBinderProviders.Add(widgetIdProvider);

            var factory = new ModelBinderFactory(metadataProvider, options);

            var context = new ModelBinderFactoryContext()
            {
                Metadata = metadataProvider.GetMetadataForType(typeof(Widget)),
                CacheToken = null, // We want the outermost provider to run twice.
            };

            // Act
            var result1 = factory.CreateBinder(context);
            var result2 = factory.CreateBinder(context);

            // Assert
            Assert.NotSame(result1, result2);

            Assert.Equal(2, widgetProvider.SuccessCount);
            Assert.Equal(1, widgetIdProvider.SuccessCount);
        }

        [Fact]
        public void CreateBinder_Caches_NonRootNodes_WhenNonRootNodeReturnsNull()
        {
            // Arrange
            var metadataProvider = new TestModelMetadataProvider();

            var options = Options.Create(new MvcOptions());

            IModelBinder inner = null;

            var widgetProvider = new TestModelBinderProvider(c =>
            {
                if (c.Metadata.ModelType == typeof(Widget))
                {
                    var binder = c.CreateBinder(c.Metadata.Properties[nameof(Widget.Id)]);
                    Assert.IsType<NoOpBinder>(binder);
                    if (inner == null)
                    {
                        inner = binder;
                    }
                    else
                    {
                        Assert.Same(inner, binder);
                    }

                    return Mock.Of<IModelBinder>();
                }

                return null;
            });

            var widgetIdProvider = new TestModelBinderProvider(c =>
            {
                Assert.Equal(typeof(WidgetId), c.Metadata.ModelType);
                return null;
            });

            options.Value.ModelBinderProviders.Add(widgetProvider);
            options.Value.ModelBinderProviders.Add(widgetIdProvider);

            var factory = new ModelBinderFactory(metadataProvider, options);

            var context = new ModelBinderFactoryContext()
            {
                Metadata = metadataProvider.GetMetadataForType(typeof(Widget)),
                CacheToken = null, // We want the outermost provider to run twice.
            };

            // Act
            var result1 = factory.CreateBinder(context);
            var result2 = factory.CreateBinder(context);

            // Assert
            Assert.NotSame(result1, result2);

            Assert.Equal(2, widgetProvider.SuccessCount);
            Assert.Equal(0, widgetIdProvider.SuccessCount);
        }

        // The fact that we use the ModelMetadata as the token is important for caching
        // and sharing with TryUpdateModel.
        [Fact]
        public void CreateBinder_Caches_NonRootNodes_UsesModelMetadataAsToken()
        {
            // Arrange
            var metadataProvider = new TestModelMetadataProvider();

            var options = Options.Create(new MvcOptions());

            IModelBinder inner = null;

            var widgetProvider = new TestModelBinderProvider(c =>
            {
                if (c.Metadata.ModelType == typeof(Widget))
                {
                    inner = c.CreateBinder(c.Metadata.Properties[nameof(Widget.Id)]);
                    return Mock.Of<IModelBinder>();
                }

                return null;
            });

            var widgetIdProvider = new TestModelBinderProvider(c =>
            {
                Assert.Equal(typeof(WidgetId), c.Metadata.ModelType);
                return Mock.Of<IModelBinder>();
            });

            options.Value.ModelBinderProviders.Add(widgetProvider);
            options.Value.ModelBinderProviders.Add(widgetIdProvider);

            var factory = new ModelBinderFactory(metadataProvider, options);

            var context = new ModelBinderFactoryContext()
            {
                Metadata = metadataProvider.GetMetadataForType(typeof(Widget)),
                CacheToken = null,
            };

            // Act 1
            var result1 = factory.CreateBinder(context);

            context.Metadata = context.Metadata.Properties[nameof(Widget.Id)];
            context.CacheToken = context.Metadata;

            // Act 2
            var result2 = factory.CreateBinder(context);

            // Assert
            Assert.Same(inner, result2);
            Assert.Equal(1, widgetProvider.SuccessCount);
            Assert.Equal(1, widgetIdProvider.SuccessCount);
        }

        // This is a really weird case, but I wanted to make sure it's covered so it doesn't
        // blow up in weird ways.
        //
        // If a binder provider tries to recursively create itself, but then returns null, we've
        // already returned and possibly cached the PlaceholderBinder instance, we want to make sure that
        // instance won't nullref.
        [Fact]
        public void CreateBinder_Caches_NonRootNodes_FixesUpPlaceholderBinder()
        {
            // Arrange
            var metadataProvider = new TestModelMetadataProvider();

            var options = Options.Create(new MvcOptions());

            IModelBinder inner = null;
            IModelBinder innerInner = null;

            var widgetProvider = new TestModelBinderProvider(c =>
            {
                if (c.Metadata.ModelType == typeof(Widget))
                {
                    inner = c.CreateBinder(c.Metadata.Properties[nameof(Widget.Id)]);
                    return Mock.Of<IModelBinder>();
                }

                return null;
            });

            var widgetIdProvider = new TestModelBinderProvider(c =>
            {
                Assert.Equal(typeof(WidgetId), c.Metadata.ModelType);
                innerInner = c.CreateBinder(c.Metadata);
                return null;
            });

            options.Value.ModelBinderProviders.Add(widgetProvider);
            options.Value.ModelBinderProviders.Add(widgetIdProvider);

            var factory = new ModelBinderFactory(metadataProvider, options);

            var context = new ModelBinderFactoryContext()
            {
                Metadata = metadataProvider.GetMetadataForType(typeof(Widget)),
                CacheToken = null,
            };

            // Act 1
            var result1 = factory.CreateBinder(context);

            context.Metadata = context.Metadata.Properties[nameof(Widget.Id)];
            context.CacheToken = context.Metadata;

            // Act 2
            var result2 = factory.CreateBinder(context);

            // Assert
            Assert.Same(inner, result2);
            Assert.NotSame(inner, innerInner);

            var placeholder = Assert.IsType<PlaceholderBinder>(innerInner);
            Assert.IsType<NoOpBinder>(placeholder.Inner);

            Assert.Equal(1, widgetProvider.SuccessCount);
            Assert.Equal(0, widgetIdProvider.SuccessCount);
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

            public int SuccessCount { get; private set; }

            public IModelBinder GetBinder(ModelBinderProviderContext context)
            {
                var binder = _factory(context);
                if (binder != null)
                {
                    SuccessCount++;
                }

                return binder;
            }
        }
    }
}
