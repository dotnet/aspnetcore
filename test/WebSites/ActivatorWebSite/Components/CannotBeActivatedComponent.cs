// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Mvc;

namespace ActivatorWebSite
{
    [ViewComponent(Name = "CannotBeActivated")]
    public class CannotBeActivatedComponent : ViewComponent
    {
        [Activate]
        private FakeType Service { get; set; }

        public IViewComponentResult Invoke()
        {
            return Content("Test");
        }

        private sealed class FakeType
        {
        }
    }
}