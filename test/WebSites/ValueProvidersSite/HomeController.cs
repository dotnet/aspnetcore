// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Mvc;

namespace ValueProvidersSite
{
    public class HomeController
    {
        public string TestValueProvider(string test)
        {
            return test;
        }

        public string DefaultValueProviders(string test)
        {
            return test;
        }

        [HttpGet("/RouteTest/{test}")]
        public string RouteValueProviders(string test)
        {
            return test;
        }
    }
}