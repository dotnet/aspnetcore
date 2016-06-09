// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Core;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.Extensions.Internal;

namespace Microsoft.AspNetCore.Mvc.Internal
{
    /// <summary>
    /// Provides a default implementation of <see cref="IControllerArgumentBinder"/>.
    /// Uses ModelBinding to populate action parameters.
    /// </summary>
    public class DefaultControllerArgumentBinder : IControllerArgumentBinder
    {
        private static readonly MethodInfo CallPropertyAddRangeOpenGenericMethod =
            typeof(DefaultControllerArgumentBinder).GetTypeInfo().GetDeclaredMethod(
                nameof(CallPropertyAddRange));

        private readonly IModelBinderFactory _modelBinderFactory;
        private readonly IModelMetadataProvider _modelMetadataProvider;
        private readonly IObjectModelValidator _validator;

        public DefaultControllerArgumentBinder(
            IModelMetadataProvider modelMetadataProvider,
            IModelBinderFactory modelBinderFactory,
            IObjectModelValidator validator)
        {
            _modelMetadataProvider = modelMetadataProvider;
            _modelBinderFactory = modelBinderFactory;
            _validator = validator;
        }

        public Task BindArgumentsAsync(
            ControllerContext controllerContext,
            object controller,
            IDictionary<string, object> arguments)
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
                return TaskCache.CompletedTask;
            }

            return BindArgumentsCoreAsync(controllerContext, controller, arguments);
        }

        private async Task BindArgumentsCoreAsync(
            ControllerContext controllerContext,
            object controller,
            IDictionary<string, object> arguments)
        {
            var valueProvider = await CompositeValueProvider.CreateAsync(controllerContext);

            var parameters = controllerContext.ActionDescriptor.Parameters;
            for (var i = 0; i < parameters.Count; i++)
            {
                var parameter = parameters[i];

                var result = await BindModelAsync(parameter, controllerContext, valueProvider);
                if (result.IsModelSet)
                {
                    arguments[parameter.Name] = result.Model;
                }
            }

            var properties = controllerContext.ActionDescriptor.BoundProperties;
            if (properties.Count == 0)
            {
                // Perf: Early exit to avoid PropertyHelper lookup in the (common) case where we have no
                // bound properties.
                return;
            }

            var propertyHelpers = PropertyHelper.GetProperties(controller);
            for (var i = 0; i < properties.Count; i++)
            {
                var property = properties[i];

                var result = await BindModelAsync(property, controllerContext, valueProvider);
                if (result.IsModelSet)
                {
                    var propertyHelper = FindPropertyHelper(propertyHelpers, property);
                    if (propertyHelper != null)
                    {
                        ActivateProperty(property, propertyHelper, controller, result.Model);
                    }
                }
            }
        }

        public async Task<ModelBindingResult> BindModelAsync(
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

            var valueProvider = await CompositeValueProvider.CreateAsync(controllerContext);

            return await BindModelAsync(parameter, controllerContext, valueProvider);
        }

        public async Task<ModelBindingResult> BindModelAsync(
            ParameterDescriptor parameter,
            ControllerContext controllerContext,
            IValueProvider valueProvider)
        {
            if (parameter == null)
            {
                throw new ArgumentNullException(nameof(parameter));
            }

            if (controllerContext == null)
            {
                throw new ArgumentNullException(nameof(controllerContext));
            }

            if (valueProvider == null)
            {
                throw new ArgumentNullException(nameof(valueProvider));
            }

            var metadata = _modelMetadataProvider.GetMetadataForType(parameter.ParameterType);
            var binder = _modelBinderFactory.CreateBinder(new ModelBinderFactoryContext()
            {
                BindingInfo = parameter.BindingInfo,
                Metadata = metadata,
                CacheToken = parameter,
            });

            var modelBindingContext = DefaultModelBindingContext.CreateBindingContext(
                controllerContext,
                valueProvider,
                metadata,
                parameter.BindingInfo,
                parameter.Name);

            var parameterModelName = parameter.BindingInfo?.BinderModelName ?? metadata.BinderModelName;
            if (parameterModelName != null)
            {
                // The name was set explicitly, always use that as the prefix.
                modelBindingContext.ModelName = parameterModelName;
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

            await binder.BindModelAsync(modelBindingContext);

            var modelBindingResult = modelBindingContext.Result;
            if (modelBindingResult.IsModelSet)
            {
                _validator.Validate(
                    controllerContext,
                    modelBindingContext.ValidationState,
                    modelBindingContext.ModelName,
                    modelBindingResult.Model);
            }

            return modelBindingResult;
        }

        private void ActivateProperty(
            ParameterDescriptor property,
            PropertyHelper propertyHelper,
            object controller,
            object value)
        {
            var propertyType = propertyHelper.Property.PropertyType;
            var metadata = _modelMetadataProvider.GetMetadataForType(propertyType);

            if (propertyHelper.Property.CanWrite && propertyHelper.Property.SetMethod?.IsPublic == true)
            {
                // Handle settable property. Do not set the property to null if the type is a non-nullable type.
                if (value != null || metadata.IsReferenceOrNullableType)
                {
                    propertyHelper.SetValue(controller, value);
                }

                return;
            }

            if (propertyType.IsArray)
            {
                // Do not attempt to copy values into an array because an array's length is immutable. This choice
                // is also consistent with MutableObjectModelBinder's handling of a read-only array property.
                return;
            }

            var target = propertyHelper.GetValue(controller);
            if (value == null || target == null)
            {
                // Nothing to do when source or target is null.
                return;
            }

            if (!metadata.IsCollectionType)
            {
                // Not a collection model.
                return;
            }

            // Handle a read-only collection property.
            var propertyAddRange = CallPropertyAddRangeOpenGenericMethod.MakeGenericMethod(
                metadata.ElementMetadata.ModelType);
            propertyAddRange.Invoke(obj: null, parameters: new[] { target, value });
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

        private static PropertyHelper FindPropertyHelper(PropertyHelper[] propertyHelpers, ParameterDescriptor property)
        {
            for (var i = 0; i < propertyHelpers.Length; i++)
            {
                var propertyHelper = propertyHelpers[i];
                if (string.Equals(propertyHelper.Name, property.Name, StringComparison.Ordinal))
                {
                    return propertyHelper;
                }
            }

            return null;
        }
    }
}
