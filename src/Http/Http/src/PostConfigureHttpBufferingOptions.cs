// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Http
{
    public class PostConfigureHttpBufferingOptions : IPostConfigureOptions<HttpBufferingOptions>
    {
        public void PostConfigure(string name, HttpBufferingOptions options)
        {
            if (string.IsNullOrEmpty(options.TempFileDirectory))
            {
                // Look for folders in the following order.
                options.TempFileDirectory =
                    Environment.GetEnvironmentVariable("ASPNETCORE_TEMP") ?? // ASPNETCORE_TEMP - User set temporary location.
                    Path.GetTempPath();                                      // Fall back.
            }
        }
    }
}
