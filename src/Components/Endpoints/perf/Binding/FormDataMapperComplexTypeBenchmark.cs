// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.Components.Endpoints.Binding;
public class FormDataMapperComplexTypeBenchmark
{
    private FormDataMapperOptions _formMapperOptions;
    private Dictionary<Prefix, StringValues> _formDataEntries;
    private FormDataReader _formDataReader;

    public static char[] Buffer = new char[2048];

    [GlobalSetup]
    public void Setup()
    {
        _formMapperOptions = new FormDataMapperOptions();
        _formDataEntries = new Dictionary<Prefix, StringValues>
        {
            [new Prefix("CompanyName".AsMemory())] = new StringValues("Foo"),
            [new Prefix("ContactName".AsMemory())] = new StringValues("Bar"),
            [new Prefix("ContactTitle".AsMemory())] = new StringValues("Baz"),
            [new Prefix("Address.Street".AsMemory())] = new StringValues("Qux"),
            [new Prefix("Address.PostalCode".AsMemory())] = new StringValues("12345"),
            [new Prefix("Address.City".AsMemory())] = new StringValues("Seattle"),
            [new Prefix("Address.Country".AsMemory())] = new StringValues("USA"),
            [new Prefix("Phone".AsMemory())] = new StringValues("555-555-5555"),
            [new Prefix("Fax".AsMemory())] = new StringValues("555-555-5556"),
        };
    }

    [IterationSetup]
    public void IterationSetup()
    {
        _formDataReader = new FormDataReader(_formDataEntries, CultureInfo.InvariantCulture, Buffer);
    }

    [Benchmark]
    public Customer ModelBinding_ComplexType_Components()
    {
        return FormDataMapper.Map<Customer>(_formDataReader, _formMapperOptions);
    }
}
