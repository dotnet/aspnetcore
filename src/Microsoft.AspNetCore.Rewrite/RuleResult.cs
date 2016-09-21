// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Rewrite
{
    /// <summary>
    /// An enum representing the result of a rule.
    /// </summary>
    public enum RuleResult
    {
        /// <summary>
        /// Default value, continue applying rules.
        /// </summary>
        ContinueRules,
        ///<summary>
        /// The rule ended the request by providing a response.
        /// </summary>
        EndResponse,
        /// <summary>
        /// Stop applying rules and send context to the next middleware
        /// </summary>
        SkipRemainingRules
    }
}
