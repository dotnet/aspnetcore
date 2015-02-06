// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq;
using Microsoft.AspNet.Mvc;
using Microsoft.AspNet.WebUtilities;

namespace FormatterWebSite.Controllers
{
    public class InputFormatterController : Controller
    {
        [HttpPost]
        public object ActionHandlesError([FromBody] DummyClass dummy)
        {
            if (!ActionContext.ModelState.IsValid)
            {
                var parameterBindingErrors = ActionContext.ModelState["dummy"].Errors;
                if (parameterBindingErrors.Count != 0)
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