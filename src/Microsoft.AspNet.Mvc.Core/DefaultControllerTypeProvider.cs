// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.AspNet.Mvc.Logging;
using Microsoft.Framework.Internal;
using Microsoft.Framework.Logging;

namespace Microsoft.AspNet.Mvc
{
    /// <summary>
    /// A <see cref="IControllerTypeProvider"/> that identifies controller types from assemblies
    /// specified by the registered <see cref="IAssemblyProvider"/>.
    /// </summary>
    public class DefaultControllerTypeProvider : IControllerTypeProvider
    {
        private const string ControllerTypeName = nameof(Controller);
        private static readonly TypeInfo ControllerTypeInfo = typeof(Controller).GetTypeInfo();
        private static readonly TypeInfo ObjectTypeInfo = typeof(object).GetTypeInfo();
        private readonly IAssemblyProvider _assemblyProvider;
        private readonly ILogger _logger;

        /// <summary>
        /// Initializes a new instance of <see cref="DefaultControllerTypeProvider"/>.
        /// </summary>
        /// <param name="assemblyProvider"><see cref="IAssemblyProvider"/> that provides assemblies to look for
        /// controllers in.</param>
        /// <param name="loggerFactory">The <see cref="ILoggerFactory"/>.</param>
        public DefaultControllerTypeProvider(IAssemblyProvider assemblyProvider,
                                             ILoggerFactory loggerFactory)
        {
            _assemblyProvider = assemblyProvider;
            _logger = loggerFactory.Create<DefaultControllerTypeProvider>();
        }

        /// <inheritdoc />
        public virtual IEnumerable<TypeInfo> ControllerTypes
        {
            get
            {
                var candidateAssemblies = new HashSet<Assembly>(_assemblyProvider.CandidateAssemblies);
                if (_logger.IsEnabled(LogLevel.Verbose))
                {
                    foreach (var assembly in candidateAssemblies)
                    {
                        _logger.WriteVerbose(new AssemblyValues(assembly));
                    }
                }

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
        protected internal virtual bool IsController([NotNull] TypeInfo typeInfo,
                                                     [NotNull] ISet<Assembly> candidateAssemblies)
        {
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
            // A type is a controller if it derives from a type that is either named "Controller" or has the suffix
            // "Controller". We'll optimize the most common case of types deriving from the Mvc Controller type and
            // walk up the object graph if that's not the case.
            if (ControllerTypeInfo.IsAssignableFrom(typeInfo))
            {
                return true;
            }

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

                typeInfo = baseTypeInfo;
            }

            return false;
        }
    }
}