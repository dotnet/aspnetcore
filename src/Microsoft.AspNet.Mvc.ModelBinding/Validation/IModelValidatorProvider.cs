using System.Collections.Generic;

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    public interface IModelValidatorProvider
    {
        IEnumerable<IModelValidator> GetValidators(ModelMetadata metadata);
    }
}
