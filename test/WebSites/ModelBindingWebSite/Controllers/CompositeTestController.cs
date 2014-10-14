// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Mvc;

namespace ModelBindingWebSite.Controllers
{
    [Route("/CompositeTest/[action]/{param}")]
    public class CompositeTestController : Controller
    {
        public string RestrictValueProvidersUsingFromQuery([FromQuery] string param)
        {
            return param;
        }

        public string RestrictValueProvidersUsingFromRoute([FromRoute] string param)
        {
            return param;
        }

        public string RestrictValueProvidersUsingFromForm([FromForm] string param)
        {
            return param;
        }
    }
}