// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.RazorPages.Internal
{
    public class PagePropertyBinderFactoryTest
    {
        [Fact]
        public void GetModelBinderFactory_ReturnsNullIfPageHasNoBoundProperties()
        {
            // Arrange
            var actionDescriptor = new CompiledPageActionDescriptor
            {
                PageTypeInfo = typeof(PageWithNoBoundProperties).GetTypeInfo(),
            };
            var modelMetadataProvider = TestModelMetadataProvider.CreateDefaultProvider();
            var binder = new ParameterBinder(
                modelMetadataProvider,
                TestModelBinderFactory.CreateDefault(),
                Mock.Of<IObjectModelValidator>());

            // Act
            var factory = PagePropertyBinderFactory.CreateBinder(binder, modelMetadataProvider, actionDescriptor);

            // Assert
            Assert.Null(factory);
        }

        [Fact]
        public void GetModelBinderFactory_ReturnsNullIfPageModelHasNoBoundProperties()
        {
            // Arrange
            var actionDescriptor = new CompiledPageActionDescriptor
            {
                PageTypeInfo = typeof(PageWithNoBoundProperties).GetTypeInfo(),
                ModelTypeInfo = typeof(PageModelWithNoBoundProperties).GetTypeInfo(),
            };
            var modelMetadataProvider = TestModelMetadataProvider.CreateDefaultProvider();

            var binder = new ParameterBinder(
                TestModelMetadataProvider.CreateDefaultProvider(),
                TestModelBinderFactory.CreateDefault(),
                Mock.Of<IObjectModelValidator>());

            // Act
            var factory = PagePropertyBinderFactory.CreateBinder(binder, modelMetadataProvider, actionDescriptor);

            // Assert
            Assert.Null(factory);
        }

        [Fact]
        public void GetModelBinderFactory_ReturnsNullIfPageHasNoVisibleBoundProperties()
        {
            // Arrange
            var actionDescriptor = new CompiledPageActionDescriptor
            {
                PageTypeInfo = typeof(PageWithNoVisibleBoundProperties).GetTypeInfo(),
            };
            var modelMetadataProvider = TestModelMetadataProvider.CreateDefaultProvider();
            var binder = new ParameterBinder(
                modelMetadataProvider,
                TestModelBinderFactory.CreateDefault(),
                Mock.Of<IObjectModelValidator>());

            // Act
            var factory = PagePropertyBinderFactory.CreateBinder(binder, modelMetadataProvider, actionDescriptor);

            // Assert
            Assert.Null(factory);
        }

        [Fact]
        public void GetModelBinderFactory_ReturnsNullIfPageModelHasNoVisibleBoundProperties()
        {
            // Arrange
            var actionDescriptor = new CompiledPageActionDescriptor
            {
                PageTypeInfo = typeof(object).GetTypeInfo(),
                ModelTypeInfo = typeof(PageModelWithNoVisibleBoundProperties).GetTypeInfo(),
            };
            var modelMetadataProvider = TestModelMetadataProvider.CreateDefaultProvider();
            var binder = new ParameterBinder(
                modelMetadataProvider,
                TestModelBinderFactory.CreateDefault(),
                Mock.Of<IObjectModelValidator>());

            // Act
            var factory = PagePropertyBinderFactory.CreateBinder(binder, modelMetadataProvider, actionDescriptor);

            // Assert
            Assert.Null(factory);
        }

        [Fact]
        public void GetModelBinderFactory_ReturnsNullIfPageHasNoSettableBoundProperties()
        {
            // Arrange
            var actionDescriptor = new CompiledPageActionDescriptor
            {
                PageTypeInfo = typeof(PageWithReadOnlyProperties).GetTypeInfo(),
            };
            var modelMetadataProvider = TestModelMetadataProvider.CreateDefaultProvider();
            var binder = new ParameterBinder(
                modelMetadataProvider,
                TestModelBinderFactory.CreateDefault(),
                Mock.Of<IObjectModelValidator>());

            // Act
            var factory = PagePropertyBinderFactory.CreateBinder(binder, modelMetadataProvider, actionDescriptor);

            // Assert
            Assert.Null(factory);
        }

        [Fact]
        public void GetModelBinderFactory_ReturnsNullIfPageModelHasNoSettableBoundProperties()
        {
            // Arrange
            var actionDescriptor = new CompiledPageActionDescriptor
            {
                PageTypeInfo = typeof(PageWithReadOnlyProperties).GetTypeInfo(),
                ModelTypeInfo = typeof(PageModelWithReadOnlyProperties).GetTypeInfo(),
            };
            var modelMetadataProvider = TestModelMetadataProvider.CreateDefaultProvider();
            var binder = new ParameterBinder(
                modelMetadataProvider,
                TestModelBinderFactory.CreateDefault(),
                Mock.Of<IObjectModelValidator>());

            // Act
            var factory = PagePropertyBinderFactory.CreateBinder(binder, modelMetadataProvider, actionDescriptor);

            // Assert
            Assert.Null(factory);
        }

        [Fact]
        public async Task ModelBinderFactory_BindsPropertiesOnPage()
        {
            // Arrange
            var actionDescriptor = new CompiledPageActionDescriptor
            {
                PageTypeInfo = typeof(PageWithProperty).GetTypeInfo(),
            };
            var modelMetadataProvider = TestModelMetadataProvider.CreateDefaultProvider();
            var binder = new TestParameterBinder(new Dictionary<string, object>
            {
                { nameof(PageWithProperty.Id), 10 },
                { nameof(PageWithProperty.RouteDifferentValue), "route-value" }
            });
            var factory = PagePropertyBinderFactory.CreateBinder(binder, modelMetadataProvider, actionDescriptor);
            var page = new PageWithProperty
            {
                PageContext = new PageContext(),
            };

            // Act
            await factory(page, null);

            // Assert
            Assert.Equal(10, page.Id);
            Assert.Equal("route-value", page.RouteDifferentValue);
            Assert.Null(page.PropertyWithNoValue);
            Assert.Collection(binder.Descriptors,
                descriptor =>
                {
                    Assert.Equal(nameof(PageWithProperty.Id), descriptor.Name);
                    Assert.Null(descriptor.BindingInfo.BinderModelName);
                    Assert.Equal(BindingSource.Query, descriptor.BindingInfo.BindingSource);
                    Assert.Null(descriptor.BindingInfo.BinderType);
                    Assert.Null(descriptor.BindingInfo.PropertyFilterProvider);
                    Assert.Equal(typeof(int), descriptor.ParameterType);
                },
                descriptor =>
                {
                    Assert.Equal(nameof(PageWithProperty.RouteDifferentValue), descriptor.Name);
                    Assert.Equal("route-value", descriptor.BindingInfo.BinderModelName);
                    Assert.Equal(BindingSource.Path, descriptor.BindingInfo.BindingSource);
                    Assert.Null(descriptor.BindingInfo.BinderType);
                    Assert.Null(descriptor.BindingInfo.PropertyFilterProvider);
                    Assert.Equal(typeof(string), descriptor.ParameterType);
                },
                descriptor =>
                {
                    Assert.Equal(nameof(PageWithProperty.PropertyWithNoValue), descriptor.Name);
                    Assert.Null(descriptor.BindingInfo.BinderModelName);
                    Assert.Equal(BindingSource.Form, descriptor.BindingInfo.BindingSource);
                    Assert.Null(descriptor.BindingInfo.BinderType);
                    Assert.Null(descriptor.BindingInfo.PropertyFilterProvider);
                    Assert.Equal(typeof(string), descriptor.ParameterType);
                });
        }

        [Fact]
        public async Task ModelBinderFactory_BindsPropertiesOnPageModel()
        {
            // Arrange
            var actionDescriptor = new CompiledPageActionDescriptor
            {
                PageTypeInfo = typeof(PageWithProperty).GetTypeInfo(),
                ModelTypeInfo = typeof(PageModelWithProperty).GetTypeInfo(),
            };
            var binder = new TestParameterBinder(new Dictionary<string, object>
            {
                { nameof(PageModelWithProperty.Id), 10 },
                { nameof(PageModelWithProperty.RouteDifferentValue), "route-value" }
            });
            var modelMetadataProvider = TestModelMetadataProvider.CreateDefaultProvider();
            var factory = PagePropertyBinderFactory.CreateBinder(binder, modelMetadataProvider, actionDescriptor);
            var page = new PageWithProperty
            {
                PageContext = new PageContext(),
            };
            var model = new PageModelWithProperty();

            // Act
            await factory(page, model);

            // Assert
            // Verify that the page properties were not bound.
            Assert.Equal(default(int), page.Id);
            Assert.Null(page.RouteDifferentValue);

            Assert.Equal(10, model.Id);
            Assert.Equal("route-value", model.RouteDifferentValue);
            Assert.Null(model.PropertyWithNoValue);

            Assert.Collection(binder.Descriptors,
                descriptor =>
                {
                    Assert.Equal(nameof(PageModelWithProperty.Id), descriptor.Name);
                    Assert.Equal(BindingSource.Query, descriptor.BindingInfo.BindingSource);
                    Assert.Null(descriptor.BindingInfo.BinderType);
                    Assert.Null(descriptor.BindingInfo.PropertyFilterProvider);
                    Assert.Equal(typeof(int), descriptor.ParameterType);
                },
                descriptor =>
                {
                    Assert.Equal(nameof(PageModelWithProperty.RouteDifferentValue), descriptor.Name);
                    Assert.Equal("route-value", descriptor.BindingInfo.BinderModelName);
                    Assert.Equal(BindingSource.Path, descriptor.BindingInfo.BindingSource);
                    Assert.Null(descriptor.BindingInfo.BinderType);
                    Assert.Null(descriptor.BindingInfo.PropertyFilterProvider);
                    Assert.Equal(typeof(string), descriptor.ParameterType);
                },
                descriptor =>
                {
                    Assert.Equal(nameof(PageModelWithProperty.PropertyWithNoValue), descriptor.Name);
                    Assert.Null(descriptor.BindingInfo.BinderModelName);
                    Assert.Equal(BindingSource.Form, descriptor.BindingInfo.BindingSource);
                    Assert.Null(descriptor.BindingInfo.BinderType);
                    Assert.Null(descriptor.BindingInfo.PropertyFilterProvider);
                    Assert.Equal(typeof(string), descriptor.ParameterType);
                });
        }

        [Fact]
        public async Task ModelBinderFactory_DiscoversBinderType()
        {
            // Arrange
            var actionDescriptor = new CompiledPageActionDescriptor
            {
                PageTypeInfo = typeof(PageWithProperty).GetTypeInfo(),
                ModelTypeInfo = typeof(PageModelWithModelBinderAttribute).GetTypeInfo(),
            };
            var expected = Guid.NewGuid();
            var binder = new TestParameterBinder(new Dictionary<string, object>
            {
                { nameof(PageModelWithModelBinderAttribute.PropertyWithBinderType), expected },
            });
            var modelMetadataProvider = TestModelMetadataProvider.CreateDefaultProvider();
            var factory = PagePropertyBinderFactory.CreateBinder(binder, modelMetadataProvider, actionDescriptor);
            var page = new PageWithProperty
            {
                PageContext = new PageContext(),
            };
            var model = new PageModelWithModelBinderAttribute();

            // Act
            await factory(page, model);

            // Assert
            Assert.Equal(expected, model.PropertyWithBinderType);
            Assert.Collection(binder.Descriptors,
                descriptor =>
                {
                    Assert.Equal(nameof(PageModelWithModelBinderAttribute.PropertyWithBinderType), descriptor.Name);
                    Assert.Equal(BindingSource.Custom, descriptor.BindingInfo.BindingSource);
                    Assert.Equal(typeof(DeclarativeSecurityAction), descriptor.BindingInfo.BinderType);
                    Assert.Null(descriptor.BindingInfo.PropertyFilterProvider);
                    Assert.Equal(typeof(Guid), descriptor.ParameterType);
                });
        }

        [Fact]
        public async Task ModelBinderFactory_DiscoversPropertyFilter()
        {
            // Arrange
            var actionDescriptor = new CompiledPageActionDescriptor
            {
                PageTypeInfo = typeof(PageWithProperty).GetTypeInfo(),
                ModelTypeInfo = typeof(PageModelWithPropertyFilterAttribute).GetTypeInfo(),
            };
            var binder = new TestParameterBinder(new Dictionary<string, object>());
            var modelMetadataProvider = TestModelMetadataProvider.CreateDefaultProvider();
            var factory = PagePropertyBinderFactory.CreateBinder(binder, modelMetadataProvider, actionDescriptor);
            var page = new PageWithProperty
            {
                PageContext = new PageContext(),
            };
            var model = new PageModelWithPropertyFilterAttribute();

            // Act
            await factory(page, model);

            // Assert
            Assert.Collection(binder.Descriptors,
                descriptor =>
                {
                    Assert.Equal(nameof(PageModelWithPropertyFilterAttribute.PropertyWithFilter), descriptor.Name);
                    Assert.Null(descriptor.BindingInfo.BindingSource);
                    Assert.Null(descriptor.BindingInfo.BinderType);
                    Assert.IsType<TestPropertyFilterProvider>(descriptor.BindingInfo.PropertyFilterProvider);
                    Assert.Equal(typeof(object), descriptor.ParameterType);
                });
        }

        [Fact]
        public async Task ModelBinderFactory_UsesDefaultValueIfModelBindingFailed()
        {
            // Arrange
            var actionDescriptor = new CompiledPageActionDescriptor
            {
                PageTypeInfo = typeof(PageWithProperty).GetTypeInfo(),
                ModelTypeInfo = typeof(PageModelWithDefaultValue).GetTypeInfo(),
            };
            var binder = new TestParameterBinder(new Dictionary<string, object>());
            var modelMetadataProvider = TestModelMetadataProvider.CreateDefaultProvider();
            var factory = PagePropertyBinderFactory.CreateBinder(binder, modelMetadataProvider, actionDescriptor);
            var page = new PageWithProperty
            {
                PageContext = new PageContext(),
            };
            var model = new PageModelWithDefaultValue();
            var defaultValue = model.PropertyWithDefaultValue;

            // Act
            await factory(page, model);

            // Assert
            Assert.Equal(defaultValue, model.PropertyWithDefaultValue);
        }

        [Fact]
        public async Task ModelBinderFactory_OverwritesDefaultValue()
        {
            // Arrange
            var actionDescriptor = new CompiledPageActionDescriptor
            {
                PageTypeInfo = typeof(PageWithProperty).GetTypeInfo(),
                ModelTypeInfo = typeof(PageModelWithDefaultValue).GetTypeInfo(),
            };
            var expected = "not-default-value";
            var binder = new TestParameterBinder(new Dictionary<string, object>
            {
                { nameof(PageModelWithDefaultValue.PropertyWithDefaultValue), expected },
            });
            var modelMetadataProvider = TestModelMetadataProvider.CreateDefaultProvider();
            var factory = PagePropertyBinderFactory.CreateBinder(binder, modelMetadataProvider, actionDescriptor);
            var page = new PageWithProperty
            {
                PageContext = new PageContext(),
            };
            var model = new PageModelWithDefaultValue();
            var defaultValue = model.PropertyWithDefaultValue;

            // Act
            await factory(page, model);

            // Assert
            Assert.Equal(expected, model.PropertyWithDefaultValue);
        }

        private class TestParameterBinder : ParameterBinder
        {
            private readonly IDictionary<string, object> _args;

            public TestParameterBinder(IDictionary<string, object> args)
                : base(
                    TestModelMetadataProvider.CreateDefaultProvider(),
                    TestModelBinderFactory.CreateDefault(),
                    Mock.Of<IObjectModelValidator>())
            {
                _args = args;
            }

            public IList<ParameterDescriptor> Descriptors { get; } = new List<ParameterDescriptor>();

            public override Task<ModelBindingResult> BindModelAsync(ActionContext actionContext, IValueProvider valueProvider, ParameterDescriptor parameter, object value)
            {
                Descriptors.Add(parameter);

                if (_args.TryGetValue(parameter.Name, out var result))
                {
                    return Task.FromResult(ModelBindingResult.Success(result));
                }

                return Task.FromResult(ModelBindingResult.Failed());
            }
        }

        private class PageModelWithNoBoundProperties : PageModel
        {
        }

        private class PageWithNoBoundProperties : Page
        {
            public override Task ExecuteAsync() => Task.FromResult(0);
        }

        private class PageWithNoVisibleBoundProperties : Page
        {
            [FromBody]
            private string FromBody { get; set; }

            [FromQuery]
            protected string FromQuery { get; set; }

            [FromRoute]
            public static int FromRoute { get; set; }

            public override Task ExecuteAsync() => Task.FromResult(0);
        }

        private class PageModelWithNoVisibleBoundProperties : PageModel
        {
            [FromBody]
            private string FromBody { get; set; }

            [FromQuery]
            protected string FromQuery { get; set; }

            [FromRoute]
            public static int FromRoute { get; set; }
        }

        private class PageWithReadOnlyProperties : Page
        {
            [FromBody]
            private string FromBody { get; }

            public override Task ExecuteAsync() => Task.FromResult(0);
        }

        private class PageModelWithReadOnlyProperties
        {
            [FromBody]
            private string FromBody { get; }
        }

        private class PageWithProperty : Page
        {
            [FromQuery]
            public int Id { get; set; }

            [FromRoute(Name = "route-value")]
            public string RouteDifferentValue { get; set; }

            [FromForm]
            public string PropertyWithNoValue { get; set; }

            public override Task ExecuteAsync() => Task.FromResult(0);
        }

        private class PageModelWithProperty : PageModel
        {
            [FromQuery]
            public int Id { get; set; }

            [FromRoute(Name = "route-value")]
            public string RouteDifferentValue { get; set; }

            [FromForm]
            public string PropertyWithNoValue { get; set; }
        }

        private class PageModelWithModelBinderAttribute
        {
            [ModelBinder(BinderType = typeof(DeclarativeSecurityAction))]
            public Guid PropertyWithBinderType { get; set; }
        }

        private class PageModelWithPropertyFilterAttribute
        {
            [ModelBinder]
            [TestPropertyFilterProvider]
            public object PropertyWithFilter { get; set; }
        }

        private class TestPropertyFilterProvider : Attribute, IPropertyFilterProvider
        {
            public Func<ModelMetadata, bool> PropertyFilter => _ => true;
        }

        private class PageModelWithDefaultValue
        {
            [ModelBinder]
            public string PropertyWithDefaultValue { get; set; } = "Hello world";
        }
    }
}
