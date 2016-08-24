// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Rewrite
{
    public class RuleResult
    {
        public static RuleResult Continue = new RuleResult { Result = RuleTermination.Continue };
        public static RuleResult ResponseComplete = new RuleResult { Result = RuleTermination.ResponseComplete };
        public static RuleResult StopRules = new RuleResult { Result = RuleTermination.StopRules };

        public RuleTermination Result { get; set; }
    }
}
