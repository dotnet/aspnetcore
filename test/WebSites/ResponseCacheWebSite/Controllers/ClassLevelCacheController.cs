// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Mvc;

namespace ResponseCacheWebSite
{
    [ResponseCache(Duration = 100, Location = ResponseCacheLocation.Any, VaryByHeader = "Accept")]
    public class ClassLevelCacheController
    {
        [HttpGet("/ClassLevelCache/GetHelloWorld")]
        public string GetHelloWorld()
        {
            return "Hello, World!";
        }

        [HttpGet("/ClassLevelCache/GetFooBar")]
        public string GetFooBar()
        {
            return "Foo Bar!";
        }

        [HttpGet("/ClassLevelCache/ConflictExistingHeader")]
        [ResponseCache(Duration = 20)]
        public string ConflictExistingHeader()
        {
            return "Conflict";
        }

        [HttpGet("/ClassLevelCache/DoNotCacheThisAction")]
        [ResponseCache(NoStore = true, Duration = 0, Location = ResponseCacheLocation.None)]
        public string DoNotCacheThisAction()
        {
            return "Conflict";
        }
    }
}