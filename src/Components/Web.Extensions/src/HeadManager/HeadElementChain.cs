using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Components.Web.Extensions
{
    internal class HeadElementChain
    {
        private readonly LinkedList<HeadElementBase> _priorityChain = new LinkedList<HeadElementBase>();

        private readonly object _initialState;

        public HeadElementChain(object initialState)
        {
            _initialState = initialState;
        }

        public async ValueTask ApplyChangeAsync(HeadElementBase newElement)
        {
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
