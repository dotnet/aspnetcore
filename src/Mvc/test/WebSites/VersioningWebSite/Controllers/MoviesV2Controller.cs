// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc;

namespace VersioningWebSite;

public class MoviesV2Controller : Controller
{
    private readonly TestResponseGenerator _generator;

    public MoviesV2Controller(TestResponseGenerator generator)
    {
        _generator = generator;
    }

    [VersionPut("Movies/{id}", versionRange: "2")]
    public IActionResult Put(int id)
    {
        return _generator.Generate();
    }
}
