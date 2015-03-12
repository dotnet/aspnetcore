// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.AspNet.Mvc;
using Microsoft.Framework.Caching.Memory;
using Microsoft.Framework.Expiration.Interfaces;
using TagHelperSample.Web.Services;

namespace TagHelperSample.Web.Components
{
    [ViewComponent(Name = "FeaturedMovies")]
    public class FeaturedMoviesComponent : ViewComponent
    {
        private MoviesService _moviesService;

        public FeaturedMoviesComponent(MoviesService moviesService)
        {
            _moviesService = moviesService;
        }

        public IViewComponentResult Invoke()
        {
            IExpirationTrigger trigger;
            var movies = _moviesService.GetFeaturedMovies(out trigger);
            
            // Add custom triggers
            EntryLinkHelpers.ContextLink.AddExpirationTriggers(new[] { trigger });

            return View(movies);
        }

        public IViewComponentResult Invoke(string movieName)
        {
            IExpirationTrigger trigger;
            var quote = _moviesService.GetCriticsQuote(out trigger);

            // This is invoked as part of a nested cache tag helper.
            EntryLinkHelpers.ContextLink.AddExpirationTriggers(new[] { trigger });

            return Content(quote);
        }
    }
}