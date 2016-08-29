// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Rewrite.Internal.CodeRules
{
    public class DelegateRule : Rule
    {
        private readonly Action<RewriteContext> _onApplyRule;

        public DelegateRule(Action<RewriteContext> onApplyRule)
        {
            _onApplyRule = onApplyRule;
        }
        public override void ApplyRule(RewriteContext context) => _onApplyRule(context);
    }
}