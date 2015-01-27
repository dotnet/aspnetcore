// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
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

        [Activate]
        public HttpRequest Request { get; set; }

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