// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace Microsoft.AspNetCore.Mvc.Filters;

public class MiddlewareFilterTest
{
    private readonly TestController _controller = new TestController();

    [Fact]
    public async Task MiddlewareFilter_SetsMiddlewareFilterFeature_OnExecution()
    {
        // Arrange
        Task requestDelegate(HttpContext context) => Task.FromResult(true);
        var middlewareFilter = new MiddlewareFilter(requestDelegate);
        var httpContext = new DefaultHttpContext();
        var resourceExecutingContext = GetResourceExecutingContext(httpContext);
        var resourceExecutionDelegate = GetResourceExecutionDelegate(httpContext);

        // Act
        await middlewareFilter.OnResourceExecutionAsync(resourceExecutingContext, resourceExecutionDelegate);

        // Assert
        var feature = resourceExecutingContext.HttpContext.Features.Get<IMiddlewareFilterFeature>();
        Assert.NotNull(feature);
        Assert.Same(resourceExecutingContext, feature.ResourceExecutingContext);
        Assert.Same(resourceExecutionDelegate, feature.ResourceExecutionDelegate);
    }

    [Fact]
    public async Task OnMiddlewareShortCircuit_DoesNotExecute_RestOfFilterPipeline()
    {
        // Arrange
        var expectedHeader = "h1";
        Pipeline1.ConfigurePipeline = (appBuilder) =>
        {
            appBuilder.Run((httpContext) =>
            {
                httpContext.Response.Headers.Add(expectedHeader, "");
                return Task.FromResult(true); // short circuit the request
            });
        };
        var resourceFilter1 = new TestResourceFilter(TestResourceFilterAction.Passthrough);
        var middlewareResourceFilter = new MiddlewareFilter(GetMiddlewarePipeline(typeof(Pipeline1)));
        var exceptionThrowingResourceFilter = new TestResourceFilter(TestResourceFilterAction.ThrowException);

        var invoker = CreateInvoker(
            new IFilterMetadata[]
            {
                    resourceFilter1,
                    middlewareResourceFilter,
                    exceptionThrowingResourceFilter,
            },
            actionThrows: true); // The action won't run

        // Act
        await invoker.InvokeAsync();

        // Assert
        var resourceExecutedContext = resourceFilter1.ResourceExecutedContext;
        Assert.True(resourceExecutedContext.HttpContext.Response.Headers.ContainsKey(expectedHeader));
        Assert.True(resourceExecutedContext.Canceled);
        Assert.False(invoker.ControllerFactory.CreateCalled);
    }

    // Example: Middleware filters are applied at Global, Controller & Action level
    [Fact]
    public async Task Multiple_MiddlewareFilters_ConcatsTheMiddlewarePipelines()
    {
        // Arrange
        var expectedHeader = "h1";
        var expectedHeaderValue = "pipeline1-pipeline2";
        Pipeline1.ConfigurePipeline = (appBuilder) =>
        {
            appBuilder.Use((httpContext, next) =>
            {
                httpContext.Response.Headers["h1"] = "pipeline1";
                return next(httpContext);
            });
        };
        Pipeline2.ConfigurePipeline = (appBuilder) =>
        {
            appBuilder.Run((httpContext) =>
            {
                httpContext.Response.Headers["h1"] = httpContext.Response.Headers["h1"] + "-pipeline2";
                return Task.FromResult(true); // short circuits the request
            });
        };
        var resourceFilter1 = new TestResourceFilter(TestResourceFilterAction.Passthrough);
        var middlewareResourceFilter1 = new MiddlewareFilter(GetMiddlewarePipeline(typeof(Pipeline1)));
        var middlewareResourceFilter2 = new MiddlewareFilter(GetMiddlewarePipeline(typeof(Pipeline2)));
        var exceptionThrowingResourceFilter = new TestResourceFilter(TestResourceFilterAction.ThrowException);

        var invoker = CreateInvoker(
            new IFilterMetadata[]
            {
                    resourceFilter1,                    // This filter will pass through
                    middlewareResourceFilter1,          // This filter will pass through
                    middlewareResourceFilter2,          // This filter will short circuit
                    exceptionThrowingResourceFilter,    // This shouldn't run
            },
            actionThrows: true); // The action won't run

        // Act
        await invoker.InvokeAsync();

        // Assert
        var resourceExecutedContext = resourceFilter1.ResourceExecutedContext;
        var response = resourceExecutedContext.HttpContext.Response;
        Assert.True(response.Headers.ContainsKey(expectedHeader));
        Assert.Equal(expectedHeaderValue, response.Headers[expectedHeader]);
        Assert.True(resourceExecutedContext.Canceled);
        Assert.False(invoker.ControllerFactory.CreateCalled);
    }

    [Fact]
    public async Task UnhandledException_InMiddleware_PropagatesBackToInvoker()
    {
        // Arrange
        var expectedMessage = "Error!!!";
        Pipeline1.ConfigurePipeline = (appBuilder) =>
        {
            appBuilder.Run((httpContext) =>
            {
                throw new InvalidOperationException(expectedMessage);
            });
        };
        var resourceFilter1 = new TestResourceFilter(TestResourceFilterAction.Passthrough);
        var middlewareResourceFilter = new MiddlewareFilter(GetMiddlewarePipeline(typeof(Pipeline1)));
        var exceptionThrowingResourceFilter = new TestResourceFilter(TestResourceFilterAction.ThrowException);

        var invoker = CreateInvoker(
            new IFilterMetadata[]
            {
                    resourceFilter1,
                    middlewareResourceFilter,
                    exceptionThrowingResourceFilter, // This shouldn't run
            },
            actionThrows: true); // The action won't run

        // Act
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () => await invoker.InvokeAsync());

        // Assert
        Assert.Equal(expectedMessage, exception.Message);
    }

    [Fact]
    public async Task ExceptionThrownInMiddleware_CanBeHandled_ByEarlierMiddleware()
    {
        // Arrange
        var expectedMessage = "Error!!!";
        Pipeline1.ConfigurePipeline = (appBuilder) =>
        {
            appBuilder.Use(async (httpContext, next) =>
            {
                try
                {
                    await next(httpContext);
                }
                catch
                {
                    httpContext.Response.StatusCode = 500;
                    httpContext.Response.Headers.Add("Error", "Error!!!!");
                }
            });
        };
        Pipeline2.ConfigurePipeline = (appBuilder) =>
        {
            appBuilder.Run((httpContext) =>
            {
                throw new InvalidOperationException(expectedMessage);
            });
        };
        var resourceFilter1 = new TestResourceFilter(TestResourceFilterAction.Passthrough);
        var middlewareResourceFilter1 = new MiddlewareFilter(GetMiddlewarePipeline(typeof(Pipeline1)));
        var middlewareResourceFilter2 = new MiddlewareFilter(GetMiddlewarePipeline(typeof(Pipeline2)));
        var exceptionThrowingResourceFilter = new TestResourceFilter(TestResourceFilterAction.ThrowException);

        var invoker = CreateInvoker(
            new IFilterMetadata[]
            {
                    resourceFilter1,
                    middlewareResourceFilter1,
                    middlewareResourceFilter2,
                    exceptionThrowingResourceFilter, // This shouldn't run
            },
            actionThrows: true); // The action won't run

        // Act
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () => await invoker.InvokeAsync());

        // Assert
        var resourceExecutedContext = resourceFilter1.ResourceExecutedContext;
        var response = resourceExecutedContext.HttpContext.Response;
        Assert.Equal(500, response.StatusCode);
        Assert.True(response.Headers.ContainsKey("Error"));
        Assert.False(invoker.ControllerFactory.CreateCalled);
    }

    private ResourceExecutingContext GetResourceExecutingContext(HttpContext httpContext)
    {
        return new ResourceExecutingContext(
            new ActionContext(httpContext, new RouteData(), new ActionDescriptor(), new ModelStateDictionary()),
            new List<IFilterMetadata>(),
            new List<IValueProviderFactory>());
    }

    private ResourceExecutionDelegate GetResourceExecutionDelegate(HttpContext httpContext)
    {
        return new ResourceExecutionDelegate(
            () => Task.FromResult(new ResourceExecutedContext(new ActionContext(), new List<IFilterMetadata>())));
    }

    private TestControllerActionInvoker CreateInvoker(
        IFilterMetadata[] filters,
        bool actionThrows = false)
    {
        var actionDescriptor = new ControllerActionDescriptor()
        {
            FilterDescriptors = new List<FilterDescriptor>(),
            Parameters = new List<ParameterDescriptor>(),
        };

        if (actionThrows)
        {
            actionDescriptor.MethodInfo = typeof(ControllerActionInvokerTest.TestController).GetMethod(
                nameof(ControllerActionInvokerTest.TestController.ThrowingActionMethod));
        }
        else
        {
            actionDescriptor.MethodInfo = typeof(ControllerActionInvokerTest.TestController).GetMethod(
                nameof(ControllerActionInvokerTest.TestController.ActionMethod));
        }
        actionDescriptor.ControllerTypeInfo = typeof(ControllerActionInvokerTest.TestController).GetTypeInfo();

        return CreateInvoker(filters, actionDescriptor, _controller);
    }

    private TestControllerActionInvoker CreateInvoker(
        IFilterMetadata[] filters,
        ControllerActionDescriptor actionDescriptor,
        object controller)
    {
        var httpContext = GetHttpContext();
        httpContext.Response.Body = new MemoryStream();

        var options = new MvcOptions();
        var optionsAccessor = new Mock<IOptions<MvcOptions>>();
        optionsAccessor
            .SetupGet(o => o.Value)
            .Returns(options);

        var actionContext = new ActionContext(httpContext, new RouteData(), actionDescriptor);

        var diagnosticListener = new DiagnosticListener("Microsoft.AspNetCore");
        diagnosticListener.SubscribeWithAdapter(new TestDiagnosticListener());

        var invoker = new TestControllerActionInvoker(
            filters,
            new MockControllerFactory(controller ?? this),
            new NullLoggerFactory().CreateLogger<ControllerActionInvoker>(),
            diagnosticListener,
            new ActionResultTypeMapper(),
            actionContext,
            new List<IValueProviderFactory>(),
            maxAllowedErrorsInModelState: 200);
        return invoker;
    }

    private class Pipeline1
    {
        public static Action<IApplicationBuilder> ConfigurePipeline { get; set; }

        public void Configure(IApplicationBuilder appBuilder)
        {
            ConfigurePipeline(appBuilder);
        }
    }

    private class Pipeline2
    {
        public static Action<IApplicationBuilder> ConfigurePipeline { get; set; }

        public void Configure(IApplicationBuilder appBuilder)
        {
            ConfigurePipeline(appBuilder);
        }
    }

    private static HttpContext GetHttpContext()
    {
        var services = CreateServices();

        var httpContext = new DefaultHttpContext
        {
            RequestServices = services.BuildServiceProvider()
        };

        return httpContext;
    }

    private RequestDelegate GetMiddlewarePipeline(Type middlewarePipelineProviderType)
    {
        var applicationServices = new ServiceCollection();
        var applicationBuilder = new ApplicationBuilder(applicationServices.BuildServiceProvider());
        var middlewareFilterBuilderService = new MiddlewareFilterBuilder(
            new MiddlewareFilterConfigurationProvider())
        {
            ApplicationBuilder = applicationBuilder
        };

        return middlewareFilterBuilderService.GetPipeline(middlewarePipelineProviderType);
    }

    private static IServiceCollection CreateServices()
    {
        var services = new ServiceCollection();

        services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
        return services;
    }

    private class MockControllerFactory
    {
        private readonly object _controller;

        public MockControllerFactory(object controller)
        {
            _controller = controller;
        }

        public bool CreateCalled { get; private set; }

        public bool ReleaseCalled { get; private set; }

        public ControllerContext ControllerContext { get; private set; }

        public object CreateController(ControllerContext context)
        {
            ControllerContext = context;
            CreateCalled = true;
            return _controller;
        }

        public void ReleaseController(ControllerContext context, object controller)
        {
            Assert.NotNull(controller);
            Assert.Same(_controller, controller);
            ReleaseCalled = true;
        }

        public ValueTask ReleaseControllerAsync(ControllerContext context, object controller)
        {
            Assert.NotNull(controller);
            Assert.Same(_controller, controller);
            ReleaseCalled = true;

            return default;
        }

        public void Verify()
        {
            if (CreateCalled && !ReleaseCalled)
            {
                Assert.False(true, "ReleaseController should have been called.");
            }
        }
    }

    private class TestControllerActionInvoker : ControllerActionInvoker
    {
        public TestControllerActionInvoker(
            IFilterMetadata[] filters,
            MockControllerFactory controllerFactory,
            ILogger logger,
            DiagnosticListener diagnosticListener,
            IActionResultTypeMapper mapper,
            ActionContext actionContext,
            IReadOnlyList<IValueProviderFactory> valueProviderFactories,
            int maxAllowedErrorsInModelState)
            : base(
                  logger,
                  diagnosticListener,
                  ActionContextAccessor.Null,
                  mapper,
                  CreateControllerContext(actionContext, valueProviderFactories, maxAllowedErrorsInModelState),
                  CreateCacheEntry((ControllerActionDescriptor)actionContext.ActionDescriptor, controllerFactory),
                  filters)
        {
            ControllerFactory = controllerFactory;
        }

        public MockControllerFactory ControllerFactory { get; }

        public override async Task InvokeAsync()
        {
            await base.InvokeAsync();

            // Make sure that the controller was disposed in every test that creates ones.
            ControllerFactory.Verify();
        }

        private static ObjectMethodExecutor CreateExecutor(ControllerActionDescriptor actionDescriptor)
        {
            return ObjectMethodExecutor.Create(actionDescriptor.MethodInfo, actionDescriptor.ControllerTypeInfo);
        }

        private static ControllerContext CreateControllerContext(
            ActionContext actionContext,
            IReadOnlyList<IValueProviderFactory> valueProviderFactories,
            int maxAllowedErrorsInModelState)
        {
            var controllerContext = new ControllerContext(actionContext)
            {
                ValueProviderFactories = valueProviderFactories.ToList()
            };
            controllerContext.ModelState.MaxAllowedErrors = maxAllowedErrorsInModelState;

            return controllerContext;
        }

        private static ControllerActionInvokerCacheEntry CreateCacheEntry(
            ControllerActionDescriptor actionDescriptor,
            MockControllerFactory controllerFactory)
        {
            var objectMethodExecutor = CreateExecutor(actionDescriptor);
            var actionMethodExecutor = ActionMethodExecutor.GetExecutor(objectMethodExecutor);
            return new ControllerActionInvokerCacheEntry(
                new FilterItem[0],
                controllerFactory.CreateController,
                controllerFactory.ReleaseControllerAsync,
                null,
                objectMethodExecutor,
                actionMethodExecutor,
                actionMethodExecutor);
        }
    }

    private sealed class TestController
    {
    }

    private enum TestResourceFilterAction
    {
        ShortCircuit,
        ThrowException,
        Passthrough
    }

    private class TestResourceFilter : IAsyncResourceFilter
    {
        private readonly TestResourceFilterAction _action;
        public TestResourceFilter(TestResourceFilterAction action)
        {
            _action = action;
        }

        public ResourceExecutedContext ResourceExecutedContext { get; private set; }

        public async Task OnResourceExecutionAsync(ResourceExecutingContext context, ResourceExecutionDelegate next)
        {
            if (_action == TestResourceFilterAction.ThrowException)
            {
                throw new NotImplementedException("This filter should not have been run!");

            }
            else if (_action == TestResourceFilterAction.Passthrough)
            {
                ResourceExecutedContext = await next();
            }
            else
            {
                context.Result = new TestActionResult();
            }
        }
    }

    public class TestActionResult : IActionResult
    {
        public Task ExecuteResultAsync(ActionContext context)
        {
            context.HttpContext.Response.StatusCode = 200;
            return context.HttpContext.Response.WriteAsync("Shortcircuited");
        }
    }
}
