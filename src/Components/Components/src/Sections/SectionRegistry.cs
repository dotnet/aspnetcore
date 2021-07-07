// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Microsoft.AspNetCore.Components.Sections
{
    internal class SectionRegistry
    {
        private static readonly ConditionalWeakTable<Dispatcher, SectionRegistry> _registries = new();

        private readonly Dictionary<string, List<Action<RenderFragment?>>> _subscriptions = new();

        public static SectionRegistry GetRegistry(RenderHandle renderHandle)
            => _registries.GetOrCreateValue(renderHandle.Dispatcher);

        public void Subscribe(string name, Action<RenderFragment?> callback)
        {
            if (!_subscriptions.TryGetValue(name, out var existingList))
            {
                existingList = new List<Action<RenderFragment?>>();
                _subscriptions.Add(name, existingList);
            }

            existingList.Add(callback);
        }

        public void Unsubscribe(string name, Action<RenderFragment?> callback)
        {
            if (_subscriptions.TryGetValue(name, out var existingList))
            {
                existingList.Remove(callback);
            }
        }

        public void SetContent(string name, RenderFragment? content)
        {
            if (_subscriptions.TryGetValue(name, out var existingList))
            {
                foreach (var callback in existingList)
                {
                    callback(content);
                }
            }
        }
    }
}
