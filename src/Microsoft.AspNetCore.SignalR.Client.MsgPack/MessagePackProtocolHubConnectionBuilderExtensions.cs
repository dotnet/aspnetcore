// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.SignalR.Internal.Protocol;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.SignalR.Client
{
    public static class MessagePackProtocolHubConnectionBuilderExtensions
    {
        public static IHubConnectionBuilder WithMessagePackProtocol(this IHubConnectionBuilder builder) =>
            WithMessagePackProtocol(builder, new MessagePackHubProtocolOptions());

        public static IHubConnectionBuilder WithMessagePackProtocol(this IHubConnectionBuilder builder, MessagePackHubProtocolOptions options)
        {
            return builder.WithHubProtocol(new MessagePackHubProtocol(Options.Create(options)));
        }
    }
}
