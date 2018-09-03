// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.AspNetCore.Routing.Matching
{
    internal class JumpTableBuilder
    {
        public static readonly int InvalidDestination = -1;

        private readonly List<(string text, int destination)> _entries = new List<(string text, int destination)>();

        // The destination state when none of the text entries match.
        public int DefaultDestination { get; set; } = InvalidDestination;

        // The destination state for a zero-length segment. This is a special
        // case because parameters don't match a zero-length segment.
        public int ExitDestination { get; set; } = InvalidDestination;

        public void AddEntry(string text, int destination)
        {
            _entries.Add((text, destination));
        }

        public JumpTable Build()
        {
            if (DefaultDestination == InvalidDestination)
            {
                var message = $"{nameof(DefaultDestination)} is not set. Please report this as a bug.";
                throw new InvalidOperationException(message);
            }

            if (ExitDestination == InvalidDestination)
            {
                var message = $"{nameof(ExitDestination)} is not set. Please report this as a bug.";
                throw new InvalidOperationException(message);
            }

            // The JumpTable implementation is chosen based on the number of entries.
            //
            // Basically the concerns that we're juggling here are that different implementations
            // make sense depending on the characteristics of the entries.
            //
            // On netcoreapp we support IL generation of optimized tries that is much faster
            // than anything we can do with string.Compare or dictionaries. However the IL emit
            // strategy requires us to produce a fallback jump table - see comments on the class.

            // We have an optimized fast path for zero entries since we don't have to
            // do any string comparisons.
            if (_entries.Count == 0)
            {
                return new ZeroEntryJumpTable(DefaultDestination, ExitDestination);
            }

            // The IL Emit jump table is not faster for a single entry
            if (_entries.Count == 1)
            {
                var entry = _entries[0];
                return new SingleEntryJumpTable(DefaultDestination, ExitDestination, entry.text, entry.destination);
            }

            // We choose a hard upper bound of 100 as the limit for when we switch to a dictionary
            // over a trie. The reason is that while the dictionary has a bigger constant factor,
            // it is O(1) vs a trie which is O(M * log(N)). Our perf testing shows that the trie
            // is better for ~90 entries based on all of Azure's route table. Anything above 100 edges
            // we'd consider to be a very very large node, and so while we don't think anyone will
            // have a node this large in practice, we want to make sure the performance is reasonable
            // for any size.
            //
            // Additionally if we're on 32bit, the scalability is worse, so switch to the dictionary at 50
            // entries.
            var threshold = IntPtr.Size == 8 ? 100 : 50;
            if (_entries.Count >= threshold)
            {
                return new DictionaryJumpTable(DefaultDestination, ExitDestination, _entries.ToArray());
            }

            // If we have more than a single string, the IL emit strategy is the fastest - but we need to decide
            // what do for the fallback case.
            JumpTable fallback;

            // Based on our testing a linear search is still faster than a dictionary at ten entries.
            if (_entries.Count <= 10)
            {
                fallback = new LinearSearchJumpTable(DefaultDestination, ExitDestination, _entries.ToArray());
            }
            else
            {
                fallback = new DictionaryJumpTable(DefaultDestination, ExitDestination, _entries.ToArray());
            }

#if IL_EMIT

            return new ILEmitTrieJumpTable(DefaultDestination, ExitDestination, _entries.ToArray(), vectorize: null, fallback);
#else
            return fallback;
#endif
        }
    }
}
