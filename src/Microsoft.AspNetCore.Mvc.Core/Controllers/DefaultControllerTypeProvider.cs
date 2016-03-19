// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Mvc.Infrastructure;

namespace Microsoft.AspNetCore.Mvc.Controllers
{
    /// <summary>
    /// A <see cref="IControllerTypeProvider"/> that identifies controller types from assemblies
    /// specified by the registered <see cref="IAssemblyProvider"/>.
    /// </summary>
    public class DefaultControllerTypeProvider : IControllerTypeProvider
    {
        private const string ControllerTypeName = "Controller";
        private static readonly TypeInfo ObjectTypeInfo = typeof(object).GetTypeInfo();
        private readonly IAssemblyProvider _assemblyProvider;

        /// <summary>
        /// Initializes a new instance of <see cref="DefaultControllerTypeProvider"/>.
        /// </summary>
        /// <param name="assemblyProvider"><see cref="IAssemblyProvider"/> that provides assemblies to look for
        /// controllers in.</param>
        public DefaultControllerTypeProvider(IAssemblyProvider assemblyProvider)
        {
            _assemblyProvider = assemblyProvider;
        }

        /// <inheritdoc />
        public virtual IEnumerable<TypeInfo> ControllerTypes
        {
            get
            {
                var candidateAssemblies = new HashSet<Assembly>(_assemblyProvider.CandidateAssemblies);
                var types = candidateAssemblies.SelectMany(a => a.DefinedTypes);
                return types.Where(typeInfo => IsController(typeInfo));
            }
        }

        /// <summary>
        /// Returns <c>true</c> if the <paramref name="typeInfo"/> is a controller. Otherwise <c>false</c>.
        /// </summary>
        /// <param name="typeInfo">The <see cref="TypeInfo"/>.</param>
        /// <returns><c>true</c> if the <paramref name="typeInfo"/> is a controller. Otherwise <c>false</c>.</returns>
        protected internal virtual bool IsController(TypeInfo typeInfo)
        {
            if (typeInfo == null)
            {
                throw new ArgumentNullException(nameof(typeInfo));
            }

            if (!typeInfo.IsClass)
            {
                return false;
            }

            if (typeInfo.IsAbstract)
            {
                return false;
            }

            // We only consider public top-level classes as controllers. IsPublic returns false for nested
            // classes, regardless of visibility modifiers
            if (!typeInfo.IsPublic)
            {
                return false;
            }

            if (typeInfo.ContainsGenericParameters)
            {
                return false;
            }

            if (typeInfo.IsDefined(typeof(NonControllerAttribute)))
            {
                return false;
            }

            if (!typeInfo.Name.EndsWith(ControllerTypeName, StringComparison.OrdinalIgnoreCase) &&
                !typeInfo.IsDefined(typeof(ControllerAttribute)))
            {
                return false;
            }

            return true;
        }
    }
}