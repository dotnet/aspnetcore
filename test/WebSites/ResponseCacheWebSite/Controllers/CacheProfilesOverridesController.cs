using System;
using Microsoft.AspNet.Mvc;

namespace ResponseCacheWebSite.Controllers
{
    public class CacheProfilesOverridesController
    {
        [HttpGet("/CacheProfileOverrides/PublicCache30SecTo15Sec")]
        [ResponseCache(CacheProfileName = "PublicCache30Sec", Duration = 15)]
        public string PublicCache30SecTo15Sec()
        {
            return "Hello World!";
        }

        [HttpGet("/CacheProfileOverrides/PublicCache30SecToPrivateCache")]
        [ResponseCache(CacheProfileName = "PublicCache30Sec", Location = ResponseCacheLocation.Client)]
        public string PublicCache30SecToPrivateCache()
        {
            return "Hello World!";
        }

        [HttpGet("/CacheProfileOverrides/PublicCache30SecToNoStore")]
        [ResponseCache(CacheProfileName = "PublicCache30Sec", NoStore = true)]
        public string PublicCache30SecToNoStore()
        {
            return "Hello World!";
        }

        [HttpGet("/CacheProfileOverrides/PublicCache30SecWithVaryByAcceptToVaryByTest")]
        [ResponseCache(CacheProfileName = "PublicCache30Sec", VaryByHeader = "Test")]
        public string PublicCache30SecWithVaryByAcceptToVaryByTest()
        {
            return "Hello World!";
        }

        [HttpGet("/CacheProfileOverrides/PublicCache30SecWithVaryByAcceptToVaryByNone")]
        [ResponseCache(CacheProfileName = "PublicCache30Sec", VaryByHeader = null)]
        public string PublicCache30SecWithVaryByAcceptToVaryByNone()
        {
            return "Hello World!";
        }
    }
}