// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Diagnostics;

namespace Microsoft.AspNetCore.Routing.Matchers
{
    [DebuggerDisplay("{DebuggerToString(),nq}")]
    internal readonly struct DfaState
    {
        public readonly CandidateSet Candidates;
        public readonly JumpTable Transitions;

        public DfaState(CandidateSet candidates, JumpTable transitions)
        {
            Candidates = candidates;
            Transitions = transitions;
        }

        public string DebuggerToString()
        {
            return $"m: {Candidates.Candidates?.Length ?? 0}, j: ({Transitions?.DebuggerToString()})";
        }
    }
}
