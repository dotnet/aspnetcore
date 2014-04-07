using System;
using System.Collections.Generic;
using Microsoft.AspNet.Mvc.ModelBinding;

namespace Microsoft.AspNet.Mvc.Rendering
{
    public class FormContext
    {
        private readonly Dictionary<string, FieldValidationMetadata> _fieldValidators =
            new Dictionary<string, FieldValidationMetadata>(StringComparer.Ordinal);
        private readonly Dictionary<string, bool> _renderedFields =
            new Dictionary<string, bool>(StringComparer.Ordinal);

        public IDictionary<string, FieldValidationMetadata> FieldValidators
        {
            get { return _fieldValidators; }
        }

        public string FormId { get; set; }

        public bool ReplaceValidationSummary { get; set; }

        public string ValidationSummaryId { get; set; }

        public FieldValidationMetadata GetValidationMetadataForField([NotNull] string fieldName)
        {
            return GetValidationMetadataForField(fieldName, createIfNotFound: false);
        }

        public FieldValidationMetadata GetValidationMetadataForField([NotNull] string fieldName, bool createIfNotFound)
        {
            FieldValidationMetadata metadata;
            if (!FieldValidators.TryGetValue(fieldName, out metadata))
            {
                if (createIfNotFound)
                {
                    metadata = new FieldValidationMetadata()
                    {
                        FieldName = fieldName
                    };
                    FieldValidators[fieldName] = metadata;
                }
            }

            return metadata;
        }

        public bool RenderedField([NotNull] string fieldName)
        {
            bool result;
            _renderedFields.TryGetValue(fieldName, out result);

            return result;
        }

        public void RenderedField([NotNull] string fieldName, bool value)
        {
            _renderedFields[fieldName] = value;
        }
    }
}
