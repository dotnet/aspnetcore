// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.Components.Endpoints.Binding;
public class FormDataMapperComplexTypeBenchmark
{
    private FormDataMapperOptions _formMapperOptions;
    private Dictionary<string, StringValues> _formDataEntries;
    private FormDataReader _formDataReader;

    [GlobalSetup]
    public void Setup()
    {
        _formMapperOptions = new FormDataMapperOptions();
        _formDataEntries = new Dictionary<string, StringValues>
        {
            ["CompanyName"] = new StringValues("Foo"),
            ["ContactName"] = new StringValues("Bar"),
            ["ContactTitle"] = new StringValues("Baz"),
            ["Address.Street"] = new StringValues("Qux"),
            ["Address.PostalCode"] = new StringValues("12345"),
            ["Address.City"] = new StringValues("Seattle"),
            ["Address.Country"] = new StringValues("USA"),
            ["Phone"] = new StringValues("555-555-5555"),
            ["Fax"] = new StringValues("555-555-5556"),
        };
    }

    [IterationSetup]
    public void IterationSetup()
    {
        _formDataReader = new FormDataReader(_formDataEntries, CultureInfo.InvariantCulture);
    }

    [Benchmark]
    public Customer ModelBinding_ComplexType_Components()
    {
        return FormDataMapper.Map<Customer>(_formDataReader, _formMapperOptions);
    }
}
