// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#if NETSTANDARD1_6

using System.Collections.Generic;

namespace System.Collections.ObjectModel
{
    /// <summary>
    /// A class that inherits from Collection of T but also exposes its underlying data as List of T for performance.
    /// </summary>
    internal sealed class ListWrapperCollection<T> : Collection<T>
    {
        private readonly List<T> _items;

        internal ListWrapperCollection()
            : this(new List<T>())
        {
        }

        internal ListWrapperCollection(List<T> list)
            : base(list)
        {
            _items = list;
        }

        internal List<T> ItemsList
        {
            get { return _items; }
        }
    }
}
#elif NET46
#else
#error target frameworks needs to be updated.
#endif
