// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Mvc.Internal
{
    public class ControllerActionDescriptorProvider : IActionDescriptorProvider
    {
        private readonly IApplicationModelProvider[] _applicationModelProviders;
        private readonly IControllerTypeProvider _controllerTypeProvider;
        private readonly IEnumerable<IApplicationModelConvention> _conventions;

        public ControllerActionDescriptorProvider(
            IControllerTypeProvider controllerTypeProvider,
            IEnumerable<IApplicationModelProvider> applicationModelProviders,
            IOptions<MvcOptions> optionsAccessor)
        {
            if (controllerTypeProvider == null)
            {
                throw new ArgumentNullException(nameof(controllerTypeProvider));
            }

            if (applicationModelProviders == null)
            {
                throw new ArgumentNullException(nameof(applicationModelProviders));
            }

            if (optionsAccessor == null)
            {
                throw new ArgumentNullException(nameof(optionsAccessor));
            }

            _controllerTypeProvider = controllerTypeProvider;
            _applicationModelProviders = applicationModelProviders.OrderBy(p => p.Order).ToArray();
            _conventions = optionsAccessor.Value.Conventions;
        }

        public int Order
        {
            get { return -1000; }
        }

        /// <inheritdoc />
        public void OnProvidersExecuting(ActionDescriptorProviderContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            foreach (var descriptor in GetDescriptors())
            {
                context.Results.Add(descriptor);
            }
        }

        /// <inheritdoc />
        public void OnProvidersExecuted(ActionDescriptorProviderContext context)
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

            for (var i = _applicationModelProviders.Length - 1; i >= 0; i--)
            {
                _applicationModelProviders[i].OnProvidersExecuted(context);
            }

            return context.Result;
        }
    }
}
