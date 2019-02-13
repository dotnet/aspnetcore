// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Microsoft.AspNetCore.Components.Forms
{
    /// <summary>
    /// Extension methods to add DataAnnotations validation to an <see cref="EditContext"/>.
    /// </summary>
    public static class EditContextDataAnnotationsExtensions
    {
        /// <summary>
        /// Adds DataAnnotations validation support to the <see cref="EditContext"/>.
        /// </summary>
        /// <param name="editContext">The <see cref="EditContext"/>.</param>
        public static EditContext AddDataAnnotationsValidation(this EditContext editContext)
        {
            if (editContext == null)
            {
                throw new ArgumentNullException(nameof(editContext));
            }

            var messages = new ValidationMessageStore(editContext);

            editContext.OnValidationRequested += (object sender, EventArgs e) =>
            {
                ValidateModel((EditContext)sender, messages);
            };

            return editContext;
        }

        private static void ValidateModel(EditContext editContext, ValidationMessageStore messages)
        {
            var validationContext = new ValidationContext(editContext.Model);
            var validationResults = new List<ValidationResult>();
            Validator.TryValidateObject(editContext.Model, validationContext, validationResults, true);

            // Transfer results to the ValidationMessageStore
            messages.Clear();
            foreach (var validationResult in validationResults)
            {
                foreach (var memberName in validationResult.MemberNames)
                {
                    messages.Add(editContext.Field(memberName), validationResult.ErrorMessage);
                }
            }

            editContext.NotifyValidationStateChanged();
        }
    }
}
