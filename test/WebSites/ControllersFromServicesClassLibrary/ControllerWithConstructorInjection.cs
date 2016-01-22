// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;

namespace ControllersFromServicesClassLibrary
{
    public class ConstructorInjectionController
    {
        public ConstructorInjectionController(IUrlHelperFactory urlHelperFactory, QueryValueService queryService)
        {
            UrlHelperFactory = urlHelperFactory;
            QueryService = queryService;
        }

        [ActionContext]
        public ActionContext ActionContext { get; set; }

        private QueryValueService QueryService { get; }

        private IUrlHelperFactory UrlHelperFactory { get; }

        [HttpGet("/constructorinjection")]
        public IActionResult Index()
        {
            var urlHelper = UrlHelperFactory.GetUrlHelper(ActionContext);

            var content = string.Join(
                " ", 
                urlHelper.Action(), 
                QueryService.GetValue(), 
                ActionContext.HttpContext.Request.Headers["Test-Header"]);

            return new ContentResult { Content = content };
        }
    }
}