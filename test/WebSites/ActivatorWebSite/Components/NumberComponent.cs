// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Mvc;

namespace ActivatorWebSite
{
    [ViewComponent(Name = "Number")]
    public class NumberComponent : ViewComponent
    {
        [Activate]
        public MyService MyTestService { get; set; }

        public IViewComponentResult Invoke(string content)
        {
            return Content(content + ":" + MyTestService.Random);
        }
    }
}