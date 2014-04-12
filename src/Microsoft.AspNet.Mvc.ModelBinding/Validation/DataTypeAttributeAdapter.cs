using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    /// <summary>
    /// A validation adapter that is used to map <see cref="DataTypeAttribute"/>'s to a single client side validation 
    /// rule.
    /// </summary>
    public class DataTypeAttributeAdapter : DataAnnotationsModelValidator
    {
        public DataTypeAttributeAdapter(DataTypeAttribute attribute,
                                        [NotNull] string ruleName)
            : base(attribute)
        {
            if (string.IsNullOrEmpty(ruleName))
            {
                throw new ArgumentException(Resources.ArgumentCannotBeNullOrEmpty, "ruleName");
            }
            RuleName = ruleName;
        }

        public string RuleName { get; private set; }

        public override IEnumerable<ModelClientValidationRule> GetClientValidationRules(
            [NotNull] ClientModelValidationContext context)
        {
            var errorMessage = GetErrorMessage(context.ModelMetadata);
            return new[] { new ModelClientValidationRule(RuleName, errorMessage) };
        }
    }
}
