// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Mvc.Filters
{
    internal class MiddlewareFilterFeature : IMiddlewareFilterFeature
    {
        public ResourceExecutingContext ResourceExecutingContext { get; set; }

        public ResourceExecutionDelegate ResourceExecutionDelegate { get; set; }
    }
}
