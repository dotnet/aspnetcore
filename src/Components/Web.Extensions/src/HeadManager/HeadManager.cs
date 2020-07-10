// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Microsoft.AspNetCore.Components.Web.Extensions
{
    /// <summary>
    /// A service that manages manipulation of the HTML head element.
    /// </summary>
    public class HeadManager
    {
        private const string JsFunctionsPrefix = "_blazorHeadManager";

        private readonly IJSRuntime _jsRuntime;

        private readonly Dictionary<object, HeadElementChain> _elementChains = new Dictionary<object, HeadElementChain>();

        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1);

        private readonly ConcurrentQueue<TaskCompletionSource> _tcsQueue = new ConcurrentQueue<TaskCompletionSource>();

        /// <summary>
        /// Creates a new <see cref="HeadManager"/> instance.
        /// </summary>
        /// <param name="jsRuntime">The <see cref="IJSRuntime" /> to use.</param>
        public HeadManager(IJSRuntime jsRuntime)
        {
            _jsRuntime = jsRuntime;
        }

        internal async ValueTask NotifyChangedAsync(HeadElementBase element)
        {
            // Wait for previous changes to be applied before proceeding.
            await AcquireLock();

            if (!_elementChains.TryGetValue(element.ElementKey, out var chain))
            {
                // No changes to the target element are being tracked - save the initial element state.
                var initialElementState = await element.GetInitialStateAsync();

                chain = new HeadElementChain(initialElementState);

                _elementChains.Add(element.ElementKey, chain);
            }

            await chain.ApplyChangeAsync(element);

            ReleaseLock();
        }

        internal async ValueTask NotifyDisposedAsync(HeadElementBase element)
        {
            // Wait for previous changes to be applied before proceeding.
            await AcquireLock();

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

            ReleaseLock();
        }

        internal ValueTask<string> GetTitleAsync()
        {
            return _jsRuntime.InvokeAsync<string>($"{JsFunctionsPrefix}.getTitle");
        }

        internal async ValueTask SetTitleAsync(object title)
        {
             await _jsRuntime.InvokeVoidAsync($"{JsFunctionsPrefix}.setTitle", title);
        }

        internal ValueTask<MetaElementState> GetMetaElementAsync(MetaElementKey key)
        {
            return _jsRuntime.InvokeAsync<MetaElementState>($"{JsFunctionsPrefix}.getMetaElement", key);
        }

        internal async ValueTask SetMetaElementAsync(MetaElementKey key, object metaElement)
        {
            await _jsRuntime.InvokeVoidAsync($"{JsFunctionsPrefix}.setMetaElement", key, metaElement);
        }

        private Task AcquireLock()
        {
            // Add a new TCS for the current acquisition.
            var tcs = new TaskCompletionSource();
            _tcsQueue.Enqueue(tcs);

            _semaphore.WaitAsync().ContinueWith(t =>
            {
                if (_tcsQueue.TryDequeue(out var completedTcs))
                {
                    // Allow the next task in the queue to proceed.
                    completedTcs.SetResult();
                }
            });

            return tcs.Task;
        }

        private void ReleaseLock()
        {
            _semaphore.Release();
        }
    }
}
