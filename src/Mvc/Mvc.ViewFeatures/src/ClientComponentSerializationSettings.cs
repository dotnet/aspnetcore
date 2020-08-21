// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Text.Json;

namespace Microsoft.AspNetCore.Components
{
    internal static class ClientComponentSerializationSettings
    {
        public static readonly JsonSerializerOptions JsonSerializationOptions =
            new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                PropertyNameCaseInsensitive = true,
                IgnoreNullValues = true
            };
    }
}
