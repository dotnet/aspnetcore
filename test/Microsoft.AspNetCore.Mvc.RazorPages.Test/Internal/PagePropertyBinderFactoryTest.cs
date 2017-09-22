// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure;
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
                Mock.Of<IModelValidatorProvider>());

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
                Mock.Of<IModelValidatorProvider>());

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
                Mock.Of<IModelValidatorProvider>());

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
                Mock.Of<IModelValidatorProvider>());

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
                Mock.Of<IModelValidatorProvider>());

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
                Mock.Of<IModelValidatorProvider>());

            // Act
            var factory = PagePropertyBinderFactory.CreateBinder(binder, modelMetadataProvider, actionDescriptor);

            // Assert
            Assert.Null(factory);
        }

        [Fact]
        public async Task ModelBinderFactory_BindsPropertiesOnPage()
        {
            // Arrange
            var type = typeof(PageWithProperty).GetTypeInfo();

            var actionDescriptor = new CompiledPageActionDescriptor
            {
                BoundProperties = new []
                {
                    new PageBoundPropertyDescriptor()
                    {
                        Name = nameof(PageWithProperty.Id),
                        ParameterType = typeof(int),
                        Property = type.GetProperty(nameof(PageWithProperty.Id)),
                    },
                    new PageBoundPropertyDescriptor()
                    {
                        Name = nameof(PageWithProperty.RouteDifferentValue),
                        ParameterType = typeof(string),
                        Property = type.GetProperty(nameof(PageWithProperty.RouteDifferentValue)),
                    },
                    new PageBoundPropertyDescriptor()
                    {
                        Name = nameof(PageWithProperty.PropertyWithNoValue),
                        ParameterType = typeof(string),
                        Property = type.GetProperty(nameof(PageWithProperty.PropertyWithNoValue)),
                    }
                },
                HandlerTypeInfo = type,
                PageTypeInfo = type,
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
                PageContext = new PageContext()
                {
                    HttpContext = new DefaultHttpContext(),
                },
            };

            // Act
            await factory(page.PageContext, page);

            // Assert
            Assert.Equal(10, page.Id);
            Assert.Equal("route-value", page.RouteDifferentValue);
            Assert.Null(page.PropertyWithNoValue);
        }

        [Fact]
        public async Task ModelBinderFactory_BindsPropertiesOnPageModel()
        {
            // Arrange
            var type = typeof(PageModelWithProperty).GetTypeInfo();

            var actionDescriptor = new CompiledPageActionDescriptor
            {
                BoundProperties = new[]
                {
                    new PageBoundPropertyDescriptor()
                    {
                        Name = nameof(PageModelWithProperty.Id),
                        ParameterType = typeof(int),
                        Property = type.GetProperty(nameof(PageModelWithProperty.Id)),
                    },
                    new PageBoundPropertyDescriptor()
                    {
                        Name = nameof(PageModelWithProperty.RouteDifferentValue),
                        ParameterType = typeof(string),
                        Property = type.GetProperty(nameof(PageModelWithProperty.RouteDifferentValue)),
                    },
                    new PageBoundPropertyDescriptor()
                    {
                        Name = nameof(PageModelWithProperty.PropertyWithNoValue),
                        ParameterType = typeof(string),
                        Property = type.GetProperty(nameof(PageModelWithProperty.PropertyWithNoValue)),
                    }
                },

                HandlerTypeInfo = typeof(PageModelWithProperty).GetTypeInfo(),
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
                PageContext = new PageContext()
                {
                    HttpContext = new DefaultHttpContext(),
                }
            };

            var model = new PageModelWithProperty();

            // Act
            await factory(page.PageContext, model);

            // Assert
            // Verify that the page properties were not bound.
            Assert.Equal(default(int), page.Id);
            Assert.Null(page.RouteDifferentValue);

            Assert.Equal(10, model.Id);
            Assert.Equal("route-value", model.RouteDifferentValue);
            Assert.Null(model.PropertyWithNoValue);
        }

        [Fact]
        public async Task ModelBinderFactory_PreservesExistingValueIfModelBindingFailed()
        {
            // Arrange
            var type = typeof(PageModelWithDefaultValue).GetTypeInfo();

            var actionDescriptor = new CompiledPageActionDescriptor
            {
                BoundProperties = new[]
                {
                    new PageBoundPropertyDescriptor()
                    {
                        Name = nameof(PageModelWithDefaultValue.PropertyWithDefaultValue),
                        ParameterType = typeof(string),
                        Property = type.GetProperty(nameof(PageModelWithDefaultValue.PropertyWithDefaultValue)),
                    },
                },

                HandlerTypeInfo = type,
                PageTypeInfo = typeof(PageWithProperty).GetTypeInfo(),
                ModelTypeInfo = type,
            };

            var binder = new TestParameterBinder(new Dictionary<string, object>());

            var modelMetadataProvider = TestModelMetadataProvider.CreateDefaultProvider();
            var factory = PagePropertyBinderFactory.CreateBinder(binder, modelMetadataProvider, actionDescriptor);

            var page = new PageWithProperty
            {
                PageContext = new PageContext()
                {
                    HttpContext = new DefaultHttpContext(),
                }
            };

            var model = new PageModelWithDefaultValue();
            var defaultValue = model.PropertyWithDefaultValue;

            // Act
            await factory(page.PageContext, model);

            // Assert
            Assert.Equal(defaultValue, model.PropertyWithDefaultValue);
        }

        [Theory]
        [InlineData("Get")]
        [InlineData("GET")]
        [InlineData("gET")]
        public async Task ModelBinderFactory_BindsPropertyWithoutSupportsGet_WhenRequestIsGet(string method)
        {
            // Arrange
            var type = typeof(PageModelWithSupportsGetProperty).GetTypeInfo();

            var actionDescriptor = new CompiledPageActionDescriptor
            {
                BoundProperties = new[]
                {
                    new PageBoundPropertyDescriptor()
                    {
                        Name = nameof(PageModelWithSupportsGetProperty.SupportsGet),
                        ParameterType = typeof(string),
                        Property = type.GetProperty(nameof(PageModelWithSupportsGetProperty.SupportsGet)),
                        BindingInfo = new BindingInfo()
                        {
                            // Simulates placing a [BindProperty] on the property
                            RequestPredicate = ((IRequestPredicateProvider)new BindPropertyAttribute() { SupportsGet = true }).RequestPredicate,
                        }
                    },
                    new PageBoundPropertyDescriptor()
                    {
                        Name = nameof(PageModelWithSupportsGetProperty.Default),
                        ParameterType = typeof(string),
                        Property = type.GetProperty(nameof(PageModelWithSupportsGetProperty.Default)),
                    },
                },

                HandlerTypeInfo = type,
                PageTypeInfo = typeof(PageWithProperty).GetTypeInfo(),
                ModelTypeInfo = type,
            };

            var binder = new TestParameterBinder(new Dictionary<string, object>()
            {
                { "SupportsGet", "value" },
                { "Default", "set" },
            });

            var modelMetadataProvider = TestModelMetadataProvider.CreateDefaultProvider();
            var factory = PagePropertyBinderFactory.CreateBinder(binder, modelMetadataProvider, actionDescriptor);

            var page = new PageWithProperty
            {
                PageContext = new PageContext()
                {
                    HttpContext = new DefaultHttpContext()
                    {
                        Request=
                        {
                            Method = method,
                        }
                    }
                }
            };

            var model = new PageModelWithSupportsGetProperty();

            // Act
            await factory(page.PageContext, model);

            // Assert
            Assert.Equal("value", model.SupportsGet);
            Assert.Equal("set", model.Default);
        }

        [Fact]
        public async Task ModelBinderFactory_BindsPropertyWithoutSupportsGet_WhenRequestIsNotGet()
        {
            // Arrange
            var type = typeof(PageModelWithSupportsGetProperty).GetTypeInfo();

            var actionDescriptor = new CompiledPageActionDescriptor
            {
                BoundProperties = new[]
                {
                    new PageBoundPropertyDescriptor()
                    {
                        Name = nameof(PageModelWithSupportsGetProperty.SupportsGet),
                        ParameterType = typeof(string),
                        Property = type.GetProperty(nameof(PageModelWithSupportsGetProperty.SupportsGet)),
                        BindingInfo = new BindingInfo()
                        {
                            RequestPredicate = ((IRequestPredicateProvider)new BindPropertyAttribute() { SupportsGet = true }).RequestPredicate,
                        }
                    },
                    new PageBoundPropertyDescriptor()
                    {
                        Name = nameof(PageModelWithSupportsGetProperty.Default),
                        ParameterType = typeof(string),
                        Property = type.GetProperty(nameof(PageModelWithSupportsGetProperty.Default)),
                    },
                },

                HandlerTypeInfo = type,
                PageTypeInfo = typeof(PageWithProperty).GetTypeInfo(),
                ModelTypeInfo = type,
            };

            var binder = new TestParameterBinder(new Dictionary<string, object>()
            {
                { "SupportsGet", "value" },
                { "Default", "value" },
            });

            var modelMetadataProvider = TestModelMetadataProvider.CreateDefaultProvider();
            var factory = PagePropertyBinderFactory.CreateBinder(binder, modelMetadataProvider, actionDescriptor);

            var page = new PageWithProperty
            {
                PageContext = new PageContext()
                {
                    HttpContext = new DefaultHttpContext(),
                }
            };

            page.HttpContext.Request.Method = "Post";

            var model = new PageModelWithSupportsGetProperty();

            // Act
            await factory(page.PageContext, model);

            // Assert
            Assert.Equal("value", model.SupportsGet);
            Assert.Equal("value", model.Default);
        }

        private class TestParameterBinder : ParameterBinder
        {
            private readonly IDictionary<string, object> _args;

            public TestParameterBinder(IDictionary<string, object> args)
                : base(
                    TestModelMetadataProvider.CreateDefaultProvider(),
                    TestModelBinderFactory.CreateDefault(),
                    Mock.Of<IModelValidatorProvider>())
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

        private class PageModelWithSupportsGetProperty
        {
            [BindProperty(SupportsGet = true)]
            public string SupportsGet { get; set; }

            public string Default { get; set; }
        }
    }
}
