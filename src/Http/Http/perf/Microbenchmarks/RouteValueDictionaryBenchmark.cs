// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BenchmarkDotNet.Attributes;

namespace Microsoft.AspNetCore.Routing;

public class RouteValueDictionaryBenchmark
{
    private RouteValueDictionary _arrayValues;
    private RouteValueDictionary _propertyValues;
    private RouteValueDictionary _arrayValuesEmpty;

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
        _arrayValuesEmpty = new RouteValueDictionary();
        _propertyValues = new RouteValueDictionary(new { action = "Index", controller = "Home", id = "17" });
    }

    [Benchmark]
    public void Ctor_Values_RouteValueDictionary_EmptyArray()
    {
        new RouteValueDictionary(_arrayValuesEmpty);
    }

    [Benchmark]
    public RouteValueDictionary Ctor_Values_RouteValueDictionary_Array()
    {
        return new RouteValueDictionary(_arrayValues);
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
    public void ContainsKey_Array_Found()
    {
        _arrayValues.ContainsKey("id");
    }

    [Benchmark]
    public void ContainsKey_Array_NotFound()
    {
        _arrayValues.ContainsKey("name");
    }

    [Benchmark]
    public void ContainsKey_Properties_Found()
    {
        _propertyValues.ContainsKey("id");
    }

    [Benchmark]
    public void ContainsKey_Properties_NotFound()
    {
        _propertyValues.ContainsKey("name");
    }

    [Benchmark]
    public void TryAdd_Properties_AtCapacity_KeyExists()
    {
        var propertyValues = new RouteValueDictionary(new { action = "Index", controller = "Home", id = "17", area = "root" });
        propertyValues.TryAdd("id", "15");
    }

    [Benchmark]
    public void TryAdd_Properties_AtCapacity_KeyDoesNotExist()
    {
        var propertyValues = new RouteValueDictionary(new { action = "Index", controller = "Home", id = "17", area = "root" });
        _propertyValues.TryAdd("name", "Service");
    }

    [Benchmark]
    public void TryAdd_Properties_NotAtCapacity_KeyExists()
    {
        var propertyValues = new RouteValueDictionary(new { action = "Index", controller = "Home", id = "17" });
        propertyValues.TryAdd("id", "15");
    }

    [Benchmark]
    public void TryAdd_Properties_NotAtCapacity_KeyDoesNotExist()
    {
        var propertyValues = new RouteValueDictionary(new { action = "Index", controller = "Home", id = "17" });
        _propertyValues.TryAdd("name", "Service");
    }

    [Benchmark]
    public void TryAdd_Array_AtCapacity_KeyExists()
    {
        var arrayValues = new RouteValueDictionary
                {
                    { "action", "Index" },
                    { "controller", "Home" },
                    { "id", "17" },
                    { "area", "root" }
                };
        arrayValues.TryAdd("id", "15");
    }

    [Benchmark]
    public void TryAdd_Array_AtCapacity_KeyDoesNotExist()
    {
        var arrayValues = new RouteValueDictionary
                {
                    { "action", "Index" },
                    { "controller", "Home" },
                    { "id", "17" },
                    { "area", "root" }
                };
        arrayValues.TryAdd("name", "Service");
    }

    [Benchmark]
    public void TryAdd_Array_NotAtCapacity_KeyExists()
    {
        var arrayValues = new RouteValueDictionary
                {
                    { "action", "Index" },
                    { "controller", "Home" },
                    { "id", "17" }
                };
        arrayValues.TryAdd("id", "15");
    }

    [Benchmark]
    public void TryAdd_Array_NotAtCapacity_KeyDoesNotExist()
    {
        var arrayValues = new RouteValueDictionary
                {
                    { "action", "Index" },
                    { "controller", "Home" },
                    { "id", "17" },
                };
        arrayValues.TryAdd("name", "Service");
    }

    [Benchmark]
    public void ConditionalAdd_Array()
    {
        var arrayValues = new RouteValueDictionary()
                {
                    { "action", "Index" },
                    { "controller", "Home" },
                    { "id", "17" },
                };

        if (!arrayValues.ContainsKey("name"))
        {
            arrayValues.Add("name", "Service");
        }
    }

    [Benchmark]
    public void ConditionalAdd_Properties()
    {
        var propertyValues = new RouteValueDictionary(new { action = "Index", controller = "Home", id = "17" });

        if (!propertyValues.ContainsKey("name"))
        {
            propertyValues.Add("name", "Service");
        }
    }

    [Benchmark]
    public RouteValueDictionary ConditionalAdd_ContainsKey_Array()
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
