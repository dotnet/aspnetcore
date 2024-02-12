// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Extensions.Caching.Distributed;

public interface IReadThroughCacheSerializerFactory
{
    bool TryCreateSerializer<T>([NotNullWhen(true)] out IReadThroughCacheSerializer<T>? serializer);
}
