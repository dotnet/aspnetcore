// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.SignalR.StackExchangeRedis.Internal
{
    public readonly struct RedisGroupCommand
    {
        /// <summary>
        /// Gets the ID of the group command.
        /// </summary>
        public int Id { get; }

        /// <summary>
        /// Gets the name of the server that sent the command.
        /// </summary>
        public string ServerName { get; }

        /// <summary>
        /// Gets the action to be performed on the group.
        /// </summary>
        public GroupAction Action { get; }

        /// <summary>
        /// Gets the group on which the action is performed.
        /// </summary>
        public string GroupName { get; }

        /// <summary>
        /// Gets the ID of the connection to be added or removed from the group.
        /// </summary>
        public string ConnectionId { get; }

        public RedisGroupCommand(int id, string serverName, GroupAction action, string groupName, string connectionId)
        {
            Id = id;
            ServerName = serverName;
            Action = action;
            GroupName = groupName;
            ConnectionId = connectionId;
        }
    }
}
