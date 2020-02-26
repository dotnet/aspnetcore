// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Mvc;

[assembly: ApiController]

namespace Microsoft.AspNetCore.Mvc.Api.Analyzers.TestFiles.ApiControllerFactsTest
{
    public class IsApiControllerAction_ReturnsTrue_IfAttributeIsDeclaredOnAssemblyController : ControllerBase
    {
        public IActionResult Action() => null;
    }
}
