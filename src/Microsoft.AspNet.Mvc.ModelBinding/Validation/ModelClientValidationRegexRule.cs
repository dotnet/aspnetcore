using System.Runtime.CompilerServices;

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    public class ModelClientValidationRegexRule : ModelClientValidationRule
    {
        private const string ValidationType = "regex";
        private const string ValidationRuleName = "pattern";

        public ModelClientValidationRegexRule(string errorMessage, string pattern)
            : base(ValidationType, errorMessage)
        {
            ValidationParameters.Add(ValidationRuleName, pattern);
        }
    }
}
