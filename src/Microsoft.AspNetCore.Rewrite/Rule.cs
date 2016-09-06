// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Rewrite
{
    /// <summary>
    /// Represents an abstract rule.
    /// </summary>
    public abstract class Rule
    {
        /// <summary>
        /// Applies the rule.
        /// Implementations of ApplyRule should set the value for <see cref="RewriteContext.Result"/> 
        /// (defaults to <see cref="RuleTermination.Continue"/> )
        /// </summary>
        /// <param name="context"></param>
        public abstract void ApplyRule(RewriteContext context);
    }
}

