// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using ConnegWebSite;
using Microsoft.AspNet.Mvc;

namespace ConnegWebsite
{
    [Produces("application/FormatFilterController")]
    public class FormatFilterController : Controller
    {
        [FormatFilter]
        public User MethodWithFormatFilter()
        {
            return new User() { Name = "Joe", Address = "1 abc way" };
        }
    }
}