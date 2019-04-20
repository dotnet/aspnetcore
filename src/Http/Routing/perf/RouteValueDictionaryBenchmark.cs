// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using BenchmarkDotNet.Attributes;

namespace Microsoft.AspNetCore.Routing
{
    public class RouteValueDictionaryBenchmark
    {
        private RouteValueDictionary _arrayValues;
        private RouteValueDictionary _propertyValues;

        // We modify the route value dictionaries in many of these benchmarks.
        [IterationSetup]
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
            var dictionary = new RouteValueDictionary
            {
                { "action", "Index" }
            };
            return dictionary;
        }

        [Benchmark]
        public RouteValueDictionary AddThreeItems()
        {
            var dictionary = new RouteValueDictionary
            {
                { "action", "Index" },
                { "controller", "Home" },
                { "id", "15" }
            };
            return dictionary;
        }

        [Benchmark]
        public RouteValueDictionary ConditionalAdd_ContainsKeyAdd()
        {
            var dictionary = _arrayValues;

            if (!dictionary.ContainsKey("action"))
            {
                dictionary.Add("action", "Index");
            }

            if (!dictionary.ContainsKey("controller"))
            {
                dictionary.Add("controller", "Home");
            }

            if (!dictionary.ContainsKey("area"))
            {
                dictionary.Add("area", "Admin");
            }

            return dictionary;
        }
        
        [Benchmark]
        public RouteValueDictionary ConditionalAdd_TryAdd()
        {
            var dictionary = _arrayValues;

            dictionary.TryAdd("action", "Index");
            dictionary.TryAdd("controller", "Home");
            dictionary.TryAdd("area", "Admin");

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
            var dictionary = _propertyValues;
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
            var dictionary = new RouteValueDictionary
            {
                ["action"] = "Index"
            };
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
            var dictionary = new RouteValueDictionary
            {
                ["action"] = "Index",
                ["controller"] = "Home",
                ["id"] = "15"
            };
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