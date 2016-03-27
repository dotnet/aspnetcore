// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.AspNetCore.Http.Internal;

namespace Microsoft.AspNetCore.Http.Features
{
    public class ItemsFeature : IItemsFeature
    {
        public ItemsFeature()
        {
            Items = new ItemsDictionary();
        }

        public IDictionary<object, object> Items { get; set; }
    }
}