// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc.Infrastructure;
using Microsoft.AspNet.Mvc.ViewFeatures;

namespace Microsoft.AspNet.Mvc.ViewComponents
{
    /// <summary>
    /// Default implementation of <see cref="IViewComponentDescriptorProvider"/>.
    /// </summary>
    public class DefaultViewComponentDescriptorProvider : IViewComponentDescriptorProvider
    {
        private const string AsyncMethodName = "InvokeAsync";
        private const string SyncMethodName = "Invoke";
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
                .Select(CreateDescriptor);
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
        /// Determines whether or not the given <see cref="TypeInfo"/> is a view component class.
        /// </summary>
        /// <param name="typeInfo">The <see cref="TypeInfo"/>.</param>
        /// <returns>
        /// <c>true</c> if <paramref name="typeInfo"/>represents a view component class, otherwise <c>false</c>.
        /// </returns>
        protected virtual bool IsViewComponentType(TypeInfo typeInfo)
        {
            if (typeInfo == null)
            {
                throw new ArgumentNullException(nameof(typeInfo));
            }

            return ViewComponentConventions.IsComponent(typeInfo);
        }

        private static ViewComponentDescriptor CreateDescriptor(TypeInfo typeInfo)
        {
            var type = typeInfo.AsType();
            var candidate = new ViewComponentDescriptor
            {
                FullName = ViewComponentConventions.GetComponentFullName(typeInfo),
                ShortName = ViewComponentConventions.GetComponentName(typeInfo),
                Type = type,
                MethodInfo = FindMethod(type)
            };

            return candidate;
        }

        private static MethodInfo FindMethod(Type componentType)
        {
            var componentName = componentType.FullName;
            var methods = componentType.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .Where(method =>
                    string.Equals(method.Name, AsyncMethodName, StringComparison.Ordinal) ||
                    string.Equals(method.Name, SyncMethodName, StringComparison.Ordinal))
                .ToArray();

            if (methods.Length == 0)
            {
                throw new InvalidOperationException(
                    Resources.FormatViewComponent_CannotFindMethod(SyncMethodName, AsyncMethodName, componentName));
            }
            else if (methods.Length > 1)
            {
                throw new InvalidOperationException(
                    Resources.FormatViewComponent_AmbiguousMethods(componentName, AsyncMethodName, SyncMethodName));
            }

            var selectedMethod = methods[0];
            if (string.Equals(selectedMethod.Name, AsyncMethodName, StringComparison.Ordinal))
            {
                if (!selectedMethod.ReturnType.GetTypeInfo().IsGenericType ||
                    selectedMethod.ReturnType.GetGenericTypeDefinition() != typeof(Task<>))
                {
                    throw new InvalidOperationException(Resources.FormatViewComponent_AsyncMethod_ShouldReturnTask(
                        AsyncMethodName,
                        componentName,
                        nameof(Task)));
                }
            }
            else
            {
                if (selectedMethod.ReturnType == typeof(void))
                {
                    throw new InvalidOperationException(Resources.FormatViewComponent_SyncMethod_ShouldReturnValue(
                        SyncMethodName,
                        componentName));
                }
                else if (selectedMethod.ReturnType.IsAssignableFrom(typeof(Task)))
                {
                    throw new InvalidOperationException(Resources.FormatViewComponent_SyncMethod_CannotReturnTask(
                        SyncMethodName,
                        componentName,
                        nameof(Task)));
                }
            }

            return selectedMethod;
        }
    }
}