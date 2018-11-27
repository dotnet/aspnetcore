// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.ComponentModel;
using Microsoft.AspNetCore.Mvc;

namespace BasicWebSite.Controllers
{
    public class DefaultValuesController : Controller
    {
        [HttpGet]
        public string EchoValue_DefaultValueAttribute([DefaultValue("hello")] string input)
        {
            return input;
        }

        [HttpGet]
        public string EchoValue_DefaultParameterValue(string input = "world")
        {
            return input;
        }

        [HttpGet]
        public string EchoValue_DefaultParameterValue_ForStructs(
            Guid guid = default(Guid),
            TimeSpan timeSpan = default(TimeSpan))
        {
            return $"{guid}, {timeSpan}";
        }
    }
}
