// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.AspNet.Http.Features.Internal
{
    public interface IItemsFeature
    {
        IDictionary<object, object> Items { get; set; }
    }
}