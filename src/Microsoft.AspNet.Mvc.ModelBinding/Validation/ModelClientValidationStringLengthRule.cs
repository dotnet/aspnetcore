namespace Microsoft.AspNet.Mvc.ModelBinding
{
    public class ModelClientValidationStringLengthRule : ModelClientValidationRule
    {
        private const string LengthValidationType = "length";
        private const string MinValidationParameter = "min";
        private const string MaxValidationParameter = "max";

        public ModelClientValidationStringLengthRule(string errorMessage, int minimumLength, int maximumLength)
            : base(LengthValidationType, errorMessage)
        {
            if (minimumLength != 0)
            {
                ValidationParameters[MinValidationParameter] = minimumLength;
            }

            if (maximumLength != int.MaxValue)
            {
                ValidationParameters[MaxValidationParameter] = maximumLength;
            }
        }
    }
}
