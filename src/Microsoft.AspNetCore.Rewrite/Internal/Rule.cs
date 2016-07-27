// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Rewrite.Internal
{
    public abstract class Rule
    {
        public abstract RuleResult ApplyRule(RewriteContext context);
    }
}

