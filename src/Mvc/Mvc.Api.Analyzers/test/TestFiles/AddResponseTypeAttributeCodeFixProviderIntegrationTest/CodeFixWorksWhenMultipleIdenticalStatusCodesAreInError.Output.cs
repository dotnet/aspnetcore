// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Mvc.Api.Analyzers._OUTPUT_
{
    [ApiController]
    [Route("[controller]/[action]")]
    public class CodeFixWorksWhenMultipleIdenticalStatusCodesAreInError : ControllerBase
    {
        public List<CodeFixWorksWhenMultipleIdenticalStatusCodesAreInErrorModel> Values { get; } = 
            new List<CodeFixWorksWhenMultipleIdenticalStatusCodesAreInErrorModel>();

        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesDefaultResponseType]
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
