// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Components.Sections;

internal sealed partial class SectionRegistry(
    ILoggerFactory loggerFactory,
    Func<IComponentRenderMode?, IComponentRenderMode?, bool>? areRenderModesCompatible)
{
    private readonly Dictionary<object, SectionOutlet> _subscribersByIdentifier = new();
    private readonly Dictionary<object, List<SectionContent>> _providersByIdentifier = new();

    private readonly ILogger _logger = loggerFactory.CreateLogger<SectionRegistry>();

    private HashSet<object>? _mismatchLoggedIdentifiers;

    public void AddProvider(object identifier, SectionContent provider, bool isDefaultProvider)
    {
        if (!_providersByIdentifier.TryGetValue(identifier, out var providers))
        {
            providers = new();
        }

        _providersByIdentifier.TryAdd(identifier, providers);

        if (isDefaultProvider)
        {
            providers.Insert(0, provider);
        }
        else
        {
            providers.Add(provider);
        }
    }

    public void RemoveProvider(object identifier, SectionContent provider)
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
            var contentProvider = GetCurrentProviderContentOrDefault(providers);
            NotifyContentChangedForSubscriber(identifier, contentProvider);
        }
    }

    public void Subscribe(object identifier, SectionOutlet subscriber)
    {
        if (_subscribersByIdentifier.ContainsKey(identifier))
        {
            throw new InvalidOperationException($"There is already a subscriber to the content with the given section ID '{identifier}'.");
        }

        // Notify the new subscriber with any existing content.
        var provider = GetCurrentProviderContentOrDefault(identifier);
        subscriber.ContentUpdated(provider);

        _subscribersByIdentifier.Add(identifier, subscriber);
    }

    public void Unsubscribe(object identifier)
    {
        if (!_subscribersByIdentifier.Remove(identifier))
        {
            throw new InvalidOperationException($"The subscriber with the given section ID '{identifier}' is already unsubscribed.");
        }

        // The section pair is no longer complete, so re-arm the mismatch diagnostic in case the outlet
        // and content are later reconnected in mismatched render modes.
        _mismatchLoggedIdentifiers?.Remove(identifier);
    }

    public void NotifyContentProviderChanged(object identifier, SectionContent provider)
    {
        if (!_providersByIdentifier.TryGetValue(identifier, out var providers))
        {
            throw new InvalidOperationException($"There are no content providers with the given section ID '{identifier}'.");
        }

        // We only notify content changed for subscribers when the content of the
        // most recently added provider changes.
        if (providers.Count != 0 && providers[^1] == provider)
        {
            NotifyContentChangedForSubscriber(identifier, provider);
        }
    }

    internal void ValidateContent(object identifier, SectionOutlet subscriber, SectionContent? provider)
    {
        ValidateRenderModeCompatibility(identifier, subscriber, provider);
        DetectRenderModeMismatch(identifier, subscriber, provider);
    }

    private void ValidateRenderModeCompatibility(object identifier, SectionOutlet? subscriber, SectionContent? provider)
    {
        // Framework-owned head/title sections use BuiltInSectionIdentifier and retain the existing warning behavior.
        // This validation is intentionally limited to application components sharing this renderer's registry.
        if (subscriber is null || provider is null || identifier is BuiltInSectionIdentifier)
        {
            return;
        }

        var outletRenderMode = subscriber.SectionRenderMode;
        var contentRenderMode = provider.SectionRenderMode;

        if (!RenderModesDiffer(outletRenderMode, contentRenderMode))
        {
            return;
        }

        var sectionId = DescribeIdentifier(identifier);
        throw new InvalidOperationException(
            $"The content provided to the section '{sectionId}' uses the render mode '{DescribeRenderMode(contentRenderMode)}', " +
            $"but the matching '{nameof(SectionOutlet)}' uses the render mode '{DescribeRenderMode(outletRenderMode)}'. " +
            $"A '{nameof(SectionContent)}' and its matching '{nameof(SectionOutlet)}' must use the same render mode, because " +
            $"the section content is rendered at the location of the '{nameof(SectionOutlet)}'. To fix this, ensure the " +
            $"'{nameof(SectionContent)}' and the '{nameof(SectionOutlet)}' for the section '{sectionId}' use the same render mode.");
    }

    private void DetectRenderModeMismatch(object identifier, SectionOutlet subscriber, SectionContent? provider)
    {
        if (provider is null)
        {
            _mismatchLoggedIdentifiers?.Remove(identifier);
            return;
        }

        var outletRenderMode = subscriber.SectionRenderMode;
        var contentRenderMode = provider.SectionRenderMode;

        if (!RenderModesDiffer(outletRenderMode, contentRenderMode))
        {
            _mismatchLoggedIdentifiers?.Remove(identifier);
            return;
        }

        if ((_mismatchLoggedIdentifiers ??= new()).Add(identifier))
        {
            Log.SectionRenderModeMismatch(_logger, DescribeIdentifier(identifier), DescribeRenderMode(outletRenderMode), DescribeRenderMode(contentRenderMode));
        }
    }

    private bool RenderModesDiffer(IComponentRenderMode? left, IComponentRenderMode? right)
        => areRenderModesCompatible is not null
            ? !areRenderModesCompatible(left, right)
            : left?.GetType() != right?.GetType();

    private static string DescribeRenderMode(IComponentRenderMode? renderMode)
        => renderMode is null ? "static server-side rendering" : renderMode.GetType().Name;

    private static string DescribeIdentifier(object identifier)
    {
        if (identifier is string id)
        {
            return id;
        }

        try
        {
            return identifier.ToString() ?? "(unknown)";
        }
        catch (Exception)
        {
            return "(unknown)";
        }
    }

    private static SectionContent? GetCurrentProviderContentOrDefault(List<SectionContent> providers)
        => providers.Count != 0
            ? providers[^1]
            : null;

    private SectionContent? GetCurrentProviderContentOrDefault(object identifier)
        => _providersByIdentifier.TryGetValue(identifier, out var existingList)
            ? GetCurrentProviderContentOrDefault(existingList)
            : null;

    private void NotifyContentChangedForSubscriber(object identifier, SectionContent? provider)
    {
        if (_subscribersByIdentifier.TryGetValue(identifier, out var subscriber))
        {
            subscriber.ContentUpdated(provider);
        }
    }

    private static partial class Log
    {
        [LoggerMessage(1, LogLevel.Warning, "The section with ID '{SectionId}' has its SectionOutlet in render mode '{OutletRenderMode}' and its SectionContent in render mode '{ContentRenderMode}'. Sections cannot connect across render mode boundaries, so the outlet will not display this content once the components become interactive.", EventName = "SectionRenderModeMismatch")]
        public static partial void SectionRenderModeMismatch(ILogger logger, string sectionId, string outletRenderMode, string contentRenderMode);
    }
}
