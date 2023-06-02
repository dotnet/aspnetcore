// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Components.Endpoints.Binding;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Microsoft.AspNetCore.Mvc.Microbenchmarks.ModelBinding;
public class ModelBindingComplexTypeBenchmark : BaseModelBindingBenchmark
{
    private static readonly string CustomerBodyContent = "CompanyName=Foo&ContactName=Bar&ContactTitle=Baz&Address.Street=Qux&Address.PostalCode=12345&Address.City=Seattle&Address.Country=USA&Phone=555-555-5555&Fax=555-555-5556";

    protected override void CreateTestContext()
    {
        _testContext = GetTestContext(request =>
        {
            SetFormDataContent(request, CustomerBodyContent);
        });
    }

    protected override Type GetParameterType()
    {
        return typeof(Customer);
    }

    [Benchmark(Baseline = true)]
    public async Task<object> ModelBinding_ComplexTypes_Mvc()
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
    public Customer ModelBinding_ComplexType_Components()
    {
        _formDataReader = new FormDataReader(new FormCollectionReadOnlyDictionary(_testContext.HttpContext.Request.Form), CultureInfo.InvariantCulture);
        return FormDataMapper.Map<Customer>(_formDataReader, _formMapperOptions);
    }
}
