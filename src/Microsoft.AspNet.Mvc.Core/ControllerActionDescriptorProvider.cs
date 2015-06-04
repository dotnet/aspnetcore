// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNet.Mvc.ApplicationModels;
using Microsoft.Framework.Internal;
using Microsoft.Framework.OptionsModel;

namespace Microsoft.AspNet.Mvc.Core
{
    public class ControllerActionDescriptorProvider : IActionDescriptorProvider
    {
        private readonly IApplicationModelProvider[] _applicationModelProviders;
        private readonly IControllerTypeProvider _controllerTypeProvider;
        private readonly IEnumerable<IApplicationModelConvention> _conventions;

        public ControllerActionDescriptorProvider(
            [NotNull] IControllerTypeProvider controllerTypeProvider,
            [NotNull] IEnumerable<IApplicationModelProvider> applicationModelProviders,
            [NotNull] IOptions<MvcOptions> optionsAccessor)
        {
            _controllerTypeProvider = controllerTypeProvider;
            _applicationModelProviders = applicationModelProviders.OrderBy(p => p.Order).ToArray();
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
            var controllerTypes = _controllerTypeProvider.ControllerTypes;
            var context = new ApplicationModelProviderContext(controllerTypes);

            for (var i = 0; i < _applicationModelProviders.Length; i++)
            {
                _applicationModelProviders[i].OnProvidersExecuting(context);
            }

            for (var i = _applicationModelProviders.Length - 1 ; i >= 0; i--)
            {
                _applicationModelProviders[i].OnProvidersExecuted(context);
            }

            return context.Result;
        }
    }
}
