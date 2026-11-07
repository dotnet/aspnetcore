// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.Mvc.Microbenchmarks;

// Binds a complex type with a nested complex property from the query string through the
// real ParameterBinder and ComplexObjectModelBinder, so each request creates a
// DefaultModelBindingContext and enters nested scopes for the model and its properties.
public class ComplexTypeModelBindingBenchmark
{
    private IServiceProvider _services;
    private ParameterBinder _parameterBinder;
    private IModelBinder _modelBinder;
    private ModelMetadata _metadata;
    private ParameterDescriptor _parameter;
    private QueryCollection _query;

    [GlobalSetup]
    public void Setup()
    {
        var services = new ServiceCollection();
        services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        services.AddMvcCore();
        _services = services.BuildServiceProvider();

        _parameterBinder = _services.GetRequiredService<ParameterBinder>();
        var metadataProvider = _services.GetRequiredService<IModelMetadataProvider>();
        var modelBinderFactory = _services.GetRequiredService<IModelBinderFactory>();

        _parameter = new ParameterDescriptor
        {
            Name = "customer",
            ParameterType = typeof(Customer),
            BindingInfo = new BindingInfo(),
        };
        _metadata = metadataProvider.GetMetadataForType(typeof(Customer));
        _modelBinder = modelBinderFactory.CreateBinder(new ModelBinderFactoryContext
        {
            BindingInfo = _parameter.BindingInfo,
            Metadata = _metadata,
            CacheToken = _parameter,
        });

        _query = new QueryCollection(new Dictionary<string, StringValues>
        {
            ["Id"] = "42",
            ["Name"] = "Contoso",
            ["Address.Street"] = "1 Microsoft Way",
            ["Address.City"] = "Redmond",
            ["Address.Zip"] = "98052",
        });
    }

    [Benchmark]
    public async Task<object> BindComplexTypeFromQuery()
    {
        var actionContext = new ActionContext(
            new DefaultHttpContext { RequestServices = _services },
            new RouteData(),
            new ActionDescriptor());
        var valueProvider = new QueryStringValueProvider(BindingSource.Query, _query, CultureInfo.InvariantCulture);

        var result = await _parameterBinder.BindModelAsync(
            actionContext,
            _modelBinder,
            valueProvider,
            _parameter,
            _metadata,
            value: null,
            container: null);

        return result.Model;
    }

    public class Customer
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public Address Address { get; set; }
    }

    public class Address
    {
        public string Street { get; set; }

        public string City { get; set; }

        public string Zip { get; set; }
    }
}
