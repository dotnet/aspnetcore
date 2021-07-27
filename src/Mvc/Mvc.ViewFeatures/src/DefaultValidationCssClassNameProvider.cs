// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Mvc.ViewFeatures
{
    /// <summary>
    /// Default implementation of <see cref="IValidationCssClassNameProvider"/>
    /// </summary>
    public class DefaultValidationCssClassNameProvider : IValidationCssClassNameProvider
    {
        /// <inheritdoc/>
        public string InputValid => "input-validation-valid";
        /// <inheritdoc/>
        public string InputInvalid => "input-validation-error";

        /// <inheritdoc/>
        public string MessageValid => "field-validation-valid";
        /// <inheritdoc/>
        public string MessageInvalid => "field-validation-error";

        /// <inheritdoc/>
        public string SummaryValid => "validation-summary-valid";
        /// <inheritdoc/>
        public string SummaryInvalid => "validation-summary-errors";
    }
}
