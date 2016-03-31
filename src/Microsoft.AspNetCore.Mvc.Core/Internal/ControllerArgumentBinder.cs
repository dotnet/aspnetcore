// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Core;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.Extensions.Internal;

namespace Microsoft.AspNetCore.Mvc.Internal
{
    /// <summary>
    /// Provides a default implementation of <see cref="IControllerActionArgumentBinder"/>.
    /// Uses ModelBinding to populate action parameters.
    /// </summary>
    public class ControllerArgumentBinder : IControllerActionArgumentBinder
    {
        private static readonly MethodInfo CallPropertyAddRangeOpenGenericMethod =
            typeof(ControllerArgumentBinder).GetTypeInfo().GetDeclaredMethod(
                nameof(CallPropertyAddRange));

        private readonly IModelBinderFactory _modelBinderFactory;
        private readonly IModelMetadataProvider _modelMetadataProvider;
        private readonly IObjectModelValidator _validator;

        public ControllerArgumentBinder(
            IModelMetadataProvider modelMetadataProvider,
            IModelBinderFactory modelBinderFactory,
            IObjectModelValidator validator)
        {
            _modelMetadataProvider = modelMetadataProvider;
            _modelBinderFactory = modelBinderFactory;
            _validator = validator;
        }

        public Task<IDictionary<string, object>> BindActionArgumentsAsync(
            ControllerContext controllerContext,
            object controller)
        {
            if (controllerContext == null)
            {
                throw new ArgumentNullException(nameof(controllerContext));
            }

            if (controller == null)
            {
                throw new ArgumentNullException(nameof(controller));
            }

            if (controllerContext.ActionDescriptor == null)
            {
                throw new ArgumentException(Resources.FormatPropertyOfTypeCannotBeNull(
                    nameof(ControllerContext.ActionDescriptor),
                    nameof(ControllerContext)));
            }

            // Perf: Avoid allocating async state machines where possible. We only need the state
            // machine if you need to bind properties.
            var actionDescriptor = controllerContext.ActionDescriptor;
            if (actionDescriptor.BoundProperties.Count == 0 &&
                actionDescriptor.Parameters.Count == 0)
            {
                return Task.FromResult<IDictionary<string, object>>(
                    new Dictionary<string, object>(StringComparer.Ordinal));
            }
            else if (actionDescriptor.BoundProperties.Count == 0)
            {
                return PopulateArgumentsAsync(controllerContext, actionDescriptor.Parameters);
            }
            else
            {
                return BindActionArgumentsAndPropertiesCoreAsync(
                    controllerContext,
                    controller,
                    actionDescriptor);
            }
        }

        private async Task<IDictionary<string, object>> BindActionArgumentsAndPropertiesCoreAsync(
            ControllerContext controllerContext,
            object controller,
            ControllerActionDescriptor actionDescriptor)
        {
            var controllerProperties = await PopulateArgumentsAsync(
                controllerContext,
                actionDescriptor.BoundProperties);
            ActivateProperties(actionDescriptor, controller, controllerProperties);

            var actionArguments = await PopulateArgumentsAsync(
                controllerContext,
                actionDescriptor.Parameters);
            return actionArguments;
        }

        public async Task<ModelBindingResult?> BindModelAsync(
            ParameterDescriptor parameter,
            ControllerContext controllerContext)
        {
            if (parameter == null)
            {
                throw new ArgumentNullException(nameof(parameter));
            }

            if (controllerContext == null)
            {
                throw new ArgumentNullException(nameof(controllerContext));
            }

            var metadata = _modelMetadataProvider.GetMetadataForType(parameter.ParameterType);
            var modelBindingContext = DefaultModelBindingContext.CreateBindingContext(
                controllerContext,
                new CompositeValueProvider(controllerContext.ValueProviders),
                metadata,
                parameter.BindingInfo,
                parameter.Name);

            if (parameter.BindingInfo?.BinderModelName != null)
            {
                // The name was set explicitly, always use that as the prefix.
                modelBindingContext.ModelName = parameter.BindingInfo.BinderModelName;
            }
            else if (modelBindingContext.ValueProvider.ContainsPrefix(parameter.Name))
            {
                // We have a match for the parameter name, use that as that prefix.
                modelBindingContext.ModelName = parameter.Name;
            }
            else
            {
                // No match, fallback to empty string as the prefix.
                modelBindingContext.ModelName = string.Empty;
            }

            var binder = _modelBinderFactory.CreateBinder(new ModelBinderFactoryContext()
            {
                BindingInfo = parameter.BindingInfo,
                Metadata = metadata,
                CacheToken = parameter,
            });

            await binder.BindModelAsync(modelBindingContext);

            var modelBindingResult = modelBindingContext.Result;
            if (modelBindingResult != null && modelBindingResult.Value.IsModelSet)
            {
                _validator.Validate(
                    controllerContext,
                    modelBindingContext.ValidationState,
                    modelBindingResult.Value.Key,
                    modelBindingResult.Value.Model);
            }

            return modelBindingResult;
        }

        // Called via reflection.
        private static void CallPropertyAddRange<TElement>(object target, object source)
        {
            var targetCollection = (ICollection<TElement>)target;
            var sourceCollection = source as IEnumerable<TElement>;
            if (sourceCollection != null && !targetCollection.IsReadOnly)
            {
                targetCollection.Clear();
                foreach (var item in sourceCollection)
                {
                    targetCollection.Add(item);
                }
            }
        }

        private void ActivateProperties(
            ControllerActionDescriptor actionDescriptor,
            object controller,
            IDictionary<string, object> properties)
        {
            var propertyHelpers = PropertyHelper.GetProperties(controller);
            for (var i = 0; i < actionDescriptor.BoundProperties.Count; i++)
            {
                var property = actionDescriptor.BoundProperties[i];

                PropertyHelper propertyHelper = null;
                for (var j = 0; j < propertyHelpers.Length; j++)
                {
                    if (string.Equals(propertyHelpers[j].Name, property.Name, StringComparison.Ordinal))
                    {
                        propertyHelper = propertyHelpers[j];
                        break;
                    }
                }

                object value;
                if (propertyHelper == null || !properties.TryGetValue(property.Name, out value))
                {
                    continue;
                }

                var propertyType = propertyHelper.Property.PropertyType;
                var metadata = _modelMetadataProvider.GetMetadataForType(propertyType);

                if (propertyHelper.Property.CanWrite && propertyHelper.Property.SetMethod?.IsPublic == true)
                {
                    // Handle settable property. Do not set the property to null if the type is a non-nullable type.
                    if (value != null || metadata.IsReferenceOrNullableType)
                    {
                        propertyHelper.SetValue(controller, value);
                    }

                    continue;
                }

                if (propertyType.IsArray)
                {
                    // Do not attempt to copy values into an array because an array's length is immutable. This choice
                    // is also consistent with MutableObjectModelBinder's handling of a read-only array property.
                    continue;
                }

                var target = propertyHelper.GetValue(controller);
                if (value == null || target == null)
                {
                    // Nothing to do when source or target is null.
                    continue;
                }

                if (!metadata.IsCollectionType)
                {
                    // Not a collection model.
                    continue;
                }

                // Handle a read-only collection property.
                var propertyAddRange = CallPropertyAddRangeOpenGenericMethod.MakeGenericMethod(
                    metadata.ElementMetadata.ModelType);
                propertyAddRange.Invoke(obj: null, parameters: new[] { target, value });
            }
        }

        private async Task<IDictionary<string, object>> PopulateArgumentsAsync(
            ControllerContext controllerContext,
            IList<ParameterDescriptor> parameters)
        {
            var arguments = new Dictionary<string, object>(StringComparer.Ordinal);

            // Perf: Avoid allocations
            for (var i = 0; i < parameters.Count; i++)
            {
                var parameter = parameters[i];
                var modelBindingResult = await BindModelAsync(parameter, controllerContext);
                if (modelBindingResult != null && modelBindingResult.Value.IsModelSet)
                {
                    arguments[parameter.Name] = modelBindingResult.Value.Model;
                }
            }

            return arguments;
        }
    }
}
