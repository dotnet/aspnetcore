// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure
{
    public class PageBinderFactoryTest
    {
        private static readonly MvcOptions _options = new MvcOptions();
        private static readonly IOptions<MvcOptions> _optionsAccessor = Options.Create(_options);

        [Fact]
        public void GetModelBinderFactory_ReturnsNullIfPageHasNoBoundProperties()
        {
            // Arrange
            var actionDescriptor = new CompiledPageActionDescriptor
            {
                PageTypeInfo = typeof(PageWithNoBoundProperties).GetTypeInfo(),
            };
            var modelMetadataProvider = TestModelMetadataProvider.CreateDefaultProvider();
            var modelBinderFactory = TestModelBinderFactory.CreateDefault();

            var binder = new ParameterBinder(
                modelMetadataProvider,
                modelBinderFactory,
                Mock.Of<IObjectModelValidator>(),
                _optionsAccessor,
                NullLoggerFactory.Instance);

            // Act
            var factory = PageBinderFactory.CreatePropertyBinder(binder, modelMetadataProvider, modelBinderFactory, actionDescriptor);

            // Assert
            Assert.Same(PageBinderFactory.NullPropertyBinder, factory);
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
            var modelBinderFactory = TestModelBinderFactory.CreateDefault();

            var binder = new ParameterBinder(
                modelMetadataProvider,
                modelBinderFactory,
                Mock.Of<IObjectModelValidator>(),
                _optionsAccessor,
                NullLoggerFactory.Instance);

            // Act
            var factory = PageBinderFactory.CreatePropertyBinder(binder, modelMetadataProvider, modelBinderFactory, actionDescriptor);

            // Assert
            Assert.Same(PageBinderFactory.NullPropertyBinder, factory);
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
            var modelBinderFactory = TestModelBinderFactory.CreateDefault();

            var binder = new ParameterBinder(
                modelMetadataProvider,
                modelBinderFactory,
                Mock.Of<IObjectModelValidator>(),
                _optionsAccessor,
                NullLoggerFactory.Instance);

            // Act
            var factory = PageBinderFactory.CreatePropertyBinder(binder, modelMetadataProvider, modelBinderFactory, actionDescriptor);

            // Assert
            Assert.Same(PageBinderFactory.NullPropertyBinder, factory);
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
            var modelBinderFactory = TestModelBinderFactory.CreateDefault();

            var binder = new ParameterBinder(
                modelMetadataProvider,
                modelBinderFactory,
                Mock.Of<IObjectModelValidator>(),
                _optionsAccessor,
                NullLoggerFactory.Instance);

            // Act
            var factory = PageBinderFactory.CreatePropertyBinder(binder, modelMetadataProvider, modelBinderFactory, actionDescriptor);

            // Assert
            Assert.Same(PageBinderFactory.NullPropertyBinder, factory);
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
            var modelBinderFactory = TestModelBinderFactory.CreateDefault();

            var binder = new ParameterBinder(
                modelMetadataProvider,
                modelBinderFactory,
                Mock.Of<IObjectModelValidator>(),
                _optionsAccessor,
                NullLoggerFactory.Instance);

            // Act
            var factory = PageBinderFactory.CreatePropertyBinder(binder, modelMetadataProvider, modelBinderFactory, actionDescriptor);

            // Assert
            Assert.Same(PageBinderFactory.NullPropertyBinder, factory);
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
            var modelBinderFactory = TestModelBinderFactory.CreateDefault();

            var binder = new ParameterBinder(
                modelMetadataProvider,
                modelBinderFactory,
                Mock.Of<IObjectModelValidator>(),
                _optionsAccessor,
                NullLoggerFactory.Instance);

            // Act
            var factory = PageBinderFactory.CreatePropertyBinder(binder, modelMetadataProvider, modelBinderFactory, actionDescriptor);

            // Assert
            Assert.Same(PageBinderFactory.NullPropertyBinder, factory);
        }

        [Fact]
        public async Task ModelBinderFactory_BindsPropertiesOnPage()
        {
            // Arrange
            var type = typeof(PageWithProperty).GetTypeInfo();

            var actionDescriptor = new CompiledPageActionDescriptor
            {
                BoundProperties = new[]
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
            var modelBinderFactory = TestModelBinderFactory.CreateDefault();

            var binder = new TestParameterBinder(new Dictionary<string, object>
            {
                { nameof(PageWithProperty.Id), 10 },
                { nameof(PageWithProperty.RouteDifferentValue), "route-value" }
            });

            var factory = PageBinderFactory.CreatePropertyBinder(binder, modelMetadataProvider, modelBinderFactory, actionDescriptor);

            var page = new PageWithProperty
            {
                PageContext = GetPageContext(),
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
            var modelBinderFactory = TestModelBinderFactory.CreateDefault();

            var factory = PageBinderFactory.CreatePropertyBinder(binder, modelMetadataProvider, modelBinderFactory, actionDescriptor);

            var page = new PageWithProperty
            {
                PageContext = GetPageContext()
            };

            var model = new PageModelWithProperty();

            // Act
            await factory(page.PageContext, model);

            // Assert
            // Verify that the page properties were not bound.
            Assert.Equal(default, page.Id);
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
            var modelBinderFactory = TestModelBinderFactory.CreateDefault();

            var factory = PageBinderFactory.CreatePropertyBinder(binder, modelMetadataProvider, modelBinderFactory, actionDescriptor);

            var page = new PageWithProperty
            {
                PageContext = GetPageContext()
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
            var modelBinderFactory = TestModelBinderFactory.CreateDefault();

            var factory = PageBinderFactory.CreatePropertyBinder(binder, modelMetadataProvider, modelBinderFactory, actionDescriptor);

            var page = new PageWithProperty
            {
                PageContext = GetPageContext(method)
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
            var modelBinderFactory = TestModelBinderFactory.CreateDefault();

            var factory = PageBinderFactory.CreatePropertyBinder(binder, modelMetadataProvider, modelBinderFactory, actionDescriptor);

            var page = new PageWithProperty
            {
                PageContext = GetPageContext()
            };

            page.HttpContext.Request.Method = "Post";

            var model = new PageModelWithSupportsGetProperty();

            // Act
            await factory(page.PageContext, model);

            // Assert
            Assert.Equal("value", model.SupportsGet);
            Assert.Equal("value", model.Default);
        }

        [Fact]
        public async Task CreatePropertyBinder_SkipsBindingPropertiesWithBindNever()
        {
            // Arrange
            var type = typeof(PageModelWithBindNeverProperty).GetTypeInfo();

            var actionDescriptor = new CompiledPageActionDescriptor
            {
                BoundProperties = new[]
                {
                    new PageBoundPropertyDescriptor()
                    {
                        Name = nameof(PageModelWithBindNeverProperty.BindNeverProperty),
                        ParameterType = typeof(string),
                        Property = type.GetProperty(nameof(PageModelWithBindNeverProperty.BindNeverProperty)),
                    },
                },

                HandlerTypeInfo = type,
                PageTypeInfo = typeof(PageWithProperty).GetTypeInfo(),
                ModelTypeInfo = type,
            };

            var binder = new TestParameterBinder(new Dictionary<string, object>
            {
                { nameof(PageModelWithBindNeverProperty.BindNeverProperty), "value" },
            });

            var modelMetadataProvider = TestModelMetadataProvider.CreateDefaultProvider();
            var modelBinderFactory = TestModelBinderFactory.CreateDefault();

            var factory = PageBinderFactory.CreatePropertyBinder(binder, modelMetadataProvider, modelBinderFactory, actionDescriptor);

            var page = new PageWithProperty
            {
                PageContext = GetPageContext()
            };

            var model = new PageModelWithBindNeverProperty();

            // Act
            await factory(page.PageContext, model);

            // Assert
            Assert.Null(model.BindNeverProperty);
        }

        [Fact]
        public async Task CreatePropertyBinder_ValidatesTopLevelProperties()
        {
            // Arrange
            var type = typeof(PageModelWithValidation).GetTypeInfo();

            var actionDescriptor = new CompiledPageActionDescriptor
            {
                BoundProperties = new[]
                {
                    new PageBoundPropertyDescriptor()
                    {
                        Name = nameof(PageModelWithValidation.Validated),
                        ParameterType = typeof(string),
                        Property = type.GetProperty(nameof(PageModelWithValidation.Validated)),
                    },
                },

                HandlerTypeInfo = type,
                PageTypeInfo = typeof(PageWithProperty).GetTypeInfo(),
                ModelTypeInfo = type,
            };

            var modelMetadataProvider = TestModelMetadataProvider.CreateDefaultProvider();
            var modelBinderFactory = TestModelBinderFactory.CreateDefault();

            var binder = new ParameterBinder(
                modelMetadataProvider,
                modelBinderFactory,
                new DefaultObjectValidator(
                    modelMetadataProvider,
                    new[] { TestModelValidatorProvider.CreateDefaultProvider() },
                    new MvcOptions()),
                _optionsAccessor,
                NullLoggerFactory.Instance);

            var factory = PageBinderFactory.CreatePropertyBinder(binder, modelMetadataProvider, modelBinderFactory, actionDescriptor);

            var page = new PageWithProperty
            {
                PageContext = GetPageContext()
            };

            var model = new PageModelWithValidation();

            // Act
            await factory(page.PageContext, model);

            // Assert
            var modelState = page.PageContext.ModelState;
            Assert.False(modelState.IsValid);
            Assert.Collection(
                modelState,
                kvp =>
                {
                    Assert.Equal(nameof(PageModelWithValidation.Validated), kvp.Key);
                });
        }

        [Fact]
        public async Task CreateHandlerBinder_BindsHandlerParameters()
        {
            // Arrange
            var type = typeof(PageModelWithExecutors);
            var actionDescriptor = GetActionDescriptorWithHandlerMethod(type, nameof(PageModelWithExecutors.OnGet));

            // Act
            var parameterBinder = new TestParameterBinder(new Dictionary<string, object>()
            {
                { "id", "value" },
            });

            var modelMetadataProvider = TestModelMetadataProvider.CreateDefaultProvider();
            var modelBinderFactory = TestModelBinderFactory.CreateDefault();
            var factory = PageBinderFactory.CreateHandlerBinder(
                parameterBinder,
                modelMetadataProvider,
                modelBinderFactory,
                actionDescriptor,
                actionDescriptor.HandlerMethods[0],
                _options);

            var page = new PageWithProperty
            {
                PageContext = GetPageContext()
            };

            var model = new PageModelWithExecutors();
            var arguments = new Dictionary<string, object>();

            // Act
            await factory(page.PageContext, arguments);

            // Assert
            Assert.Collection(
                arguments,
                kvp =>
                {
                    Assert.Equal("id", kvp.Key);
                    Assert.Equal("value", kvp.Value);
                });
        }

        [Fact]
        public async Task CreateHandlerBinder_SkipBindingParametersThatDisallowBinding()
        {
            // Arrange
            var type = typeof(PageModelWithExecutors);
            var actionDescriptor = GetActionDescriptorWithHandlerMethod(type, nameof(PageModelWithExecutors.OnGetWithBindNever));

            // Act
            var parameterBinder = new TestParameterBinder(new Dictionary<string, object>()
            {
                { "id", "value" },
            });

            var modelMetadataProvider = TestModelMetadataProvider.CreateDefaultProvider();
            var modelBinderFactory = TestModelBinderFactory.CreateDefault();
            var factory = PageBinderFactory.CreateHandlerBinder(
                parameterBinder,
                modelMetadataProvider,
                modelBinderFactory,
                actionDescriptor,
                actionDescriptor.HandlerMethods[0],
                _options);

            var page = new PageWithProperty
            {
                PageContext = GetPageContext()
            };

            var model = new PageModelWithExecutors();
            var arguments = new Dictionary<string, object>();

            // Act
            await factory(page.PageContext, arguments);

            // Assert
            Assert.Empty(arguments);
        }

        [Fact]
        public async Task CreateHandlerBinder_ValidatesTopLevelParameters()
        {
            // Arrange
            var type = typeof(PageModelWithExecutors);
            var actionDescriptor = GetActionDescriptorWithHandlerMethod(type, nameof(PageModelWithExecutors.OnPostWithValidation));

            // Act

            var modelMetadataProvider = TestModelMetadataProvider.CreateDefaultProvider();
            var modelBinderFactory = TestModelBinderFactory.CreateDefault();
            var parameterBinder = new ParameterBinder(
                modelMetadataProvider,
                modelBinderFactory,
                new DefaultObjectValidator(
                    modelMetadataProvider,
                    new[] { TestModelValidatorProvider.CreateDefaultProvider() },
                    new MvcOptions()),
                _optionsAccessor,
                NullLoggerFactory.Instance);

            var factory = PageBinderFactory.CreateHandlerBinder(
                parameterBinder,
                modelMetadataProvider,
                modelBinderFactory,
                actionDescriptor,
                actionDescriptor.HandlerMethods[0],
                _options);

            var page = new PageWithProperty
            {
                PageContext = GetPageContext()
            };

            var model = new PageModelWithExecutors();
            var arguments = new Dictionary<string, object>();

            // Act
            await factory(page.PageContext, arguments);

            // Assert
            var modelState = page.PageContext.ModelState;
            Assert.False(modelState.IsValid);
            Assert.Collection(
                modelState,
                kvp =>
                {
                    Assert.Equal("name", kvp.Key);
                });
        }

        [Fact]
        public async Task FactoryRecordsErrorWhenValueProviderThrowsValueProviderException()
        {
            // Arrange
            var type = typeof(PageModelWithExecutors);
            var actionDescriptor = GetActionDescriptorWithHandlerMethod(type, nameof(PageModelWithExecutors.OnGet));

            // Act
            var parameterBinder = new TestParameterBinder(new Dictionary<string, object>()
            {
                { "id", "value" },
            });

            var modelMetadataProvider = TestModelMetadataProvider.CreateDefaultProvider();
            var modelBinderFactory = TestModelBinderFactory.CreateDefault();
            var factory = PageBinderFactory.CreateHandlerBinder(
                parameterBinder,
                modelMetadataProvider,
                modelBinderFactory,
                actionDescriptor,
                actionDescriptor.HandlerMethods[0],
                _options);

            var pageContext = GetPageContext();
            var page = new PageWithProperty
            {
                PageContext = pageContext,
            };

            var valueProviderFactory = new Mock<IValueProviderFactory>();
            valueProviderFactory.Setup(f => f.CreateValueProviderAsync(It.IsAny<ValueProviderFactoryContext>()))
                .Throws(new ValueProviderException("Some error"));

            pageContext.ValueProviderFactories.Add(valueProviderFactory.Object);

            var model = new PageModelWithExecutors();
            var arguments = new Dictionary<string, object>();

            // Act
            await factory(page.PageContext, arguments);

            // Assert
            var modelState = pageContext.ModelState;
            var entry = Assert.Single(modelState);
            Assert.Empty(entry.Key);
            var error = Assert.Single(entry.Value.Errors);

            Assert.Equal("Some error", error.ErrorMessage);
        }


        private static CompiledPageActionDescriptor GetActionDescriptorWithHandlerMethod(Type type, string method)
        {
            var handlerMethodInfo = type.GetMethod(method);
            var parameterInfo = handlerMethodInfo.GetParameters()[0];

            var actionDescriptor = new CompiledPageActionDescriptor
            {
                HandlerTypeInfo = type.GetTypeInfo(),
                HandlerMethods = new[]
                {
                    new HandlerMethodDescriptor
                    {
                        HttpMethod = "Post",
                        MethodInfo = handlerMethodInfo,
                        Parameters = new[]
                        {
                            new HandlerParameterDescriptor
                            {
                                ParameterInfo = parameterInfo,
                                ParameterType = parameterInfo.ParameterType,
                                Name = parameterInfo.Name
                            },
                        },
                    },
                },
            };
            return actionDescriptor;
        }

        private PageContext GetPageContext(string httpMethod = null)
        {
            var services = new ServiceCollection();
            services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);

            var httpContext = new DefaultHttpContext()
            {
                RequestServices = services.BuildServiceProvider()
            };

            if (httpMethod != null)
            {
                httpContext.Request.Method = httpMethod;
            }

            return new PageContext()
            {
                HttpContext = httpContext
            };
        }

        private class TestParameterBinder : ParameterBinder
        {
            private readonly IDictionary<string, object> _args;

            public TestParameterBinder(IDictionary<string, object> args)
                : base(
                    TestModelMetadataProvider.CreateDefaultProvider(),
                    TestModelBinderFactory.CreateDefault(),
                    Mock.Of<IObjectModelValidator>(),
                    _optionsAccessor,
                    NullLoggerFactory.Instance)
            {
                _args = args;
            }

            public IList<ParameterDescriptor> Descriptors { get; } = new List<ParameterDescriptor>();

            public override Task<ModelBindingResult> BindModelAsync(
                ActionContext actionContext,
                IModelBinder modelBinder,
                IValueProvider valueProvider,
                ParameterDescriptor parameter,
                ModelMetadata metadata,
                object value)
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

        private class PageModelWithBindNeverProperty
        {
            [BindNever]
            public string BindNeverProperty { get; set; }
        }

        private class PageModelWithValidation
        {
            [Required]
            public string Validated { get; set; }
        }

        private class PageModelWithExecutors
        {
            public void OnGetWithBindNever([BindNever] string id)
            {
            }

            public void OnGet(string id)
            {
            }

            public void OnPostWithValidation([Required] string name)
            {
            }
        }
    }
}
