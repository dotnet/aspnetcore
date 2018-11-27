// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;

namespace ApiExplorerWebSite
{
    [Route("ApiExplorerApiController/[action]")]
    [ApiController]
    public class ApiExplorerApiController : Controller
    {
        public IActionResult ActionWithoutParameters() => Ok();

        public void ActionWithSomeParameters(object input)
        {
        }

        public void ActionWithIdParameter(int id, string name)
        {
        }

        public void ActionWithIdSuffixParameter(int personId, string personName)
        {
        }

        public void ActionWithFormFileCollectionParameter(IFormFileCollection formFile)
        {
        }
    }
}