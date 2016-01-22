// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Mvc;

namespace FormatFilterSample.Web
{
    [Produces("application/controller")]
    [Route("[controller]/[action]")]
    public class ProducesOverrideController
    {
        [Produces("application/custom")]
        public string ReturnClassName()
        {
            return "ProducesOverrideController";
        }
    }
}