using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Microsoft.AspNetCore.Components.Web.Extensions
{
    public class HeadManager
    {
        private const string JsFunctionsPrefix = "_blazorHeadManager";

        private readonly IJSRuntime _jsRuntime;

        private readonly Dictionary<object, LinkedList<HeadElementBase>> stateMap =
            new Dictionary<object, LinkedList<HeadElementBase>>();

        public HeadManager(IJSRuntime jsRuntime)
        {
            _jsRuntime = jsRuntime;
        }

        internal async ValueTask NotifyChangedAsync(HeadElementBase headElement)
        {
            if (!stateMap.TryGetValue(headElement.ElementKey, out var states))
            {
                await headElement.SaveInitialStateAsync();

                states = new LinkedList<HeadElementBase>();

                // TODO: Save initial state, maybe add type for state tracking.

                stateMap.Add(headElement.ElementKey, states);
            }

            if (headElement.Node != states.Last)
            {
                if (headElement.Node.List == states)
                {
                    states.Remove(headElement.Node);
                }

                states.AddLast(headElement.Node);
            }

            await headElement.ApplyChangesAsync();

            PrintStateMap();
        }

        internal void NotifyDisposed(HeadElementBase headElement)
        {
            if (!stateMap.TryGetValue(headElement.ElementKey, out var states))
            {
                throw new InvalidOperationException(); // TOOD
            }

            Debug.Assert(headElement.Node!.List == states);

            bool needsToApplyChanges = headElement.Node == states.Last;

            states.Remove(headElement.Node);

            if (needsToApplyChanges)
            {
                if (states.Count > 0)
                {
                    states.Last!.Value.ApplyChangesAsync(); // TODO - async problems?
                }
                else
                {
                    // TODO: Apply initial state.
                }
            }

            PrintStateMap();
        }

        internal async ValueTask SetTitleAsync(string title)
        {
             await _jsRuntime.InvokeVoidAsync($"{JsFunctionsPrefix}.setTitle", title);
        }

        private void PrintStateMap()
        {
            foreach (var (stateKey, states) in stateMap)
            {
                Console.WriteLine($"{stateKey}: {string.Join(", ", states)}");
            }
        }
    }
}
