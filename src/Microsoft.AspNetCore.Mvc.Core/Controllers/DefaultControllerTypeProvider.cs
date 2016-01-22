// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.AspNet.Mvc.Infrastructure;

namespace Microsoft.AspNet.Mvc.Controllers
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
                return types.Where(typeInfo => IsController(typeInfo, candidateAssemblies));
            }
        }

        /// <summary>
        /// Returns <c>true</c> if the <paramref name="typeInfo"/> is a controller. Otherwise <c>false</c>.
        /// </summary>
        /// <param name="typeInfo">The <see cref="TypeInfo"/>.</param>
        /// <param name="candidateAssemblies">The set of candidate assemblies.</param>
        /// <returns><c>true</c> if the <paramref name="typeInfo"/> is a controller. Otherwise <c>false</c>.</returns>
        protected internal virtual bool IsController(
            TypeInfo typeInfo,
            ISet<Assembly> candidateAssemblies)
        {
            if (typeInfo == null)
            {
                throw new ArgumentNullException(nameof(typeInfo));
            }

            if (candidateAssemblies == null)
            {
                throw new ArgumentNullException(nameof(candidateAssemblies));
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
            if (!typeInfo.Name.EndsWith(ControllerTypeName, StringComparison.OrdinalIgnoreCase) &&
                !DerivesFromController(typeInfo, candidateAssemblies))
            {
                return false;
            }
            if (typeInfo.IsDefined(typeof(NonControllerAttribute)))
            {
                return false;
            }

            return true;
        }

        private bool DerivesFromController(TypeInfo typeInfo, ISet<Assembly> candidateAssemblies)
        {
            while (typeInfo != ObjectTypeInfo)
            {
                var baseTypeInfo = typeInfo.BaseType.GetTypeInfo();

                // A base type will be treated as a controller if
                // a) it ends in the term "Controller" and
                // b) it's assembly is one of the candidate assemblies we're considering. This ensures that the assembly
                // the base type is declared in references Mvc.
                if (baseTypeInfo.Name.EndsWith(ControllerTypeName, StringComparison.Ordinal) &&
                    candidateAssemblies.Contains(baseTypeInfo.Assembly))
                {
                    return true;
                }

                // c). The base type is called 'Controller.
                if (string.Equals(baseTypeInfo.Name, ControllerTypeName, StringComparison.Ordinal))
                {
                    return true;
                }

                typeInfo = baseTypeInfo;
            }

            return false;
        }
    }
}