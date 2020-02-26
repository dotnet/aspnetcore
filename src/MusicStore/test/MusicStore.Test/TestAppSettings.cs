// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Extensions.Options;

namespace MusicStore.Test
{
    public class TestAppSettings : IOptions<AppSettings>
    {
        private readonly AppSettings _appSettings;

        public TestAppSettings(bool storeInCache = true)
        {
            _appSettings = new AppSettings()
            {
                SiteTitle = "ASP.NET MVC Music Store",
                CacheDbResults = storeInCache
            };
        }

        public AppSettings Value
        {
            get
            {
                return _appSettings;
            }
        }
    }
}
