// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.AspNetCore.Http.Features
{
    /// <summary>
    /// Provides a key/value collection that can be used to share data within the scope of this request.
    /// </summary>
    public interface IItemsFeature
    {
        /// <summary>
        /// Gets or sets a a key/value collection that can be used to share data within the scope of this request.
        /// </summary>
        IDictionary<object, object?> Items { get; set; }
    }
}
