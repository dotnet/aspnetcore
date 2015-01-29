// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Mvc;

namespace ResponseCacheWebSite
{
    [ResponseCache(Duration = 100, Location = ResponseCacheLocation.Any, VaryByHeader = "Accept")]
    public class ClassLevelCacheController
    {
        public string GetHelloWorld()
        {
            return "Hello, World!";
        }

        public string GetFooBar()
        {
            return "Foo Bar!";
        }

        [ResponseCache(Duration = 20)]
        public string ConflictExistingHeader()
        {
            return "Conflict";
        }

        [ResponseCache(NoStore = true, Duration = 0, Location = ResponseCacheLocation.None)]
        public string DoNotCacheThisAction()
        {
            return "Conflict";
        }
    }
}