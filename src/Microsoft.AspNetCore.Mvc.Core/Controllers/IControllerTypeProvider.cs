// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Reflection;

namespace Microsoft.AspNet.Mvc.Controllers
{
    /// <summary>
    /// Provides methods for discovery of controller types.
    /// </summary>
    public interface IControllerTypeProvider
    {
        /// <summary>
        /// Gets a <see cref="IEnumerable{T}"/> of controller <see cref="TypeInfo"/>s.
        /// </summary>
        IEnumerable<TypeInfo> ControllerTypes { get; }
    }
}