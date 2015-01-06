// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Mvc;

namespace ResponseCacheWebSite
{
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None, Duration = 0)]
    public class ClassLevelNoStoreController
    {
        public string GetHelloWorld()
        {
            return "Hello, World!";
        }

        [ResponseCache(VaryByHeader = "Accept", Duration = 10)]
        public string CacheThisAction()
        {
            return "Conflict";
        }
    }
}