// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Components.Rendering
{
    // Used internally during diffing to track what we know about keyed items and their positions
    internal readonly struct KeyedItemInfo
    {
        public readonly int OldIndex;
        public readonly int NewIndex;
        public readonly int OldSiblingIndex;
        public readonly int NewSiblingIndex;
        public readonly bool IsUnique;

        public KeyedItemInfo(int oldIndex, int newIndex, bool isUnique)
        {
            OldIndex = oldIndex;
            NewIndex = newIndex;
            OldSiblingIndex = -1;
            NewSiblingIndex = -1;

            // Non-unique keys are problematic, because there's no way to know which instance
            // should match with which other, plus they would force us to keep track of which
            // usages have been consumed as we proceed through the diff. Since this is such
            // an edge case, we "tolerate" it just by tracking which keys have duplicates, and
            // for those ones, we never treat them as moved. Instead for those we fall back on
            // insert+delete behavior, i.e., not preserving elements/components.
            //
            // Guidance for developers is therefore to use distinct keys.
            IsUnique = isUnique;
        }

        private KeyedItemInfo(in KeyedItemInfo copyFrom, int oldSiblingIndex, int newSiblingIndex)
        {
            this = copyFrom;
            OldSiblingIndex = oldSiblingIndex;
            NewSiblingIndex = newSiblingIndex;
        }

        public KeyedItemInfo WithOldSiblingIndex(int oldSiblingIndex)
            => new KeyedItemInfo(this, oldSiblingIndex, NewSiblingIndex);

        public KeyedItemInfo WithNewSiblingIndex(int newSiblingIndex)
            => new KeyedItemInfo(this, OldSiblingIndex, newSiblingIndex);
    }
}
