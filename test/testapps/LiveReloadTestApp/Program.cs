// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Blazor.Browser.Rendering;

namespace LiveReloadTestApp
{
    public class Program
    {
        static void Main(string[] args)
        {
            new BrowserRenderer().AddComponent<Home>("app");
        }
    }
}
