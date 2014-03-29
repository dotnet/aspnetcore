using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    public class FieldValidationMetadata
    {
        private readonly List<ModelClientValidationRule> _validationRules =
            new List<ModelClientValidationRule>();
        private string _fieldName = string.Empty;

        public string FieldName
        {
            get { return _fieldName; }
            set { _fieldName = value ?? string.Empty; }
        }

        public bool ReplaceValidationMessageContents { get; set; }

        public string ValidationMessageId { get; set; }

        public IList<ModelClientValidationRule> ValidationRules
        {
            get { return _validationRules; }
        }
    }
}
