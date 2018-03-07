// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Reflection;

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
        public static readonly string DefaultContextName = "Default";

        /// <summary>
        /// Gets one or more <see cref="ApplicationPart"/> instances for the specified <paramref name="assembly"/>.
        /// </summary>
        /// <param name="assembly">The <see cref="Assembly"/>.</param>
        /// <param name="context">
        /// The context name. By default, value of this parameter is <see cref="DefaultContextName"/>.
        /// </param>
        public abstract IEnumerable<ApplicationPart> GetApplicationParts(Assembly assembly, string context);
    }
}
