// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Internal.Dictionary;
using Microsoft.Extensions.Primitives;
namespace Microsoft.AspNetCore.Http
{
    public class SmallCapacityDictionaryBenchmark
    {
        private SmallCapacityDictionary<string, string> _smallCapDict;
        private SmallCapacityDictionary<string, string> _smallCapDictFour;
        private Dictionary<string, string> _dict;
        private Dictionary<string, string> _dictFour;
        private KeyValuePair<string, string> _oneValue;
        private List<KeyValuePair<string, string>> _fourValues;

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

            _smallCapDict = new SmallCapacityDictionary<string, string>(StringComparer.OrdinalIgnoreCase, capacity: 1);
            _smallCapDictFour = new SmallCapacityDictionary<string, string>(StringComparer.OrdinalIgnoreCase, capacity: 4);
            _dict = new Dictionary<string, string>(1, StringComparer.OrdinalIgnoreCase);
            _dictFour = new Dictionary<string, string>(4, StringComparer.OrdinalIgnoreCase);
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
        public void SmallDict()
        {
            _ = new SmallCapacityDictionary<string, string>(capacity: 1);
        }

        [Benchmark]
        public void Dict()
        {
            _ = new Dictionary<string, string>(capacity: 1);
        }

    }
}
