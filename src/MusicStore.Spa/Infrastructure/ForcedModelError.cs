using System;
using System.Globalization;

namespace System.ComponentModel.DataAnnotations
{
    [AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false)]
    public class ForcedModelErrorAttribute : ValidationAttribute
    {
        public ForcedModelErrorAttribute(object failValue)
        {
            FailValue = failValue;
        }

        public object FailValue { get; private set; }

        public override string FormatErrorMessage(string name)
        {
            return string.Format(CultureInfo.CurrentCulture, "The field {0} was forced to fail model validation.", name);
        }

        public override bool IsValid(object value)
        {
#if DEBUG
            return value == null || !value.Equals(FailValue);
#else
            return true;
#endif
        }
    }
}
