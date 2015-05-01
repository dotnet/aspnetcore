// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Mvc;

namespace ResponseCacheWebSite.Controllers
{
    public class CacheProfilesController
    {
        [HttpGet("/CacheProfiles/PublicCache30Sec")]
        [ResponseCache(CacheProfileName = "PublicCache30Sec")]
        public string PublicCache30Sec()
        {
            return "Hello World!";
        }

        [HttpGet("/CacheProfiles/PrivateCache30Sec")]
        [ResponseCache(CacheProfileName = "PrivateCache30Sec")]
        public string PrivateCache30Sec()
        {
            return "Hello World!";
        }

        [HttpGet("/CacheProfiles/NoCache")]
        [ResponseCache(CacheProfileName = "NoCache")]
        public string NoCache()
        {
            return "Hello World!";
        }

        [HttpGet("/CacheProfiles/CacheProfileAddParameter")]
        [ResponseCache(CacheProfileName = "PublicCache30Sec", VaryByHeader = "Accept")]
        public string CacheProfileAddParameter()
        {
            return "Hello World!";
        }

        [HttpGet("/CacheProfiles/CacheProfileOverride")]
        [ResponseCache(CacheProfileName = "PublicCache30Sec", Duration = 10)]
        public string CacheProfileOverride()
        {
            return "Hello World!";
        }

        [HttpGet("/CacheProfiles/FallbackToFilter")]
        public string FallbackToFilter()
        {
            return "Hello World!";
        }
    }
}