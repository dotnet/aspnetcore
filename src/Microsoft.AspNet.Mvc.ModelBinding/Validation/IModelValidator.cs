using System.Collections.Generic;

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    public interface IModelValidator
    {
        bool IsRequired { get; }

        IEnumerable<ModelValidationResult> Validate(ModelValidationContext context);
    }
}
