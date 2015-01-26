// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

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

        public virtual async Task<bool> BindModelAsync([NotNull] ModelBindingContext bindingContext)
        {
            var newBindingContext = CreateNewBindingContext(bindingContext,
                                                            bindingContext.ModelName,
                                                            reuseValidationNode: true);

            var boundSuccessfully = await TryBind(newBindingContext);
            if (!boundSuccessfully && !string.IsNullOrEmpty(bindingContext.ModelName)
                && bindingContext.FallbackToEmptyPrefix)
            {
                // fallback to empty prefix?
                newBindingContext = CreateNewBindingContext(bindingContext,
                                                            modelName: string.Empty,
                                                            reuseValidationNode: false);
                boundSuccessfully = await TryBind(newBindingContext);
            }

            if (!boundSuccessfully)
            {
                return false; // something went wrong
            }

            // Only perform validation at the root of the object graph. ValidationNode will recursively walk the graph.
            // Ignore ComplexModelDto since it essentially wraps the primary object.
            if (newBindingContext.IsModelSet && IsBindingAtRootOfObjectGraph(newBindingContext))
            {
                // run validation and return the model
                // If we fell back to an empty prefix above and are dealing with simple types,
                // propagate the non-blank model name through for user clarity in validation errors.
                // Complex types will reveal their individual properties as model names and do not require this.
                if (!newBindingContext.ModelMetadata.IsComplexType &&
                    string.IsNullOrEmpty(newBindingContext.ModelName))
                {
                    newBindingContext.ValidationNode = new ModelValidationNode(newBindingContext.ModelMetadata,
                                                                               bindingContext.ModelName);
                }

                var validationContext = new ModelValidationContext(
                    bindingContext.OperationBindingContext.MetadataProvider,
                    bindingContext.OperationBindingContext.ValidatorProvider,
                    bindingContext.ModelState,
                    bindingContext.ModelMetadata,
                    containerMetadata: null);

                newBindingContext.ValidationNode.Validate(validationContext, parentNode: null);
            }

            bindingContext.OperationBindingContext.BodyBindingState =
                newBindingContext.OperationBindingContext.BodyBindingState;

            if (newBindingContext.IsModelSet)
            {
                bindingContext.Model = newBindingContext.Model;
            }

            return true;
        }

        private async Task<bool> TryBind(ModelBindingContext bindingContext)
        {
            RuntimeHelpers.EnsureSufficientExecutionStack();

            foreach (var binder in ModelBinders)
            {
                if (await binder.BindModelAsync(bindingContext))
                {
                    return true;
                }
            }

            // Either we couldn't find a binder, or the binder couldn't bind. Distinction is not important.
            return false;
        }

        private static bool IsBindingAtRootOfObjectGraph(ModelBindingContext bindingContext)
        {
            // We're at the root of the object graph if the model does does not have a container.
            // This statement is true for complex types at the root twice over - once with the actual model
            // and once when when it is represented by a ComplexModelDto. Ignore the latter case.

            return bindingContext.ModelMetadata.ContainerType == null &&
                   bindingContext.ModelMetadata.ModelType != typeof(ComplexModelDto);
        }

        private static ModelBindingContext CreateNewBindingContext(ModelBindingContext oldBindingContext,
                                                                   string modelName,
                                                                   bool reuseValidationNode)
        {
            var newBindingContext = new ModelBindingContext
            {
                IsModelSet = oldBindingContext.IsModelSet,
                ModelMetadata = oldBindingContext.ModelMetadata,
                ModelName = modelName,
                ModelState = oldBindingContext.ModelState,
                ValueProvider = oldBindingContext.ValueProvider,
                OperationBindingContext = oldBindingContext.OperationBindingContext,
                PropertyFilter = oldBindingContext.PropertyFilter,
            };

            // validation is expensive to create, so copy it over if we can
            if (reuseValidationNode)
            {
                newBindingContext.ValidationNode = oldBindingContext.ValidationNode;
            }

            newBindingContext.OperationBindingContext.BodyBindingState = GetBodyBindingState(oldBindingContext);

            // If the property has a specified data binding sources, we need to filter the set of value providers
            // to just those that match. We can skip filtering when IsGreedy == true, because that can't use
            // value providers.
            //
            // We also want to base this filtering on the - top-level value profider in case the data source
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
