// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Mvc;

namespace ApiExplorerWebSite
{
    [ApiExplorerSettings(GroupName = "SetOnController")]
    [Route("ApiExplorerNameSetExplicitly")]
    public class ApiExplorerNameSetExplicitlyController : Controller
    {
        [HttpGet("SetOnController")]
        public void SetOnController()
        {
        }

        [ApiExplorerSettings(GroupName = "SetOnAction")]
        [HttpGet("SetOnAction")]
        public void SetOnAction()
        {
        }
    }
}