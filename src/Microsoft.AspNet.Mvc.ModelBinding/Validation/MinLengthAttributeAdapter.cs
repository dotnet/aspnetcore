using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    public class MinLengthAttributeAdapter : DataAnnotationsModelValidator<MinLengthAttribute>
    {
        public MinLengthAttributeAdapter(MinLengthAttribute attribute)
            : base(attribute)
        {
        }

        public override IEnumerable<ModelClientValidationRule> GetClientValidationRules(
            [NotNull] ClientModelValidationContext context)
        {
            var message = GetErrorMessage(context.ModelMetadata);
            return new[] { new ModelClientValidationMinLengthRule(message, Attribute.Length) };
        }
    }
}
