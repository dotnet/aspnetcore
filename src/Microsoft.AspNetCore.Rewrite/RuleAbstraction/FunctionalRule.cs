// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Rewrite.RuleAbstraction
{
    public class FunctionalRule : Rule
    {
        public Func<UrlRewriteContext, RuleResult> OnApplyRule { get; set; }
        public Transformation OnCompletion { get; set; } = Transformation.Rewrite;
        public override RuleResult ApplyRule(UrlRewriteContext context) => OnApplyRule(context);
    }
}