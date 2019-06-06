// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Reflection;

namespace Microsoft.AspNetCore.Mvc.ApplicationParts
{
    /// <summary>
    /// Configures an assembly as a <see cref="CompiledRazorAssemblyPart"/>.
    /// </summary>
    public class CompiledRazorAssemblyApplicationPartFactory : ApplicationPartFactory
    {
        /// <summary>
        /// Gets the sequence of <see cref="ApplicationPart"/> instances that are created by this instance of <see cref="DefaultApplicationPartFactory"/>.
        /// <para>
        /// Applications may use this method to get the same behavior as this factory produces during MVC's default part discovery.
        /// </para>
        /// </summary>
        /// <param name="assembly">The <see cref="Assembly"/>.</param>
        /// <returns>The sequence of <see cref="ApplicationPart"/> instances.</returns>
        public static IEnumerable<ApplicationPart> GetDefaultApplicationParts(Assembly assembly)
        {
            if (assembly == null)
            {
                throw new ArgumentNullException(nameof(assembly));
            }

            yield return new CompiledRazorAssemblyPart(assembly);
        }

        /// <inheritdoc />
        public override IEnumerable<ApplicationPart> GetApplicationParts(Assembly assembly) => GetDefaultApplicationParts(assembly);
    }
}
