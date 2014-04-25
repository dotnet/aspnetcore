namespace Microsoft.AspNet.Mvc.ModelBinding
{
    public class ModelClientValidationRequiredRule : ModelClientValidationRule
    {
        private const string RequiredValidationType = "required";

        public ModelClientValidationRequiredRule(string errorMessage) : 
            base(RequiredValidationType, errorMessage)
        {
        }
    }
}
