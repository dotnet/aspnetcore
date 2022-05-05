// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.ObjectPool;
using Newtonsoft.Json;

namespace Microsoft.AspNetCore.Mvc.NewtonsoftJson;

/// <summary>
/// <see cref="IPooledObjectPolicy{T}"/> for <see cref="JsonSerializer"/>.
/// </summary>
internal sealed class JsonSerializerObjectPolicy : IPooledObjectPolicy<JsonSerializer>
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
