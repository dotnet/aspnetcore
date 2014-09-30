// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Mvc;

namespace ActivatorWebSite
{
    /// <summary>
    /// Controller that verifies if view activation works.
    /// </summary>
    public class ViewController : Controller
    {
        public ViewResult ConsumeDefaultProperties()
        {
            return View();
        }

        public ViewResult ConsumeInjectedService()
        {
            return View();
        }

        public ViewResult ConsumeServicesFromBaseType()
        {
            return View();
        }

        public ViewResult ConsumeViewComponent()
        {
            return View();
        }

        public ViewResult ConsumeValueComponent()
        {
            return View();
        }

        public ViewResult ConsumeViewAndValueComponent()
        {
            return View();
        }

        public ViewResult ConsumeCannotBeActivatedComponent()
        {
            return View();
        }
    }
}