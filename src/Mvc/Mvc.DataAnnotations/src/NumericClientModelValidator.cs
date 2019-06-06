// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace Microsoft.AspNetCore.Mvc.DataAnnotations
{
    /// <summary>
    /// An implementation of <see cref="IClientModelValidator"/> that provides the rule for validating
    /// numeric types.
    /// </summary>
    internal class NumericClientModelValidator : IClientModelValidator
    {
        /// <inheritdoc />
        public void AddValidation(ClientModelValidationContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            MergeAttribute(context.Attributes, "data-val", "true");
            MergeAttribute(context.Attributes, "data-val-number", GetErrorMessage(context.ModelMetadata));
        }

        private static void MergeAttribute(IDictionary<string, string> attributes, string key, string value)
        {
            if (!attributes.ContainsKey(key))
            {
                attributes.Add(key, value);
            }
        }

        private string GetErrorMessage(ModelMetadata modelMetadata)
        {
            if (modelMetadata == null)
            {
                throw new ArgumentNullException(nameof(modelMetadata));
            }

            var messageProvider = modelMetadata.ModelBindingMessageProvider;
            var name = modelMetadata.DisplayName ?? modelMetadata.Name;
            if (name == null)
            {
                return messageProvider.NonPropertyValueMustBeANumberAccessor();
            }

            return messageProvider.ValueMustBeANumberAccessor(name);
        }
    }
}
