// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Reflection;

namespace Microsoft.AspNetCore.Mvc.ApplicationParts
{
    /// <summary>
    /// Exposes a set of types from an <see cref="ApplicationPart"/>.
    /// </summary>
    public interface IApplicationPartTypeProvider
    {
        /// <summary>
        /// Gets the list of available types in the <see cref="ApplicationPart"/>.
        /// </summary>
        IEnumerable<TypeInfo> Types { get; }
    }
}
