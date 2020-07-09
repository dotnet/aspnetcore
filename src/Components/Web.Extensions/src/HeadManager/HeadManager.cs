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

        private readonly Dictionary<object, HeadElementChain> _elementChains = new Dictionary<object, HeadElementChain>();

        public HeadManager(IJSRuntime jsRuntime)
        {
            _jsRuntime = jsRuntime;
        }

        internal async ValueTask NotifyChangedAsync(HeadElementBase element)
        {
            if (!_elementChains.TryGetValue(element.ElementKey, out var chain))
            {
                var initialElementState = await element.GetInitialStateAsync();

                chain = new HeadElementChain(initialElementState);

                _elementChains.Add(element.ElementKey, chain);
            }

            await chain.ApplyChangeAsync(element);
        }

        internal async ValueTask NotifyDisposedAsync(HeadElementBase element)
        {
            if (_elementChains.TryGetValue(element.ElementKey, out var chain))
            {
                var isChainEmpty = await chain.DiscardChangeAsync(element);

                if (isChainEmpty)
                {
                    _elementChains.Remove(element.ElementKey);
                }
            }
            else
            {
                // This should never happen, but if it does, we'd like to know.
                Debug.Fail("Element key not found in state map.");
            }
        }

        internal ValueTask<string> GetTitleAsync()
        {
            return _jsRuntime.InvokeAsync<string>($"{JsFunctionsPrefix}.getTitle");
        }

        internal ValueTask<MetaElement> GetMetaElementByNameAsync(string name)
        {
            return _jsRuntime.InvokeAsync<MetaElement>($"{JsFunctionsPrefix}.getMetaElementByName", name);
        }

        internal async ValueTask SetTitleAsync(object title)
        {
             await _jsRuntime.InvokeVoidAsync($"{JsFunctionsPrefix}.setTitle", title);
        }

        internal async ValueTask SetMetaElementByNameAsync(string name, object metaElement)
        {
            await _jsRuntime.InvokeVoidAsync($"{JsFunctionsPrefix}.setMetaElementByName", name, metaElement);
        }
    }
}
