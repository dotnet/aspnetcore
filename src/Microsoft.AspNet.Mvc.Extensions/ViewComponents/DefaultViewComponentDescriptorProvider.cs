// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Mvc.ViewComponents
{
    /// <summary>
    /// Default implementation of <see cref="IViewComponentDescriptorProvider"/>.
    /// </summary>
    public class DefaultViewComponentDescriptorProvider : IViewComponentDescriptorProvider
    {
        private readonly IAssemblyProvider _assemblyProvider;

        /// <summary>
        /// Creates a new <see cref="DefaultViewComponentDescriptorProvider"/>.
        /// </summary>
        /// <param name="assemblyProvider">The <see cref="IAssemblyProvider"/>.</param>
        public DefaultViewComponentDescriptorProvider(IAssemblyProvider assemblyProvider)
        {
            _assemblyProvider = assemblyProvider;
        }

        /// <inheritdoc />
        public virtual IEnumerable<ViewComponentDescriptor> GetViewComponents()
        {
            var types = GetCandidateTypes();

            return types
                .Where(IsViewComponentType)
                .Select(CreateCandidate);
        }

        /// <summary>
        /// Gets the candidate <see cref="TypeInfo"/> instances. The results of this will be provided to
        /// <see cref="IsViewComponentType"/> for filtering.
        /// </summary>
        /// <returns>A list of <see cref="TypeInfo"/> instances.</returns>
        protected virtual IEnumerable<TypeInfo> GetCandidateTypes()
        {
            var assemblies = _assemblyProvider.CandidateAssemblies;
            return assemblies.SelectMany(a => a.ExportedTypes).Select(t => t.GetTypeInfo());
        }

        /// <summary>
        /// Determines whether or not the given <see cref="TypeInfo"/> is a View Component class.
        /// </summary>
        /// <param name="typeInfo">The <see cref="TypeInfo"/>.</param>
        /// <returns>
        /// <c>true</c> if <paramref name="typeInfo"/>represents a View Component class, otherwise <c>false</c>.
        /// </returns>
        protected virtual bool IsViewComponentType([NotNull] TypeInfo typeInfo)
        {
            return ViewComponentConventions.IsComponent(typeInfo);
        }

        private static ViewComponentDescriptor CreateCandidate(TypeInfo typeInfo)
        {
            var candidate = new ViewComponentDescriptor()
            {
                FullName = ViewComponentConventions.GetComponentFullName(typeInfo),
                ShortName = ViewComponentConventions.GetComponentName(typeInfo),
                Type = typeInfo.AsType(),
            };

            return candidate;
        }
    }
}