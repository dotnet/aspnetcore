// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Components.RenderTree;

namespace Microsoft.AspNetCore.Components
{
    internal static class ArrayRangeExtensions
    {
        public static IEnumerable<T> AsEnumerable<T>(this ArrayRange<T> source)
        {
            // This is very allocatey, hence it only existing in test code.
            // If we need a way to enumerate ArrayRange in product code, we should
            // consider adding an AsSpan() method or a struct enumerator.
            return new ArraySegment<T>(source.Array, 0, source.Count);
        }
    }
}
