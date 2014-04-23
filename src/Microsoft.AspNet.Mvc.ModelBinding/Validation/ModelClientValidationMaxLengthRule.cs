namespace Microsoft.AspNet.Mvc.ModelBinding
{
    public class ModelClientValidationMaxLengthRule : ModelClientValidationRule
    {
        private const string MaxLengthValidationType = "maxlength";
        private const string MaxLengthValidationParameter = "max";

        public ModelClientValidationMaxLengthRule([NotNull] string errorMessage, int maximumLength)
            : base(MaxLengthValidationType, errorMessage)
        {
            ValidationParameters[MaxLengthValidationParameter] = maximumLength;
        }
    }
}
