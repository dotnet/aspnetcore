// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Mvc.Routing;

namespace Microsoft.AspNet.Mvc.Internal.Routing
{
    public struct LinkGenerationMatch
    {
        private readonly bool _isFallbackMatch;
        private readonly AttributeRouteLinkGenerationEntry _entry;

        public LinkGenerationMatch(AttributeRouteLinkGenerationEntry entry, bool isFallbackMatch)
        {
            _entry = entry;
            _isFallbackMatch = isFallbackMatch;
        }

        public AttributeRouteLinkGenerationEntry Entry { get { return _entry; } }

        public bool IsFallbackMatch { get { return _isFallbackMatch; } }
    }
}