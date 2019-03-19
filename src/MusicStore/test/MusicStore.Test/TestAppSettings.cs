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
