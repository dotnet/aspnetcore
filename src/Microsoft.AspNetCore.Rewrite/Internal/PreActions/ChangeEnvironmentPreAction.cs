// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Rewrite.Internal.PreActions
{
    public class ChangeEnvironmentPreAction : PreAction
    {
        public ChangeEnvironmentPreAction(string env)
        {
            // TODO
            throw new NotImplementedException();
        }

        public override void ApplyAction(HttpContext context, MatchResults ruleMatch, MatchResults condMatch)
        {
            // Do stuff to modify the env
            throw new NotImplementedException();
        }
    }
}
