// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Microsoft.AspNetCore.Mvc.ApplicationParts
{
    /// <summary>
    /// Configures an <see cref="ApplicationPart" /> that contains controllers, as well as Razor views and Pages.
    /// <para>
    /// Combines the results of <see cref="DefaultApplicationPartFactory.GetApplicationParts(Assembly)"/> and
    /// <see cref="CompiledRazorAssemblyApplicationPartFactory.GetApplicationParts(Assembly)"/>. This part factory
    /// may be used if Razor views or Razor Pages are compiled in to with other types including controllers.
    /// </para>
    /// </summary>
    public sealed class ConsolidatedAssemblyApplicationPartFactory : ApplicationPartFactory
    {
        /// <inheritdoc />
        public override IEnumerable<ApplicationPart> GetApplicationParts(Assembly assembly)
        {
            return Enumerable.Concat(
                DefaultApplicationPartFactory.GetDefaultApplicationParts(assembly),
                CompiledRazorAssemblyApplicationPartFactory.GetDefaultApplicationParts(assembly));
        }
    }
}
