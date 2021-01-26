// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Mvc.ModelBinding.Validation
{
    /// <summary>
    /// The result of model validation.
    /// </summary>
    public class ModelValidationResult
    {
        /// <summary>
        /// Initializes a new instance of <see cref="ModelValidationResult"/>.
        /// </summary>
        /// <param name="memberName">The name of the entry on which validation was performed.</param>
        /// <param name="message">The validation message.</param>
        public ModelValidationResult(string memberName, string message)
        {
            MemberName = memberName ?? string.Empty;
            Message = message ?? string.Empty;
        }

        /// <summary>
        /// Gets the name of the entry on which validation was performed.
        /// </summary>
        public string MemberName { get; }

        /// <summary>
        /// Gets the validation message.
        /// </summary>
        public string Message { get; }
    }
}
