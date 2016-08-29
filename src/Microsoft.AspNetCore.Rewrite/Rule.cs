// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Rewrite
{
    // make this public and doc comements
    // caller must set the context.Results field appropriately in rule.
    public abstract class Rule
    {
        public abstract void ApplyRule(RewriteContext context);
    }
}

