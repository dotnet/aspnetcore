using System;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Microsoft.AspNetCore.Mvc.ModelBinding.Binders
{
    /// <summary>
    /// Provides a model binder for parameters annotated with FromFormOrJsonAttribute.
    /// </summary>
    public class FromFormOrJsonModelBinderProvider : IModelBinderProvider
    {
        public IModelBinder? GetBinder(ModelBinderProviderContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var binderAttribute = context.Metadata.BinderMetadata as FromFormOrJsonAttribute;
            if (binderAttribute != null)
            {
                var binderType = typeof(FromFormOrJsonModelBinder<>).MakeGenericType(context.Metadata.ModelType);
                return (IModelBinder?)Activator.CreateInstance(binderType);
            }

            return null;
        }
    }
}
