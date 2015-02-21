// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    /// <summary>
    /// Represents an <see cref="IModelBinder"/> that delegates to one of a collection of <see cref="IModelBinder"/>
    /// instances.
    /// </summary>
    /// <remarks>
    /// If no binder is available and the <see cref="ModelBindingContext"/> allows it,
    /// this class tries to find a binder using an empty prefix.
    /// </remarks>
    public class CompositeModelBinder : ICompositeModelBinder
    {
        /// <summary>
        /// Initializes a new instance of the CompositeModelBinder class.
        /// </summary>
        /// <param name="modelBinders">A collection of <see cref="IModelBinder"/> instances.</param>
        public CompositeModelBinder([NotNull] IEnumerable<IModelBinder> modelBinders)
        {
            ModelBinders = new List<IModelBinder>(modelBinders);
        }

        /// <inheritdoc />
        public IReadOnlyList<IModelBinder> ModelBinders { get; }

        public virtual async Task<ModelBindingResult> BindModelAsync([NotNull] ModelBindingContext bindingContext)
        {
            var newBindingContext = CreateNewBindingContext(bindingContext,
                                                            bindingContext.ModelName);

            var modelBindingResult = await TryBind(newBindingContext);
            if (modelBindingResult == null && !string.IsNullOrEmpty(bindingContext.ModelName)
                && bindingContext.FallbackToEmptyPrefix)
            {
                // fallback to empty prefix?
                newBindingContext = CreateNewBindingContext(bindingContext,
                                                            modelName: string.Empty);
                modelBindingResult = await TryBind(newBindingContext);
            }

            if (modelBindingResult == null)
            {
                return null; // something went wrong
            }

            bindingContext.OperationBindingContext.BodyBindingState =
                newBindingContext.OperationBindingContext.BodyBindingState;

            if (modelBindingResult.IsModelSet)
            {
                bindingContext.ModelMetadata.Model = modelBindingResult.Model;

                // Update the model state key if we are bound using an empty prefix and it is a complex type.
                // This is needed as validation uses the model state key to log errors. The client validation expects
                // the errors with property names rather than the full name.
                if (newBindingContext.ModelMetadata.IsComplexType && string.IsNullOrEmpty(modelBindingResult.Key))
                {
                    // For non-complex types, if we fell back to the empty prefix, we should still be using the name
                    // of the parameter/property. Complex types have their own property names which acts as model
                    // state keys and do not need special treatment.
                    // For example :
                    //
                    // public class Model
                    // {
                    //     public int SimpleType { get; set; }
                    // }
                    // public void Action(int id, Model model)
                    // {
                    // }
                    //
                    // In this case, for the model parameter the key would be SimpleType instead of model.SimpleType. 
                    // (i.e here the prefix for the model key is empty).
                    // For the id parameter the key would be id.
                    return modelBindingResult;
                }
            }

            return new ModelBindingResult(
                modelBindingResult.Model,
                bindingContext.ModelName,
                modelBindingResult.IsModelSet);
        }

        private async Task<ModelBindingResult> TryBind(ModelBindingContext bindingContext)
        {
            RuntimeHelpers.EnsureSufficientExecutionStack();

            foreach (var binder in ModelBinders)
            {
                var result = await binder.BindModelAsync(bindingContext);
                if (result != null)
                {
                    return result;
                }
            }

            // Either we couldn't find a binder, or the binder couldn't bind. Distinction is not important.
            return null;
        }

        private static ModelBindingContext CreateNewBindingContext(ModelBindingContext oldBindingContext,
                                                                   string modelName)
        {
            var newBindingContext = new ModelBindingContext
            {
                ModelMetadata = oldBindingContext.ModelMetadata,
                ModelName = modelName,
                ModelState = oldBindingContext.ModelState,
                ValueProvider = oldBindingContext.ValueProvider,
                OperationBindingContext = oldBindingContext.OperationBindingContext,
                PropertyFilter = oldBindingContext.PropertyFilter,
            };

            newBindingContext.OperationBindingContext.BodyBindingState = GetBodyBindingState(oldBindingContext);

            // If the property has a specified data binding sources, we need to filter the set of value providers
            // to just those that match. We can skip filtering when IsGreedy == true, because that can't use
            // value providers.
            //
            // We also want to base this filtering on the - top-level value provider in case the data source
            // on this property doesn't intersect with the ambient data source.
            //
            // Ex:
            //
            // public class Person
            // {
            //      [FromQuery]
            //      public int Id { get; set; }
            // }
            //
            // public IActionResult UpdatePerson([FromForm] Person person) { }
            //
            // In this example, [FromQuery] overrides the ambient data source (form).
            var bindingSource = BindingSource.GetBindingSource(oldBindingContext.ModelMetadata.BinderMetadata);
            if (bindingSource != null && !bindingSource.IsGreedy)
            {
                var valueProvider =
                    oldBindingContext.OperationBindingContext.ValueProvider as IBindingSourceValueProvider;
                if (valueProvider != null)
                {
                    newBindingContext.ValueProvider = valueProvider.Filter(bindingSource);
                }
            }

            return newBindingContext;
        }

        private static BodyBindingState GetBodyBindingState(ModelBindingContext oldBindingContext)
        {
            var bindingSource = BindingSource.GetBindingSource(oldBindingContext.ModelMetadata.BinderMetadata);

            var willReadBodyWithFormatter = bindingSource == BindingSource.Body;
            var willReadBodyAsFormData = bindingSource == BindingSource.Form;

            var currentModelNeedsToReadBody = willReadBodyWithFormatter || willReadBodyAsFormData;
            var oldState = oldBindingContext.OperationBindingContext.BodyBindingState;

            // We need to throw if there are multiple models which can cause body to be read multiple times.
            // Reading form data multiple times is ok since we cache form data. For the models marked to read using
            // formatters, multiple reads are not allowed.
            if (oldState == BodyBindingState.FormatterBased && currentModelNeedsToReadBody ||
                oldState == BodyBindingState.FormBased && willReadBodyWithFormatter)
            {
                throw new InvalidOperationException(Resources.MultipleBodyParametersOrPropertiesAreNotAllowed);
            }

            var state = oldBindingContext.OperationBindingContext.BodyBindingState;
            if (willReadBodyWithFormatter)
            {
                state = BodyBindingState.FormatterBased;
            }
            else if (willReadBodyAsFormData && oldState != BodyBindingState.FormatterBased)
            {
                // Only update the model binding state if we have not discovered formatter based state already.
                state = BodyBindingState.FormBased;
            }

            return state;
        }
    }
}
