// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Sections;

internal sealed class SectionRegistry
{
    private readonly Dictionary<object, ISectionContentSubscriber> _subscribersByIdentifier = new();
    private readonly Dictionary<object, List<ISectionContentProvider>> _providersByIdentifier = new();

    public void AddProvider(object identifier, ISectionContentProvider provider, bool isDefaultProvider)
    {
        if (!_providersByIdentifier.TryGetValue(identifier, out var providers))
        {
            providers = new();
            _providersByIdentifier.Add(identifier, providers);
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

    public void RemoveProvider(object identifier, ISectionContentProvider provider)
    {
        if (!_providersByIdentifier.TryGetValue(identifier, out var providers))
        {
            throw new InvalidOperationException($"There are no content providers with the given section ID '{identifier}'.");
        }

        var index = providers.LastIndexOf(provider);

        if (index < 0)
        {
            throw new InvalidOperationException($"The provider was not found in the providers list of the given section ID '{identifier}'.");
        }

        providers.RemoveAt(index);

        if (index == providers.Count)
        {
            // We just removed the most recently added provider, meaning we need to change
            // the current content to that of second most recently added provider.
            var content = GetCurrentProviderContentOrDefault(providers);
            NotifyContentChangedForSubscriber(identifier, content);
        }
    }

    public void Subscribe(object identifier, ISectionContentSubscriber subscriber)
    {
        if (_subscribersByIdentifier.ContainsKey(identifier))
        {
            throw new InvalidOperationException($"There is already a subscriber to the content with the given section ID '{identifier}'.");
        }

        // Notify the new subscriber with any existing content.
        var content = GetCurrentProviderContentOrDefault(identifier);
        subscriber.ContentChanged(content);

        _subscribersByIdentifier.Add(identifier, subscriber);
    }

    public void Unsubscribe(object identifier)
    {
        if (!_subscribersByIdentifier.Remove(identifier))
        {
            throw new InvalidOperationException($"The subscriber with the given section ID '{identifier}' is already unsubscribed.");
        }
    }

    public void NotifyContentChanged(object identifier, ISectionContentProvider provider)
    {
        if (!_providersByIdentifier.TryGetValue(identifier, out var providers))
        {
            throw new InvalidOperationException($"There are no content providers with the given section ID '{identifier}'.");
        }

        // We only notify content changed for subscribers when the content of the
        // most recently added provider changes.
        if (providers.Count != 0 && providers[^1] == provider)
        {
            NotifyContentChangedForSubscriber(identifier, provider.Content);
        }
    }

    private static RenderFragment? GetCurrentProviderContentOrDefault(List<ISectionContentProvider> providers)
        => providers.Count != 0
            ? providers[^1].Content
            : null;

    private RenderFragment? GetCurrentProviderContentOrDefault(object identifier)
        => _providersByIdentifier.TryGetValue(identifier, out var existingList)
            ? GetCurrentProviderContentOrDefault(existingList)
            : null;

    private void NotifyContentChangedForSubscriber(object identifier, RenderFragment? content)
    {
        if (_subscribersByIdentifier.TryGetValue(identifier, out var subscriber))
        {
            subscriber.ContentChanged(content);
        }
    }
}
