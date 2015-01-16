// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Mvc;

namespace FormatFilterWebSite
{
    public class ProducesBaseController : Controller
    {
        [Produces("application/custom_ProducesBaseController_Action")]
        public virtual string ReturnClassName()
        {
            // Should be written using the action's content type. Overriding the one at the class.
            return "ProducesBaseController";
        }
    }
}