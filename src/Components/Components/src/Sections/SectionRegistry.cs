// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Sections;

internal sealed class SectionRegistry
{
    private readonly Dictionary<string, ISectionContentSubscriber> _subscribersByName = new();
    private readonly Dictionary<string, List<ISectionContentProvider>> _providersByName = new();

    public void AddProvider(string name, ISectionContentProvider provider, bool isDefaultProvider)
    {
        if (!_providersByName.TryGetValue(name, out var providers))
        {
            providers = new();
            _providersByName.Add(name, providers);
        }

        if (isDefaultProvider)
        {
            providers.Insert(0, provider);
        }
        else
        {
            providers.Add(provider);
        }
    }

    public void RemoveProvider(string name, ISectionContentProvider provider)
    {
        if (!_providersByName.TryGetValue(name, out var providers))
        {
            throw new InvalidOperationException($"There are no content providers with the name '{name}'.");
        }

        var index = providers.LastIndexOf(provider);

        if (index < 0)
        {
            throw new InvalidOperationException($"The provider was not found in the providers list of name '{name}'.");
        }

        providers.RemoveAt(index);

        if (index == providers.Count)
        {
            // We just removed the most recently added provider, meaning we need to change
            // the current content to that of second most recently added provider.
            var content = GetCurrentProviderContentOrDefault(providers);
            NotifyContentChangedForSubscriber(name, content);
        }
    }

    public void Subscribe(string name, ISectionContentSubscriber subscriber)
    {
        if (_subscribersByName.ContainsKey(name))
        {
            throw new InvalidOperationException($"There is already a subscriber to the content '{name}'.");
        }

        // Notify the new subscriber with any existing content.
        var content = GetCurrentProviderContentOrDefault(name);
        subscriber.ContentChanged(content);

        _subscribersByName.Add(name, subscriber);
    }

    public void Unsubscribe(string name)
    {
        if (!_subscribersByName.Remove(name))
        {
            throw new InvalidOperationException($"The subscriber with name '{name}' is already unsubscribed.");
        }
    }

    public void NotifyContentChanged(string name, ISectionContentProvider provider)
    {
        if (!_providersByName.TryGetValue(name, out var providers))
        {
            throw new InvalidOperationException($"There are no content providers with the name '{name}'.");
        }

        // We only notify content changed for subscribers when the content of the
        // most recently added provider changes.
        if (providers.Count != 0 && providers[^1] == provider)
        {
            NotifyContentChangedForSubscriber(name, provider.Content);
        }
    }

    private static RenderFragment? GetCurrentProviderContentOrDefault(List<ISectionContentProvider> providers)
        => providers.Count != 0
            ? providers[^1].Content
            : null;

    private RenderFragment? GetCurrentProviderContentOrDefault(string name)
        => _providersByName.TryGetValue(name, out var existingList)
            ? GetCurrentProviderContentOrDefault(existingList)
            : null;

    private void NotifyContentChangedForSubscriber(string name, RenderFragment? content)
    {
        if (_subscribersByName.TryGetValue(name, out var subscriber))
        {
            subscriber.ContentChanged(content);
        }
    }
}
