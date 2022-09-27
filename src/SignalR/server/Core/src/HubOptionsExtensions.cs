// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.SignalR.Internal;

namespace Microsoft.AspNetCore.SignalR;

/// <summary>
/// Methods to add <see cref="IHubFilter"/>'s to Hubs.
/// </summary>
public static class HubOptionsExtensions
{
    /// <summary>
    /// Adds an instance of an <see cref="IHubFilter"/> to the <see cref="HubOptions"/>.
    /// </summary>
    /// <param name="options">The options to add a filter to.</param>
    /// <param name="hubFilter">The filter instance to add to the options.</param>
    public static void AddFilter(this HubOptions options, IHubFilter hubFilter)
    {
        _ = options ?? throw new ArgumentNullException(nameof(options));
        _ = hubFilter ?? throw new ArgumentNullException(nameof(hubFilter));

        if (options.HubFilters == null)
        {
            options.HubFilters = new List<IHubFilter>();
        }

        options.HubFilters.Add(hubFilter);
    }

    /// <summary>
    /// Adds an <see cref="IHubFilter"/> type to the <see cref="HubOptions"/> that will be resolved via DI or type activated.
    /// </summary>
    /// <typeparam name="TFilter">The <see cref="IHubFilter"/> type that will be added to the options.</typeparam>
    /// <param name="options">The options to add a filter to.</param>
    public static void AddFilter<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TFilter>(this HubOptions options) where TFilter : IHubFilter
    {
        _ = options ?? throw new ArgumentNullException(nameof(options));

        options.AddFilter(typeof(TFilter));
    }

    /// <summary>
    /// Adds an <see cref="IHubFilter"/> type to the <see cref="HubOptions"/> that will be resolved via DI or type activated.
    /// </summary>
    /// <param name="options">The options to add a filter to.</param>
    /// <param name="filterType">The <see cref="IHubFilter"/> type that will be added to the options.</param>
    public static void AddFilter(this HubOptions options, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type filterType)
    {
        _ = options ?? throw new ArgumentNullException(nameof(options));
        _ = filterType ?? throw new ArgumentNullException(nameof(filterType));

        options.AddFilter(new HubFilterFactory(filterType));
    }
}
