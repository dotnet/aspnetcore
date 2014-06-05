// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Routing;

namespace Microsoft.AspNet.Mvc
{
    public interface IActionConstraint
    {
        bool Accept([NotNull] RouteContext context);
    }
}
