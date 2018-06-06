// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Routing.Matchers
{
    internal abstract class MatcherBuilder
    {
        public abstract void AddEntry(string template, MatcherEndpoint endpoint);

        public abstract Matcher Build();
    }
}
