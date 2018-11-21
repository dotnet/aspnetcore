// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;

namespace Microsoft.AspNetCore.Blazor.BuildTools.Core.ILWipe
{
    class SpecList
    {
        private List<SpecListEntry> _itemSpecs;

        public SpecList(string[] fromSpecLines)
        {
            var linesToUse = fromSpecLines.Where(
                line => !string.IsNullOrWhiteSpace(line) && !line.StartsWith('#'));
            _itemSpecs = new List<SpecListEntry>(
                linesToUse.Select(line => new SpecListEntry(line)));
        }

        public bool IsEmpty
        {
            get => _itemSpecs.Count == 0;
        }

        public bool Match(AssemblyItem item)
        {
            // If this needs to be faster, consider implementing some kind of matching tree.
            var lastMatchingSpec = _itemSpecs.LastOrDefault(spec => spec.IsMatch(item));
            return lastMatchingSpec == null ? false : !lastMatchingSpec.Negated;
        }
    }
}
