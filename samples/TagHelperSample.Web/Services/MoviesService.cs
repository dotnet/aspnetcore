// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.Framework.Caching;
using Microsoft.Framework.Caching.Memory;
using TagHelperSample.Web.Models;

namespace TagHelperSample.Web.Services
{
    public class MoviesService
    {
        private readonly Random _random = new Random();

        private CancellationTokenSource _featuredMoviesTokenSource;
        private CancellationTokenSource _quotesTokenSource;

        public IEnumerable<FeaturedMovies> GetFeaturedMovies(out IExpirationTrigger expirationTrigger)
        {
            _featuredMoviesTokenSource = new CancellationTokenSource();

            expirationTrigger = new CancellationTokenTrigger(_featuredMoviesTokenSource.Token);
            return GetMovies().OrderBy(m => m.Rank).Take(2);
        }

        public void UpdateMovieRating()
        {
            _featuredMoviesTokenSource.Cancel();
            _featuredMoviesTokenSource.Dispose();
            _featuredMoviesTokenSource = null;
        }

        public string GetCriticsQuote(out IExpirationTrigger trigger)
        {
            _quotesTokenSource = new CancellationTokenSource();

            var quotes = new[]
            {
                "A must see for iguana lovers everywhere",
                "Slightly better than watching paint dry",
                "Never felt more relieved seeing the credits roll",
                "Bravo!"
            };

            trigger = new CancellationTokenTrigger(_quotesTokenSource.Token);
            return quotes[_random.Next(0, quotes.Length)];
        }

        public void UpdateCriticsQuotes()
        {
            _quotesTokenSource.Cancel();
            _quotesTokenSource.Dispose();
            _quotesTokenSource = null;
        }

        private IEnumerable<FeaturedMovies> GetMovies()
        {
            yield return new FeaturedMovies { Name = "A day in the life of a blue whale", Rank = _random.Next(1, 10) };
            yield return new FeaturedMovies { Name = "FlashForward", Rank = _random.Next(1, 10) };
            yield return new FeaturedMovies { Name = "Frontier", Rank = _random.Next(1, 10) };
            yield return new FeaturedMovies { Name = "Attack of the space spiders", Rank = _random.Next(1, 10) };
            yield return new FeaturedMovies { Name = "Rift 3", Rank = _random.Next(1, 10) };
        }
    }
}