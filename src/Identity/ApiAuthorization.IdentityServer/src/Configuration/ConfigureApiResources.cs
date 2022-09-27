// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Duende.IdentityServer.Models;
using Microsoft.AspNetCore.ApiAuthorization.IdentityServer;
using Microsoft.AspNetCore.ApiAuthorization.IdentityServer.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.DependencyInjection;

internal sealed class ConfigureApiResources : IConfigureOptions<ApiAuthorizationOptions>
{
    private const char ScopesSeparator = ' ';

    private readonly IConfiguration _configuration;
    private readonly ILogger<ConfigureApiResources> _logger;
    private readonly IIdentityServerJwtDescriptor _localApiDescriptor;

    public ConfigureApiResources(
        IConfiguration configuration,
        IIdentityServerJwtDescriptor localApiDescriptor,
        ILogger<ConfigureApiResources> logger)
    {
        _configuration = configuration;
        _localApiDescriptor = localApiDescriptor;
        _logger = logger;
    }

    public void Configure(ApiAuthorizationOptions options)
    {
        var resources = GetApiResources();
        foreach (var resource in resources)
        {
            options.ApiResources.Add(resource);
        }
    }

    internal IEnumerable<ApiResource> GetApiResources()
    {
        var data = _configuration
            .Get<Dictionary<string, ResourceDefinition>>();

        if (data != null)
        {
            foreach (var kvp in data)
            {
                _logger.LogInformation(LoggerEventIds.ConfiguringAPIResource, "Configuring API resource '{ApiResourceName}'.", kvp.Key);
                yield return GetResource(kvp.Key, kvp.Value);
            }
        }

        var localResources = _localApiDescriptor?.GetResourceDefinitions();
        if (localResources != null)
        {
            foreach (var kvp in localResources)
            {
                _logger.LogInformation(LoggerEventIds.ConfiguringLocalAPIResource, "Configuring local API resource '{ApiResourceName}'.", kvp.Key);
                yield return GetResource(kvp.Key, kvp.Value);
            }
        }
    }

    public static ApiResource GetResource(string name, ResourceDefinition definition)
    {
        switch (definition.Profile)
        {
            case ApplicationProfiles.API:
                return GetAPI(name, definition);
            case ApplicationProfiles.IdentityServerJwt:
                return GetLocalAPI(name, definition);
            default:
                throw new InvalidOperationException($"Type '{definition.Profile}' is not supported.");
        }
    }

    private static string[] ParseScopes(string scopes)
    {
        if (scopes == null)
        {
            return null;
        }

        var parsed = scopes.Split(ScopesSeparator, StringSplitOptions.RemoveEmptyEntries);
        if (parsed.Length == 0)
        {
            return null;
        }

        return parsed;
    }

    private static ApiResource GetAPI(string name, ResourceDefinition definition) =>
        ApiResourceBuilder.ApiResource(name)
            .FromConfiguration()
            .WithAllowedClients(ApplicationProfilesPropertyValues.AllowAllApplications)
            .ReplaceScopes(ParseScopes(definition.Scopes) ?? new[] { name })
            .Build();

    private static ApiResource GetLocalAPI(string name, ResourceDefinition definition) =>
        ApiResourceBuilder.IdentityServerJwt(name)
            .FromConfiguration()
            .WithAllowedClients(ApplicationProfilesPropertyValues.AllowAllApplications)
            .ReplaceScopes(ParseScopes(definition.Scopes) ?? new[] { name })
            .Build();
}
