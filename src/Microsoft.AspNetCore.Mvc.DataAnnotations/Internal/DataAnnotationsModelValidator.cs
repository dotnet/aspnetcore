// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.Extensions.Localization;

namespace Microsoft.AspNetCore.Mvc.DataAnnotations.Internal
{
    /// <summary>
    /// Validates based on the given <see cref="ValidationAttribute"/>.
    /// </summary>
    public class DataAnnotationsModelValidator : IModelValidator
    {
        private static readonly object _emptyValidationContextInstance = new object();
        private readonly IStringLocalizer _stringLocalizer;
        private readonly IValidationAttributeAdapterProvider _validationAttributeAdapterProvider;

        /// <summary>
        ///  Create a new instance of <see cref="DataAnnotationsModelValidator"/>.
        /// </summary>
        /// <param name="attribute">The <see cref="ValidationAttribute"/> that defines what we're validating.</param>
        /// <param name="stringLocalizer">The <see cref="IStringLocalizer"/> used to create messages.</param>
        /// <param name="validationAttributeAdapterProvider">The <see cref="IValidationAttributeAdapterProvider"/>
        /// which <see cref="ValidationAttributeAdapter{TAttribute}"/>'s will be created from.</param>
        public DataAnnotationsModelValidator(
            IValidationAttributeAdapterProvider validationAttributeAdapterProvider,
            ValidationAttribute attribute,
            IStringLocalizer stringLocalizer)
        {
            if (validationAttributeAdapterProvider == null)
            {
                throw new ArgumentNullException(nameof(validationAttributeAdapterProvider));
            }

            if (attribute == null)
            {
                throw new ArgumentNullException(nameof(attribute));
            }

            _validationAttributeAdapterProvider = validationAttributeAdapterProvider;
            Attribute = attribute;
            _stringLocalizer = stringLocalizer;
        }

        /// <summary>
        /// The attribute being validated against.
        /// </summary>
        public ValidationAttribute Attribute { get; }

        /// <summary>
        /// Validates the context against the <see cref="ValidationAttribute"/>.
        /// </summary>
        /// <param name="validationContext">The context being validated.</param>
        /// <returns>An enumerable of the validation results.</returns>
        public IEnumerable<ModelValidationResult> Validate(ModelValidationContext validationContext)
        {
            if (validationContext == null)
            {
                throw new ArgumentNullException(nameof(validationContext));
            }
            if (validationContext.ModelMetadata == null)
            {
                throw new ArgumentException(
                    Resources.FormatPropertyOfTypeCannotBeNull(
                        nameof(validationContext.ModelMetadata),
                        typeof(ModelValidationContext)),
                    nameof(validationContext));
            }
            if (validationContext.MetadataProvider == null)
            {
                throw new ArgumentException(
                    Resources.FormatPropertyOfTypeCannotBeNull(
                        nameof(validationContext.MetadataProvider),
                        typeof(ModelValidationContext)),
                    nameof(validationContext));
            }

            var metadata = validationContext.ModelMetadata;
            var memberName = metadata.PropertyName;
            var container = validationContext.Container;

            var context = new ValidationContext(
                instance: container ?? validationContext.Model ?? _emptyValidationContextInstance,
                serviceProvider: validationContext.ActionContext?.HttpContext?.RequestServices,
                items: null)
            {
                DisplayName = metadata.GetDisplayName(),
                MemberName = memberName
            };

            var result = Attribute.GetValidationResult(validationContext.Model, context);
            if (result != ValidationResult.Success)
            {
                string errorMessage;
                if (_stringLocalizer != null &&
                    !string.IsNullOrEmpty(Attribute.ErrorMessage) &&
                    string.IsNullOrEmpty(Attribute.ErrorMessageResourceName) &&
                    Attribute.ErrorMessageResourceType == null)
                {
                    errorMessage = GetErrorMessage(validationContext) ?? result.ErrorMessage;
                }
                else
                {
                    errorMessage = result.ErrorMessage;
                }

                var validationResults = new List<ModelValidationResult>();
                if (result.MemberNames != null)
                {
                    foreach (var resultMemberName in result.MemberNames)
                    {
                        // ModelValidationResult.MemberName is used by invoking validators (such as ModelValidator) to
                        // append construct the ModelKey for ModelStateDictionary. When validating at type level we
                        // want the returned MemberNames if specified (e.g. "person.Address.FirstName"). For property
                        // validation, the ModelKey can be constructed using the ModelMetadata and we should ignore
                        // MemberName (we don't want "person.Name.Name"). However the invoking validator does not have
                        // a way to distinguish between these two cases. Consequently we'll only set MemberName if this
                        // validation returns a MemberName that is different from the property being validated.
                        var newMemberName = string.Equals(resultMemberName, memberName, StringComparison.Ordinal) ?
                            null :
                            resultMemberName;
                        var validationResult = new ModelValidationResult(newMemberName, errorMessage);

                        validationResults.Add(validationResult);
                    }
                }

                if (validationResults.Count == 0)
                {
                    // result.MemberNames was null or empty.
                    validationResults.Add(new ModelValidationResult(memberName: null, message: errorMessage));
                }

                return validationResults;
            }

            return Enumerable.Empty<ModelValidationResult>();
        }

        private string GetErrorMessage(ModelValidationContextBase validationContext)
        {
            var adapter = _validationAttributeAdapterProvider.GetAttributeAdapter(Attribute, _stringLocalizer);
            return adapter?.GetErrorMessage(validationContext);
        }
    }
}
