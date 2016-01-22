// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.AspNet.Mvc.ModelBinding.Validation
{
    public class ModelClientValidationRule
    {
        private readonly Dictionary<string, object> _validationParameters =
            new Dictionary<string, object>(StringComparer.Ordinal);

        public ModelClientValidationRule(string errorMessage)
            : this(validationType: string.Empty, errorMessage: errorMessage)
        {
            if (errorMessage == null)
            {
                throw new ArgumentNullException(nameof(errorMessage));
            }
        }

        public ModelClientValidationRule(
            string validationType,
            string errorMessage)
        {
            if (validationType == null)
            {
                throw new ArgumentNullException(nameof(validationType));
            }

            if (errorMessage == null)
            {
                throw new ArgumentNullException(nameof(errorMessage));
            }

            ValidationType = validationType;
            ErrorMessage = errorMessage;
        }

        public string ErrorMessage { get; private set; }

        /// <summary>
        /// Identifier of the <see cref="ModelClientValidationRule"/>. If client-side validation is enabled, default
        /// validation attribute generator uses this <see cref="string"/> as part of the generated "data-val"
        /// attribute name. Must be unique in the set of enabled validation rules.
        /// </summary>
        public string ValidationType { get; private set; }

        public IDictionary<string, object> ValidationParameters
        {
            get { return _validationParameters; }
        }
    }
}
