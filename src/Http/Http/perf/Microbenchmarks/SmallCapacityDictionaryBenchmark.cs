// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Internal;

namespace Microsoft.AspNetCore.Http
{
    public class AdaptiveCapacityDictionaryBenchmark
    {
        private AdaptiveCapacityDictionary<string, string> _smallCapDict;
        private AdaptiveCapacityDictionary<string, string> _smallCapDictFour;
        private AdaptiveCapacityDictionary<string, string> _smallCapDictTen;
        private Dictionary<string, string> _dict;
        private Dictionary<string, string> _dictFour;
        private Dictionary<string, string> _dictTen;

        private KeyValuePair<string, string> _oneValue;
        private List<KeyValuePair<string, string>> _fourValues;
        private List<KeyValuePair<string, string>> _tenValues;

        [IterationSetup]
        public void Setup()
        {
            _oneValue = new KeyValuePair<string, string>("a", "b");

            _fourValues = new List<KeyValuePair<string, string>>()
            {
                new KeyValuePair<string, string>("a", "b"),
                new KeyValuePair<string, string>("c", "d"),
                new KeyValuePair<string, string>("e", "f"),
                new KeyValuePair<string, string>("g", "h"),
            };

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
            _smallCapDictFour = new AdaptiveCapacityDictionary<string, string>(capacity: 4, StringComparer.OrdinalIgnoreCase);
            _smallCapDictTen = new AdaptiveCapacityDictionary<string, string>(capacity: 10, StringComparer.OrdinalIgnoreCase);
            _dict = new Dictionary<string, string>(1, StringComparer.OrdinalIgnoreCase);
            _dictFour = new Dictionary<string, string>(4, StringComparer.OrdinalIgnoreCase);
            _dictTen = new Dictionary<string, string>(10, StringComparer.OrdinalIgnoreCase);
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
        public void FourValues_SmallDict()
        {
            foreach (var val in _fourValues)
            {
                _smallCapDictFour[val.Key] = val.Value;
                _ = _smallCapDictFour[val.Key];
            }
        }

        [Benchmark]
        public void FourValues_Dict()
        {
            foreach (var val in _fourValues)
            {
                _dictFour[val.Key] = val.Value;
                _ = _dictFour[val.Key];
            }
        }

        [Benchmark]
        public void TenValues_SmallDict()
        {
            foreach (var val in _tenValues)
            {
                _smallCapDictTen[val.Key] = val.Value;
                _ = _smallCapDictTen[val.Key];
            }
        }

        [Benchmark]
        public void TenValues_Dict()
        {
            foreach (var val in _tenValues)
            {
                _dictTen[val.Key] = val.Value;
                _ = _dictTen[val.Key];
            }
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
}
