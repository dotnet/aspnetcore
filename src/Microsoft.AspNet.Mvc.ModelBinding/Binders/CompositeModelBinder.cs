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
        private readonly IModelBinderProvider _modelBindersProvider;
        private IReadOnlyList<IModelBinder> _binders;

        /// <summary>
        /// Initializes a new instance of the CompositeModelBinder class.
        /// </summary>
        /// <param name="modelBindersProvider">Provides a collection of <see cref="IModelBinder"/> instances.</param>
        public CompositeModelBinder(IModelBinderProvider modelBindersProvider)
        {
            _modelBindersProvider = modelBindersProvider;
        }

        /// <inheritdoc />
        public IReadOnlyList<IModelBinder> ModelBinders
        {
            get
            {
                if (_binders == null)
                {
                    _binders = _modelBindersProvider.ModelBinders;
                }
                return _binders;
            }
        }

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
            if (IsBindingAtRootOfObjectGraph(newBindingContext))
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
            bindingContext.Model = newBindingContext.Model;
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

            // look at the value providers and see if they need to be restricted.
            var metadata = oldBindingContext.ModelMetadata.BinderMetadata as IValueProviderMetadata;
            if (metadata != null)
            {
                // ValueProvider property might contain a filtered list of value providers.
                // While deciding to bind a particular property which is annotated with a IValueProviderMetadata,
                // instead of refiltering an already filtered list, we need to filter value providers from a global
                // list of all value providers. This is so that every artifact that is explicitly marked using an
                // IValueProviderMetadata can restrict model binding to only use value providers which support this
                // IValueProviderMetadata.
                var valueProvider =
                    oldBindingContext.OperationBindingContext.ValueProvider as IMetadataAwareValueProvider;
                if (valueProvider != null)
                {
                    newBindingContext.ValueProvider = valueProvider.Filter(metadata);
                }
            }

            return newBindingContext;
        }

        private static BodyBindingState GetBodyBindingState(ModelBindingContext oldBindingContext)
        {
            var binderMetadata = oldBindingContext.ModelMetadata.BinderMetadata;
            var newIsFormatterBasedMetadataFound = binderMetadata is IFormatterBinderMetadata;
            var newIsFormBasedMetadataFound = binderMetadata is IFormDataValueProviderMetadata;
            var currentModelNeedsToReadBody = newIsFormatterBasedMetadataFound || newIsFormBasedMetadataFound;
            var oldState = oldBindingContext.OperationBindingContext.BodyBindingState;

            // We need to throw if there are multiple models which can cause body to be read multiple times.
            // Reading form data multiple times is ok since we cache form data. For the models marked to read using
            // formatters, multiple reads are not allowed.
            if (oldState == BodyBindingState.FormatterBased && currentModelNeedsToReadBody ||
                oldState == BodyBindingState.FormBased && newIsFormatterBasedMetadataFound)
            {
                throw new InvalidOperationException(Resources.MultipleBodyParametersOrPropertiesAreNotAllowed);
            }

            var state = oldBindingContext.OperationBindingContext.BodyBindingState;
            if (newIsFormatterBasedMetadataFound)
            {
                state = BodyBindingState.FormatterBased;
            }
            else if (newIsFormBasedMetadataFound && oldState != BodyBindingState.FormatterBased)
            {
                // Only update the model binding state if we have not discovered formatter based state already.
                state = BodyBindingState.FormBased;
            }

            return state;
        }
    }
}
