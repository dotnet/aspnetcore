// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
namespace Microsoft.AspNetCore.SignalR;

/// <summary>
/// 
/// </summary>
public interface IHubDefinition
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="name"></param>
    /// <param name="handler"></param>
    /// <param name="parameterTypes"></param>
    void AddHubMethod(string name, HubInvocationDelegate handler);
    /// <summary>
    /// 
    /// </summary>
    /// <param name="initializer"></param>
    void SetHubInitializer(HubInitializerDelegate initializer);
}

/// <summary>
/// 
/// </summary>
public interface IStreamTracker
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="name"></param>
    /// <param name="writeStreamItem"></param>
    /// <param name="completeStream"></param>
    void AddStream(string name, System.Func<object, ValueTask> writeStreamItem, System.Func<System.Exception, bool> completeStream);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="name"></param>
    void RemoveStream(string name);
}

/// <summary>
/// 
/// </summary>
/// <param name="hub"></param>
/// <param name="connection"></param>
/// <param name="streamTracker"></param>
/// <param name="message"></param>
/// <param name="cancellationToken"></param>
/// <returns></returns>
public delegate Task HubInvocationDelegate(Microsoft.AspNetCore.SignalR.Hub hub, Microsoft.AspNetCore.SignalR.HubConnectionContext connection, Microsoft.AspNetCore.SignalR.IStreamTracker streamTracker, Microsoft.AspNetCore.SignalR.Protocol.HubMessage message, System.Threading.CancellationToken cancellationToken);

/// <summary>
/// 
/// </summary>
/// <param name="hub"></param>
/// <param name="connection"></param>
/// <param name="clients"></param>
public delegate void HubInitializerDelegate(Microsoft.AspNetCore.SignalR.Hub hub, Microsoft.AspNetCore.SignalR.HubConnectionContext connection, Microsoft.AspNetCore.SignalR.IHubCallerClients clients);
