// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Routing;

namespace Microsoft.AspNetCore.Mvc.Microbenchmarks;

// Mirrors what the route, query string and form value providers do per request:
// create a PrefixContainer over the request's keys and probe it for a couple of model names.
[MemoryDiagnoser]
public class PrefixContainerBenchmark
{
    private RouteValueDictionary _routeValues;
    private ICollection<string> _keys;
    private string[] _probes;

    [Params(1, 3, 6, 12)]
    public int KeyCount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        var names = new[] { "action", "controller", "id", "name", "page", "sort", "filter", "Input.Name", "Input.Age", "Input.Email", "Input.Tags[0]", "returnUrl" };
        _routeValues = new RouteValueDictionary();
        for (var i = 0; i < KeyCount; i++)
        {
            _routeValues[names[i]] = "value";
        }
        _keys = new Dictionary<string, string>(_routeValues.ToDictionary(k => k.Key, k => "value"), StringComparer.OrdinalIgnoreCase).Keys;
        _probes = new[] { "id", "model" };
    }

    [Benchmark]
    public bool RouteValueKeys()
    {
        var container = new PrefixContainer(_routeValues.Keys);
        var result = false;
        foreach (var probe in _probes)
        {
            result |= container.ContainsPrefix(probe);
        }
        return result;
    }

    [Benchmark]
    public bool DictionaryKeys()
    {
        var container = new PrefixContainer(_keys);
        var result = false;
        foreach (var probe in _probes)
        {
            result |= container.ContainsPrefix(probe);
        }
        return result;
    }
}
