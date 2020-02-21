// Copyright (c) .NET Foundation. All rights reserved. 
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information. 

using System;
using System.Globalization;
using Microsoft.Extensions.Logging;

namespace Microsoft.Extensions.Localization.Internal
{
    internal static class ResourceManagerStringLocalizerLoggerExtensions
    {
        private static readonly Action<ILogger, string, string, CultureInfo, Exception> _searchedLocation;

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
