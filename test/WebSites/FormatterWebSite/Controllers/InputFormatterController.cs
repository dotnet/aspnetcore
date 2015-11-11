// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Mvc;

namespace FormatterWebSite.Controllers
{
    public class InputFormatterController : Controller
    {
        [HttpPost]
        public object ActionHandlesError([FromBody] DummyClass dummy)
        {
            if (!ModelState.IsValid)
            {
                // Body model binder normally reports errors for parameters using the empty name.
                var parameterBindingErrors = ModelState["dummy"]?.Errors ?? ModelState[string.Empty]?.Errors;
                if (parameterBindingErrors != null && parameterBindingErrors.Count != 0)
                {
                    return new ErrorInfo
                    {
                        ActionName = "ActionHandlesError",
                        ParameterName = "dummy",
                        Errors = parameterBindingErrors.Select(x => x.ErrorMessage).ToList(),
                        Source = "action"
                    };
                }
            }

            return dummy;
        }

        [HttpPost]
        [ValidateBodyParameter]
        public object ActionFilterHandlesError([FromBody] DummyClass dummy)
        {
            return dummy;
        }

        public IActionResult ReturnInput([FromBody] string test)
        {
            if (!ModelState.IsValid)
            {
                return new HttpStatusCodeResult(StatusCodes.Status400BadRequest);
            }

            return Content(test);
        }
    }
}