// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Razor.Compilation;
using Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure
{
    internal class DefaultPageLoader : IPageLoader
    {
        private readonly IPageApplicationModelProvider[] _applicationModelProviders;
        private readonly IViewCompilerProvider _viewCompilerProvider;
        private readonly PageConventionCollection _conventions;
        private readonly FilterCollection _globalFilters;

        public DefaultPageLoader(
            IEnumerable<IPageApplicationModelProvider> applicationModelProviders,
            IViewCompilerProvider viewCompilerProvider,
            IOptions<RazorPagesOptions> pageOptions,
            IOptions<MvcOptions> mvcOptions)
        {
            _applicationModelProviders = applicationModelProviders
                .OrderBy(p => p.Order)
                .ToArray();
            _viewCompilerProvider = viewCompilerProvider;
            _conventions = pageOptions.Value.Conventions;
            _globalFilters = mvcOptions.Value.Filters;
        }

        private IViewCompiler Compiler => _viewCompilerProvider.GetCompiler();

        public CompiledPageActionDescriptor Load(PageActionDescriptor actionDescriptor)
        {
            if (actionDescriptor == null)
            {
                throw new ArgumentNullException(nameof(actionDescriptor));
            }

            var compileTask = Compiler.CompileAsync(actionDescriptor.RelativePath);
            var viewDescriptor = compileTask.GetAwaiter().GetResult();

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

            return CompiledPageActionDescriptorBuilder.Build(context.PageApplicationModel, _globalFilters);
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