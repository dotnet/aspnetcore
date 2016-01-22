// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Primitives;
using TagHelperSample.Web.Services;

namespace TagHelperSample.Web.Components
{
    [ViewComponent(Name = "Movies")]
    public class MoviesComponent : ViewComponent
    {
        private readonly IMemoryCache _cache;
        private readonly MoviesService _moviesService;

        public MoviesComponent(MoviesService moviesService, IMemoryCache cache)
        {
            _moviesService = moviesService;
            _cache = cache;
        }

        public IViewComponentResult Invoke(string movieName)
        {
            string quote;
            if (!_cache.TryGetValue(movieName, out quote))
            {
                IChangeToken expirationToken;
                quote = _moviesService.GetCriticsQuote(out expirationToken);
                _cache.Set(movieName, quote, new MemoryCacheEntryOptions().AddExpirationToken(expirationToken));
            }

            return Content(quote);
        }
    }
}