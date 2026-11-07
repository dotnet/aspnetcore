// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Mvc.Microbenchmarks;

// Binds and validates simple action parameters (an int from route values and a string from the query string),
// the way ControllerBinderDelegateProvider does for every request.
[MemoryDiagnoser]
public class ParameterBinderBenchmark
{
    private ParameterBinder _parameterBinder;
    private IList<IValueProviderFactory> _valueProviderFactories;
    private BoundParameter[] _parameters;
    private ActionDescriptor _actionDescriptor;

    [GlobalSetup]
    public void Setup()
    {
        var services = new ServiceCollection()
            .AddLogging()
            .AddMvcCore()
            .Services
            .BuildServiceProvider();

        _parameterBinder = services.GetRequiredService<ParameterBinder>();
        _valueProviderFactories = services.GetRequiredService<IOptions<MvcOptions>>().Value.ValueProviderFactories;

        var metadataProvider = services.GetRequiredService<IModelMetadataProvider>();
        var modelBinderFactory = services.GetRequiredService<IModelBinderFactory>();
        var method = typeof(ParameterBinderBenchmark).GetMethod(nameof(Action), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

        _parameters = method.GetParameters().Select(p =>
        {
            var descriptor = new ControllerParameterDescriptor
            {
                Name = p.Name,
                ParameterType = p.ParameterType,
                ParameterInfo = p,
                BindingInfo = BindingInfo.GetBindingInfo(p.GetCustomAttributes(inherit: true)),
            };
            var metadata = ((ModelMetadataProvider)metadataProvider).GetMetadataForParameter(p);
            var binder = modelBinderFactory.CreateBinder(new ModelBinderFactoryContext
            {
                BindingInfo = descriptor.BindingInfo,
                Metadata = metadata,
                CacheToken = descriptor,
            });
            return new BoundParameter(descriptor, metadata, binder);
        }).ToArray();

        _actionDescriptor = new ControllerActionDescriptor { Parameters = _parameters.Select(p => (ParameterDescriptor)p.Descriptor).ToList() };
    }

    [Benchmark]
    public async Task<object> BindSimpleParameters()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.QueryString = new QueryString("?name=value");
        var routeData = new RouteData();
        routeData.Values["id"] = "42";
        var actionContext = new ActionContext(httpContext, routeData, _actionDescriptor);

        var valueProvider = await CompositeValueProvider.CreateAsync(actionContext, _valueProviderFactories);

        object last = null;
        foreach (var parameter in _parameters)
        {
            var result = await _parameterBinder.BindModelAsync(
                actionContext,
                parameter.Binder,
                valueProvider,
                parameter.Descriptor,
                parameter.Metadata,
                value: null,
                container: null);
            last = result.Model;
        }

        return last;
    }

    private static void Action(int id, string name)
    {
    }

    private sealed record BoundParameter(ParameterDescriptor Descriptor, ModelMetadata Metadata, IModelBinder Binder);
}
