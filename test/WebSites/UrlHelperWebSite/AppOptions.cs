using System;

namespace UrlHelperWebSite
{
    public class AppOptions
    {
        public bool ServeCDNContent { get; set; }

        public string CDNServerBaseUrl { get; set; }

        public bool GenerateLowercaseUrls { get; set; }
    }
}