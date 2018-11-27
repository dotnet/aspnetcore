// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Mvc;

namespace FormatterWebSite.Controllers
{
    public class HomeController : Controller
    {
        [HttpPost]
        public IActionResult Index([FromBody]DummyClass dummyObject)
        {
            return Content(dummyObject.SampleInt.ToString());
        }

        [HttpPost]
        public DummyClass GetDummyClass(int sampleInput)
        {
            return new DummyClass { SampleInt = sampleInput };
        }

        [HttpPost]
        public bool CheckIfDummyIsNull([FromBody] DummyClass dummy)
        {
            return dummy != null;
        }

        [HttpPost]
        public DummyClass GetDerivedDummyClass(int sampleInput)
        {
            return new DerivedDummyClass
            {
                SampleInt = sampleInput,
                SampleIntInDerived = 50
            };
        }
    }
}