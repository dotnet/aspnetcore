// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Reflection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace Microsoft.AspNetCore.Mvc.Infrastructure;

public class ControllerActionInvokerProviderTest
{
    [Fact]
    public void OnExecuting_ConfiguresModelState_WithMvcOptions()
    {
        // Arrange
        var provider = CreateInvokerProvider(new MvcOptions() { MaxValidationDepth = 1, MaxModelBindingRecursionDepth = 2, MaxModelValidationErrors = 3 });

        var context = new ActionInvokerProviderContext(new ActionContext()
        {
            ActionDescriptor = GetControllerActionDescriptor(),
            HttpContext = new DefaultHttpContext(),
            RouteData = new RouteData(),
        });

        // Act
        provider.OnProvidersExecuting(context);

        // Assert
        var invoker = Assert.IsType<ControllerActionInvoker>(context.Result);
        Assert.Equal(1, invoker.ControllerContext.ModelState.MaxValidationDepth);
        Assert.Equal(2, invoker.ControllerContext.ModelState.MaxStateDepth);
        Assert.Equal(3, invoker.ControllerContext.ModelState.MaxAllowedErrors);

    }

    private static ControllerActionDescriptor GetControllerActionDescriptor()
    {
        var method = typeof(TestActions).GetMethod(nameof(TestActions.GetAction));
        var actionDescriptor = new ControllerActionDescriptor
        {
            MethodInfo = method,
            FilterDescriptors = new List<FilterDescriptor>(),
            ControllerTypeInfo = typeof(TestActions).GetTypeInfo(),
        };

        foreach (var filterAttribute in method.GetCustomAttributes().OfType<IFilterMetadata>())
        {
            actionDescriptor.FilterDescriptors.Add(new FilterDescriptor(filterAttribute, FilterScope.Action));
        }

        return actionDescriptor;
    }

    private static ControllerActionInvokerProvider CreateInvokerProvider(MvcOptions mvcOptions = null)
    {
        var modelMetadataProvider = TestModelMetadataProvider.CreateDefaultProvider();
        var modelBinderFactory = TestModelBinderFactory.CreateDefault();
        mvcOptions ??= new MvcOptions();

        var parameterBinder = new ParameterBinder(
            modelMetadataProvider,
            TestModelBinderFactory.CreateDefault(),
            Mock.Of<IObjectModelValidator>(),
            Options.Create(mvcOptions),
            NullLoggerFactory.Instance);

        var cache = new ControllerActionInvokerCache(
            parameterBinder,
            modelBinderFactory,
            modelMetadataProvider,
            new[] { new DefaultFilterProvider() },
            Mock.Of<IControllerFactoryProvider>(),
            Options.Create(mvcOptions));

        return new(
            cache,
            Options.Create(mvcOptions),
            NullLoggerFactory.Instance,
            new DiagnosticListener("Microsoft.AspNetCore"),
            new ActionResultTypeMapper());
    }

    private class TestActions : Controller
    {
        public IActionResult GetAction() => new OkResult();
    }
}
