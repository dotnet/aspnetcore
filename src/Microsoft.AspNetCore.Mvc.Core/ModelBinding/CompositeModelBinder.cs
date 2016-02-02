// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace Microsoft.AspNetCore.Mvc.ModelBinding
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
        public CompositeModelBinder(IList<IModelBinder> modelBinders)
        {
            if (modelBinders == null)
            {
                throw new ArgumentNullException(nameof(modelBinders));
            }

            ModelBinders = modelBinders;
        }

        /// <inheritdoc />
        public IList<IModelBinder> ModelBinders { get; }

        public virtual Task BindModelAsync(ModelBindingContext bindingContext)
        {
            if (bindingContext == null)
            {
                throw new ArgumentNullException(nameof(bindingContext));
            }

            return RunModelBinders(bindingContext);
        }

        private async Task RunModelBinders(ModelBindingContext bindingContext)
        {
            RuntimeHelpers.EnsureSufficientExecutionStack();

            ModelBindingResult? overallResult = null;
            try
            {
                using (bindingContext.EnterNestedScope())
                {
                    if (PrepareBindingContext(bindingContext))
                    {
                        // Perf: Avoid allocations
                        for (var i = 0; i < ModelBinders.Count; i++)
                        {
                            var binder = ModelBinders[i];
                            await binder.BindModelAsync(bindingContext);
                            if (bindingContext.Result != null)
                            {
                                var result = bindingContext.Result.Value;
                                // This condition is necessary because the ModelState entry would never be validated if
                                // caller fell back to the empty prefix, leading to an possibly-incorrect !IsValid. In most
                                // (hopefully all) cases, the ModelState entry exists because some binders add errors before
                                // returning a result with !IsModelSet. Those binders often cannot run twice anyhow.
                                if (result.IsModelSet ||
                                    bindingContext.ModelState.ContainsKey(bindingContext.ModelName))
                                {
                                    if (bindingContext.IsTopLevelObject && result.Model != null)
                                    {
                                        ValidationStateEntry entry;
                                        if (!bindingContext.ValidationState.TryGetValue(result.Model, out entry))
                                        {
                                            entry = new ValidationStateEntry()
                                            {
                                                Key = result.Key,
                                                Metadata = bindingContext.ModelMetadata,
                                            };
                                            bindingContext.ValidationState.Add(result.Model, entry);
                                        }
                                    }

                                    overallResult = bindingContext.Result;
                                    return;
                                }

                                // Current binder should have been able to bind value but found nothing. Exit loop in a way that
                                // tells caller to fall back to the empty prefix, if appropriate. Do not return result because it
                                // means only "other binders are not applicable".

                                // overallResult MUST still be null at this return statement.
                                return;
                            }
                        }
                    }
                }
            }
            finally
            {
                bindingContext.Result = overallResult;
            }
        }

        private static bool PrepareBindingContext(ModelBindingContext bindingContext)
        {
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

            var valueProvider = bindingContext.ValueProvider;
            var bindingSource = bindingContext.BindingSource;
            var modelName = bindingContext.ModelName;
            var fallbackToEmptyPrefix = bindingContext.FallbackToEmptyPrefix;

            if (bindingSource != null && !bindingSource.IsGreedy)
            {
                var bindingSourceValueProvider = valueProvider as IBindingSourceValueProvider;
                if (bindingSourceValueProvider != null)
                {
                    valueProvider = bindingSourceValueProvider.Filter(bindingSource);
                    if (valueProvider == null)
                    {
                        // Unable to find a value provider for this binding source.
                        return false;
                    }
                }
            }

            if (bindingSource != null && bindingSource.IsGreedy)
            {
                bindingContext.ModelName = modelName;
            }
            else if (
                !fallbackToEmptyPrefix ||
                valueProvider.ContainsPrefix(bindingContext.ModelName))
            {
                bindingContext.ModelName = modelName;
            }
            else
            {
                bindingContext.ModelName = string.Empty;
            }

            bindingContext.ValueProvider = valueProvider;
            bindingContext.FallbackToEmptyPrefix = false;

            return true;
        }
    }
}
