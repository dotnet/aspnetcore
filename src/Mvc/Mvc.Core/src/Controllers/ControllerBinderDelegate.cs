// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Mvc.Controllers
{
    internal delegate Task ControllerBinderDelegate(
        ControllerContext controllerContext,
        object controller,
        Dictionary<string, object> arguments);
}
