using System.Runtime.CompilerServices;

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    public class ModelClientValidationRegexRule : ModelClientValidationRule
    {
        private const string RegexValidationType = "regex";
        private const string RegexValidationRuleName = "pattern";

        public ModelClientValidationRegexRule(string errorMessage, string pattern)
            : base(RegexValidationType, errorMessage)
        {
            ValidationParameters.Add(RegexValidationRuleName, pattern);
        }
    }
}
