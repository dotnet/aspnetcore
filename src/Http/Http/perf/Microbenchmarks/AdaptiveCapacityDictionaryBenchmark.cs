// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Internal;

namespace Microsoft.AspNetCore.Http;

public class AdaptiveCapacityDictionaryBenchmark
{
    private AdaptiveCapacityDictionary<string, string> _filledSmallDictionary;
    private Dictionary<string, string> _filledDictTen;
    private KeyValuePair<string, string> _oneValue;
    private List<KeyValuePair<string, string>> _tenValues;

    [GlobalSetup]
    public void Setup()
    {
        _oneValue = new KeyValuePair<string, string>("a", "b");

        _tenValues = new List<KeyValuePair<string, string>>()
            {
                new KeyValuePair<string, string>("a", "b"),
                new KeyValuePair<string, string>("c", "d"),
                new KeyValuePair<string, string>("e", "f"),
                new KeyValuePair<string, string>("g", "h"),
                new KeyValuePair<string, string>("i", "j"),
                new KeyValuePair<string, string>("k", "l"),
                new KeyValuePair<string, string>("m", "n"),
                new KeyValuePair<string, string>("o", "p"),
                new KeyValuePair<string, string>("q", "r"),
                new KeyValuePair<string, string>("s", "t"),
            };

        _filledSmallDictionary = new AdaptiveCapacityDictionary<string, string>(capacity: 10, StringComparer.OrdinalIgnoreCase);
        foreach (var a in _tenValues)
        {
            _filledSmallDictionary[a.Key] = a.Value;
        }

        _filledDictTen = new Dictionary<string, string>(10, StringComparer.OrdinalIgnoreCase);

        foreach (var a in _tenValues)
        {
            _filledDictTen[a.Key] = a.Value;
        }
    }

    [Benchmark]
    public void OneValue_SmallDict()
    {
        var smallCapDict = new AdaptiveCapacityDictionary<string, string>(capacity: 1, StringComparer.OrdinalIgnoreCase);
        smallCapDict[_oneValue.Key] = _oneValue.Value;
        _ = smallCapDict[_oneValue.Key];
    }

    [Benchmark]
    public void OneValue_Dict()
    {
        var dict = new Dictionary<string, string>(1, StringComparer.OrdinalIgnoreCase);
        dict[_oneValue.Key] = _oneValue.Value;
        _ = dict[_oneValue.Key];
    }

    [Benchmark]
    public void OneValue_SmallDict_Set()
    {
        var smallCapDict = new AdaptiveCapacityDictionary<string, string>(capacity: 1, StringComparer.OrdinalIgnoreCase);
        smallCapDict[_oneValue.Key] = _oneValue.Value;
    }

    [Benchmark]
    public void OneValue_Dict_Set()
    {
        var dict = new Dictionary<string, string>(1, StringComparer.OrdinalIgnoreCase);
        dict[_oneValue.Key] = _oneValue.Value;
    }

    [Benchmark]
    public void OneValue_SmallDict_Get()
    {
        var smallCapDict = new AdaptiveCapacityDictionary<string, string>(capacity: 1, StringComparer.OrdinalIgnoreCase);
        smallCapDict.TryGetValue("test", out var val);
    }

    [Benchmark]
    public void OneValue_Dict_Get()
    {
        var dict = new Dictionary<string, string>(1, StringComparer.OrdinalIgnoreCase);
        dict.TryGetValue("test", out var val);
    }

    [Benchmark]
    public void FourValues_SmallDict()
    {
        var smallCapDictTen = new AdaptiveCapacityDictionary<string, string>(capacity: 10, StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < 4; i++)
        {
            var val = _tenValues[i];
            smallCapDictTen[val.Key] = val.Value;
            _ = smallCapDictTen[val.Key];
        }
    }

    [Benchmark]
    public void FiveValues_SmallDict()
    {
        var smallCapDictTen = new AdaptiveCapacityDictionary<string, string>(capacity: 10, StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < 5; i++)
        {
            var val = _tenValues[i];
            smallCapDictTen[val.Key] = val.Value;
            _ = smallCapDictTen[val.Key];
        }
    }

    [Benchmark]
    public void SixValues_SmallDict()
    {
        var smallCapDictTen = new AdaptiveCapacityDictionary<string, string>(capacity: 10, StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < 6; i++)
        {
            var val = _tenValues[i];
            smallCapDictTen[val.Key] = val.Value;
            _ = smallCapDictTen[val.Key];
        }
    }

    [Benchmark]
    public void SevenValues_SmallDict()
    {
        var smallCapDictTen = new AdaptiveCapacityDictionary<string, string>(capacity: 10, StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < 7; i++)
        {
            var val = _tenValues[i];
            smallCapDictTen[val.Key] = val.Value;
            _ = smallCapDictTen[val.Key];
        }
    }

    [Benchmark]
    public void EightValues_SmallDict()
    {
        var smallCapDictTen = new AdaptiveCapacityDictionary<string, string>(capacity: 10, StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < 8; i++)
        {
            var val = _tenValues[i];
            smallCapDictTen[val.Key] = val.Value;
            _ = smallCapDictTen[val.Key];
        }
    }

    [Benchmark]
    public void NineValues_SmallDict()
    {
        var smallCapDictTen = new AdaptiveCapacityDictionary<string, string>(capacity: 10, StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < 9; i++)
        {
            var val = _tenValues[i];
            smallCapDictTen[val.Key] = val.Value;
            _ = smallCapDictTen[val.Key];
        }
    }

    [Benchmark]
    public void TenValues_SmallDict()
    {
        var smallCapDictTen = new AdaptiveCapacityDictionary<string, string>(capacity: 10, StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < 10; i++)
        {
            var val = _tenValues[i];
            smallCapDictTen[val.Key] = val.Value;
            _ = smallCapDictTen[val.Key];
        }
    }

    [Benchmark]
    public void FourValues_Dict()
    {
        var dictTen = new Dictionary<string, string>(10, StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < 4; i++)
        {
            var val = _tenValues[i];
            dictTen[val.Key] = val.Value;
            _ = dictTen[val.Key];
        }
    }

    [Benchmark]
    public void FiveValues_Dict()
    {
        var dictTen = new Dictionary<string, string>(10, StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < 5; i++)
        {
            var val = _tenValues[i];
            dictTen[val.Key] = val.Value;
            _ = dictTen[val.Key];
        }
    }
    [Benchmark]
    public void SixValues_Dict()
    {
        var dictTen = new Dictionary<string, string>(10, StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < 6; i++)
        {
            var val = _tenValues[i];
            dictTen[val.Key] = val.Value;
            _ = dictTen[val.Key];
        }
    }
    [Benchmark]
    public void SevenValues_Dict()
    {
        var dictTen = new Dictionary<string, string>(10, StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < 7; i++)
        {
            var val = _tenValues[i];
            dictTen[val.Key] = val.Value;
            _ = dictTen[val.Key];
        }
    }
    [Benchmark]
    public void EightValues_Dict()
    {
        var dictTen = new Dictionary<string, string>(10, StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < 8; i++)
        {
            var val = _tenValues[i];
            dictTen[val.Key] = val.Value;
            _ = dictTen[val.Key];
        }
    }
    [Benchmark]
    public void NineValues_Dict()
    {
        var dictTen = new Dictionary<string, string>(10, StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < 9; i++)
        {
            var val = _tenValues[i];
            dictTen[val.Key] = val.Value;
            _ = dictTen[val.Key];
        }
    }

    [Benchmark]
    public void TenValues_Dict()
    {
        var dictTen = new Dictionary<string, string>(10, StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < 10; i++)
        {
            var val = _tenValues[i];
            dictTen[val.Key] = val.Value;
            _ = dictTen[val.Key];
        }
    }

    [Benchmark]
    public void FourValues_SmallDictGet()
    {
        _ = _filledSmallDictionary["g"];
    }

    [Benchmark]
    public void FiveValues_SmallDictGet()
    {
        _ = _filledSmallDictionary["i"];
    }

    [Benchmark]
    public void SixValues_SmallDictGetGet()
    {
        _ = _filledSmallDictionary["k"];

    }

    [Benchmark]
    public void SevenValues_SmallDictGetGet()
    {
        _ = _filledSmallDictionary["m"];
    }

    [Benchmark]
    public void EightValues_SmallDictGet()
    {
        _ = _filledSmallDictionary["o"];
    }

    [Benchmark]
    public void NineValues_SmallDictGet()
    {
        _ = _filledSmallDictionary["q"];
    }

    [Benchmark]
    public void TenValues_SmallDictGet()
    {
        _ = _filledSmallDictionary["s"];
    }

    [Benchmark]
    public void TenValues_DictGet()
    {
        _ = _filledDictTen["s"];
    }

    [Benchmark]
    public void SmallDict()
    {
        _ = new AdaptiveCapacityDictionary<string, string>(capacity: 1);
    }

    [Benchmark]
    public void Dict()
    {
        _ = new Dictionary<string, string>(capacity: 1);
    }

    [Benchmark]
    public void SmallDictFour()
    {
        _ = new AdaptiveCapacityDictionary<string, string>(capacity: 4);
    }

    [Benchmark]
    public void DictFour()
    {
        _ = new Dictionary<string, string>(capacity: 4);
    }

    [Benchmark]
    public void SmallDictTen()
    {
        _ = new AdaptiveCapacityDictionary<string, string>(capacity: 10);
    }

    [Benchmark]
    public void DictTen()
    {
        _ = new Dictionary<string, string>(capacity: 10);
    }
}
