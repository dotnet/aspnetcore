// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.AspNetCore.Mvc.ApplicationModels
{
    /// <summary>
    /// An interface which is used to represent something with properties.
    /// </summary>
    public interface IPropertyModel
    {
        /// <summary>
        /// The properties.
        /// </summary>
        IDictionary<object, object> Properties { get; }
    }
}
