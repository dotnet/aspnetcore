// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.HttpSys;

internal class ServerDelegationPropertyFeature : IServerDelegationFeature
{
    private readonly ILogger _logger;
    private readonly UrlGroup _urlGroup;

    public ServerDelegationPropertyFeature(UrlGroup urlGroup, ILogger logger)
    {
        _urlGroup = urlGroup ?? throw new ArgumentNullException(nameof(urlGroup));
        _logger = logger;
    }

    public DelegationRule CreateDelegationRule(string queueName, string uri)
    {
        var rule = new DelegationRule(_urlGroup, queueName, uri, _logger);
        _urlGroup.SetDelegationProperty(rule.Queue);
        return rule;
    }
}
