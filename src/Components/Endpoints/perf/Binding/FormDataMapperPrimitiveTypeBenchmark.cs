// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.Components.Endpoints.Binding;

public class FormDataMapperPrimitiveTypeBenchmark
{
    private FormDataMapperOptions _formMapperOptions;
    private Dictionary<string, StringValues> _formDataEntries;
    private FormDataReader _formDataReader;

    [GlobalSetup]
    public void Setup()
    {
        _formMapperOptions = new FormDataMapperOptions();
        _formDataEntries = new Dictionary<string, StringValues> { ["value"] = "3" };
    }

    [IterationSetup]
    public void IterationSetup()
    {
        _formDataReader = new FormDataReader(_formDataEntries, CultureInfo.InvariantCulture);
    }

    [Benchmark]
    public int ModelBinding_PrimitiveType_Components()
    {
        _formDataReader.PushPrefix("value");
        return FormDataMapper.Map<int>(_formDataReader, _formMapperOptions);
    }
}
