// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Components.Rendering
{
    // Used internally during diffing to track what we know about
    // keyed items and their positions
    internal struct KeyedItemInfo
    {
        public int OldIndex;
        public int NewIndex;
        public int OldSiblingIndex;
        public int NewSiblingIndex;

        public KeyedItemInfo(int oldIndex, int newIndex)
        {
            OldIndex = oldIndex;
            NewIndex = newIndex;
            OldSiblingIndex = -1;
            NewSiblingIndex = -1;
        }
    }
}
