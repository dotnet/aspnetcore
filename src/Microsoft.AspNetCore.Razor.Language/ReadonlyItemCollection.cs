// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Razor.Language
{
    internal class ReadOnlyItemCollection : ItemCollection
    {
        public static readonly ItemCollection Empty = new ReadOnlyItemCollection();

        public override object this[object key]
        {
            get => null;
            set => throw new NotSupportedException();
        }
    }
}
