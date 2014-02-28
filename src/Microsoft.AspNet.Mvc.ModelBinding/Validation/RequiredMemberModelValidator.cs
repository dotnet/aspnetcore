using System.Collections.Generic;
using System.Linq;

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    public class RequiredMemberModelValidator : IModelValidator
    {
        public bool IsRequired
        {
            get { return true; }
        }

        public IEnumerable<ModelValidationResult> Validate(ModelValidationContext context)
        {
            return Enumerable.Empty<ModelValidationResult>();
        }
    }
}
