// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Formatters;

namespace FormatterWebSite;

public class XmlSerializerController : Controller
{
    public override void OnActionExecuted(ActionExecutedContext context)
    {
        var result = context.Result as ObjectResult;
        if (result != null)
        {
            result.Formatters.Add(new XmlSerializerOutputFormatter());
        }

        base.OnActionExecuted(context);
    }

    [HttpPost]
    public DummyClass GetDummyClass(int sampleInput)
    {
        return new DummyClass { SampleInt = sampleInput };
    }

    [HttpPost]
    public DummyClass GetDerivedDummyClass(int sampleInput)
    {
        return new DerivedDummyClass
        {
            SampleInt = sampleInput,
            SampleIntInDerived = 50
        };
    }

    [HttpPost]
    public Dictionary<string, string> GetDictionary()
    {
        return new Dictionary<string, string>
            {
                { "Hello", "World" }
            };
    }

    [HttpPost]
    public Task<DummyClass> GetTaskOfDummyClass()
    {
        return Task.FromResult(new DummyClass { SampleInt = 10 });
    }

    [HttpPost]
    public Task<object> GetTaskOfDummyClassAsObject()
    {
        return Task.FromResult<object>(new DummyClass { SampleInt = 10 });
    }
}
