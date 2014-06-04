// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    /// <summary>
    /// This class is an <see cref="IModelBinder"/> that delegates to one of a collection of
    /// <see cref="IModelBinder"/> instances.
    /// </summary>
    /// <remarks>
    /// If no binder is available and the <see cref="ModelBindingContext"/> allows it,
    /// this class tries to find a binder using an empty prefix.
    /// </remarks>
    public class CompositeModelBinder : IModelBinder
    {
        public CompositeModelBinder([NotNull] IEnumerable<IModelBinder> binders)
            : this(binders.ToArray())
        {
        }

        public CompositeModelBinder(params IModelBinder[] binders)
        {
            Binders = binders;
        }

        private IModelBinder[] Binders { get; set; }

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
            if (newBindingContext.ModelMetadata.ContainerType == null &&
                newBindingContext.ModelMetadata.ModelType != typeof(ComplexModelDto))
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

                var validationContext = new ModelValidationContext(bindingContext.MetadataProvider,
                                                                   bindingContext.ValidatorProviders,
                                                                   bindingContext.ModelState,
                                                                   bindingContext.ModelMetadata,
                                                                   containerMetadata: null);

                newBindingContext.ValidationNode.Validate(validationContext, parentNode: null);
            }

            bindingContext.Model = newBindingContext.Model;
            return true;
        }

        private async Task<bool> TryBind([NotNull] ModelBindingContext bindingContext)
        {
            // TODO: RuntimeHelpers.EnsureSufficientExecutionStack does not exist in the CoreCLR.
            // Protects against stack overflow for deeply nested model binding
            // RuntimeHelpers.EnsureSufficientExecutionStack();

            foreach (var binder in Binders)
            {
                if (await binder.BindModelAsync(bindingContext))
                {
                    return true;
                }
            }

            // Either we couldn't find a binder, or the binder couldn't bind. Distinction is not important.
            return false;
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
                ValidatorProviders = oldBindingContext.ValidatorProviders,
                MetadataProvider = oldBindingContext.MetadataProvider,
                ModelBinder = oldBindingContext.ModelBinder,
                HttpContext = oldBindingContext.HttpContext
            };

            // validation is expensive to create, so copy it over if we can
            if (reuseValidationNode)
            {
                newBindingContext.ValidationNode = oldBindingContext.ValidationNode;
            }

            return newBindingContext;
        }
    }
}
