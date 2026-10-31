// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Microsoft.AspNetCore.OutputCaching;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOutputCache(options =>
{
    // Define policies for all requests which are not configured per endpoint or per request
    options.AddBasePolicy(builder => builder.With(c => c.HttpContext.Request.Path.StartsWithSegments("/js")).Expire(TimeSpan.FromDays(1)));
    options.AddBasePolicy(builder => builder.With(c => c.HttpContext.Request.Path.StartsWithSegments("/js")).NoCache());

    options.AddPolicy("NoCache", b => b.NoCache());
});

var app = builder.Build();

app.UseOutputCache();

app.MapGet("/", Gravatar.WriteGravatar);

app.MapGet("/cached", Gravatar.WriteGravatar).CacheOutput();

app.MapGet("/nocache", Gravatar.WriteGravatar).CacheOutput(x => x.NoCache());

app.MapGet("/profile", Gravatar.WriteGravatar).CacheOutput("NoCache");

app.MapGet("/attribute", [OutputCache(PolicyName = "NoCache")] (context) => Gravatar.WriteGravatar(context));

var blog = app.MapGroup("blog").CacheOutput(x => x.Tag("blog"));
blog.MapGet("/", Gravatar.WriteGravatar);
blog.MapGet("/post/{id}", Gravatar.WriteGravatar).CacheOutput(x => x.Tag("blog", "byid")); // Calling CacheOutput() here overwrites the group's policy

app.MapPost("/purge/{tag}", async (IOutputCacheStore cache, string tag) =>
{
    // POST such that the endpoint is not cached itself

    await cache.EvictByTagAsync(tag, default);
});

// Cached entries will vary by culture, but any other additional query is ignored and returns the same cached content
app.MapGet("/query", Gravatar.WriteGravatar).CacheOutput(p => p.SetVaryByQuery("culture"));

app.MapGet("/vary", Gravatar.WriteGravatar).CacheOutput(c => c.VaryByValue((context) => new KeyValuePair<string, string>("time", (DateTime.Now.Second % 2).ToString(CultureInfo.InvariantCulture))));

long requests = 0;

// Locking is enabled by default
app.MapGet("/lock", async (context) =>
{
    await Task.Delay(1000);
    await context.Response.WriteAsync($"<pre>{requests++}</pre>");
}).CacheOutput(p => p.SetLocking(false).Expire(TimeSpan.FromMilliseconds(1)));

// Etag
app.MapGet("/etag", async (context) =>
{
    // If the client sends an If-None-Match header with the etag value, the server
    // returns 304 if the cache entry is fresh instead of the full response

    var etag = $"\"{Guid.NewGuid():n}\"";
    context.Response.Headers.ETag = etag;

    await Gravatar.WriteGravatar(context);

    var cacheContext = context.Features.Get<IOutputCacheFeature>()?.Context;

}).CacheOutput();

// When the request header If-Modified-Since is provided, return 304 if the cached entry is older
app.MapGet("/ims", Gravatar.WriteGravatar).CacheOutput();

await app.RunAsync();
