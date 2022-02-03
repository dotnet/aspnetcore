// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.Localization;

internal static partial class RequestCultureProviderLoggerExtensions
{
    [LoggerMessage(1, LogLevel.Debug, "{requestCultureProvider} returned the following unsupported cultures '{cultures}'.", EventName = "UnsupportedCulture")]
    public static partial void UnsupportedCultures(this ILogger logger, string requestCultureProvider, IList<StringSegment> cultures);

    [LoggerMessage(2, LogLevel.Debug, "{requestCultureProvider} returned the following unsupported UI Cultures '{uiCultures}'.", EventName = "UnsupportedUICulture")]
    public static partial void UnsupportedUICultures(this ILogger logger, string requestCultureProvider, IList<StringSegment> uiCultures);
}
