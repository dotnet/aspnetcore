// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNet.Mvc.ApplicationModels;
using Microsoft.AspNet.Mvc.Filters;
using Microsoft.AspNet.Mvc.Logging;
using Microsoft.Framework.Internal;
using Microsoft.Framework.Logging;
using Microsoft.Framework.OptionsModel;

namespace Microsoft.AspNet.Mvc
{
    public class ControllerActionDescriptorProvider : IActionDescriptorProvider
    {
        private readonly IControllerModelBuilder _applicationModelBuilder;
        private readonly IControllerTypeProvider _controllerTypeProvider;
        private readonly IReadOnlyList<IFilter> _globalFilters;
        private readonly IEnumerable<IApplicationModelConvention> _conventions;
        private readonly ILogger _logger;

        public ControllerActionDescriptorProvider([NotNull] IControllerTypeProvider controllerTypeProvider,
                                                  [NotNull] IControllerModelBuilder applicationModelBuilder,
                                                  [NotNull] IGlobalFilterProvider globalFilters,
                                                  [NotNull] IOptions<MvcOptions> optionsAccessor,
                                                  [NotNull] ILoggerFactory loggerFactory)
        {
            _controllerTypeProvider = controllerTypeProvider;
            _applicationModelBuilder = applicationModelBuilder;
            _globalFilters = globalFilters.Filters;
            _conventions = optionsAccessor.Options.Conventions;
            _logger = loggerFactory.Create<ControllerActionDescriptorProvider>();
        }

        public int Order
        {
            get { return DefaultOrder.DefaultFrameworkSortOrder; }
        }

        public void Invoke(ActionDescriptorProviderContext context, Action callNext)
        {
            foreach (var descriptor in GetDescriptors())
            {
                context.Results.Add(descriptor);
            }

            callNext();
        }

        public IEnumerable<ControllerActionDescriptor> GetDescriptors()
        {
            var applicationModel = BuildModel();
            ApplicationModelConventions.ApplyConventions(applicationModel, _conventions);
            if (_logger.IsEnabled(LogLevel.Verbose))
            {
                foreach (var controller in applicationModel.Controllers)
                {
                    _logger.WriteVerbose(new ControllerModelValues(controller));
                }
            }
            return ControllerActionDescriptorBuilder.Build(applicationModel);
        }

        public ApplicationModel BuildModel()
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