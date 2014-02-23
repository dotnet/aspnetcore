using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.AspNet.Mvc.ModelBinding.Internal;

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    // Describes a complex model, but uses a collection rather than individual properties as the data store.
    public class ComplexModelDto
    {
        public ComplexModelDto(ModelMetadata modelMetadata, IEnumerable<ModelMetadata> propertyMetadata)
        {
            if (modelMetadata == null)
            {
                throw Error.ArgumentNull("modelMetadata");
            }

            if (propertyMetadata == null)
            {
                throw Error.ArgumentNull("propertyMetadata");
            }

            ModelMetadata = modelMetadata;
            PropertyMetadata = new Collection<ModelMetadata>(propertyMetadata.ToList());
            Results = new Dictionary<ModelMetadata, ComplexModelDtoResult>();
        }

        public ModelMetadata ModelMetadata { get; private set; }

        public Collection<ModelMetadata> PropertyMetadata { get; private set; }

        // Contains entries corresponding to each property against which binding was
        // attempted. If binding failed, the entry's value will be null. If binding
        // was never attempted, this dictionary will not contain a corresponding
        // entry.
        public IDictionary<ModelMetadata, ComplexModelDtoResult> Results { get; private set; }
    }
}
