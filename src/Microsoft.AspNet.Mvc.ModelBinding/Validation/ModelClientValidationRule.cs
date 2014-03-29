using System;
using System.Collections.Generic;

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    public class ModelClientValidationRule
    {
        private readonly Dictionary<string, object> _validationParameters =
            new Dictionary<string, object>(StringComparer.Ordinal);
        private string _validationType = string.Empty;

        public string ErrorMessage { get; set; }

        public IDictionary<string, object> ValidationParameters
        {
            get { return _validationParameters; }
        }

        /// <summary>
        /// Identifier of the <see cref="ModelClientValidationRule"/>. If client-side unobtrustive validation is
        /// enabled, use this <see langref="string"/> as part of the generated "data-val" attribute name. Must be
        /// unique in the set of enabled validation rules.
        /// </summary>
        public string ValidationType
        {
            get { return _validationType; }
            set { _validationType = value ?? string.Empty; }
        }
    }
}
