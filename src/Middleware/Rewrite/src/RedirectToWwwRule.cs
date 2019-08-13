// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Rewrite
{
    internal class RedirectToWwwRule : RedirectToWwwRuleBase
    {
        public RedirectToWwwRule(int statusCode)
            : base(statusCode) { }

        public RedirectToWwwRule(int statusCode, params string[] domains)
            : base(statusCode, domains) { }

        protected override bool RedirectToWww => true;
    }
}
