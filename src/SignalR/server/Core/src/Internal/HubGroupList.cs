// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Concurrent;

namespace Microsoft.AspNetCore.SignalR.Internal;

internal sealed class HubGroupList : IReadOnlyCollection<ConcurrentDictionary<string, HubConnectionContext>>
{
    private readonly ConcurrentDictionary<string, GroupConnectionList> _groups =
        new ConcurrentDictionary<string, GroupConnectionList>(StringComparer.Ordinal);

    private static readonly GroupConnectionList EmptyGroupConnectionList = new GroupConnectionList();

    public ConcurrentDictionary<string, HubConnectionContext>? this[string groupName]
    {
        get
        {
            _groups.TryGetValue(groupName, out var group);
            return group;
        }
    }

    public void Add(HubConnectionContext connection, string groupName)
    {
        CreateOrUpdateGroupWithConnection(groupName, connection);
    }

    public void Remove(string connectionId, string groupName)
    {
        if (_groups.TryGetValue(groupName, out var connections))
        {
            if (connections.TryRemove(connectionId, out var _) && connections.IsEmpty)
            {
                // If group is empty after connection remove, don't need empty group in dictionary.
                // Why this way? Because ICollection.Remove implementation of dictionary checks for key and value. When we remove empty group,
                // it checks if no connection added from another thread.
                var groupToRemove = new KeyValuePair<string, GroupConnectionList>(groupName, EmptyGroupConnectionList);
                ((ICollection<KeyValuePair<string, GroupConnectionList>>)(_groups)).Remove(groupToRemove);
            }
        }
    }

    public int Count => _groups.Count;

    public IEnumerator<ConcurrentDictionary<string, HubConnectionContext>> GetEnumerator()
    {
        return _groups.Values.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    private void CreateOrUpdateGroupWithConnection(string groupName, HubConnectionContext connection)
    {
        _groups.AddOrUpdate(groupName, _ => AddConnectionToGroup(connection, new GroupConnectionList()),
            (key, oldCollection) =>
            {
                AddConnectionToGroup(connection, oldCollection);
                return oldCollection;
            });
    }

    private static GroupConnectionList AddConnectionToGroup(
        HubConnectionContext connection, GroupConnectionList group)
    {
        group.AddOrUpdate(connection.ConnectionId, connection, (_, __) => connection);
        return group;
    }
}

internal sealed class GroupConnectionList : ConcurrentDictionary<string, HubConnectionContext>
{
    public override bool Equals(object? obj)
    {
        if (obj is ConcurrentDictionary<string, HubConnectionContext> list)
        {
            return list.Count == Count;
        }

        return false;
    }

    public override int GetHashCode()
    {
        return base.GetHashCode();
    }
}
