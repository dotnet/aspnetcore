// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.Components.Endpoints.Binding;

public class FormDataMapperCollectionBenchmark
{
    private FormDataMapperOptions _formMapperOptions;
    private Dictionary<FormKey, StringValues> _formDataEntries;
    private FormDataReader _formDataReader;

    [Params(0, 1, 10, 100, 1000)]
    public int CollectionSize { get; set; }

    public static char[] Buffer = new char[2048];

    [GlobalSetup]
    public void Setup()
    {
        _formMapperOptions = new FormDataMapperOptions();
        _formDataEntries = Enumerable.Range(0, CollectionSize)
            .ToDictionary(i => new FormKey($"[{i}]".AsMemory()), i => new StringValues(i.ToString(CultureInfo.InvariantCulture)));
    }

    [IterationSetup]
    public void IterationSetup()
    {
        _formDataReader = new FormDataReader(_formDataEntries, CultureInfo.InvariantCulture, Buffer);
    }

    [Benchmark]
    public List<int> MapPrimitiveCollectionType()
    {
        return FormDataMapper.Map<List<int>>(_formDataReader, _formMapperOptions);
    }
}
