// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.AspNetCore.Rewrite
{
    internal class DelegateRule : IRule
    {
        private readonly Action<RewriteContext> _onApplyRule;

        public DelegateRule(Action<RewriteContext> onApplyRule)
        {
            _onApplyRule = onApplyRule;
        }
        public void ApplyRule(RewriteContext context) => _onApplyRule(context);
    }
}