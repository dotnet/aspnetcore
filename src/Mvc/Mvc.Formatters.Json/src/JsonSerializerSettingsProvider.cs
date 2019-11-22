// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Microsoft.AspNetCore.Mvc.Formatters
{
    /// <summary>
    /// Helper class which provides <see cref="JsonSerializerSettings"/>.
    /// </summary>
    public static class JsonSerializerSettingsProvider
    {
        private const int DefaultMaxDepth = 32;

        // return shared resolver by default for perf so slow reflection logic is cached once
        // developers can set their own resolver after the settings are returned if desired
        private static readonly DefaultContractResolver SharedContractResolver;

        static JsonSerializerSettingsProvider()
        {
            SharedContractResolver = CreateContractResolver();
        }

        /// <summary>
        /// Creates default <see cref="JsonSerializerSettings"/>.
        /// </summary>
        /// <returns>Default <see cref="JsonSerializerSettings"/>.</returns>
        public static JsonSerializerSettings CreateSerializerSettings()
        {
            return new JsonSerializerSettings
            {
                ContractResolver = SharedContractResolver,

                MissingMemberHandling = MissingMemberHandling.Ignore,

                // Limit the object graph we'll consume to a fixed depth. This prevents stackoverflow exceptions
                // from deserialization errors that might occur from deeply nested objects.
                MaxDepth = DefaultMaxDepth,

                // Do not change this setting
                // Setting this to None prevents Json.NET from loading malicious, unsafe, or security-sensitive types
                TypeNameHandling = TypeNameHandling.None,
            };
        }

        // To enable unit testing
        internal static DefaultContractResolver CreateContractResolver()
        {
            return new DefaultContractResolver
            {
                NamingStrategy = new CamelCaseNamingStrategy(),
            };
        }
    }
}
