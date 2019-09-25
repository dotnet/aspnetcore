// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace Microsoft.AspNetCore.Components.Forms
{
    public class BlazorDataAnnotationsValidator : ComponentBase
    {
        private static readonly object ValidationContextValidatorKey = new object();
        private ValidationMessageStore _validationMessageStore;

        [CascadingParameter]
        internal EditContext EditContext { get; set; }

        protected override void OnInitialized()
        {
            _validationMessageStore = new ValidationMessageStore(EditContext);

            // Perform object-level validation (starting from the root model) on request
            EditContext.OnValidationRequested += (sender, eventArgs) =>
            {
                _validationMessageStore.Clear();
                ValidateObject(EditContext.Model);
                EditContext.NotifyValidationStateChanged();
            };

            // Perform per-field validation on each field edit
            EditContext.OnFieldChanged += (sender, eventArgs) =>
                ValidateField(EditContext, _validationMessageStore, eventArgs.FieldIdentifier);
        }

        internal void ValidateObject(object value)
        {
            if (value is null)
            {
                return;
            }

            if (value is IEnumerable<object> enumerable)
            {
                var index = 0;
                foreach (var item in enumerable)
                {
                    ValidateObject(item);
                    index++;
                }

                return;
            }

            var validationResults = new List<ValidationResult>();
            ValidateObject(value, validationResults);

            // Transfer results to the ValidationMessageStore
            foreach (var validationResult in validationResults)
            {
                if (!validationResult.MemberNames.Any())
                {
                    _validationMessageStore.Add(new FieldIdentifier(value, string.Empty), validationResult.ErrorMessage);
                    continue;
                }

                foreach (var memberName in validationResult.MemberNames)
                {
                    var fieldIdentifier = new FieldIdentifier(value, memberName);
                    _validationMessageStore.Add(fieldIdentifier, validationResult.ErrorMessage);
                }
            }
        }

        private void ValidateObject(object value, List<ValidationResult> validationResults)
        {
            var validationContext = new ValidationContext(value);
            validationContext.Items.Add(ValidationContextValidatorKey, this);
            Validator.TryValidateObject(value, validationContext, validationResults, validateAllProperties: true);
        }

        internal static bool TryValidateRecursive(object value, ValidationContext validationContext)
        {
            if (validationContext.Items.TryGetValue(ValidationContextValidatorKey, out var result) && result is BlazorDataAnnotationsValidator validator)
            {
                validator.ValidateObject(value);

                return true;
            }

            return false;
        }

        private static void ValidateField(EditContext editContext, ValidationMessageStore messages, in FieldIdentifier fieldIdentifier)
        {
            // DataAnnotations only validates public properties, so that's all we'll look for
            var propertyInfo = fieldIdentifier.Model.GetType().GetProperty(fieldIdentifier.FieldName);
            if (propertyInfo != null)
            {
                var propertyValue = propertyInfo.GetValue(fieldIdentifier.Model);
                var validationContext = new ValidationContext(fieldIdentifier.Model)
                {
                    MemberName = propertyInfo.Name
                };
                var results = new List<ValidationResult>();

                Validator.TryValidateProperty(propertyValue, validationContext, results);
                messages.Clear(fieldIdentifier);
                messages.Add(fieldIdentifier, results.Select(result => result.ErrorMessage));

                // We have to notify even if there were no messages before and are still no messages now,
                // because the "state" that changed might be the completion of some async validation task
                editContext.NotifyValidationStateChanged();
            }
        }
    }
}
