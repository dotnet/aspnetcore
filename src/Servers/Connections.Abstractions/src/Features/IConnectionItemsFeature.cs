// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.AspNetCore.Connections.Features
{
    /// <summary>
    /// A bag of items associated with a given connection.
    /// </summary>
    public interface IConnectionItemsFeature
    {
        /// <summary>
        /// Gets or sets the items associated with the connection.
        /// </summary>
        IDictionary<object, object?> Items { get; set; }
    }
}
