// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace MvcMusicStore
{
    public static class ListExtensions
    {
        public static void ForEach<T>(this List<T> list, Action<T> each)
        {
            foreach (var item in list)
            {
                each(item);
            }
        }
    }
}
