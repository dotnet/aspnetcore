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

            // The JumpTable implementation is chosen based on the number of entries. Right
            // now this is simple and minimal.
            if (_entries.Count == 0)
            {
                return new ZeroEntryJumpTable(DefaultDestination, ExitDestination);
            }

            if (_entries.Count == 1)
            {
                var entry = _entries[0];
                return new SingleEntryJumpTable(DefaultDestination, ExitDestination, entry.text, entry.destination);
            }

            if (_entries.Count < 10)
            {
                return new LinearSearchJumpTable(DefaultDestination, ExitDestination, _entries.ToArray());
            }

            return new DictionaryJumpTable(DefaultDestination, ExitDestination, _entries.ToArray());
        }
    }
}
