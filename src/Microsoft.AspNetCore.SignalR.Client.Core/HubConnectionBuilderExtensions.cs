// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.SignalR.Client
{
    public static class HubConnectionBuilderExtensions
    {
        public static IHubConnectionBuilder ConfigureLogging(this IHubConnectionBuilder hubConnectionBuilder, Action<ILoggingBuilder> configureLogging)
        {
            hubConnectionBuilder.Services.AddLogging(configureLogging);
            return hubConnectionBuilder;
        }
    }
}