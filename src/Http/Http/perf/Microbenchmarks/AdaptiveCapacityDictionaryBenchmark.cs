// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Internal;

namespace Microsoft.AspNetCore.Http;

public class AdaptiveCapacityDictionaryBenchmark
{
    private AdaptiveCapacityDictionary<string, string> _smallCapDict;
    private AdaptiveCapacityDictionary<string, string> _smallCapDictTen;
    private AdaptiveCapacityDictionary<string, string> _filledSmallDictionary;
    private Dictionary<string, string> _dict;
    private Dictionary<string, string> _dictTen;
    private Dictionary<string, string> _filledDictTen;
    private KeyValuePair<string, string> _oneValue;
    private List<KeyValuePair<string, string>> _tenValues;

    [IterationSetup]
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

        _smallCapDict = new AdaptiveCapacityDictionary<string, string>(capacity: 1, StringComparer.OrdinalIgnoreCase);
        _smallCapDictTen = new AdaptiveCapacityDictionary<string, string>(capacity: 10, StringComparer.OrdinalIgnoreCase);
        _filledSmallDictionary = new AdaptiveCapacityDictionary<string, string>(capacity: 10, StringComparer.OrdinalIgnoreCase);
        foreach (var a in _tenValues)
        {
            _filledSmallDictionary[a.Key] = a.Value;
        }

        _dict = new Dictionary<string, string>(1, StringComparer.OrdinalIgnoreCase);
        _dictTen = new Dictionary<string, string>(10, StringComparer.OrdinalIgnoreCase);
        _filledDictTen = new Dictionary<string, string>(10, StringComparer.OrdinalIgnoreCase);

        foreach (var a in _tenValues)
        {
            _filledDictTen[a.Key] = a.Value;
        }
    }

    [Benchmark]
    public void OneValue_SmallDict()
    {
        _smallCapDict[_oneValue.Key] = _oneValue.Value;
        _ = _smallCapDict[_oneValue.Key];
    }

    [Benchmark]
    public void OneValue_Dict()
    {
        _dict[_oneValue.Key] = _oneValue.Value;
        _ = _dict[_oneValue.Key];
    }

    [Benchmark]
    public void OneValue_SmallDict_Set()
    {
        _smallCapDict[_oneValue.Key] = _oneValue.Value;
    }

    [Benchmark]
    public void OneValue_Dict_Set()
    {
        _dict[_oneValue.Key] = _oneValue.Value;
    }

    [Benchmark]
    public void OneValue_SmallDict_Get()
    {
        _smallCapDict.TryGetValue("test", out var val);
    }

    [Benchmark]
    public void OneValue_Dict_Get()
    {
        _dict.TryGetValue("test", out var val);
    }

    [Benchmark]
    public void FourValues_SmallDict()
    {
        for (var i = 0; i < 4; i++)
        {
            var val = _tenValues[i];
            _smallCapDictTen[val.Key] = val.Value;
            _ = _smallCapDictTen[val.Key];
        }
    }

    [Benchmark]
    public void FiveValues_SmallDict()
    {
        for (var i = 0; i < 5; i++)
        {
            var val = _tenValues[i];
            _smallCapDictTen[val.Key] = val.Value;
            _ = _smallCapDictTen[val.Key];
        }
    }

    [Benchmark]
    public void SixValues_SmallDict()
    {
        for (var i = 0; i < 6; i++)
        {
            var val = _tenValues[i];
            _smallCapDictTen[val.Key] = val.Value;
            _ = _smallCapDictTen[val.Key];
        }
    }

    [Benchmark]
    public void SevenValues_SmallDict()
    {
        for (var i = 0; i < 7; i++)
        {
            var val = _tenValues[i];
            _smallCapDictTen[val.Key] = val.Value;
            _ = _smallCapDictTen[val.Key];
        }
    }

    [Benchmark]
    public void EightValues_SmallDict()
    {
        for (var i = 0; i < 8; i++)
        {
            var val = _tenValues[i];
            _smallCapDictTen[val.Key] = val.Value;
            _ = _smallCapDictTen[val.Key];
        }
    }

    [Benchmark]
    public void NineValues_SmallDict()
    {
        for (var i = 0; i < 9; i++)
        {
            var val = _tenValues[i];
            _smallCapDictTen[val.Key] = val.Value;
            _ = _smallCapDictTen[val.Key];
        }
    }

    [Benchmark]
    public void TenValues_SmallDict()
    {
        for (var i = 0; i < 10; i++)
        {
            var val = _tenValues[i];
            _smallCapDictTen[val.Key] = val.Value;
            _ = _smallCapDictTen[val.Key];
        }
    }

    [Benchmark]
    public void FourValues_Dict()
    {
        for (var i = 0; i < 4; i++)
        {
            var val = _tenValues[i];
            _dictTen[val.Key] = val.Value;
            _ = _dictTen[val.Key];
        }
    }

    [Benchmark]
    public void FiveValues_Dict()
    {
        for (var i = 0; i < 5; i++)
        {
            var val = _tenValues[i];
            _dictTen[val.Key] = val.Value;
            _ = _dictTen[val.Key];
        }
    }
    [Benchmark]
    public void SixValues_Dict()
    {
        for (var i = 0; i < 6; i++)
        {
            var val = _tenValues[i];
            _dictTen[val.Key] = val.Value;
            _ = _dictTen[val.Key];
        }
    }
    [Benchmark]
    public void SevenValues_Dict()
    {
        for (var i = 0; i < 7; i++)
        {
            var val = _tenValues[i];
            _dictTen[val.Key] = val.Value;
            _ = _dictTen[val.Key];
        }
    }
    [Benchmark]
    public void EightValues_Dict()
    {
        for (var i = 0; i < 8; i++)
        {
            var val = _tenValues[i];
            _dictTen[val.Key] = val.Value;
            _ = _dictTen[val.Key];
        }
    }
    [Benchmark]
    public void NineValues_Dict()
    {
        for (var i = 0; i < 9; i++)
        {
            var val = _tenValues[i];
            _dictTen[val.Key] = val.Value;
            _ = _dictTen[val.Key];
        }
    }

    [Benchmark]
    public void TenValues_Dict()
    {
        for (var i = 0; i < 10; i++)
        {
            var val = _tenValues[i];
            _dictTen[val.Key] = val.Value;
            _ = _dictTen[val.Key];
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
