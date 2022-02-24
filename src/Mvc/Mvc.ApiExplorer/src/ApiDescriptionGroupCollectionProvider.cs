// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using Microsoft.AspNetCore.Mvc.Infrastructure;

namespace Microsoft.AspNetCore.Mvc.ApiExplorer;

/// <inheritdoc />
public class ApiDescriptionGroupCollectionProvider : IApiDescriptionGroupCollectionProvider
{
    private readonly IActionDescriptorCollectionProvider _actionDescriptorCollectionProvider;
    private readonly IApiDescriptionProvider[] _apiDescriptionProviders;

    private ApiDescriptionGroupCollection? _apiDescriptionGroups;

    /// <summary>
    /// Creates a new instance of <see cref="ApiDescriptionGroupCollectionProvider"/>.
    /// </summary>
    /// <param name="actionDescriptorCollectionProvider">
    /// The <see cref="IActionDescriptorCollectionProvider"/>.
    /// </param>
    /// <param name="apiDescriptionProviders">
    /// The <see cref="IEnumerable{IApiDescriptionProvider}"/>.
    /// </param>
    public ApiDescriptionGroupCollectionProvider(
        IActionDescriptorCollectionProvider actionDescriptorCollectionProvider,
        IEnumerable<IApiDescriptionProvider> apiDescriptionProviders)
    {
        _actionDescriptorCollectionProvider = actionDescriptorCollectionProvider;
        _apiDescriptionProviders = apiDescriptionProviders.OrderBy(item => item.Order).ToArray();
    }

    /// <inheritdoc />
    public ApiDescriptionGroupCollection ApiDescriptionGroups
    {
        get
        {
            var actionDescriptors = _actionDescriptorCollectionProvider.ActionDescriptors;
            if (_apiDescriptionGroups == null || _apiDescriptionGroups.Version != actionDescriptors.Version)
            {
                _apiDescriptionGroups = GetCollection(actionDescriptors);
            }

            return _apiDescriptionGroups;
        }
    }

    private ApiDescriptionGroupCollection GetCollection(ActionDescriptorCollection actionDescriptors)
    {
        var context = new ApiDescriptionProviderContext(actionDescriptors.Items);

        foreach (var provider in _apiDescriptionProviders)
        {
            provider.OnProvidersExecuting(context);
        }

        for (var i = _apiDescriptionProviders.Length - 1; i >= 0; i--)
        {
            _apiDescriptionProviders[i].OnProvidersExecuted(context);
        }

        var groups = context.Results
            .GroupBy(d => d.GroupName)
            .Select(g => new ApiDescriptionGroup(g.Key, g.ToArray()))
            .ToArray();

        return new ApiDescriptionGroupCollection(groups, actionDescriptors.Version);
    }
}
