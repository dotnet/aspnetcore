using System;
using System.Collections.Generic;

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    public class ModelClientValidationRule
    {
        private readonly Dictionary<string, object> _validationParameters =
            new Dictionary<string, object>(StringComparer.Ordinal);

        public ModelClientValidationRule([NotNull] string errorMessage)
            : this(validationType: string.Empty, errorMessage: errorMessage)
        {
        }

        public ModelClientValidationRule([NotNull] string validationType,
                                         [NotNull] string errorMessage)
        {
            ValidationType = validationType;
            ErrorMessage = errorMessage;
        }

        public string ErrorMessage { get; private set; }

        /// <summary>
        /// Identifier of the <see cref="ModelClientValidationRule"/>. If client-side unobtrustive validation is
        /// enabled, use this <see langref="string"/> as part of the generated "data-val" attribute name. Must be
        /// unique in the set of enabled validation rules.
        /// </summary>
        public string ValidationType { get; private set; }

        public IDictionary<string, object> ValidationParameters
        {
            get { return _validationParameters; }
        }
    }
}
