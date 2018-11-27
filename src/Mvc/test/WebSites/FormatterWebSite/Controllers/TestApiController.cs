// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using FormatterWebSite.Models;
using Microsoft.AspNetCore.Mvc;

namespace FormatterWebSite.Controllers
{
    [ApiController]
    [Route("[controller]/[action]")]
    public class TestApiController : ControllerBase
    {
        [HttpPost]
        public IActionResult PostBookWithNoValidation(BookModelWithNoValidation bookModel) => Ok();
    }
}
