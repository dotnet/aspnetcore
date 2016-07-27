// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Rewrite.Internal.ModRewrite
{
    public class Condition
    {
        public Pattern TestStringSegments { get; }
        public ConditionExpression ConditionExpression { get; }
        public ConditionFlags Flags { get; }
        public Condition(Pattern testStringSegments, ConditionExpression conditionRegex, ConditionFlags flags)
        {
            TestStringSegments = testStringSegments;
            ConditionExpression = conditionRegex;
            Flags = flags;
        }
    }
}
