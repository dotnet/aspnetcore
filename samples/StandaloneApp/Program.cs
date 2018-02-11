// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Blazor.Browser.Rendering;

namespace StandaloneApp
{
    public class ProgramX
    {
        public static void Main(string[] args)
        {
            new BrowserRenderer().AddComponent<Home>("app");
        }
    }

    public class ProgramY
    {
        public static void Main(string[] args)
        {
            new BrowserRenderer().AddComponent<App>("app");
        }
    }
}
