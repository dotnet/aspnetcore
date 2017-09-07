// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.SignalR.Client
{
    public interface IHubConnectionBuilder
    {
        void AddSetting<T>(string name, T value);
        bool TryGetSetting<T>(string name, out T value);
        void ConfigureConnectionFactory(ConnectionFactoryDelegate connectionFactory);
        HubConnection Build();
    }
}
