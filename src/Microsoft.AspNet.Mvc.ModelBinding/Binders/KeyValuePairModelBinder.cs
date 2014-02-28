using System.Collections.Generic;
using Microsoft.AspNet.Mvc.ModelBinding.Internal;

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    public sealed class KeyValuePairModelBinder<TKey, TValue> : IModelBinder
    {
        public bool BindModel(ModelBindingContext bindingContext)
        {
            ModelBindingHelper.ValidateBindingContext(bindingContext, typeof(KeyValuePair<TKey, TValue>), allowNullModel: true);

            TKey key;
            bool keyBindingSucceeded = TryBindStrongModel(bindingContext, "key", out key);

            TValue value;
            bool valueBindingSucceeded = TryBindStrongModel(bindingContext, "value", out value);

            if (keyBindingSucceeded && valueBindingSucceeded)
            {
                bindingContext.Model = new KeyValuePair<TKey, TValue>(key, value);
            }
            return keyBindingSucceeded || valueBindingSucceeded;
        }

        // TODO: Make this internal
        public bool TryBindStrongModel<TModel>(ModelBindingContext parentBindingContext,
                                                string propertyName,
                                                out TModel model)
        {
            ModelBindingContext propertyBindingContext = new ModelBindingContext(parentBindingContext)
            {
                ModelMetadata = parentBindingContext.MetadataProvider.GetMetadataForType(modelAccessor: null, modelType: typeof(TModel)),
                ModelName = ModelBindingHelper.CreatePropertyModelName(parentBindingContext.ModelName, propertyName)
            };

            if (propertyBindingContext.ModelBinder.BindModel(propertyBindingContext))
            {
                object untypedModel = propertyBindingContext.Model;
                model = ModelBindingHelper.CastOrDefault<TModel>(untypedModel);
                parentBindingContext.ValidationNode.ChildNodes.Add(propertyBindingContext.ValidationNode);
                return true;
            }

            model = default(TModel);
            return false;
        }
    }
}
