using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    public class StringLengthAttributeAdapter : DataAnnotationsModelValidator<StringLengthAttribute>
    {
        public StringLengthAttributeAdapter(StringLengthAttribute attribute)
            : base(attribute)
        {
        }

        public override IEnumerable<ModelClientValidationRule> GetClientValidationRules(
            [NotNull] ClientModelValidationContext context)
        {
            var errorMessage = GetErrorMessage(context.ModelMetadata);
            return new[] { new ModelClientValidationStringLengthRule(errorMessage, 
                                                                     Attribute.MinimumLength, 
                                                                     Attribute.MaximumLength) };
        }
    }
}