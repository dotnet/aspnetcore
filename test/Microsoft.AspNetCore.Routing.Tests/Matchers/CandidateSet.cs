// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Routing.Matchers
{
    // This is not yet fleshed out - this is a work in progress to 
    // unblock the benchmarks.
    internal class CandidateSet
    {
        public static readonly CandidateSet Empty = new CandidateSet(Array.Empty<Candidate>(), Array.Empty<int>());

        // The array of candidates.
        public readonly Candidate[] Candidates;

        // The number of groups.
        public readonly int GroupCount;

        // The array of groups. Groups define a contiguous sets of indices into
        // the candidates array.
        //
        // The groups array always contains N+1 entries where N is the number of groups.
        // The extra array entry is there to make indexing easier, so we can lookup the 'end'
        // of the last group without branching.
        //
        // Example:
        //    Group0: Candidates[0], Candidates[1]
        //    Group1: Candidates[2], Candidates[3], Candidates[4]
        //
        // The groups array would look like: { 0, 2, 5, }
        public readonly int[] Groups;

        public CandidateSet(Candidate[] candidates, int[] groups)
        {
            Candidates = candidates;
            Groups = groups;

            GroupCount = groups.Length == 0 ? 0 : groups.Length - 1;
        }

        // See description on Groups.
        public static int[] MakeGroups(int[] lengths)
        {
            var groups = new int[lengths.Length + 1];

            var sum = 0;
            for (var i = 0; i < lengths.Length; i++)
            {
                groups[i] = sum;
                sum += lengths[i];
            }

            groups[lengths.Length] = sum;

            return groups;
        }
    }
}
