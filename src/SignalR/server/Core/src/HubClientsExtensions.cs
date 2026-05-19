// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;

namespace Microsoft.AspNetCore.SignalR;

/// <summary>
/// Extension methods for <see cref="IHubClients{T}"/>.
/// </summary>
public static class HubClientsExtensions
{
    /// <summary>
    /// Gets a <typeparamref name="T" /> that can be used to invoke methods on all clients connected to the hub excluding the specified connection.
    /// </summary>
    /// <param name="hubClients">The abstraction that provides access to connections.</param>
    /// <param name="excludedConnectionId1">The first connection to exclude.</param>
    /// <returns>A <typeparamref name="T" /> representing the methods that can be invoked on the clients.</returns>
    public static T AllExcept<T>(this IHubClients<T> hubClients, string excludedConnectionId1)
    {
        return hubClients.AllExcept(new[] { excludedConnectionId1 });
    }

    /// <summary>
    /// Gets a <typeparamref name="T" /> that can be used to invoke methods on all clients connected to the hub excluding the specified connections.
    /// </summary>
    /// <param name="hubClients">The abstraction that provides access to connections.</param>
    /// <param name="excludedConnectionId1">The first connection to exclude.</param>
    /// <param name="excludedConnectionId2">The second connection to exclude.</param>
    /// <returns>A <typeparamref name="T" /> representing the methods that can be invoked on the clients.</returns>
    public static T AllExcept<T>(this IHubClients<T> hubClients, string excludedConnectionId1, string excludedConnectionId2)
    {
        return hubClients.AllExcept(new[] { excludedConnectionId1, excludedConnectionId2 });
    }

    /// <summary>
    /// Gets a <typeparamref name="T" /> that can be used to invoke methods on all clients connected to the hub excluding the specified connections.
    /// </summary>
    /// <param name="hubClients">The abstraction that provides access to connections.</param>
    /// <param name="excludedConnectionId1">The first connection to exclude.</param>
    /// <param name="excludedConnectionId2">The second connection to exclude.</param>
    /// <param name="excludedConnectionId3">The third connection to exclude.</param>
    /// <returns>A <typeparamref name="T" /> representing the methods that can be invoked on the clients.</returns>
    public static T AllExcept<T>(this IHubClients<T> hubClients, string excludedConnectionId1, string excludedConnectionId2, string excludedConnectionId3)
    {
        return hubClients.AllExcept(new[] { excludedConnectionId1, excludedConnectionId2, excludedConnectionId3 });
    }

    /// <summary>
    /// Gets a <typeparamref name="T" /> that can be used to invoke methods on all clients connected to the hub excluding the specified connections.
    /// </summary>
    /// <param name="hubClients">The abstraction that provides access to connections.</param>
    /// <param name="excludedConnectionId1">The first connection to exclude.</param>
    /// <param name="excludedConnectionId2">The second connection to exclude.</param>
    /// <param name="excludedConnectionId3">The third connection to exclude.</param>
    /// <param name="excludedConnectionId4">The fourth connection to exclude.</param>
    /// <returns>A <typeparamref name="T" /> representing the methods that can be invoked on the clients.</returns>
    public static T AllExcept<T>(this IHubClients<T> hubClients, string excludedConnectionId1, string excludedConnectionId2, string excludedConnectionId3, string excludedConnectionId4)
    {
        return hubClients.AllExcept(new[] { excludedConnectionId1, excludedConnectionId2, excludedConnectionId3, excludedConnectionId4 });
    }

    /// <summary>
    /// Gets a <typeparamref name="T" /> that can be used to invoke methods on all clients connected to the hub excluding the specified connections.
    /// </summary>
    /// <param name="hubClients">The abstraction that provides access to connections.</param>
    /// <param name="excludedConnectionId1">The first connection to exclude.</param>
    /// <param name="excludedConnectionId2">The second connection to exclude.</param>
    /// <param name="excludedConnectionId3">The third connection to exclude.</param>
    /// <param name="excludedConnectionId4">The fourth connection to exclude.</param>
    /// <param name="excludedConnectionId5">The fifth connection to exclude.</param>
    /// <returns>A <typeparamref name="T" /> representing the methods that can be invoked on the clients.</returns>
    public static T AllExcept<T>(this IHubClients<T> hubClients, string excludedConnectionId1, string excludedConnectionId2, string excludedConnectionId3, string excludedConnectionId4, string excludedConnectionId5)
    {
        return hubClients.AllExcept(new[] { excludedConnectionId1, excludedConnectionId2, excludedConnectionId3, excludedConnectionId4, excludedConnectionId5 });
    }

    /// <summary>
    /// Gets a <typeparamref name="T" /> that can be used to invoke methods on all clients connected to the hub excluding the specified connections.
    /// </summary>
    /// <param name="hubClients">The abstraction that provides access to connections.</param>
    /// <param name="excludedConnectionId1">The first connection to exclude.</param>
    /// <param name="excludedConnectionId2">The second connection to exclude.</param>
    /// <param name="excludedConnectionId3">The third connection to exclude.</param>
    /// <param name="excludedConnectionId4">The fourth connection to exclude.</param>
    /// <param name="excludedConnectionId5">The fifth connection to exclude.</param>
    /// <param name="excludedConnectionId6">The sixth connection to exclude.</param>
    /// <returns>A <typeparamref name="T" /> representing the methods that can be invoked on the clients.</returns>
    public static T AllExcept<T>(this IHubClients<T> hubClients, string excludedConnectionId1, string excludedConnectionId2, string excludedConnectionId3, string excludedConnectionId4, string excludedConnectionId5, string excludedConnectionId6)
    {
        return hubClients.AllExcept(new[] { excludedConnectionId1, excludedConnectionId2, excludedConnectionId3, excludedConnectionId4, excludedConnectionId5, excludedConnectionId6 });
    }

    /// <summary>
    /// Gets a <typeparamref name="T" /> that can be used to invoke methods on all clients connected to the hub excluding the specified connections.
    /// </summary>
    /// <param name="hubClients">The abstraction that provides access to connections.</param>
    /// <param name="excludedConnectionId1">The first connection to exclude.</param>
    /// <param name="excludedConnectionId2">The second connection to exclude.</param>
    /// <param name="excludedConnectionId3">The third connection to exclude.</param>
    /// <param name="excludedConnectionId4">The fourth connection to exclude.</param>
    /// <param name="excludedConnectionId5">The fifth connection to exclude.</param>
    /// <param name="excludedConnectionId6">The sixth connection to exclude.</param>
    /// <param name="excludedConnectionId7">The seventh connection to exclude.</param>
    /// <returns>A <typeparamref name="T" /> representing the methods that can be invoked on the clients.</returns>
    public static T AllExcept<T>(this IHubClients<T> hubClients, string excludedConnectionId1, string excludedConnectionId2, string excludedConnectionId3, string excludedConnectionId4, string excludedConnectionId5, string excludedConnectionId6, string excludedConnectionId7)
    {
        return hubClients.AllExcept(new[] { excludedConnectionId1, excludedConnectionId2, excludedConnectionId3, excludedConnectionId4, excludedConnectionId5, excludedConnectionId6, excludedConnectionId7 });
    }

    /// <summary>
    /// Gets a <typeparamref name="T" /> that can be used to invoke methods on all clients connected to the hub excluding the specified connections.
    /// </summary>
    /// <param name="hubClients">The abstraction that provides access to connections.</param>
    /// <param name="excludedConnectionId1">The first connection to exclude.</param>
    /// <param name="excludedConnectionId2">The second connection to exclude.</param>
    /// <param name="excludedConnectionId3">The third connection to exclude.</param>
    /// <param name="excludedConnectionId4">The fourth connection to exclude.</param>
    /// <param name="excludedConnectionId5">The fifth connection to exclude.</param>
    /// <param name="excludedConnectionId6">The sixth connection to exclude.</param>
    /// <param name="excludedConnectionId7">The seventh connection to exclude.</param>
    /// <param name="excludedConnectionId8">The eighth connection to exclude.</param>
    /// <returns>A <typeparamref name="T" /> representing the methods that can be invoked on the clients.</returns>
    public static T AllExcept<T>(this IHubClients<T> hubClients, string excludedConnectionId1, string excludedConnectionId2, string excludedConnectionId3, string excludedConnectionId4, string excludedConnectionId5, string excludedConnectionId6, string excludedConnectionId7, string excludedConnectionId8)
    {
        return hubClients.AllExcept(new[] { excludedConnectionId1, excludedConnectionId2, excludedConnectionId3, excludedConnectionId4, excludedConnectionId5, excludedConnectionId6, excludedConnectionId7, excludedConnectionId8 });
    }

    /// <summary>
    /// Gets a <typeparamref name="T" /> that can be used to invoke methods on all clients connected to the hub excluding the specified connections.
    /// </summary>
    /// <param name="hubClients">The abstraction that provides access to connections.</param>
    /// <param name="excludedConnectionIds">The connection IDs to exclude.</param>
    /// <returns>A <typeparamref name="T" /> representing the methods that can be invoked on the clients.</returns>
    public static T AllExcept<T>(this IHubClients<T> hubClients, IEnumerable<string> excludedConnectionIds)
    {
        return hubClients.AllExcept(excludedConnectionIds.ToList());
    }

    /// <summary>
    /// Gets a <typeparamref name="T" /> that can be used to invoke methods on the specified connections.
    /// </summary>
    /// <param name="hubClients">The abstraction that provides access to connections.</param>
    /// <param name="connection1">The first connection to include.</param>
    /// <returns>A <typeparamref name="T" /> representing the methods that can be invoked on the clients.</returns>
    public static T Clients<T>(this IHubClients<T> hubClients, string connection1)
    {
        return hubClients.Clients(new[] { connection1 });
    }

    /// <summary>
    /// Gets a <typeparamref name="T" /> that can be used to invoke methods on the specified connections.
    /// </summary>
    /// <param name="hubClients">The abstraction that provides access to connections.</param>
    /// <param name="connection1">The first connection to include.</param>
    /// <param name="connection2">The second connection to include.</param>
    /// <returns>A <typeparamref name="T" /> representing the methods that can be invoked on the clients.</returns>
    public static T Clients<T>(this IHubClients<T> hubClients, string connection1, string connection2)
    {
        return hubClients.Clients(new[] { connection1, connection2 });
    }

    /// <summary>
    /// Gets a <typeparamref name="T" /> that can be used to invoke methods on the specified connections.
    /// </summary>
    /// <param name="hubClients">The abstraction that provides access to connections.</param>
    /// <param name="connection1">The first connection to include.</param>
    /// <param name="connection2">The second connection to include.</param>
    /// <param name="connection3">The third connection to include.</param>
    /// <returns>A <typeparamref name="T" /> representing the methods that can be invoked on the clients.</returns>
    public static T Clients<T>(this IHubClients<T> hubClients, string connection1, string connection2, string connection3)
    {
        return hubClients.Clients(new[] { connection1, connection2, connection3 });
    }

    /// <summary>
    /// Gets a <typeparamref name="T" /> that can be used to invoke methods on the specified connections.
    /// </summary>
    /// <param name="hubClients">The abstraction that provides access to connections.</param>
    /// <param name="connection1">The first connection to include.</param>
    /// <param name="connection2">The second connection to include.</param>
    /// <param name="connection3">The third connection to include.</param>
    /// <param name="connection4">The fourth connection to include.</param>
    /// <returns>A <typeparamref name="T" /> representing the methods that can be invoked on the clients.</returns>
    public static T Clients<T>(this IHubClients<T> hubClients, string connection1, string connection2, string connection3, string connection4)
    {
        return hubClients.Clients(new[] { connection1, connection2, connection3, connection4 });
    }

    /// <summary>
    /// Gets a <typeparamref name="T" /> that can be used to invoke methods on the specified connections.
    /// </summary>
    /// <param name="hubClients">The abstraction that provides access to connections.</param>
    /// <param name="connection1">The first connection to include.</param>
    /// <param name="connection2">The second connection to include.</param>
    /// <param name="connection3">The third connection to include.</param>
    /// <param name="connection4">The fourth connection to include.</param>
    /// <param name="connection5">The fifth connection to include.</param>
    /// <returns>A <typeparamref name="T" /> representing the methods that can be invoked on the clients.</returns>
    public static T Clients<T>(this IHubClients<T> hubClients, string connection1, string connection2, string connection3, string connection4, string connection5)
    {
        return hubClients.Clients(new[] { connection1, connection2, connection3, connection4, connection5 });
    }

    /// <summary>
    /// Gets a <typeparamref name="T" /> that can be used to invoke methods on the specified connections.
    /// </summary>
    /// <param name="hubClients">The abstraction that provides access to connections.</param>
    /// <param name="connection1">The first connection to include.</param>
    /// <param name="connection2">The second connection to include.</param>
    /// <param name="connection3">The third connection to include.</param>
    /// <param name="connection4">The fourth connection to include.</param>
    /// <param name="connection5">The fifth connection to include.</param>
    /// <param name="connection6">The sixth connection to include.</param>
    /// <returns>A <typeparamref name="T" /> representing the methods that can be invoked on the clients.</returns>
    public static T Clients<T>(this IHubClients<T> hubClients, string connection1, string connection2, string connection3, string connection4, string connection5, string connection6)
    {
        return hubClients.Clients(new[] { connection1, connection2, connection3, connection4, connection5, connection6 });
    }

    /// <summary>
    /// Gets a <typeparamref name="T" /> that can be used to invoke methods on the specified connections.
    /// </summary>
    /// <param name="hubClients">The abstraction that provides access to connections.</param>
    /// <param name="connection1">The first connection to include.</param>
    /// <param name="connection2">The second connection to include.</param>
    /// <param name="connection3">The third connection to include.</param>
    /// <param name="connection4">The fourth connection to include.</param>
    /// <param name="connection5">The fifth connection to include.</param>
    /// <param name="connection6">The sixth connection to include.</param>
    /// <param name="connection7">The seventh connection to include.</param>
    /// <returns>A <typeparamref name="T" /> representing the methods that can be invoked on the clients.</returns>
    public static T Clients<T>(this IHubClients<T> hubClients, string connection1, string connection2, string connection3, string connection4, string connection5, string connection6, string connection7)
    {
        return hubClients.Clients(new[] { connection1, connection2, connection3, connection4, connection5, connection6, connection7 });
    }

    /// <summary>
    /// Gets a <typeparamref name="T" /> that can be used to invoke methods on the specified connections.
    /// </summary>
    /// <param name="hubClients">The abstraction that provides access to connections.</param>
    /// <param name="connection1">The first connection to include.</param>
    /// <param name="connection2">The second connection to include.</param>
    /// <param name="connection3">The third connection to include.</param>
    /// <param name="connection4">The fourth connection to include.</param>
    /// <param name="connection5">The fifth connection to include.</param>
    /// <param name="connection6">The sixth connection to include.</param>
    /// <param name="connection7">The seventh connection to include.</param>
    /// <param name="connection8">The eighth connection to include.</param>
    /// <returns>A <typeparamref name="T" /> representing the methods that can be invoked on the clients.</returns>
    public static T Clients<T>(this IHubClients<T> hubClients, string connection1, string connection2, string connection3, string connection4, string connection5, string connection6, string connection7, string connection8)
    {
        return hubClients.Clients(new[] { connection1, connection2, connection3, connection4, connection5, connection6, connection7, connection8 });
    }

    /// <summary>
    /// Gets a <typeparamref name="T" /> that can be used to invoke methods on the specified connections.
    /// </summary>
    /// <param name="hubClients">The abstraction that provides access to connections.</param>
    /// <param name="connectionIds">The connection IDs.</param>
    /// <returns>A <typeparamref name="T" /> representing the methods that can be invoked on the clients.</returns>
    public static T Clients<T>(this IHubClients<T> hubClients, IEnumerable<string> connectionIds)
    {
        return hubClients.Clients(connectionIds.ToList());
    }

    /// <summary>
    /// Gets a <typeparamref name="T" /> that can be used to invoke methods on all connections in all of the specified groups.
    /// </summary>
    /// <param name="hubClients">The abstraction that provides access to connections.</param>
    /// <param name="group1">The first group to include.</param>
    /// <returns>A <typeparamref name="T" /> representing the methods that can be invoked on the clients.</returns>
    public static T Groups<T>(this IHubClients<T> hubClients, string group1)
    {
        return hubClients.Groups(new[] { group1 });
    }

    /// <summary>
    /// Gets a <typeparamref name="T" /> that can be used to invoke methods on all connections in all of the specified groups.
    /// </summary>
    /// <param name="hubClients">The abstraction that provides access to connections.</param>
    /// <param name="group1">The first group to include.</param>
    /// <param name="group2">The second group to include.</param>
    /// <returns>A <typeparamref name="T" /> representing the methods that can be invoked on the clients.</returns>
    public static T Groups<T>(this IHubClients<T> hubClients, string group1, string group2)
    {
        return hubClients.Groups(new[] { group1, group2 });
    }

    /// <summary>
    /// Gets a <typeparamref name="T" /> that can be used to invoke methods on all connections in all of the specified groups.
    /// </summary>
    /// <param name="hubClients">The abstraction that provides access to connections.</param>
    /// <param name="group1">The first group to include.</param>
    /// <param name="group2">The second group to include.</param>
    /// <param name="group3">The third group to include.</param>
    /// <returns>A <typeparamref name="T" /> representing the methods that can be invoked on the clients.</returns>
    public static T Groups<T>(this IHubClients<T> hubClients, string group1, string group2, string group3)
    {
        return hubClients.Groups(new[] { group1, group2, group3 });
    }

    /// <summary>
    /// Gets a <typeparamref name="T" /> that can be used to invoke methods on all connections in all of the specified groups.
    /// </summary>
    /// <param name="hubClients">The abstraction that provides access to connections.</param>
    /// <param name="group1">The first group to include.</param>
    /// <param name="group2">The second group to include.</param>
    /// <param name="group3">The third group to include.</param>
    /// <param name="group4">The fourth group to include.</param>
    /// <returns>A <typeparamref name="T" /> representing the methods that can be invoked on the clients.</returns>
    public static T Groups<T>(this IHubClients<T> hubClients, string group1, string group2, string group3, string group4)
    {
        return hubClients.Groups(new[] { group1, group2, group3, group4 });
    }

    /// <summary>
    /// Gets a <typeparamref name="T" /> that can be used to invoke methods on all connections in all of the specified groups.
    /// </summary>
    /// <param name="hubClients">The abstraction that provides access to connections.</param>
    /// <param name="group1">The first group to include.</param>
    /// <param name="group2">The second group to include.</param>
    /// <param name="group3">The third group to include.</param>
    /// <param name="group4">The fourth group to include.</param>
    /// <param name="group5">The fifth group to include.</param>
    /// <returns>A <typeparamref name="T" /> representing the methods that can be invoked on the clients.</returns>
    public static T Groups<T>(this IHubClients<T> hubClients, string group1, string group2, string group3, string group4, string group5)
    {
        return hubClients.Groups(new[] { group1, group2, group3, group4, group5 });
    }

    /// <summary>
    /// Gets a <typeparamref name="T" /> that can be used to invoke methods on all connections in all of the specified groups.
    /// </summary>
    /// <param name="hubClients">The abstraction that provides access to connections.</param>
    /// <param name="group1">The first group to include.</param>
    /// <param name="group2">The second group to include.</param>
    /// <param name="group3">The third group to include.</param>
    /// <param name="group4">The fourth group to include.</param>
    /// <param name="group5">The fifth group to include.</param>
    /// <param name="group6">The sixth group to include.</param>
    /// <returns>A <typeparamref name="T" /> representing the methods that can be invoked on the clients.</returns>
    public static T Groups<T>(this IHubClients<T> hubClients, string group1, string group2, string group3, string group4, string group5, string group6)
    {
        return hubClients.Groups(new[] { group1, group2, group3, group4, group5, group6 });
    }

    /// <summary>
    /// Gets a <typeparamref name="T" /> that can be used to invoke methods on all connections in all of the specified groups.
    /// </summary>
    /// <param name="hubClients">The abstraction that provides access to connections.</param>
    /// <param name="group1">The first group to include.</param>
    /// <param name="group2">The second group to include.</param>
    /// <param name="group3">The third group to include.</param>
    /// <param name="group4">The fourth group to include.</param>
    /// <param name="group5">The fifth group to include.</param>
    /// <param name="group6">The sixth group to include.</param>
    /// <param name="group7">The seventh group to include.</param>
    /// <returns>A <typeparamref name="T" /> representing the methods that can be invoked on the clients.</returns>
    public static T Groups<T>(this IHubClients<T> hubClients, string group1, string group2, string group3, string group4, string group5, string group6, string group7)
    {
        return hubClients.Groups(new[] { group1, group2, group3, group4, group5, group6, group7 });
    }

    /// <summary>
    /// Gets a <typeparamref name="T" /> that can be used to invoke methods on all connections in all of the specified groups.
    /// </summary>
    /// <param name="hubClients">The abstraction that provides access to connections.</param>
    /// <param name="group1">The first group to include.</param>
    /// <param name="group2">The second group to include.</param>
    /// <param name="group3">The third group to include.</param>
    /// <param name="group4">The fourth group to include.</param>
    /// <param name="group5">The fifth group to include.</param>
    /// <param name="group6">The sixth group to include.</param>
    /// <param name="group7">The seventh group to include.</param>
    /// <param name="group8">The eighth group to include.</param>
    /// <returns>A <typeparamref name="T" /> representing the methods that can be invoked on the clients.</returns>
    public static T Groups<T>(this IHubClients<T> hubClients, string group1, string group2, string group3, string group4, string group5, string group6, string group7, string group8)
    {
        return hubClients.Groups(new[] { group1, group2, group3, group4, group5, group6, group7, group8 });
    }

    /// <summary>
    /// Gets a <typeparamref name="T" /> that can be used to invoke methods on all connections in all of the specified groups.
    /// </summary>
    /// <param name="hubClients">The abstraction that provides access to connections.</param>
    /// <param name="groupNames">The group names.</param>
    /// <returns>A <typeparamref name="T" /> representing the methods that can be invoked on the clients.</returns>
    public static T Groups<T>(this IHubClients<T> hubClients, IEnumerable<string> groupNames)
    {
        return hubClients.Groups(groupNames.ToList());
    }

    /// <summary>
    /// Gets a <typeparamref name="T" /> that can be used to invoke methods on all connections in the specified group excluding the specified connections.
    /// </summary>
    /// <param name="hubClients">The abstraction that provides access to connections.</param>
    /// <param name="groupName">The group name.</param>
    /// <param name="excludedConnectionId1">The first connection to exclude.</param>
    /// <returns>A <typeparamref name="T" /> representing the methods that can be invoked on the clients.</returns>
    public static T GroupExcept<T>(this IHubClients<T> hubClients, string groupName, string excludedConnectionId1)
    {
        return hubClients.GroupExcept(groupName, new[] { excludedConnectionId1 });
    }

    /// <summary>
    /// Gets a <typeparamref name="T" /> that can be used to invoke methods on all connections in the specified group excluding the specified connections.
    /// </summary>
    /// <param name="hubClients">The abstraction that provides access to connections.</param>
    /// <param name="groupName">The group name.</param>
    /// <param name="excludedConnectionId1">The first connection to exclude.</param>
    /// <param name="excludedConnectionId2">The second connection to exclude.</param>
    /// <returns>A <typeparamref name="T" /> representing the methods that can be invoked on the clients.</returns>
    public static T GroupExcept<T>(this IHubClients<T> hubClients, string groupName, string excludedConnectionId1, string excludedConnectionId2)
    {
        return hubClients.GroupExcept(groupName, new[] { excludedConnectionId1, excludedConnectionId2 });
    }

    /// <summary>
    /// Gets a <typeparamref name="T" /> that can be used to invoke methods on all connections in the specified group excluding the specified connections.
    /// </summary>
    /// <param name="hubClients">The abstraction that provides access to connections.</param>
    /// <param name="groupName">The group name.</param>
    /// <param name="excludedConnectionId1">The first connection to exclude.</param>
    /// <param name="excludedConnectionId2">The second connection to exclude.</param>
    /// <param name="excludedConnectionId3">The third connection to exclude.</param>
    /// <returns>A <typeparamref name="T" /> representing the methods that can be invoked on the clients.</returns>
    public static T GroupExcept<T>(this IHubClients<T> hubClients, string groupName, string excludedConnectionId1, string excludedConnectionId2, string excludedConnectionId3)
    {
        return hubClients.GroupExcept(groupName, new[] { excludedConnectionId1, excludedConnectionId2, excludedConnectionId3 });
    }

    /// <summary>
    /// Gets a <typeparamref name="T" /> that can be used to invoke methods on all connections in the specified group excluding the specified connections.
    /// </summary>
    /// <param name="hubClients">The abstraction that provides access to connections.</param>
    /// <param name="groupName">The group name.</param>
    /// <param name="excludedConnectionId1">The first connection to exclude.</param>
    /// <param name="excludedConnectionId2">The second connection to exclude.</param>
    /// <param name="excludedConnectionId3">The third connection to exclude.</param>
    /// <param name="excludedConnectionId4">The fourth connection to exclude.</param>
    /// <returns>A <typeparamref name="T" /> representing the methods that can be invoked on the clients.</returns>
    public static T GroupExcept<T>(this IHubClients<T> hubClients, string groupName, string excludedConnectionId1, string excludedConnectionId2, string excludedConnectionId3, string excludedConnectionId4)
    {
        return hubClients.GroupExcept(groupName, new[] { excludedConnectionId1, excludedConnectionId2, excludedConnectionId3, excludedConnectionId4 });
    }

    /// <summary>
    /// Gets a <typeparamref name="T" /> that can be used to invoke methods on all connections in the specified group excluding the specified connections.
    /// </summary>
    /// <param name="hubClients">The abstraction that provides access to connections.</param>
    /// <param name="groupName">The group name.</param>
    /// <param name="excludedConnectionId1">The first connection to exclude.</param>
    /// <param name="excludedConnectionId2">The second connection to exclude.</param>
    /// <param name="excludedConnectionId3">The third connection to exclude.</param>
    /// <param name="excludedConnectionId4">The fourth connection to exclude.</param>
    /// <param name="excludedConnectionId5">The fifth connection to exclude.</param>
    /// <returns>A <typeparamref name="T" /> representing the methods that can be invoked on the clients.</returns>
    public static T GroupExcept<T>(this IHubClients<T> hubClients, string groupName, string excludedConnectionId1, string excludedConnectionId2, string excludedConnectionId3, string excludedConnectionId4, string excludedConnectionId5)
    {
        return hubClients.GroupExcept(groupName, new[] { excludedConnectionId1, excludedConnectionId2, excludedConnectionId3, excludedConnectionId4, excludedConnectionId5 });
    }

    /// <summary>
    /// Gets a <typeparamref name="T" /> that can be used to invoke methods on all connections in the specified group excluding the specified connections.
    /// </summary>
    /// <param name="hubClients">The abstraction that provides access to connections.</param>
    /// <param name="groupName">The group name.</param>
    /// <param name="excludedConnectionId1">The first connection to exclude.</param>
    /// <param name="excludedConnectionId2">The second connection to exclude.</param>
    /// <param name="excludedConnectionId3">The third connection to exclude.</param>
    /// <param name="excludedConnectionId4">The fourth connection to exclude.</param>
    /// <param name="excludedConnectionId5">The fifth connection to exclude.</param>
    /// <param name="excludedConnectionId6">The sixth connection to exclude.</param>
    /// <returns>A <typeparamref name="T" /> representing the methods that can be invoked on the clients.</returns>
    public static T GroupExcept<T>(this IHubClients<T> hubClients, string groupName, string excludedConnectionId1, string excludedConnectionId2, string excludedConnectionId3, string excludedConnectionId4, string excludedConnectionId5, string excludedConnectionId6)
    {
        return hubClients.GroupExcept(groupName, new[] { excludedConnectionId1, excludedConnectionId2, excludedConnectionId3, excludedConnectionId4, excludedConnectionId5, excludedConnectionId6 });
    }

    /// <summary>
    /// Gets a <typeparamref name="T" /> that can be used to invoke methods on all connections in the specified group excluding the specified connections.
    /// </summary>
    /// <param name="hubClients">The abstraction that provides access to connections.</param>
    /// <param name="groupName">The group name.</param>
    /// <param name="excludedConnectionId1">The first connection to exclude.</param>
    /// <param name="excludedConnectionId2">The second connection to exclude.</param>
    /// <param name="excludedConnectionId3">The third connection to exclude.</param>
    /// <param name="excludedConnectionId4">The fourth connection to exclude.</param>
    /// <param name="excludedConnectionId5">The fifth connection to exclude.</param>
    /// <param name="excludedConnectionId6">The sixth connection to exclude.</param>
    /// <param name="excludedConnectionId7">The seventh connection to exclude.</param>
    /// <returns>A <typeparamref name="T" /> representing the methods that can be invoked on the clients.</returns>
    public static T GroupExcept<T>(this IHubClients<T> hubClients, string groupName, string excludedConnectionId1, string excludedConnectionId2, string excludedConnectionId3, string excludedConnectionId4, string excludedConnectionId5, string excludedConnectionId6, string excludedConnectionId7)
    {
        return hubClients.GroupExcept(groupName, new[] { excludedConnectionId1, excludedConnectionId2, excludedConnectionId3, excludedConnectionId4, excludedConnectionId5, excludedConnectionId6, excludedConnectionId7 });
    }

    /// <summary>
    /// Gets a <typeparamref name="T" /> that can be used to invoke methods on all connections in the specified group excluding the specified connections.
    /// </summary>
    /// <param name="hubClients">The abstraction that provides access to connections.</param>
    /// <param name="groupName">The group name.</param>
    /// <param name="excludedConnectionId1">The first connection to exclude.</param>
    /// <param name="excludedConnectionId2">The second connection to exclude.</param>
    /// <param name="excludedConnectionId3">The third connection to exclude.</param>
    /// <param name="excludedConnectionId4">The fourth connection to exclude.</param>
    /// <param name="excludedConnectionId5">The fifth connection to exclude.</param>
    /// <param name="excludedConnectionId6">The sixth connection to exclude.</param>
    /// <param name="excludedConnectionId7">The seventh connection to exclude.</param>
    /// <param name="excludedConnectionId8">The eighth connection to exclude.</param>
    /// <returns>A <typeparamref name="T" /> representing the methods that can be invoked on the clients.</returns>
    public static T GroupExcept<T>(this IHubClients<T> hubClients, string groupName, string excludedConnectionId1, string excludedConnectionId2, string excludedConnectionId3, string excludedConnectionId4, string excludedConnectionId5, string excludedConnectionId6, string excludedConnectionId7, string excludedConnectionId8)
    {
        return hubClients.GroupExcept(groupName, new[] { excludedConnectionId1, excludedConnectionId2, excludedConnectionId3, excludedConnectionId4, excludedConnectionId5, excludedConnectionId6, excludedConnectionId7, excludedConnectionId8 });
    }

    /// <summary>
    /// Gets a <typeparamref name="T" /> that can be used to invoke methods on all connections in the specified group excluding the specified connections.
    /// </summary>
    /// <param name="hubClients">The abstraction that provides access to connections.</param>
    /// <param name="groupName">The group name.</param>
    /// <param name="excludedConnectionIds">The connection IDs to exclude.</param>
    /// <returns>A <typeparamref name="T" /> representing the methods that can be invoked on the clients.</returns>
    public static T GroupExcept<T>(this IHubClients<T> hubClients, string groupName, IEnumerable<string> excludedConnectionIds)
    {
        return hubClients.GroupExcept(groupName, excludedConnectionIds.ToList());
    }

    /// <summary>
    /// Gets a <typeparamref name="T" /> that can be used to invoke methods on all connections associated with all of the specified users.
    /// </summary>
    /// <param name="hubClients">The abstraction that provides access to connections.</param>
    /// <param name="user1">The first user to include.</param>
    /// <returns>A <typeparamref name="T" /> representing the methods that can be invoked on the clients.</returns>
    public static T Users<T>(this IHubClients<T> hubClients, string user1)
    {
        return hubClients.Users(new[] { user1 });
    }

    /// <summary>
    /// Gets a <typeparamref name="T" /> that can be used to invoke methods on all connections associated with all of the specified users.
    /// </summary>
    /// <param name="hubClients">The abstraction that provides access to connections.</param>
    /// <param name="user1">The first user to include.</param>
    /// <param name="user2">The second user to include.</param>
    /// <returns>A <typeparamref name="T" /> representing the methods that can be invoked on the clients.</returns>
    public static T Users<T>(this IHubClients<T> hubClients, string user1, string user2)
    {
        return hubClients.Users(new[] { user1, user2 });
    }

    /// <summary>
    /// Gets a <typeparamref name="T" /> that can be used to invoke methods on all connections associated with all of the specified users.
    /// </summary>
    /// <param name="hubClients">The abstraction that provides access to connections.</param>
    /// <param name="user1">The first user to include.</param>
    /// <param name="user2">The second user to include.</param>
    /// <param name="user3">The third user to include.</param>
    /// <returns>A <typeparamref name="T" /> representing the methods that can be invoked on the clients.</returns>
    public static T Users<T>(this IHubClients<T> hubClients, string user1, string user2, string user3)
    {
        return hubClients.Users(new[] { user1, user2, user3 });
    }

    /// <summary>
    /// Gets a <typeparamref name="T" /> that can be used to invoke methods on all connections associated with all of the specified users.
    /// </summary>
    /// <param name="hubClients">The abstraction that provides access to connections.</param>
    /// <param name="user1">The first user to include.</param>
    /// <param name="user2">The second user to include.</param>
    /// <param name="user3">The third user to include.</param>
    /// <param name="user4">The fourth user to include.</param>
    /// <returns>A <typeparamref name="T" /> representing the methods that can be invoked on the clients.</returns>
    public static T Users<T>(this IHubClients<T> hubClients, string user1, string user2, string user3, string user4)
    {
        return hubClients.Users(new[] { user1, user2, user3, user4 });
    }

    /// <summary>
    /// Gets a <typeparamref name="T" /> that can be used to invoke methods on all connections associated with all of the specified users.
    /// </summary>
    /// <param name="hubClients">The abstraction that provides access to connections.</param>
    /// <param name="user1">The first user to include.</param>
    /// <param name="user2">The second user to include.</param>
    /// <param name="user3">The third user to include.</param>
    /// <param name="user4">The fourth user to include.</param>
    /// <param name="user5">The fifth user to include.</param>
    /// <returns>A <typeparamref name="T" /> representing the methods that can be invoked on the clients.</returns>
    public static T Users<T>(this IHubClients<T> hubClients, string user1, string user2, string user3, string user4, string user5)
    {
        return hubClients.Users(new[] { user1, user2, user3, user4, user5 });
    }

    /// <summary>
    /// Gets a <typeparamref name="T" /> that can be used to invoke methods on all connections associated with all of the specified users.
    /// </summary>
    /// <param name="hubClients">The abstraction that provides access to connections.</param>
    /// <param name="user1">The first user to include.</param>
    /// <param name="user2">The second user to include.</param>
    /// <param name="user3">The third user to include.</param>
    /// <param name="user4">The fourth user to include.</param>
    /// <param name="user5">The fifth user to include.</param>
    /// <param name="user6">The sixth user to include.</param>
    /// <returns>A <typeparamref name="T" /> representing the methods that can be invoked on the clients.</returns>
    public static T Users<T>(this IHubClients<T> hubClients, string user1, string user2, string user3, string user4, string user5, string user6)
    {
        return hubClients.Users(new[] { user1, user2, user3, user4, user5, user6 });
    }

    /// <summary>
    /// Gets a <typeparamref name="T" /> that can be used to invoke methods on all connections associated with all of the specified users.
    /// </summary>
    /// <param name="hubClients">The abstraction that provides access to connections.</param>
    /// <param name="user1">The first user to include.</param>
    /// <param name="user2">The second user to include.</param>
    /// <param name="user3">The third user to include.</param>
    /// <param name="user4">The fourth user to include.</param>
    /// <param name="user5">The fifth user to include.</param>
    /// <param name="user6">The sixth user to include.</param>
    /// <param name="user7">The seventh user to include.</param>
    /// <returns>A <typeparamref name="T" /> representing the methods that can be invoked on the clients.</returns>
    public static T Users<T>(this IHubClients<T> hubClients, string user1, string user2, string user3, string user4, string user5, string user6, string user7)
    {
        return hubClients.Users(new[] { user1, user2, user3, user4, user5, user6, user7 });
    }

    /// <summary>
    /// Gets a <typeparamref name="T" /> that can be used to invoke methods on all connections associated with all of the specified users.
    /// </summary>
    /// <param name="hubClients">The abstraction that provides access to connections.</param>
    /// <param name="user1">The first user to include.</param>
    /// <param name="user2">The second user to include.</param>
    /// <param name="user3">The third user to include.</param>
    /// <param name="user4">The fourth user to include.</param>
    /// <param name="user5">The fifth user to include.</param>
    /// <param name="user6">The sixth user to include.</param>
    /// <param name="user7">The seventh user to include.</param>
    /// <param name="user8">The eighth user to include.</param>
    /// <returns>A <typeparamref name="T" /> representing the methods that can be invoked on the clients.</returns>
    public static T Users<T>(this IHubClients<T> hubClients, string user1, string user2, string user3, string user4, string user5, string user6, string user7, string user8)
    {
        return hubClients.Users(new[] { user1, user2, user3, user4, user5, user6, user7, user8 });
    }

    /// <summary>
    /// Gets a <typeparamref name="T" /> that can be used to invoke methods on all connections associated with all of the specified users.
    /// </summary>
    /// <param name="hubClients">The abstraction that provides access to connections.</param>
    /// <param name="userIds">The user IDs.</param>
    /// <returns>A <typeparamref name="T" /> representing the methods that can be invoked on the clients.</returns>
    public static T Users<T>(this IHubClients<T> hubClients, IEnumerable<string> userIds)
    {
        return hubClients.Users(userIds.ToList());
    }
}
