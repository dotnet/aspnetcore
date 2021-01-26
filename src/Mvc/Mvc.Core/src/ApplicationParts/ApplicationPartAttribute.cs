// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Mvc.ApplicationParts
{
    /// <summary>
    /// Specifies an assembly to be added as an <see cref="ApplicationPart" />.
    /// <para>
    /// In the ordinary case, MVC will generate <see cref="ApplicationPartAttribute" />
    /// instances on the entry assembly for each dependency that references MVC.
    /// Each of these assemblies is treated as an <see cref="ApplicationPart" />.
    /// </para>
    /// </summary>
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
    public sealed class ApplicationPartAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of <see cref="ApplicationPartAttribute" />.
        /// </summary>
        /// <param name="assemblyName">The assembly name.</param>
        public ApplicationPartAttribute(string assemblyName)
        {
            AssemblyName = assemblyName ?? throw new ArgumentNullException(nameof(assemblyName));
        }

        /// <summary>
        /// Gets the assembly name.
        /// </summary>
        public string AssemblyName { get; }
    }
}
