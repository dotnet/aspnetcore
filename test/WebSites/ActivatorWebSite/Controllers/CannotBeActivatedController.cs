// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Mvc;

namespace ActivatorWebSite
{
    public class CannotBeActivatedController
    {
        public CannotBeActivatedController(FakeType service)
        {
        }

        public IActionResult Index()
        {
            return new NoContentResult();
        }

        public sealed class FakeType
        {
        }
    }
}