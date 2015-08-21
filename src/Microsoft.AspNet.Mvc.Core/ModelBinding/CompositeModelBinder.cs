// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc.Core;
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
            // Will there be a last chance (fallback) binding attempt?
            var isFirstChanceBinding = bindingContext.FallbackToEmptyPrefix &&
                !string.IsNullOrEmpty(bindingContext.ModelName);

            var newBindingContext = CreateNewBindingContext(bindingContext, bindingContext.ModelName);
            if (newBindingContext == null)
            {
                // Unable to find a value provider for this binding source. Binding will fail.
                return null;
            }

            newBindingContext.IsFirstChanceBinding = isFirstChanceBinding;
            var modelBindingResult = await TryBind(newBindingContext);

            if (modelBindingResult == null && isFirstChanceBinding)
            {
                // Fall back to empty prefix.
                newBindingContext = CreateNewBindingContext(bindingContext, modelName: string.Empty);
                Debug.Assert(newBindingContext != null, "Should have failed on first attempt.");

                modelBindingResult = await TryBind(newBindingContext);
            }

            if (modelBindingResult == null)
            {
                // Unable to bind or something went wrong.
                return null;
            }

            var bindingKey = bindingContext.ModelName;
            if (modelBindingResult.IsModelSet)
            {
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
                    bindingKey = string.Empty;
                }
            }

            return new ModelBindingResult(
                modelBindingResult.Model,
                bindingKey,
                modelBindingResult.IsModelSet,
                modelBindingResult.ValidationNode);
        }

        private async Task<ModelBindingResult> TryBind(ModelBindingContext bindingContext)
        {
            RuntimeHelpers.EnsureSufficientExecutionStack();

            foreach (var binder in ModelBinders)
            {
                var result = await binder.BindModelAsync(bindingContext);
                if (result != null)
                {
                    // Use returned ModelBindingResult if it indicates the model was set, indicates the binder
                    // encountered a fatal error, or is related to a ModelState entry.
                    //
                    // The second condition is necessary because the BodyModelBinder unconditionally binds during the
                    // first attempt and does not always create ModelState values using ModelName.
                    //
                    // The third condition is necessary because the ModelState entry would never be validated if
                    // caller fell back to the empty prefix, leading to an possibly-incorrect !IsValid. In most
                    // (hopefully all) cases, the ModelState entry exists because some binders add errors before
                    // returning a result with !IsModelSet. Those binders often cannot run twice anyhow.
                    if (result.IsFatalError ||
                        result.IsModelSet ||
                        bindingContext.ModelState.ContainsKey(bindingContext.ModelName))
                    {
                        return result;
                    }

                    // Current binder should have been able to bind value but found nothing. Exit loop in a way that
                    // tells caller to fall back to the empty prefix, if appropriate. Do not return result because it
                    // means only "other binders are not applicable".
                    break;
                }
            }

            // Either we couldn't find a binder, or the binder couldn't bind. Distinction is not important.
            return null;
        }

        private static ModelBindingContext CreateNewBindingContext(
            ModelBindingContext oldBindingContext,
            string modelName)
        {
            var newBindingContext = new ModelBindingContext
            {
                Model = oldBindingContext.Model,
                ModelMetadata = oldBindingContext.ModelMetadata,
                ModelName = modelName,
                FieldName = oldBindingContext.FieldName,
                ModelState = oldBindingContext.ModelState,
                ValueProvider = oldBindingContext.ValueProvider,
                OperationBindingContext = oldBindingContext.OperationBindingContext,
                PropertyFilter = oldBindingContext.PropertyFilter,
                BinderModelName = oldBindingContext.BinderModelName,
                BindingSource = oldBindingContext.BindingSource,
                BinderType = oldBindingContext.BinderType,
                IsTopLevelObject = oldBindingContext.IsTopLevelObject,
            };

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
            var bindingSource = oldBindingContext.BindingSource;
            if (bindingSource != null && !bindingSource.IsGreedy)
            {
                var valueProvider =
                    oldBindingContext.OperationBindingContext.ValueProvider as IBindingSourceValueProvider;
                if (valueProvider != null)
                {
                    newBindingContext.ValueProvider = valueProvider.Filter(bindingSource);
                    if (newBindingContext.ValueProvider == null)
                    {
                        // Unable to find a value provider for this binding source.
                        return null;
                    }
                }
            }

            return newBindingContext;
        }
    }
}
