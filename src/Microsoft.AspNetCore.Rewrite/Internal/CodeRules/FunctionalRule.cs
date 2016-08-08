// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Rewrite.Internal.CodeRules
{
    public class FunctionalRule : Rule
    {
        public Func<RewriteContext, RuleResult> OnApplyRule { get; set; }
        public override RuleResult ApplyRule(RewriteContext context) => OnApplyRule(context);
    }
}