// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using Microsoft.AspNetCore.SignalR.Internal.Protocol;
using Microsoft.AspNetCore.Sockets.Client;
using Newtonsoft.Json;

namespace Microsoft.AspNetCore.SignalR.Client
{
    public class HubConnectionBuilder : IHubConnectionBuilder
    {
        private readonly Dictionary<KeyValuePair<string, Type>, object> _settings = new Dictionary<KeyValuePair<string, Type>, object>();
        private Func<IConnection> _connectionFactoryDelegate;

        public void ConfigureConnectionFactory(Func<IConnection> connectionFactoryDelegate) =>
            _connectionFactoryDelegate = connectionFactoryDelegate;

        public void AddSetting<T>(string name, T value)
        {
            _settings[new KeyValuePair<string, Type>(name, typeof(T))] = value;
        }

        public bool TryGetSetting<T>(string name, out T value)
        {
            value = default;
            if (!_settings.TryGetValue(new KeyValuePair<string, Type>(name, typeof(T)), out var setting))
            {
                return false;
            }

            value = (T)setting;
            return true;
        }

        public HubConnection Build()
        {
            if (_connectionFactoryDelegate == null)
            {
                throw new InvalidOperationException("Cannot create IConnection instance. The connection factory was not configured.");
            }

            IHubConnectionBuilder builder = this;

            var loggerFactory = builder.GetLoggerFactory();
            var hubProtocol = builder.GetHubProtocol();

            return new HubConnection(_connectionFactoryDelegate, hubProtocol ?? new JsonHubProtocol(), loggerFactory);
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public override string ToString()
        {
            return base.ToString();
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public new Type GetType()
        {
            return base.GetType();
        }
    }
}
