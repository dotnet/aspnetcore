// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Rewrite.Internal
{
    public class RuleResult
    {
        public static RuleResult Continue = new RuleResult { Result = RuleTerminiation.Continue };
        public static RuleResult ResponseComplete = new RuleResult { Result = RuleTerminiation.ResponseComplete };
        public static RuleResult StopRules = new RuleResult { Result = RuleTerminiation.StopRules };

        public RuleTerminiation Result { get; set; }
    }
}
