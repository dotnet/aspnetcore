using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNet.Mvc.ModelBinding.Internal;

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
        public CompositeModelBinder(IEnumerable<IModelBinder> binders)
            : this(binders.ToArray())
        {
        }

        public CompositeModelBinder(params IModelBinder[] binders)
        {
            Binders = binders;
        }

        private IModelBinder[] Binders { get; set; }

        public virtual bool BindModel(ModelBindingContext bindingContext)
        {
            ModelBindingContext newBindingContext = CreateNewBindingContext(bindingContext, bindingContext.ModelName);

            bool boundSuccessfully = TryBind(newBindingContext);
            if (!boundSuccessfully && !String.IsNullOrEmpty(bindingContext.ModelName)
                && bindingContext.FallbackToEmptyPrefix)
            {
                // fallback to empty prefix?
                newBindingContext = CreateNewBindingContext(bindingContext, modelName: String.Empty);
                boundSuccessfully = TryBind(newBindingContext);
            }

            if (!boundSuccessfully)
            {
                return false; // something went wrong
            }

            // run validation and return the model
            // If we fell back to an empty prefix above and are dealing with simple types,
            // propagate the non-blank model name through for user clarity in validation errors.
            // Complex types will reveal their individual properties as model names and do not require this.
            // TODO: Validation
            //if (!newBindingContext.ModelMetadata.IsComplexType && String.IsNullOrEmpty(newBindingContext.ModelName))
            //{
            //    newBindingContext.ValidationNode = new Validation.ModelValidationNode(newBindingContext.ModelMetadata, bindingContext.ModelName);
            //}

            //newBindingContext.ValidationNode.Validate(context, null /* parentNode */);
            bindingContext.Model = newBindingContext.Model;
            return true;
        }

        private bool TryBind(ModelBindingContext bindingContext)
        {
            // TODO: The body of this method existed as HttpActionContextExtensions.Bind. We might have to refactor it into
            // something that is shared.
            if (bindingContext == null)
            {
                throw Error.ArgumentNull("bindingContext");
            }

            // TODO: RuntimeHelpers.EnsureSufficientExecutionStack does not exist in the CoreCLR.
            // Protects against stack overflow for deeply nested model binding
            // RuntimeHelpers.EnsureSufficientExecutionStack();

            bool requiresBodyBinder = bindingContext.ModelMetadata.IsFromBody;
            foreach (IModelBinder binder in Binders)
            {
                if (binder.BindModel(bindingContext))
                {
                    return true;
                }
            }

            // Either we couldn't find a binder, or the binder couldn't bind. Distinction is not important.
            return false;
        }

        private static ModelBindingContext CreateNewBindingContext(ModelBindingContext oldBindingContext, string modelName)
        {
            var newBindingContext = new ModelBindingContext
            {
                ModelMetadata = oldBindingContext.ModelMetadata,
                ModelName = modelName,
                ModelState = oldBindingContext.ModelState,
                ValueProvider = oldBindingContext.ValueProvider,
                MetadataProvider = oldBindingContext.MetadataProvider,
                ModelBinder = oldBindingContext.ModelBinder,
                HttpContext = oldBindingContext.HttpContext               
            };

            // TODO: Validation
            //// validation is expensive to create, so copy it over if we can
            //if (Object.ReferenceEquals(modelName, oldBindingContext.ModelName))
            //{
            //    newBindingContext.ValidationNode = oldBindingContext.ValidationNode;
            //}

            return newBindingContext;
        }
    }
}
