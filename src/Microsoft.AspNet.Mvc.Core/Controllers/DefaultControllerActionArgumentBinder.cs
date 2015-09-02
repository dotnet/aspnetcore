// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc.Abstractions;
using Microsoft.AspNet.Mvc.Core;
using Microsoft.AspNet.Mvc.ModelBinding;
using Microsoft.AspNet.Mvc.ModelBinding.Validation;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Mvc.Controllers
{
    /// <summary>
    /// Provides a default implementation of <see cref="IControllerActionArgumentBinder"/>.
    /// Uses ModelBinding to populate action parameters.
    /// </summary>
    public class DefaultControllerActionArgumentBinder : IControllerActionArgumentBinder
    {
        private static readonly MethodInfo CallPropertyAddRangeOpenGenericMethod =
            typeof(DefaultControllerActionArgumentBinder).GetTypeInfo().GetDeclaredMethod(
                nameof(CallPropertyAddRange));

        private readonly IModelMetadataProvider _modelMetadataProvider;
        private readonly IObjectModelValidator _validator;

        public DefaultControllerActionArgumentBinder(
            IModelMetadataProvider modelMetadataProvider,
            IObjectModelValidator validator)
        {
            _modelMetadataProvider = modelMetadataProvider;
            _validator = validator;
        }

        public async Task<IDictionary<string, object>> BindActionArgumentsAsync(
            ActionContext actionContext,
            ActionBindingContext actionBindingContext,
            object controller)
        {
            var actionDescriptor = actionContext.ActionDescriptor as ControllerActionDescriptor;
            if (actionDescriptor == null)
            {
                throw new ArgumentException(
                    Resources.FormatActionDescriptorMustBeBasedOnControllerAction(
                        typeof(ControllerActionDescriptor)),
                        nameof(actionContext));
            }

            var operationBindingContext = GetOperationBindingContext(actionContext, actionBindingContext);
            var controllerProperties = new Dictionary<string, object>(StringComparer.Ordinal);
            await PopulateArgumentsAsync(
                operationBindingContext,
                actionContext.ModelState,
                controllerProperties,
                actionDescriptor.BoundProperties);
            var controllerType = actionDescriptor.ControllerTypeInfo.AsType();
            ActivateProperties(controller, controllerType, controllerProperties);

            var actionArguments = new Dictionary<string, object>(StringComparer.Ordinal);
            await PopulateArgumentsAsync(
                operationBindingContext,
                actionContext.ModelState,
                actionArguments,
                actionDescriptor.Parameters);
            return actionArguments;
        }

        public async Task<ModelBindingResult> BindModelAsync(
            [NotNull] ParameterDescriptor parameter,
            [NotNull] ModelStateDictionary modelState,
            [NotNull] OperationBindingContext operationContext)
        {
            var metadata = _modelMetadataProvider.GetMetadataForType(parameter.ParameterType);
            var modelBindingContext = ModelBindingContext.CreateBindingContext(
                operationContext,
                modelState,
                metadata,
                parameter.BindingInfo,
                parameter.Name);

            var modelBindingResult = await operationContext.ModelBinder.BindModelAsync(modelBindingContext);
            if (modelBindingResult.IsModelSet)
            {
                _validator.Validate(
                    operationContext.ValidatorProvider,
                    modelState,
                    modelBindingContext.ValidationState,
                    modelBindingResult.Key,
                    modelBindingResult.Model);
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

        private void ActivateProperties(object controller, Type containerType, Dictionary<string, object> properties)
        {
            var propertyHelpers = PropertyHelper.GetProperties(controller);
            foreach (var property in properties)
            {
                var propertyHelper = propertyHelpers.First(helper =>
                    string.Equals(helper.Name, property.Key, StringComparison.Ordinal));
                var propertyType = propertyHelper.Property.PropertyType;
                var metadata = _modelMetadataProvider.GetMetadataForType(propertyType);
                var source = property.Value;
                if (propertyHelper.Property.CanWrite && propertyHelper.Property.SetMethod?.IsPublic == true)
                {
                    // Handle settable property. Do not set the property to null if the type is a non-nullable type.
                    if (source != null || metadata.IsReferenceOrNullableType)
                    {
                        propertyHelper.SetValue(controller, source);
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
                if (source == null || target == null)
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
                propertyAddRange.Invoke(obj: null, parameters: new[] { target, source });
            }
        }

        private async Task PopulateArgumentsAsync(
            OperationBindingContext operationContext,
            ModelStateDictionary modelState,
            IDictionary<string, object> arguments,
            IEnumerable<ParameterDescriptor> parameterMetadata)
        {
            foreach (var parameter in parameterMetadata)
            {
                var modelBindingResult = await BindModelAsync(parameter, modelState, operationContext);
                if (modelBindingResult.IsModelSet)
                {
                    arguments[parameter.Name] = modelBindingResult.Model;
                }
            }
        }

        private OperationBindingContext GetOperationBindingContext(
            ActionContext actionContext,
            ActionBindingContext bindingContext)
        {
            return new OperationBindingContext
            {
                InputFormatters = bindingContext.InputFormatters,
                ModelBinder = bindingContext.ModelBinder,
                ValidatorProvider = bindingContext.ValidatorProvider,
                MetadataProvider = _modelMetadataProvider,
                HttpContext = actionContext.HttpContext,
                ValueProvider = bindingContext.ValueProvider,
            };
        }
    }
}
