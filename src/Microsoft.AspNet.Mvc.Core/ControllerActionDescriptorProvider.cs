// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.AspNet.Mvc.ApplicationModels;
using Microsoft.AspNet.Mvc.Filters;
using Microsoft.Framework.Internal;
using Microsoft.Framework.OptionsModel;

namespace Microsoft.AspNet.Mvc.Core
{
    public class ControllerActionDescriptorProvider : IActionDescriptorProvider
    {
        private readonly IControllerModelBuilder _applicationModelBuilder;
        private readonly IControllerTypeProvider _controllerTypeProvider;
        private readonly IReadOnlyList<IFilter> _globalFilters;
        private readonly IEnumerable<IApplicationModelConvention> _conventions;

        public ControllerActionDescriptorProvider([NotNull] IControllerTypeProvider controllerTypeProvider,
                                                  [NotNull] IControllerModelBuilder applicationModelBuilder,
                                                  [NotNull] IGlobalFilterProvider globalFilters,
                                                  [NotNull] IOptions<MvcOptions> optionsAccessor)
        {
            _controllerTypeProvider = controllerTypeProvider;
            _applicationModelBuilder = applicationModelBuilder;
            _globalFilters = globalFilters.Filters;
            _conventions = optionsAccessor.Options.Conventions;
        }

        public int Order
        {
            get { return DefaultOrder.DefaultFrameworkSortOrder; }
        }

        /// <inheritdoc />
        public void OnProvidersExecuting ([NotNull] ActionDescriptorProviderContext context)
        {
            foreach (var descriptor in GetDescriptors())
            {
                context.Results.Add(descriptor);
            }
        }

        /// <inheritdoc />
        public void OnProvidersExecuted([NotNull] ActionDescriptorProviderContext context)
        {
        }

        internal protected IEnumerable<ControllerActionDescriptor> GetDescriptors()
        {
            var applicationModel = BuildModel();
            ApplicationModelConventions.ApplyConventions(applicationModel, _conventions);
            return ControllerActionDescriptorBuilder.Build(applicationModel);
        }

        internal protected ApplicationModel BuildModel()
        {
            var applicationModel = new ApplicationModel();
            foreach (var filter in _globalFilters)
            {
                applicationModel.Filters.Add(filter);
            }

            foreach (var type in _controllerTypeProvider.ControllerTypes)
            {
                var controllerModel = _applicationModelBuilder.BuildControllerModel(type);
                if (controllerModel != null)
                {
                    controllerModel.Application = applicationModel;
                    applicationModel.Controllers.Add(controllerModel);
                }
            }

            return applicationModel;
        }
    }
}