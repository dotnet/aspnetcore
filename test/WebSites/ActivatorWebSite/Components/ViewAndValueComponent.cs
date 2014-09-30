// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Mvc;

namespace ActivatorWebSite
{
    [ViewComponent(Name = "ViewAndValue")]
    public class ViewAndValueComponent : ViewComponent
    {
        [Activate]
        public MyService MySampleService { get; set; }

        [Activate]
        public ViewService MyViewService { get; set; }

        public IViewComponentResult Invoke(string content)
        {
            return Content(content + ":" + MySampleService.Random + " " + MyViewService.GetValue());
        }
    }
}