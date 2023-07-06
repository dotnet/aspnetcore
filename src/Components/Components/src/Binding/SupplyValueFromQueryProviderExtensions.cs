// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Components.Binding;

/// <summary>
/// Enables component parameters to be supplied from the query string with <see cref="SupplyParameterFromQueryAttribute"/>.
/// </summary>
public static class SupplyValueFromQueryProviderExtensions
{
    /// <summary>
    /// Enables component parameters to be supplied from the query string with <see cref="SupplyParameterFromQueryAttribute"/>.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/>.</param>
    /// <returns>The <see cref="IServiceCollection"/>.</returns>
    public static IServiceCollection AddSupplyValueFromQueryProvider(this IServiceCollection services)
    {
        return services.AddScoped<ICascadingValueSupplier, SupplyValueFromQueryProvider>();
    }

    private sealed class SupplyValueFromQueryProvider : ICascadingValueSupplier, IDisposable
    {
        private readonly QueryParameterValueSupplier _queryParameterValueSupplier = new();
        private readonly NavigationManager _navigationManager;
        private HashSet<ComponentState>? _subscribers;
        private bool _isSubscribedToLocationChanges;
        private bool _queryParametersMightHaveChanged = true;

        public SupplyValueFromQueryProvider(NavigationManager navigationManager)
        {
            _navigationManager = navigationManager;
        }

        public bool IsFixed => false;

        public bool CanSupplyValue(in CascadingParameterInfo parameterInfo)
            => parameterInfo.Attribute is SupplyParameterFromQueryAttribute;

        public object? GetCurrentValue(in CascadingParameterInfo parameterInfo)
        {
            if (_queryParametersMightHaveChanged)
            {
                _queryParametersMightHaveChanged = false;
                UpdateQueryParameters();
            }

            var queryParameterName = parameterInfo.Attribute.Name ?? parameterInfo.PropertyName;
            return _queryParameterValueSupplier.GetQueryParameterValue(parameterInfo.PropertyType, queryParameterName);
        }

        public void Subscribe(ComponentState subscriber, in CascadingParameterInfo parameterInfo)
        {
            SubscribeToLocationChanges();

            _subscribers ??= new();
            _subscribers.Add(subscriber);
        }

        public void Unsubscribe(ComponentState subscriber, in CascadingParameterInfo parameterInfo)
        {
            _subscribers!.Remove(subscriber);

            if (_subscribers.Count == 0)
            {
                UnsubscribeFromLocationChanges();
            }
        }

        private void UpdateQueryParameters()
        {
            var query = GetQueryString(_navigationManager.Uri);

            _queryParameterValueSupplier.ReadParametersFromQuery(query);

            static ReadOnlyMemory<char> GetQueryString(string url)
            {
                var queryStartPos = url.IndexOf('?');

                if (queryStartPos < 0)
                {
                    return default;
                }

                var queryEndPos = url.IndexOf('#', queryStartPos);
                return url.AsMemory(queryStartPos..(queryEndPos < 0 ? url.Length : queryEndPos));
            }
        }

        private void SubscribeToLocationChanges()
        {
            if (_isSubscribedToLocationChanges)
            {
                return;
            }

            _isSubscribedToLocationChanges = true;
            _queryParametersMightHaveChanged = true;
            _navigationManager.LocationChanged += OnLocationChanged;
        }

        private void UnsubscribeFromLocationChanges()
        {
            if (!_isSubscribedToLocationChanges)
            {
                return;
            }

            _isSubscribedToLocationChanges = false;
            _navigationManager.LocationChanged -= OnLocationChanged;
        }

        private void OnLocationChanged(object? sender, LocationChangedEventArgs args)
        {
            _queryParametersMightHaveChanged = true;

            if (_subscribers is not null)
            {
                foreach (var subscriber in _subscribers)
                {
                    subscriber.NotifyCascadingValueChanged(ParameterViewLifetime.Unbound);
                }
            }
        }

        public void Dispose()
        {
            UnsubscribeFromLocationChanges();
        }
    }
}
