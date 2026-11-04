// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Text;

namespace Microsoft.AspNetCore.Routing.Matching;

internal static class JumpTableBuilder
{
    public const int InvalidDestination = -1;

    public static JumpTable Build(int defaultDestination, int exitDestination, (string text, int destination)[] pathEntries)
    {
        if (defaultDestination == InvalidDestination)
        {
            var message = $"{nameof(defaultDestination)} is not set. Please report this as a bug.";
            throw new InvalidOperationException(message);
        }

        if (exitDestination == InvalidDestination)
        {
            var message = $"{nameof(exitDestination)} is not set. Please report this as a bug.";
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
        if (pathEntries == null || pathEntries.Length == 0)
        {
            return new ZeroEntryJumpTable(defaultDestination, exitDestination);
        }

        // The IL Emit jump table is not faster for a single entry - but we have an optimized version when all text
        // is ASCII
        if (pathEntries.Length == 1 && Ascii.IsValid(pathEntries[0].text))
        {
            var entry = pathEntries[0];
            return new SingleEntryAsciiJumpTable(defaultDestination, exitDestination, entry.text, entry.destination);
        }

        // We have a fallback that works for non-ASCII
        if (pathEntries.Length == 1)
        {
            var entry = pathEntries[0];
            return new SingleEntryJumpTable(defaultDestination, exitDestination, entry.text, entry.destination);
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
        if (pathEntries.Length >= threshold)
        {
            return new DictionaryJumpTable(defaultDestination, exitDestination, pathEntries);
        }

        // If we have more than a single string, the IL emit strategy is the fastest - but we need to decide
        // what do for the fallback case.
        JumpTable fallback;

        // Based on our testing a linear search is still faster than a dictionary at ten entries.
        if (pathEntries.Length <= 10)
        {
            fallback = new LinearSearchJumpTable(defaultDestination, exitDestination, pathEntries);
        }
        else
        {
            fallback = new DictionaryJumpTable(defaultDestination, exitDestination, pathEntries);
        }

        // Use the ILEmitTrieJumpTable if the IL is going to be compiled (not interpreted)
        return MakeILEmitTrieJumpTableIfSupported(defaultDestination, exitDestination, pathEntries, fallback);

        static JumpTable MakeILEmitTrieJumpTableIfSupported(int defaultDestination, int exitDestination, (string text, int destination)[] pathEntries, JumpTable fallback)
        {
            // ILEmitTrieJumpTable use IL emit to generate a custom, high-performance jump table.
            // EL emit requires IsDynamicCodeCompiled to be true. Fallback to another jump table implementation if not available.
            return RuntimeFeature.IsDynamicCodeCompiled
                ? new ILEmitTrieJumpTable(defaultDestination, exitDestination, pathEntries, vectorize: null, fallback)
                : fallback;
        }
    }
}
