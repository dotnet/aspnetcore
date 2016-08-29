// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Rewrite.Internal.UrlActions
{
    public class ChangeCookieAction : UrlAction
    {
        public ChangeCookieAction(string cookie)
        {
            // TODO
            throw new NotImplementedException(cookie);
        }

        public override void ApplyAction(RewriteContext context, MatchResults ruleMatch, MatchResults condMatch)
        {
            // modify the cookies

        }
    }
}
