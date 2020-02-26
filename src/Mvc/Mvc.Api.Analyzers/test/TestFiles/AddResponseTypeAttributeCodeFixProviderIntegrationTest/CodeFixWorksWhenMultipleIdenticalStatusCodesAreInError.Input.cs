// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;

namespace Microsoft.AspNetCore.Mvc.Api.Analyzers._INPUT_
{
    [ApiController]
    [Route("[controller]/[action]")]
    public class CodeFixWorksWhenMultipleIdenticalStatusCodesAreInError : ControllerBase
    {
        public List<CodeFixWorksWhenMultipleIdenticalStatusCodesAreInErrorModel> Values { get; } = 
            new List<CodeFixWorksWhenMultipleIdenticalStatusCodesAreInErrorModel>();

        public ActionResult<CodeFixWorksWhenMultipleIdenticalStatusCodesAreInErrorModel> GetItem(int id)
        {
            if (id == 0)
            {
                return NotFound();
            }

            var model = Values.FirstOrDefault(m => m.Id == id);
            if (model == null)
            {
                return NotFound();
            }

            return model;
        }
    }

    public class CodeFixWorksWhenMultipleIdenticalStatusCodesAreInErrorModel
    {
        public int Id { get; set; }
    }
}
