using System.Collections.Generic;

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    public interface IClientModelValidator
    {
        IEnumerable<ModelClientValidationRule> GetClientValidationRules(ClientModelValidationContext context);
    }
}
