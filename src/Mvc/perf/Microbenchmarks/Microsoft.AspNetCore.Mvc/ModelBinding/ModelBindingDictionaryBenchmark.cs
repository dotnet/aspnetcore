// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Text;
using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Components.Endpoints.Binding;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Microsoft.AspNetCore.Mvc.Microbenchmarks.ModelBinding;
public class ModelBindingDictionaryBenchmark : BaseModelBindingBenchmark
{
    [Params(0, 1, 10, 100, 1000)]
    public int DictionarySize { get; set; }

    protected override void CreateTestContext()
    {
        _testContext = GetTestContext(request =>
        {
            SetFormDataContent(request, GetDictionaryData());
        });
    }

    private string GetDictionaryData()
    {
        if (DictionarySize == 0)
        {
            return "";
        }

        var builder = new StringBuilder();
        builder.Append("[0]=0");
        for (var i = 1; i < DictionarySize; i++)
        {
            builder.Append(CultureInfo.InvariantCulture, $"&[{i}]={i}");
        }

        return builder.ToString();
    }

    protected override Type GetParameterType()
    {
        return typeof(Dictionary<int, int>);
    }

    [Benchmark(Baseline = true)]
    public async Task<object> ModelBinding_Dictionary_Mvc()
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
    public Dictionary<int, int> ModelBinding_Dictionary_Components()
    {
        _formDataReader = new FormDataReader(new FormCollectionReadOnlyDictionary(_testContext.HttpContext.Request.Form), CultureInfo.InvariantCulture);
        return FormDataMapper.Map<Dictionary<int, int>>(_formDataReader, _formMapperOptions);
    }
}
