// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using BenchmarkDotNet.Attributes;

namespace Microsoft.AspNetCore.Routing
{
    public class RouteValueDictionaryBenchmark
    {
        // These dictionaries are used by a few tests over and over, so don't modify them destructively.
        private RouteValueDictionary _arrayValues;
        private RouteValueDictionary _propertyValues;

        [GlobalSetup]
        public void Setup()
        {
            _arrayValues = new RouteValueDictionary()
            {
                { "action", "Index" },
                { "controller", "Home" },
                { "id", "17" },
            };
            _propertyValues = new RouteValueDictionary(new { action = "Index", controller = "Home", id = "17" });
        }

        [Benchmark]
        public RouteValueDictionary AddSingleItem()
        {
            var dictionary = new RouteValueDictionary();
            dictionary.Add("action", "Index");
            return dictionary;
        }

        [Benchmark]
        public RouteValueDictionary AddThreeItems()
        {
            var dictionary = new RouteValueDictionary();
            dictionary.Add("action", "Index");
            dictionary.Add("controller", "Home");
            dictionary.Add("id", "15");
            return dictionary;
        }

        [Benchmark]
        public RouteValueDictionary ForEachThreeItems_Array()
        {
            var dictionary = _arrayValues;
            foreach (var kvp in dictionary)
            {
                GC.KeepAlive(kvp.Value);
            }
            return dictionary;
        }

        [Benchmark]
        public RouteValueDictionary ForEachThreeItems_Properties()
        {
            var dictionary = _arrayValues;
            foreach (var kvp in dictionary)
            {
                GC.KeepAlive(kvp.Value);
            }
            return dictionary;
        }

        [Benchmark]
        public RouteValueDictionary GetThreeItems_Array()
        {
            var dictionary = _arrayValues;
            GC.KeepAlive(dictionary["action"]);
            GC.KeepAlive(dictionary["controller"]);
            GC.KeepAlive(dictionary["id"]);
            return dictionary;
        }

        [Benchmark]
        public RouteValueDictionary GetThreeItems_Properties()
        {
            var dictionary = _propertyValues;
            GC.KeepAlive(dictionary["action"]);
            GC.KeepAlive(dictionary["controller"]);
            GC.KeepAlive(dictionary["id"]);
            return dictionary;
        }

        [Benchmark]
        public RouteValueDictionary SetSingleItem()
        {
            var dictionary = new RouteValueDictionary();
            dictionary["action"] = "Index";
            return dictionary;
        }

        [Benchmark]
        public RouteValueDictionary SetExistingItem()
        {
            var dictionary = _arrayValues;
            dictionary["action"] = "About";
            return dictionary;
        }

        [Benchmark]
        public RouteValueDictionary SetThreeItems()
        {
            var dictionary = new RouteValueDictionary();
            dictionary["action"] = "Index";
            dictionary["controller"] = "Home";
            dictionary["id"] = "15";
            return dictionary;
        }

        [Benchmark]
        public RouteValueDictionary TryGetValueThreeItems_Array()
        {
            var dictionary = _arrayValues;
            dictionary.TryGetValue("action", out var action);
            dictionary.TryGetValue("controller", out var controller);
            dictionary.TryGetValue("id", out var id);
            GC.KeepAlive(action);
            GC.KeepAlive(controller);
            GC.KeepAlive(id);
            return dictionary;
        }

        [Benchmark]
        public RouteValueDictionary TryGetValueThreeItems_Properties()
        {
            var dictionary = _propertyValues;
            dictionary.TryGetValue("action", out var action);
            dictionary.TryGetValue("controller", out var controller);
            dictionary.TryGetValue("id", out var id);
            GC.KeepAlive(action);
            GC.KeepAlive(controller);
            GC.KeepAlive(id);
            return dictionary;
        }
    }
}
