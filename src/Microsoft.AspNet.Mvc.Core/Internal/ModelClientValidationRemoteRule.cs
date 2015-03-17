// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Mvc.ModelBinding.Validation;

namespace Microsoft.AspNet.Mvc.Internal
{
    /// <summary>
    /// <see cref="ModelClientValidationRule"/> containing information for HTML attribute generation in fields a
    /// <see cref="RemoteAttribute"/> targets.
    /// </summary>
    public class ModelClientValidationRemoteRule : ModelClientValidationRule
    {
        private const string RemoteValidationType = "remote";
        private const string AdditionalFieldsValidationParameter = "additionalfields";
        private const string TypeValidationParameter = "type";
        private const string UrlValidationParameter = "url";

        /// <summary>
        /// Initializes a new instance of the <see cref="ModelClientValidationRemoteRule"/> class.
        /// </summary>
        /// <param name="errorMessage">Error message client should display when validation fails.</param>
        /// <param name="url">URL where client should send a validation request.</param>
        /// <param name="httpMethod">
        /// HTTP method (<c>"GET"</c> or <c>"POST"</c>) client should use when sending a validation request.
        /// </param>
        /// <param name="additionalFields">
        /// Comma-separated names of fields the client should include in a validation request.
        /// </param>
        public ModelClientValidationRemoteRule(
            string errorMessage,
            string url,
            string httpMethod,
            string additionalFields)
            : base(validationType: RemoteValidationType, errorMessage: errorMessage)
        {
            ValidationParameters[UrlValidationParameter] = url;
            if (!string.IsNullOrEmpty(httpMethod))
            {
                ValidationParameters[TypeValidationParameter] = httpMethod;
            }

            ValidationParameters[AdditionalFieldsValidationParameter] = additionalFields;
        }
    }
}
