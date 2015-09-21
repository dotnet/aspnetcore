// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNet.Mvc.ModelBinding.Validation
{
    /// <summary>
    /// This is a <see cref="ModelClientValidationRule"/> for numeric values.
    /// </summary>
    public class ModelClientValidationNumericRule : ModelClientValidationRule
    {
        private const string NumericValidationType = "number";

        /// <summary>
        /// Creates an instance of <see cref="ModelClientValidationNumericRule"/>
        /// with the given <paramref name="errorMessage"/>.
        /// </summary>
        /// <param name="errorMessage">The error message to be displayed.</param>
        public ModelClientValidationNumericRule(string errorMessage)
            : base(NumericValidationType, errorMessage)
        {
        }
    }
}
