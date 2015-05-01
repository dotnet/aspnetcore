// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Mvc;

namespace ViewComponentWebSite.Namespace2
{
    // The full name is different here from the other view component with the same short name.
    public class SameNameViewComponent : ViewComponent
    {
        public string Invoke()
        {
            return "ViewComponentWebSite.Namespace2.SameName";
        }
    }
}