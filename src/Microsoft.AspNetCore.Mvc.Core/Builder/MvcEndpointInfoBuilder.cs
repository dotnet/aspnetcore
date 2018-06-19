// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.AspNetCore.Routing;

namespace Microsoft.AspNetCore.Builder
{
    public class MvcEndpointInfoBuilder
    {
        public MvcEndpointInfoBuilder(IInlineConstraintResolver constraintResolver)
        {
            ConstraintResolver = constraintResolver;
        }

        public List<MvcEndpointInfo> EndpointInfos { get; } = new List<MvcEndpointInfo>();
        public IInlineConstraintResolver ConstraintResolver { get; }
    }
}
