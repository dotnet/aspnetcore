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
    /// Interface for specifying the CSS class names to use for validation
    /// </summary>
    public interface IValidationCssClassNameProvider
    {
        /// <summary>
        /// CSS class name for valid input validation
        /// </summary>
        public string InputValid { get; }

        /// <summary>
        /// CSS class name for invalid input validation
        /// </summary>
        public string InputInvalid { get; }

        /// <summary>
        /// CSS class name for valid field validation
        /// </summary>
        public string MessageValid { get; }

        /// <summary>
        /// CSS class name for invalid field validation
        /// </summary>
        public string MessageInvalid { get; }

        /// <summary>
        /// CSS class name for valid validation summary
        /// </summary>
        public string SummaryValid { get; }

        /// <summary>
        /// CSS class name for invalid validation summary
        /// </summary>
        public string SummaryInvalid { get; }
    }
}
