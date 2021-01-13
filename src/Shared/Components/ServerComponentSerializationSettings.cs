// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Text.Json;

namespace Microsoft.AspNetCore.Components
{
    internal static class ServerComponentSerializationSettings
    {
        public const string DataProtectionProviderPurpose = "Microsoft.AspNetCore.Components.ComponentDescriptorSerializer,V1";

        public static readonly JsonSerializerOptions JsonSerializationOptions =
            new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                PropertyNameCaseInsensitive = true,
                IgnoreNullValues = true
            };

        // This setting is not configurable, but realistically we don't expect an app to take more than 30 seconds from when
        // it got rendrered to when the circuit got started, and having an expiration on the serialized server-components helps
        // prevent old payloads from being replayed.
        public static readonly TimeSpan DataExpiration = TimeSpan.FromMinutes(5);
    }
}
