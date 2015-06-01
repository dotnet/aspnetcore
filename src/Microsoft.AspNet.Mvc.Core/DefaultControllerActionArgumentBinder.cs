// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc.Core;
using Microsoft.AspNet.Mvc.ModelBinding;
using Microsoft.AspNet.Mvc.ModelBinding.Validation;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Mvc
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
            var modelBindingContext = GetModelBindingContext(
                parameter.Name,
                metadata,
                parameter.BindingInfo,
                modelState,
                operationContext);

            var modelBindingResult = await operationContext.ModelBinder.BindModelAsync(modelBindingContext);
            if (modelBindingResult != null &&
                modelBindingResult.IsModelSet &&
                modelBindingResult.ValidationNode != null)
            {
                var modelExplorer = new ModelExplorer(
                    _modelMetadataProvider,
                    metadata,
                    modelBindingResult.Model);
                var validationContext = new ModelValidationContext(
                    modelBindingContext.BindingSource,
                    operationContext.ValidatorProvider,
                    modelState,
                    modelExplorer);

                _validator.Validate(validationContext, modelBindingResult.ValidationNode);
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
                var source = property.Value;
                if (propertyHelper.Property.CanWrite && propertyHelper.Property.SetMethod?.IsPublic == true)
                {
                    // Handle settable property. Do not set the property if the type is a non-nullable type.
                    if (source != null || TypeHelper.AllowsNullValue(propertyType))
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

                // Determine T if this is an ICollection<T> property.
                var collectionTypeArguments = ClosedGenericMatcher.ExtractGenericInterface(
                        propertyType,
                        typeof(ICollection<>))
                    ?.GenericTypeArguments;
                if (collectionTypeArguments == null)
                {
                    // Not a collection model.
                    continue;
                }

                // Handle a read-only collection property.
                var propertyAddRange = CallPropertyAddRangeOpenGenericMethod.MakeGenericMethod(
                    collectionTypeArguments);
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
                if (modelBindingResult != null && modelBindingResult.IsModelSet)
                {
                    arguments[parameter.Name] = modelBindingResult.Model;
                }
            }
        }

        private static ModelBindingContext GetModelBindingContext(
            string parameterName,
            ModelMetadata metadata,
            BindingInfo bindingInfo,
            ModelStateDictionary modelState,
            OperationBindingContext operationBindingContext)
        {
            var modelBindingContext = ModelBindingContext.GetModelBindingContext(
                metadata,
                bindingInfo,
                parameterName);

            modelBindingContext.ModelState = modelState;
            modelBindingContext.ValueProvider = operationBindingContext.ValueProvider;
            modelBindingContext.OperationBindingContext = operationBindingContext;

            return modelBindingContext;
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
