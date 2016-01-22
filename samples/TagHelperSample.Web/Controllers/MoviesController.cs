// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Mvc;
using TagHelperSample.Web.Services;

namespace TagHelperSample.Web.Controllers
{
    public class MoviesController : Controller
    {
        private MoviesService _moviesService;

        public MoviesController(MoviesService moviesService)
        {
            _moviesService = moviesService;
        }

        // Sample exhibiting the use of nested cache tag helpers with custom user expiration tokens.
        // Trigger expirations cascade, expiration of the inner tag helper's content either due to absolute or sliding
        // expiration or due to a user specified expiration token would cause the outer cache tag helper to also expire.
        public IActionResult Index()
        {
            ViewData["Title"] = "Movies";
            return View();
        }

        [HttpPost]
        public IActionResult UpdateMovieRatings()
        {
            _moviesService.UpdateMovieRating();

            ViewData["Title"] = "Movies with updated ratings";
            return View("Index");
        }

        [HttpPost]
        public IActionResult UpdateCriticsQuotes()
        {
            _moviesService.UpdateCriticsQuotes();

            ViewData["Title"] = "Movies with updated critics quotes";
            return View("Index");
        }
    }
}