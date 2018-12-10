// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.AspNetCore.Routing
{
    public class CompositeEndpointConventionBuilder : IEndpointConventionBuilder
    {
        public CompositeEndpointConventionBuilder(IReadOnlyList<IEndpointConventionBuilder> builders)
        {
            if (builders == null)
            {
                throw new ArgumentNullException(nameof(builders));
            }

            Builders = builders;
        }

        public IReadOnlyList<IEndpointConventionBuilder> Builders { get; }

        public void Apply(Action<EndpointModel> convention)
        {
            if (convention == null)
            {
                throw new ArgumentNullException(nameof(convention));
            }
            
            for (var i = 0; i < Builders.Count; i++)
            {
                Builders[i].Apply(convention);
            }
        }
    }
}
