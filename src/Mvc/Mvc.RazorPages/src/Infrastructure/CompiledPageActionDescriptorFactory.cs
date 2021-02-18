// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#nullable enable

using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Razor.Compilation;

namespace Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure
{
    /// <summary>
    /// A factory for producing <see cref="CompiledPageActionDescriptor"/> given a <see cref="PageActionDescriptor"/>.
    /// </summary>
    internal sealed class CompiledPageActionDescriptorFactory
    {
        private readonly IPageApplicationModelProvider[] _applicationModelProviders;
        private readonly PageConventionCollection _conventions;
        private readonly FilterCollection _globalFilters;

        public CompiledPageActionDescriptorFactory(
            IEnumerable<IPageApplicationModelProvider> applicationModelProviders,
            MvcOptions mvcOptions,
            RazorPagesOptions pageOptions)
        {
            _applicationModelProviders = applicationModelProviders.OrderBy(a => a.Order).ToArray();
            _conventions = pageOptions.Conventions;
            _globalFilters = mvcOptions.Filters;
        }

        public CompiledPageActionDescriptor CreateCompiledDescriptor(
            PageActionDescriptor actionDescriptor,
            CompiledViewDescriptor viewDescriptor)
        {
            var context = new PageApplicationModelProviderContext(actionDescriptor, viewDescriptor.Type.GetTypeInfo());
            for (var i = 0; i < _applicationModelProviders.Length; i++)
            {
                _applicationModelProviders[i].OnProvidersExecuting(context);
            }

            for (var i = _applicationModelProviders.Length - 1; i >= 0; i--)
            {
                _applicationModelProviders[i].OnProvidersExecuted(context);
            }

            ApplyConventions(_conventions, context.PageApplicationModel);

            var compiled = CompiledPageActionDescriptorBuilder.Build(context.PageApplicationModel, _globalFilters);
            actionDescriptor.CompiledPageDescriptor = compiled;

            return compiled;
        }

        internal static void ApplyConventions(
            PageConventionCollection conventions,
            PageApplicationModel pageApplicationModel)
        {
            var applicationModelConventions = GetConventions<IPageApplicationModelConvention>(pageApplicationModel.HandlerTypeAttributes);
            foreach (var convention in applicationModelConventions)
            {
                convention.Apply(pageApplicationModel);
            }

            var handlers = pageApplicationModel.HandlerMethods.ToArray();
            foreach (var handlerModel in handlers)
            {
                var handlerModelConventions = GetConventions<IPageHandlerModelConvention>(handlerModel.Attributes);
                foreach (var convention in handlerModelConventions)
                {
                    convention.Apply(handlerModel);
                }

                var parameterModels = handlerModel.Parameters.ToArray();
                foreach (var parameterModel in parameterModels)
                {
                    var parameterModelConventions = GetConventions<IParameterModelBaseConvention>(parameterModel.Attributes);
                    foreach (var convention in parameterModelConventions)
                    {
                        convention.Apply(parameterModel);
                    }
                }
            }

            var properties = pageApplicationModel.HandlerProperties.ToArray();
            foreach (var propertyModel in properties)
            {
                var propertyModelConventions = GetConventions<IParameterModelBaseConvention>(propertyModel.Attributes);
                foreach (var convention in propertyModelConventions)
                {
                    convention.Apply(propertyModel);
                }
            }

            IEnumerable<TConvention> GetConventions<TConvention>(
                IReadOnlyList<object> attributes)
            {
                return Enumerable.Concat(
                    conventions.OfType<TConvention>(),
                    attributes.OfType<TConvention>());
            }
        }
    }
}
