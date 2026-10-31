// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace SimpleWebSite.Controllers;

public class HomeController
{
    public IDictionary<string, string> Index()
    {
        return new Dictionary<string, string> {
                {"first", "wall" },
                {"second", "floor" }
            };
    }
}
