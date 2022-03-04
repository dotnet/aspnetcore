// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Runtime.CompilerServices;

namespace Microsoft.AspNetCore.SignalR.StackExchangeRedis.Internal
{
    internal class RedisChannels
    {
        private readonly string _prefix;

        /// <summary>
        /// Gets the name of the channel for sending to all connections.
        /// </summary>
        /// <remarks>
        /// The payload on this channel is <see cref="RedisInvocation"/> objects containing
        /// invocations to be sent to all connections
        /// </remarks>
        public string All { get; }

        /// <summary>
        /// Gets the name of the internal channel for group management messages.
        /// </summary>
        public string GroupManagement { get; }

        public RedisChannels(string prefix)
        {
            _prefix = prefix;

            All = prefix + ":all";
            GroupManagement = prefix + ":internal:groups";
        }

        /// <summary>
        /// Gets the name of the channel for sending a message to a specific connection.
        /// </summary>
        /// <param name="connectionId">The ID of the connection to get the channel for.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string Connection(string connectionId)
        {
            return _prefix + ":connection:" + connectionId;
        }

        /// <summary>
        /// Gets the name of the channel for sending a message to a named group of connections.
        /// </summary>
        /// <param name="groupName">The name of the group to get the channel for.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string Group(string groupName)
        {
            return _prefix + ":group:" + groupName;
        }

        /// <summary>
        /// Gets the name of the channel for sending a message to all collections associated with a user.
        /// </summary>
        /// <param name="userId">The ID of the user to get the channel for.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string User(string userId)
        {
            return _prefix + ":user:" + userId;
        }

        /// <summary>
        /// Gets the name of the acknowledgement channel for the specified server.
        /// </summary>
        /// <param name="serverName">The name of the server to get the acknowledgement channel for.</param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string Ack(string serverName)
        {
            return _prefix + ":internal:ack:" + serverName;
        }
    }
}
