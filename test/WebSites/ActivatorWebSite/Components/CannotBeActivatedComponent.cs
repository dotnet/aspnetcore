// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Mvc;

namespace ActivatorWebSite
{
    [ViewComponent(Name = "CannotBeActivated")]
    public class CannotBeActivatedComponent : ViewComponent
    {
        public CannotBeActivatedComponent(FakeType fakeType)
        {
        }

        public IViewComponentResult Invoke()
        {
            return Content("Test");
        }

        public sealed class FakeType
        {
        }
    }
}