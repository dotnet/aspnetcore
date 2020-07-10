// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Components.Web.Extensions
{
    /// <summary>
    /// Tracks <see cref="HeadElementBase"/> instances whose effects override each other, organizes them
    /// into a priority queue, and applies changes from the correct <see cref="HeadElementBase"/>.
    /// </summary>
    internal class HeadElementChain
    {
        private readonly LinkedList<HeadElementBase> _priorityChain = new LinkedList<HeadElementBase>();

        private readonly object? _initialState;

        public HeadElementChain(object? initialState)
        {
            _initialState = initialState;
        }

        public async ValueTask ApplyChangeAsync(HeadElementBase newElement)
        {
            // Move the element to the end of the priority chain.
            if (newElement.Node != _priorityChain.Last)
            {
                if (newElement.Node.List == _priorityChain)
                {
                    _priorityChain.Remove(newElement.Node);
                }

                _priorityChain.AddLast(newElement.Node);
            }

            await newElement.ApplyAsync();
        }

        // Returns true if the chain is now empty.
        public async ValueTask<bool> DiscardChangeAsync(HeadElementBase discardedElement)
        {
            Debug.Assert(discardedElement.Node.List == _priorityChain);

            bool needsToApplyChanges = discardedElement.Node == _priorityChain.Last;

            _priorityChain.Remove(discardedElement.Node);

            if (needsToApplyChanges)
            {
                if (_priorityChain.Last == null)
                {
                    await discardedElement.ResetInitialStateAsync(_initialState);
                    return true;
                }

                await _priorityChain.Last.Value.ApplyAsync();
            }

            return false;
        }
    }
}
