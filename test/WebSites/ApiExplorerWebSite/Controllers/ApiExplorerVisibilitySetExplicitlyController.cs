// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Mvc;

namespace ApiExplorer
{
    [ApiExplorerSettings(IgnoreApi = true)]
    [Route("ApiExplorerVisibilitySetExplicitly")]
    public class ApiExplorerVisibilitySetExplicitlyController : Controller
    {
        [ApiExplorerSettings(IgnoreApi = false)]
        [HttpGet("Enabled")]
        public void Enabled()
        {
        }

        [HttpGet("Disabled")]
        public void Disabled()
        {
        }
    }
}