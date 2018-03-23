// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.AspNetCore.Mvc.Core;

namespace Microsoft.AspNetCore.Mvc.ApplicationParts
{
    /// <summary>
    /// Specifies a contract for synthesizing one or more <see cref="ApplicationPart"/> instances
    /// from an <see cref="Assembly"/>.
    /// <para>
    /// By default, Mvc registers each application assembly that it discovers as an <see cref="AssemblyPart"/>.
    /// Assemblies can optionally specify an <see cref="ApplicationPartFactory"/> to configure parts for the assembly
    /// by using <see cref="ProvideApplicationPartFactoryAttribute"/>.
    /// </para>
    /// </summary>
    public abstract class ApplicationPartFactory
    {
        /// <summary>
        /// Gets one or more <see cref="ApplicationPart"/> instances for the specified <paramref name="assembly"/>.
        /// </summary>
        /// <param name="assembly">The <see cref="Assembly"/>.</param>
        public abstract IEnumerable<ApplicationPart> GetApplicationParts(Assembly assembly);

        /// <summary>
        /// Gets the <see cref="ApplicationPartFactory"/> for the specified assembly.
        /// <para>
        /// An assembly may specify an <see cref="ApplicationPartFactory"/> using <see cref="ProvideApplicationPartFactoryAttribute"/>.
        /// Otherwise, <see cref="DefaultApplicationPartFactory"/> is used.
        /// </para>
        /// </summary>
        /// <param name="assembly">The <see cref="Assembly"/>.</param>
        /// <returns>An instance of <see cref="ApplicationPartFactory"/>.</returns>
        public static ApplicationPartFactory GetApplicationPartFactory(Assembly assembly)
        {
            if (assembly == null)
            {
                throw new ArgumentNullException(nameof(assembly));
            }

            var provideAttribute = assembly.GetCustomAttribute<ProvideApplicationPartFactoryAttribute>();
            if (provideAttribute == null)
            {
                return DefaultApplicationPartFactory.Instance;
            }

            var type = provideAttribute.GetFactoryType();
            if (!typeof(ApplicationPartFactory).IsAssignableFrom(type))
            {
                throw new InvalidOperationException(Resources.FormatApplicationPartFactory_InvalidFactoryType(
                    type,
                    nameof(ProvideApplicationPartFactoryAttribute),
                    typeof(ApplicationPartFactory)));
            }

            return (ApplicationPartFactory)Activator.CreateInstance(type);
        }
    }
}
