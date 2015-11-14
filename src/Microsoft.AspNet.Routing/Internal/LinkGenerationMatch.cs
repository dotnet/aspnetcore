// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Routing.Tree;

namespace Microsoft.AspNet.Routing.Internal
{
    public struct LinkGenerationMatch
    {
        private readonly bool _isFallbackMatch;
        private readonly TreeRouteLinkGenerationEntry _entry;

        public LinkGenerationMatch(TreeRouteLinkGenerationEntry entry, bool isFallbackMatch)
        {
            _entry = entry;
            _isFallbackMatch = isFallbackMatch;
        }

        public TreeRouteLinkGenerationEntry Entry { get { return _entry; } }

        public bool IsFallbackMatch { get { return _isFallbackMatch; } }
    }
}