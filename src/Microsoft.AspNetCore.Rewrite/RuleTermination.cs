// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Rewrite
{
    /// <summary>
    /// An enum representing the result of a rule.
    /// </summary>
    public enum RuleTermination
    {
        /// <summary>
        /// Default value, continue applying rules.
        /// </summary>
        Continue,
        ///<summary>
        /// Redirect occured, should send back new rewritten url.
        /// </summary> 
        ResponseComplete,
        /// <summary>
        /// Stop applying rules and send context to the next middleware
        /// </summary>
        StopRules
    }
}
