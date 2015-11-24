// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Mvc;
using Microsoft.AspNet.Mvc.Routing;

namespace UrlHelperSample.Web.Controllers
{
    [Route("api/[controller]/{id?}", Name = "SimplePocoApi")]
    public class SimplePocoController
    {
        private readonly IUrlHelper _urlHelper;

        public SimplePocoController(IUrlHelper urlHelper)
        {
            _urlHelper = urlHelper;
        }

        [HttpGet]
        public string GetById(int id)
        {
            return "value:" + id;
        }
    }
}