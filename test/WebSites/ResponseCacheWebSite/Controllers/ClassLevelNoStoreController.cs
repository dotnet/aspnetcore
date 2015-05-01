// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Mvc;

namespace ResponseCacheWebSite
{
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None, Duration = 0)]
    public class ClassLevelNoStoreController
    {
        [HttpGet("/ClassLevelNoStore/GetHelloWorld")]
        public string GetHelloWorld()
        {
            return "Hello, World!";
        }

        [HttpGet("/ClassLevelNoStore/CacheThisAction")]
        [ResponseCache(VaryByHeader = "Accept", Duration = 10)]
        public string CacheThisAction()
        {
            return "Conflict";
        }

        [HttpGet("/ClassLevelNoStore/CacheThisActionWithProfileSettings")]
        [ResponseCache(CacheProfileName = "PublicCache30Sec", VaryByHeader = "Accept")]
        public string CacheThisActionWithProfileSettings()
        {
            return "Conflict";
        }
    }
}