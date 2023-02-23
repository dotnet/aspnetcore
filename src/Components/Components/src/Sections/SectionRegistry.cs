// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Sections;

internal sealed class SectionRegistry
{
    private readonly Dictionary<object, ISectionContentSubscriber> _subscribersBySectionId = new();
    private readonly Dictionary<object, List<ISectionContentProvider>> _providersBySectionId = new();

    public void AddProvider(object sectionId, ISectionContentProvider provider, bool isDefaultProvider)
    {
        if (!_providersBySectionId.TryGetValue(sectionId, out var providers))
        {
            providers = new();
            _providersBySectionId.Add(sectionId, providers);
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

    public void RemoveProvider(object sectionId, ISectionContentProvider provider)
    {
        if (!_providersBySectionId.TryGetValue(sectionId, out var providers))
        {
            throw new InvalidOperationException($"There are no content providers with the given SectionId.");
        }

        var index = providers.LastIndexOf(provider);

        if (index < 0)
        {
            throw new InvalidOperationException($"The provider was not found in the providers list of the given SectionId.");
        }

        providers.RemoveAt(index);

        if (index == providers.Count)
        {
            // We just removed the most recently added provider, meaning we need to change
            // the current content to that of second most recently added provider.
            var content = GetCurrentProviderContentOrDefault(providers);
            NotifyContentChangedForSubscriber(sectionId, content);
        }
    }

    public void Subscribe(object sectionId, ISectionContentSubscriber subscriber)
    {
        if (_subscribersBySectionId.ContainsKey(sectionId))
        {
            throw new InvalidOperationException($"There is already a subscriber to the content with the given SectionId.");
        }

        // Notify the new subscriber with any existing content.
        var content = GetCurrentProviderContentOrDefault(sectionId);
        subscriber.ContentChanged(content);

        _subscribersBySectionId.Add(sectionId, subscriber);
    }

    public void Unsubscribe(object sectionId)
    {
        if (!_subscribersBySectionId.Remove(sectionId))
        {
            throw new InvalidOperationException($"The subscriber with the given SectionId is already unsubscribed.");
        }
    }

    public void NotifyContentChanged(object sectionId, ISectionContentProvider provider)
    {
        if (!_providersBySectionId.TryGetValue(sectionId, out var providers))
        {
            throw new InvalidOperationException($"There are no content providers with the given SectionId.");
        }

        // We only notify content changed for subscribers when the content of the
        // most recently added provider changes.
        if (providers.Count != 0 && providers[^1] == provider)
        {
            NotifyContentChangedForSubscriber(sectionId, provider.Content);
        }
    }

    private static RenderFragment? GetCurrentProviderContentOrDefault(List<ISectionContentProvider> providers)
        => providers.Count != 0
            ? providers[^1].Content
            : null;

    private RenderFragment? GetCurrentProviderContentOrDefault(object sectionId)
        => _providersBySectionId.TryGetValue(sectionId, out var existingList)
            ? GetCurrentProviderContentOrDefault(existingList)
            : null;

    private void NotifyContentChangedForSubscriber(object sectionId, RenderFragment? content)
    {
        if (_subscribersBySectionId.TryGetValue(sectionId, out var subscriber))
        {
            subscriber.ContentChanged(content);
        }
    }
}
