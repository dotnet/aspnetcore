// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Mvc;

namespace ModelBindingWebSite
{
    [Route("DataMemberRequired")]
    public class DataMemberRequiredController : Controller
    {
        // If the results are valid you get back the model, otherwise you get the ModelState errors.
        [HttpPost("EchoModelValues")]
        public object EchoModelValues(DataMemberRequiredModel model)
        {
            if (!ModelState.IsValid)
            {
                return HttpBadRequest(ModelState);
            }

            return model;
        }
    }
}
