// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Http.Core;
using Xunit;

namespace Microsoft.AspNet.Builder.Extensions
{
    using AppFunc = Func<IDictionary<string, object>, Task>;
    using Predicate = Func<HttpContext, bool>;
    using PredicateAsync = Func<HttpContext, Task<bool>>;

    public class MapPredicateMiddlewareTests
    {
        private static readonly Predicate NotImplementedPredicate = new Predicate(envionment => { throw new NotImplementedException(); });
        private static readonly PredicateAsync NotImplementedPredicateAsync = new PredicateAsync(envionment => { throw new NotImplementedException(); });

        private static Task Success(HttpContext context)
        {
            context.Response.StatusCode = 200;
            return Task.FromResult<object>(null);
        }

        private static void UseSuccess(IApplicationBuilder app)
        {
            app.Run(Success);
        }

        private static Task NotImplemented(HttpContext context)
        {
            throw new NotImplementedException();
        }

        private static void UseNotImplemented(IApplicationBuilder app)
        {
            app.Run(NotImplemented);
        }

        private bool TruePredicate(HttpContext context)
        {
            return true;
        }

        private bool FalsePredicate(HttpContext context)
        {
            return false;
        }

        private Task<bool> TruePredicateAsync(HttpContext context)
        {
            return Task.FromResult<bool>(true);
        }

        private Task<bool> FalsePredicateAsync(HttpContext context)
        {
            return Task.FromResult<bool>(false);
        }

        [Fact]
        public void NullArguments_ArgumentNullException()
        {
            var builder = new ApplicationBuilder(serviceProvider: null);
            var noMiddleware = new ApplicationBuilder(serviceProvider: null).Build();
            var noOptions = new MapWhenOptions();
            // TODO: [NotNull] Assert.Throws<ArgumentNullException>(() => builder.MapWhen(null, UseNotImplemented));
            // TODO: [NotNull] Assert.Throws<ArgumentNullException>(() => builder.MapWhen(NotImplementedPredicate, (Action<IBuilder>)null));
            // TODO: [NotNull] Assert.Throws<ArgumentNullException>(() => new MapWhenMiddleware(null, noOptions));
            // TODO: [NotNull] Assert.Throws<ArgumentNullException>(() => new MapWhenMiddleware(noMiddleware, null));

            // TODO: [NotNull] Assert.Throws<ArgumentNullException>(() => builder.MapWhenAsync(null, UseNotImplemented));
            // TODO: [NotNull] Assert.Throws<ArgumentNullException>(() => builder.MapWhenAsync(NotImplementedPredicateAsync, (Action<IBuilder>)null));
            // TODO: [NotNull] Assert.Throws<ArgumentNullException>(() => new MapWhenMiddleware(null, noOptions));
            // TODO: [NotNull] Assert.Throws<ArgumentNullException>(() => new MapWhenMiddleware(noMiddleware, null));
        }

        [Fact]
        public void PredicateTrue_BranchTaken()
        {
            HttpContext context = CreateRequest();
            var builder = new ApplicationBuilder(serviceProvider: null);
            builder.MapWhen(TruePredicate, UseSuccess);
            var app = builder.Build();
            app.Invoke(context).Wait();

            Assert.Equal(200, context.Response.StatusCode);
        }

        [Fact]
        public void PredicateTrueAction_BranchTaken()
        {
            HttpContext context = CreateRequest();
            var builder = new ApplicationBuilder(serviceProvider: null);
            builder.MapWhen(TruePredicate, UseSuccess);
            var app = builder.Build();
            app.Invoke(context).Wait();

            Assert.Equal(200, context.Response.StatusCode);
        }

        [Fact]
        public void PredicateFalseAction_PassThrough()
        {
            HttpContext context = CreateRequest();
            var builder = new ApplicationBuilder(serviceProvider: null);
            builder.MapWhen(FalsePredicate, UseNotImplemented);
            builder.Run(Success);
            var app = builder.Build();
            app.Invoke(context).Wait();

            Assert.Equal(200, context.Response.StatusCode);
        }

        [Fact]
        public void PredicateAsyncTrueAction_BranchTaken()
        {
            HttpContext context = CreateRequest();
            var builder = new ApplicationBuilder(serviceProvider: null);
            builder.MapWhenAsync(TruePredicateAsync, UseSuccess);
            var app = builder.Build();
            app.Invoke(context).Wait();

            Assert.Equal(200, context.Response.StatusCode);
        }

        [Fact]
        public void PredicateAsyncFalseAction_PassThrough()
        {
            HttpContext context = CreateRequest();
            var builder = new ApplicationBuilder(serviceProvider: null);
            builder.MapWhenAsync(FalsePredicateAsync, UseNotImplemented);
            builder.Run(Success);
            var app = builder.Build();
            app.Invoke(context).Wait();

            Assert.Equal(200, context.Response.StatusCode);
        }

        [Fact]
        public void ChainedPredicates_Success()
        {
            var builder = new ApplicationBuilder(serviceProvider: null);
            builder.MapWhen(TruePredicate, map1 =>
            {
                map1.MapWhen((Predicate)FalsePredicate, UseNotImplemented);
                map1.MapWhen((Predicate)TruePredicate, map2 => map2.MapWhen((Predicate)TruePredicate, UseSuccess));
                map1.Run(NotImplemented);
            });
            var app = builder.Build();

            HttpContext context = CreateRequest();
            app.Invoke(context).Wait();
            Assert.Equal(200, context.Response.StatusCode);
        }

        [Fact]
        public void ChainedPredicatesAsync_Success()
        {
            var builder = new ApplicationBuilder(serviceProvider: null);
            builder.MapWhenAsync(TruePredicateAsync, map1 =>
            {
                map1.MapWhenAsync((PredicateAsync)FalsePredicateAsync, UseNotImplemented);
                map1.MapWhenAsync((PredicateAsync)TruePredicateAsync, map2 => map2.MapWhenAsync((PredicateAsync)TruePredicateAsync, UseSuccess));
                map1.Run(NotImplemented);
            });
            var app = builder.Build();

            HttpContext context = CreateRequest();
            app.Invoke(context).Wait();
            Assert.Equal(200, context.Response.StatusCode);
        }

        private HttpContext CreateRequest()
        {
            HttpContext context = new DefaultHttpContext();
            return context;
        }
    }
}
