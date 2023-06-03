// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.Components.Endpoints.Binding;
public class FormDataMapperComplexValueTypeBenchmark
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
            [new Prefix("Street".AsMemory())] = new StringValues("Qux"),
            [new Prefix("PostalCode".AsMemory())] = new StringValues("12345"),
            [new Prefix("City".AsMemory())] = new StringValues("Seattle"),
            [new Prefix("Country".AsMemory())] = new StringValues("USA"),
        };
        _formDataReader = new FormDataReader(_formDataEntries, CultureInfo.InvariantCulture, Buffer);
        _formMapperOptions.ResolveConverter<Address>();
    }

    [IterationSetup]
    public void IterationSetup()
    {
    }

    [Benchmark]
    public Address ModelBinding_ComplexValueType_Components()
    {
        return FormDataMapper.Map<Address>(_formDataReader, _formMapperOptions);
    }
}
