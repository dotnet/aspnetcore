// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc;

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
    }

    public class TestActionResult : IActionResult
    {
        public Task ExecuteResultAsync(ActionContext context)
        {
            return Task.FromResult(true);
        }
    }
}