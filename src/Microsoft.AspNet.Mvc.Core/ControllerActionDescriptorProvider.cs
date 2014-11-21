// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNet.Mvc.ApplicationModels;
using Microsoft.AspNet.Mvc.Filters;
using Microsoft.Framework.OptionsModel;

namespace Microsoft.AspNet.Mvc
{
    public class ControllerActionDescriptorProvider : IActionDescriptorProvider
    {
        private readonly IControllerModelBuilder _applicationModelBuilder;
        private readonly IAssemblyProvider _assemblyProvider;
        private readonly IReadOnlyList<IFilter> _globalFilters;
        private readonly IEnumerable<IApplicationModelConvention> _modelConventions;

        public ControllerActionDescriptorProvider(IAssemblyProvider assemblyProvider,
                                                 IControllerModelBuilder applicationModelBuilder,
                                                 IGlobalFilterProvider globalFilters,
                                                 IOptions<MvcOptions> optionsAccessor)
        {
            _assemblyProvider = assemblyProvider;
            _applicationModelBuilder = applicationModelBuilder;
            _globalFilters = globalFilters.Filters;
            _modelConventions = optionsAccessor.Options.ApplicationModelConventions;
        }

        public int Order
        {
            get { return DefaultOrder.DefaultFrameworkSortOrder; }
        }

        public void Invoke(ActionDescriptorProviderContext context, Action callNext)
        {
            context.Results.AddRange(GetDescriptors());
            callNext();
        }

        public IEnumerable<ControllerActionDescriptor> GetDescriptors()
        {
            var applicationModel = BuildModel();
            ApplicationModelConventions.ApplyConventions(applicationModel, _modelConventions);
            return ControllerActionDescriptorBuilder.Build(applicationModel);
        }

        public ApplicationModel BuildModel()
        {
            var applicationModel = new ApplicationModel();
            applicationModel.Filters.AddRange(_globalFilters);

            var assemblies = _assemblyProvider.CandidateAssemblies;
            var types = assemblies.SelectMany(a => a.DefinedTypes);

            foreach (var type in types)
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