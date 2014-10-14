// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Mvc;

namespace ModelBindingWebSite.Controllers
{
    public class MultipleParametersFromBodyController : Controller
    {
        public void MultipleParametersFromBodyThrows([FromBody] int i, [FromBody] string emp)
        {
        }
    }
}