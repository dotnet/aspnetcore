// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Extensions.ObjectPool;
using Newtonsoft.Json;

namespace Microsoft.AspNetCore.Mvc.Formatters
{
    /// <summary>
    /// <see cref="IPooledObjectPolicy{T}"/> for <see cref="JsonSerializer"/>.
    /// </summary>
    internal class JsonSerializerObjectPolicy : IPooledObjectPolicy<JsonSerializer>
    {
        private readonly JsonSerializerSettings _serializerSettings;

        /// <summary>
        /// Initializes a new instance of <see cref="JsonSerializerObjectPolicy"/>.
        /// </summary>
        /// <param name="serializerSettings">The <see cref="JsonSerializerSettings"/> used to instantiate
        /// <see cref="JsonSerializer"/> instances.</param>
        public JsonSerializerObjectPolicy(JsonSerializerSettings serializerSettings)
        {
            _serializerSettings = serializerSettings;
        }

        /// <inheritdoc />
        public JsonSerializer Create() => JsonSerializer.Create(_serializerSettings);

        /// <inheritdoc />
        public bool Return(JsonSerializer serializer) => true;
    }
}