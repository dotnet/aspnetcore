// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Mvc;

namespace ActivatorWebSite
{
    [ViewComponent(Name ="Value")]
    public class ValueComponent : ViewComponent
    {
        [Activate]
        public ViewService MyViewService { get; set; }

        public IViewComponentResult Invoke()
        {
            return Content(MyViewService.GetValue());
        }
    }
}