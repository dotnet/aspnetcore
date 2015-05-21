// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Mvc;

namespace ActivatorWebSite
{
    [ViewComponent(Name = "Number")]
    public class NumberComponent : ViewComponent
    {
        public NumberComponent(MyService myTestService)
        {
            MyTestService = myTestService;
        }

        private MyService MyTestService { get; }

        public IViewComponentResult Invoke(string content)
        {
            return Content(content + ":" + MyTestService.Random);
        }
    }
}