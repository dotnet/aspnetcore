// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Primitives;
using TagHelperSample.Web.Models;
using TagHelperSample.Web.Services;

namespace TagHelperSample.Web.Components
{
    [ViewComponent(Name = "FeaturedMovies")]
    public class FeaturedMoviesComponent : ViewComponent
    {
        private readonly IMemoryCache _cache;
        private readonly MoviesService _moviesService;

        public FeaturedMoviesComponent(MoviesService moviesService, IMemoryCache cache)
        {
            _moviesService = moviesService;
            _cache = cache;
        }

        public IViewComponentResult Invoke()
        {
            // Since this component is invoked from within a CacheTagHelper,
            // cache the movie list and provide an expiration token, which when notified causes the
            // CacheTagHelper's cached data to be invalidated.
            var cacheKey = "featured_movies";
            IEnumerable<FeaturedMovies> movies;
            if (!_cache.TryGetValue(cacheKey, out movies))
            {
                IChangeToken expirationToken;
                movies = _moviesService.GetFeaturedMovies(out expirationToken);
                _cache.Set(cacheKey, movies, new MemoryCacheEntryOptions().AddExpirationToken(expirationToken));
            }

            return View(movies);
        }
    }
}