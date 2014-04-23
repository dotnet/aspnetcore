namespace Microsoft.AspNet.Mvc.ModelBinding
{
    public class ModelClientValidationMinLengthRule : ModelClientValidationRule
    {
        private const string MinLengthValidationType = "minlength";
        private const string MinLengthValidationParameter = "min";

        public ModelClientValidationMinLengthRule([NotNull] string errorMessage, int minimumLength)
            : base(MinLengthValidationType, errorMessage)
        {
            ValidationParameters[MinLengthValidationParameter] = minimumLength;
        }
    }
}
