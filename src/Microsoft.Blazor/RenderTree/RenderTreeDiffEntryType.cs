// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.Blazor.RenderTree
{
    internal enum RenderTreeDiffEntryType: int
    {
        Continue = 1,
        PrependNode = 2,
        RemoveNode = 3,
        UpdateAttribute = 4,
        UpdateText = 5,
    }
}
