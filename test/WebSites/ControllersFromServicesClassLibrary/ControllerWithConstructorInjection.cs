// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Http;
using Microsoft.AspNet.Mvc;

namespace ControllersFromServicesClassLibrary
{
    public class ConstructorInjectionController
    {
        public ConstructorInjectionController(IUrlHelper urlHelper,
                                              QueryValueService queryService)
        {
            UrlHelper = urlHelper;
            QueryService = queryService;
        }

        private IUrlHelper UrlHelper { get; }

        private QueryValueService QueryService { get; }

        [ActionContext]
        public ActionContext ActionContext { get; set; }

        public HttpRequest Request => ActionContext.HttpContext.Request;

        [HttpGet("/constructorinjection")]
        public IActionResult Index()
        {
            var content = string.Join(" ", 
                                      UrlHelper.Action(), 
                                      QueryService.GetValue(), 
                                      Request.Headers["Test-Header"]);

            return new ContentResult { Content = content };
        }
    }
}