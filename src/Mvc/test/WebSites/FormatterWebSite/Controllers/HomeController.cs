// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;
using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace FormatterWebSite.Controllers;

public class HomeController : Controller
{
    [HttpPost]
    public IActionResult Index([FromBody] DummyClass dummyObject)
    {
        return Content(dummyObject.SampleInt.ToString(CultureInfo.InvariantCulture));
    }

    [HttpPost]
    public DummyClass GetDummyClass(int sampleInput)
    {
        return new DummyClass { SampleInt = sampleInput };
    }

    [HttpPost]
    public bool CheckIfDummyIsNull([FromBody] DummyClass dummy)
    {
        return dummy != null;
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
    public IActionResult DefaultBody([FromBody] DummyClass dummy)
        => ModelState.IsValid ? Ok() : ValidationProblem();

    [HttpPost]
    public IActionResult OptionalBody([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] DummyClass dummy)
        => ModelState.IsValid ? Ok() : ValidationProblem();

    [HttpPost]
    public IActionResult DefaultValueBody([FromBody] DummyClass dummy = null)
        => ModelState.IsValid ? Ok() : ValidationProblem();

#nullable enable
    [HttpPost]
    public IActionResult NonNullableBody([FromBody] DummyClass dummy)
        => ModelState.IsValid ? Ok() : ValidationProblem();

    [HttpPost]
    public IActionResult NullableBody([FromBody] DummyClass? dummy)
        => ModelState.IsValid ? Ok() : ValidationProblem();
#nullable restore
}
