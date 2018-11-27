// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Mvc.Internal
{
    public class ControllerActionDescriptorProvider : IActionDescriptorProvider
    {
        private readonly ApplicationPartManager _partManager;
        private readonly IApplicationModelProvider[] _applicationModelProviders;
        private readonly IEnumerable<IApplicationModelConvention> _conventions;

        public ControllerActionDescriptorProvider(
            ApplicationPartManager partManager,
            IEnumerable<IApplicationModelProvider> applicationModelProviders,
            IOptions<MvcOptions> optionsAccessor)
        {
            if (partManager == null)
            {
                throw new ArgumentNullException(nameof(partManager));
            }

            if (applicationModelProviders == null)
            {
                throw new ArgumentNullException(nameof(applicationModelProviders));
            }

            if (optionsAccessor == null)
            {
                throw new ArgumentNullException(nameof(optionsAccessor));
            }

            _partManager = partManager;
            _applicationModelProviders = applicationModelProviders.OrderBy(p => p.Order).ToArray();
            _conventions = optionsAccessor.Value.Conventions;
        }

        public int Order => -1000;

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
            // After all of the providers have run, we need to provide a 'null' for each all of route values that 
            // participate in action selection.
            //
            // This is important for scenarios like Razor Pages, that use the 'page' route value. An action that
            // uses 'page' shouldn't match when 'action' is set, and an action that uses 'action' shouldn't match when
            // 'page is specified.
            //
            // Or for another example, consider areas. A controller that's not in an area needs a 'null' value for
            // area so it can't match when the route produces an 'area' value.
            var keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < context.Results.Count; i++)
            {
                var action = context.Results[i];
                foreach (var key in action.RouteValues.Keys)
                {
                    keys.Add(key);
                }
            }

            for (var i = 0; i < context.Results.Count; i++)
            {
                var action = context.Results[i];
                foreach (var key in keys)
                {
                    if (!action.RouteValues.ContainsKey(key))
                    {
                        action.RouteValues.Add(key, null);
                    }
                }
            }
        }

        protected internal IEnumerable<ControllerActionDescriptor> GetDescriptors()
        {
            var applicationModel = BuildModel();
            ApplicationModelConventions.ApplyConventions(applicationModel, _conventions);
            return ControllerActionDescriptorBuilder.Build(applicationModel);
        }

        protected internal ApplicationModel BuildModel()
        {
            var controllerTypes = GetControllerTypes();
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

        private IEnumerable<TypeInfo> GetControllerTypes()
        {
            var feature = new ControllerFeature();
            _partManager.PopulateFeature(feature);

            return feature.Controllers;
        }
    }
}
