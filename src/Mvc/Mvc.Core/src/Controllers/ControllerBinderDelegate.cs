// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Mvc.Controllers;

internal delegate Task ControllerBinderDelegate(
    ControllerContext controllerContext,
    object controller,
    Dictionary<string, object?> arguments);
