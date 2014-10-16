// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Mvc;

namespace ViewComponentWebSite
{
    public class TestViewComponent : ViewComponent
    {
        public IViewComponentResult Invoke(string valueFromView)
        {
            var model = new SampleModel
            {
                Prop1 = "value-from-component",
                Prop2 = valueFromView
            };
            return View(model);
        }
    }
}