// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Globalization;
using Microsoft.Extensions.Configuration;

namespace Microsoft.AspNetCore.Server.Kestrel
{
    public class KestrelServerPoolingParameters
    {
        public KestrelServerPoolingParameters(IConfiguration configuration)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            MaxPooledStreams = GetPooledCount(configuration["kestrel.maxPooledStreams"]);
            MaxPooledHeaders = GetPooledCount(configuration["kestrel.maxPooledHeaders"]);
        }

        public int MaxPooledStreams { get; set; }

        public int MaxPooledHeaders { get; set; }

        private static int GetPooledCount(string countString)
        {
            if (string.IsNullOrEmpty(countString))
            {
                return 0;
            }

            int count;
            if (int.TryParse(countString, NumberStyles.Integer, CultureInfo.InvariantCulture, out count))
            {
                return count;
            }

            return 0;
        }
    }
}
