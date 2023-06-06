// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.Components.Endpoints.Binding;

public class FormDataMapperPrimitiveTypeBenchmark
{
    private FormDataMapperOptions _formMapperOptions;
    private Dictionary<FormKey, StringValues> _formDataEntries;
    private FormDataReader _formDataReader;

    public static char[] Buffer = new char[2048];

    [GlobalSetup]
    public void Setup()
    {
        _formMapperOptions = new FormDataMapperOptions();
        _formDataEntries = new Dictionary<FormKey, StringValues> { [new FormKey("value".AsMemory())] = "3" };
        _formDataReader = new FormDataReader(_formDataEntries, CultureInfo.InvariantCulture, Buffer);
        _formDataReader.PushPrefix("value");
    }

    [Benchmark]
    public int ModelBinding_PrimitiveType_Components()
    {
        var result = FormDataMapper.Map<int>(_formDataReader, _formMapperOptions);
        if (result != 3)
        {
            throw new InvalidOperationException();
        }
        return result;
    }
}
