// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Components.Endpoints.Binding;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Microsoft.AspNetCore.Mvc.Microbenchmarks.ModelBinding;

public class ModelBindingPrimitiveTypeBenchmark : BaseModelBindingBenchmark
{
    private static readonly string PrimitiveTypesContent = "parameter=3&";

    protected override void CreateTestContext()
    {
        _testContext = GetTestContext(request =>
        {
            SetFormDataContent(request, PrimitiveTypesContent);
        });
    }

    protected override Type GetParameterType()
    {
        return typeof(int);
    }

    [Benchmark(Baseline = true)]
    public async Task<object> ModelBinding_PrimitiveTypes_Mvc()
    {
        _valueProvider = await CompositeValueProvider.CreateAsync(_testContext);
        var modelBindingResult = await _parameterBinder.BindModelAsync(
            _testContext,
            _modelBinder,
            _valueProvider,
            _parameter,
            _metadata,
            value: null);

        return modelBindingResult.Model;
    }

    [Benchmark]
    public int ModelBinding_PrimitiveType_Components()
    {
        _formDataReader = new FormDataReader(new FormCollectionReadOnlyDictionary(_testContext.HttpContext.Request.Form), CultureInfo.InvariantCulture);
        _formDataReader.PushPrefix("parameter");
        return FormDataMapper.Map<int>(_formDataReader, _formMapperOptions);
    }
}
