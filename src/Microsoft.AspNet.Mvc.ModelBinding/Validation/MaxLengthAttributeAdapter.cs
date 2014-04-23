using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    public class MaxLengthAttributeAdapter : DataAnnotationsModelValidator<MaxLengthAttribute>
    {
        public MaxLengthAttributeAdapter(MaxLengthAttribute attribute)
            : base(attribute)
        {
        }

        public override IEnumerable<ModelClientValidationRule> GetClientValidationRules(
            [NotNull] ClientModelValidationContext context)
        {
            var message = GetErrorMessage(context.ModelMetadata);
            return new[] { new ModelClientValidationMaxLengthRule(message, Attribute.Length) };
        }
    }
}
