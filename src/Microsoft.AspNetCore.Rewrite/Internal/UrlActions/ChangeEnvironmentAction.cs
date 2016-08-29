// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Rewrite.Internal.UrlActions
{
    public class ChangeEnvironmentAction : UrlAction
    {
        public ChangeEnvironmentAction(string env)
        {
            // TODO
            throw new NotImplementedException();
        }

        public override void ApplyAction(RewriteContext context, MatchResults ruleMatch, MatchResults condMatch)
        {
            // Do stuff to modify the env
            throw new NotImplementedException();
        }
    }
}
