using System.ComponentModel.DataAnnotations;

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    public class DataAnnotationsModelValidator<TAttribute> : DataAnnotationsModelValidator
        where TAttribute : ValidationAttribute
    {
        public DataAnnotationsModelValidator(TAttribute attribute)
            : base(attribute)
        {
        }

        protected new TAttribute Attribute
        {
            get { return (TAttribute)base.Attribute; }
        }
    }
}
