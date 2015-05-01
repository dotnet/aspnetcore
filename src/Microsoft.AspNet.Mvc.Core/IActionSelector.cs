// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.AspNet.Routing;

namespace Microsoft.AspNet.Mvc
{
    public interface IActionSelector
    {
        Task<ActionDescriptor> SelectAsync(RouteContext context);

        bool HasValidAction(VirtualPathContext context);
    }
}
