using System.Collections.Generic;
using System.Linq;

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    public class ArrayModelBinder<TElement> : CollectionModelBinder<TElement>
    {
        public override bool BindModel(ModelBindingContext bindingContext)
        {
            if (bindingContext.ModelMetadata.IsReadOnly)
            {
                return false;
            }

            return base.BindModel(bindingContext);
        }

        protected override bool CreateOrReplaceCollection(ModelBindingContext bindingContext, IList<TElement> newCollection)
        {
            bindingContext.Model = newCollection.ToArray();
            return true;
        }
    }
}
