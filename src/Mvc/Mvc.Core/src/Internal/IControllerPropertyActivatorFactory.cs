// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Mvc.Controllers;

namespace Microsoft.AspNetCore.Mvc.Internal
{
    public interface IControllerPropertyActivator
    {
        void Activate(ControllerContext context, object controller);

        Action<ControllerContext, object> GetActivatorDelegate(ControllerActionDescriptor actionDescriptor);
    }
}
