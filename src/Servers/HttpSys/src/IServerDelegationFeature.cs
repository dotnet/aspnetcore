// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Server.HttpSys;

/// <summary>
/// This exposes the creation of delegation rules on request queues owned by the server.
/// </summary>
public interface IServerDelegationFeature
{
    /// <summary>
    /// Create a delegation rule on request queue owned by the server.
    /// </summary>
    /// <param name="queueName">The name of the Http.Sys request queue.</param>
    /// <param name="urlPrefix">The URL of the Http.Sys Url Prefix.</param>
    /// <returns>
    /// Creates a <see cref="DelegationRule"/> that can used to delegate individual requests.
    /// </returns>
    DelegationRule CreateDelegationRule(string queueName, string urlPrefix);
}
