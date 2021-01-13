// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Routing.Matching
{
    internal abstract class MatcherBuilder
    {
        public abstract void AddEndpoint(RouteEndpoint endpoint);

        public abstract Matcher Build();
    }
}
