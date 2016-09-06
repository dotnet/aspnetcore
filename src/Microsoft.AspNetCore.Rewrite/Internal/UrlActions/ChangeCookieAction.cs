// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Rewrite.Internal.UrlActions
{
    public class ChangeCookieAction : UrlAction
    {
        public ChangeCookieAction(string cookie)
        {
            // TODO
            throw new NotImplementedException("Changing the cookie is not implemented");
        }

        public override void ApplyAction(RewriteContext context, MatchResults ruleMatch, MatchResults condMatch)
        {
            // modify the cookies
            throw new NotImplementedException("Changing the cookie is not implemented");
        }
    }
}
