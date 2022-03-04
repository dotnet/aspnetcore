// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Reflection;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Mvc.Controllers
{
    /// <summary>
    /// The list of controllers types in an MVC application. The <see cref="ControllerFeature"/> can be populated
    /// using the <see cref="ApplicationPartManager"/> that is available during startup at <see cref="IMvcBuilder.PartManager"/>
    /// and <see cref="IMvcCoreBuilder.PartManager"/> or at a later stage by requiring the <see cref="ApplicationPartManager"/>
    /// as a dependency in a component.
    /// </summary>
    public class ControllerFeature
    {
        /// <summary>
        /// Gets the list of controller types in an MVC application.
        /// </summary>
        public IList<TypeInfo> Controllers { get; } = new List<TypeInfo>();
    }
}
