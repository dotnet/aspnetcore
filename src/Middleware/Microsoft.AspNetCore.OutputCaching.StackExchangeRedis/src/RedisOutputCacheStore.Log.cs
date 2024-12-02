// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace Microsoft.AspNetCore.OutputCaching.StackExchangeRedis;

internal partial class RedisOutputCacheStore
{
    [LoggerMessage(1, LogLevel.Warning, "Transient error occurred executing redis output-cache GC loop.", EventName = "RedisOutputCacheGCTransientError")]
    internal static partial void RedisOutputCacheGCTransientFault(ILogger logger, Exception exception);

    [LoggerMessage(2, LogLevel.Error, "Fatal error occurred executing redis output-cache GC loop.", EventName = "RedisOutputCacheGCFatalError")]
    internal static partial void RedisOutputCacheGCFatalError(ILogger logger, Exception exception);

    [LoggerMessage(3, LogLevel.Debug, "Unable to add library name suffix.", EventName = "UnableToAddLibraryNameSuffix")]
    internal static partial void UnableToAddLibraryNameSuffix(ILogger logger, Exception exception);
}
