// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Rewrite.Internal.PreActions
{
    public class ChangeCookiePreAction : PreAction
    {
        public ChangeCookiePreAction(string cookie)
        {
            // TODO
            throw new NotImplementedException(cookie);
        }

        public override void ApplyAction(HttpContext context, MatchResults ruleMatch, MatchResults condMatch)
        {
            // modify the cookies
           
        }
    }
}
