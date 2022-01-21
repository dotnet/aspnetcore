// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Mvc.Controllers;

internal interface IControllerPropertyActivator
{
    void Activate(ControllerContext context, object controller);

    Action<ControllerContext, object> GetActivatorDelegate(ControllerActionDescriptor actionDescriptor);
}
