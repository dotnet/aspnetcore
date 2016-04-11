// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.AspNetCore.Routing.Tree;

namespace Microsoft.AspNetCore.Mvc.Internal
{
    public class AttributeRouteEntries
    {
        public List<TreeRouteLinkGenerationEntry> LinkGenerationEntries { get; } = new List<TreeRouteLinkGenerationEntry>();

        public List<TreeRouteMatchingEntry> MatchingEntries { get; } = new List<TreeRouteMatchingEntry>();
    }
}
