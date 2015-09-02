// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.AspNet.Mvc;
using Microsoft.AspNet.Mvc.ActionResults;
using Microsoft.AspNet.Mvc.Actions;

namespace FiltersWebSite
{
    public class DummyClassController : Controller
    {
        [ModifyResultsFilter]
        public DummyClass GetDummyClass()
        {
            return new DummyClass()
            {
                SampleInt = 10
            };
        }

        [AddHeader]
        public IActionResult GetEmptyActionResult()
        {
            return new TestActionResult();
        }

        [ShortCircuitActionFilter]
        public string ActionNeverGetsExecuted()
        {
            return "Hello World!";
        }

        [ShortCircuitResultFilter]
        public IActionResult ResultNeverGetsExecuted()
        {
            return new ObjectResult("Returned in Object Result");
        }
    }

    public class TestActionResult : IActionResult
    {
        public Task ExecuteResultAsync(ActionContext context)
        {
            return Task.FromResult(true);
        }
    }
}