// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Connections.Features;
using Microsoft.AspNetCore.Http.Connections;

namespace SocialWeather;

public class PersistentConnectionLifeTimeManager
{
    private readonly FormatterResolver _formatterResolver;
    private readonly ConnectionList _connectionList = new ConnectionList();

    public PersistentConnectionLifeTimeManager(FormatterResolver formatterResolver)
    {
        _formatterResolver = formatterResolver;
    }

    public void OnConnectedAsync(ConnectionContext connection)
    {
        connection.Items["groups"] = new HashSet<string>();
        var format = connection.GetHttpContext().Request.Query["formatType"].ToString();
        connection.Items["format"] = format;
        if (string.Equals(format, "protobuf", StringComparison.OrdinalIgnoreCase))
        {
            var transferFormatFeature = connection.Features.Get<ITransferFormatFeature>();
            if (transferFormatFeature != null)
            {
                transferFormatFeature.ActiveFormat = TransferFormat.Binary;
            }
        }
        _connectionList.Add(connection);
    }

    public void OnDisconnectedAsync(ConnectionContext connection)
    {
        _connectionList.Remove(connection);
    }

    public async Task SendToAllAsync<T>(T data)
    {
        foreach (var connection in _connectionList)
        {
            var context = connection.GetHttpContext();
            var formatter = _formatterResolver.GetFormatter<T>((string)connection.Items["format"]);
            var ms = new MemoryStream();
            await formatter.WriteAsync(data, ms);

            await connection.Transport.Output.WriteAsync(ms.ToArray());
        }
    }

    public Task InvokeConnectionAsync(string connectionId, object data)
    {
        throw new NotImplementedException();
    }

    public Task InvokeGroupAsync(string groupName, object data)
    {
        throw new NotImplementedException();
    }

    public Task InvokeUserAsync(string userId, object data)
    {
        throw new NotImplementedException();
    }

    public void AddGroupAsync(ConnectionContext connection, string groupName)
    {
        var groups = (HashSet<string>)connection.Items["groups"];
        lock (groups)
        {
            groups.Add(groupName);
        }
    }

    public void RemoveGroupAsync(ConnectionContext connection, string groupName)
    {
        var groups = (HashSet<string>)connection.Items["groups"];
        if (groups != null)
        {
            lock (groups)
            {
                groups.Remove(groupName);
            }
        }
    }
}
