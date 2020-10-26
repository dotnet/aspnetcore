// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Rewrite.PatternSegments
{
    internal class IISServerVariableSegment : PatternSegment
    {
        private readonly string _variableName;
        private readonly Func<PatternSegment> _fallbackThunk;

        public IISServerVariableSegment(string variableName, Func<PatternSegment> fallbackThunk)
        {
            _variableName = variableName;
            _fallbackThunk = fallbackThunk;
        }

        public override string Evaluate(RewriteContext context, BackReferenceCollection ruleBackReferences, BackReferenceCollection conditionBackReferences)
        {
            return context.HttpContext.GetServerVariable(_variableName) ?? _fallbackThunk().Evaluate(context, ruleBackReferences, conditionBackReferences);
        }
    }
}
