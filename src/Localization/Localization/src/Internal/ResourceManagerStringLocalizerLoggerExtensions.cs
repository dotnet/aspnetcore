// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Globalization;
using Microsoft.Extensions.Logging;

namespace Microsoft.Extensions.Localization.Internal
{
    internal static class ResourceManagerStringLocalizerLoggerExtensions
    {
        private static readonly Action<ILogger, string, string, CultureInfo, Exception?> _searchedLocation;

        static ResourceManagerStringLocalizerLoggerExtensions()
        {
            _searchedLocation = LoggerMessage.Define<string, string, CultureInfo>(
                LogLevel.Debug,
                new EventId(1, "SearchedLocation"),
                $"{nameof(ResourceManagerStringLocalizer)} searched for '{{Key}}' in '{{LocationSearched}}' with culture '{{Culture}}'.");
        }

        public static void SearchedLocation(this ILogger logger, string key, string searchedLocation, CultureInfo culture)
        {
            _searchedLocation(logger, key, searchedLocation, culture, null);
        }
    }
}
