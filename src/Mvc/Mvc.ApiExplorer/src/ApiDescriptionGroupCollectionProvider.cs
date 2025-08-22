// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Mvc.ApiExplorer;

/// <inheritdoc />
public partial class ApiDescriptionGroupCollectionProvider : IApiDescriptionGroupCollectionProvider
{
    private readonly IActionDescriptorCollectionProvider _actionDescriptorCollectionProvider;
    private readonly IApiDescriptionProvider[] _apiDescriptionProviders;

    private ApiDescriptionGroupCollection? _apiDescriptionGroups;
    private readonly ILogger? _logger;

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
        _apiDescriptionProviders = [.. apiDescriptionProviders.OrderBy(item => item.Order)];
    }

    internal ApiDescriptionGroupCollectionProvider(
        IActionDescriptorCollectionProvider actionDescriptorCollectionProvider,
        IEnumerable<IApiDescriptionProvider> apiDescriptionProviders,
        ILoggerFactory loggerFactory) : this(actionDescriptorCollectionProvider, apiDescriptionProviders)
    {
        _logger = loggerFactory.CreateLogger<ApiDescriptionGroupCollectionProvider>();
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
            if (_logger is not null)
            {
                Log.ApiDescriptionProviderExecuting(_logger, provider.GetType().Name, provider.GetType().Assembly.GetName().Name, provider.GetType().Assembly.GetName().Version?.ToString());
            }
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

    private static partial class Log
    {
        [LoggerMessage(2, LogLevel.Debug, "Executing API description provider '{ProviderName}' from assembly {ProviderAssembly} v{AssemblyVersion}.", EventName = "ApiDescriptionProviderExecuting")]
        public static partial void ApiDescriptionProviderExecuting(ILogger logger, string providerName, string? providerAssembly, string? assemblyVersion);
    }
}
