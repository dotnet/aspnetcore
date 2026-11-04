// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Matching;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure;

internal sealed class PageLoaderMatcherPolicy : MatcherPolicy, IEndpointSelectorPolicy
{
    private PageLoader? _loader;

    /// <remarks>
    /// The <see cref="PageLoader"/> service is configured by <c>app.AddRazorPages()</c>.
    /// If the app is configured as <c>app.AddControllersWithViews().AddRazorRuntimeCompilation()</c>, the <see cref="PageLoader"/>
    /// service will not be registered. Since Razor Pages is not a pre-req for runtime compilation, we'll defer reading the service
    /// until we need to load a page in the body of <see cref="ApplyAsync(HttpContext, CandidateSet)"/>.
    /// </remarks>
    public PageLoaderMatcherPolicy()
        : this(loader: null)
    {
    }

    public PageLoaderMatcherPolicy(PageLoader? loader)
    {
        _loader = loader;
    }

    public override int Order => int.MinValue + 100;

    public bool AppliesToEndpoints(IReadOnlyList<Endpoint> endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        // We don't mark Pages as dynamic endpoints because that causes all matcher policies
        // to run in *slow mode*. Instead we produce the same metadata for things that would affect matcher
        // policies on both endpoints (uncompiled and compiled).
        //
        // This means that something like putting [Consumes] on a page wouldn't work. We've never said that it would.
        for (var i = 0; i < endpoints.Count; i++)
        {
            var page = endpoints[i].Metadata.GetMetadata<PageActionDescriptor>();
            if (page is not null and not CompiledPageActionDescriptor)
            {
                // Found an uncompiled page
                return true;
            }
        }

        return false;
    }

    public Task ApplyAsync(HttpContext httpContext, CandidateSet candidates)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        ArgumentNullException.ThrowIfNull(candidates);

        for (var i = 0; i < candidates.Count; i++)
        {
            if (!candidates.IsValidCandidate(i))
            {
                continue;
            }

            ref var candidate = ref candidates[i];
            var endpoint = candidate.Endpoint;

            var page = endpoint.Metadata.GetMetadata<PageActionDescriptor>();
            if (page != null)
            {
                _loader ??= httpContext.RequestServices.GetRequiredService<PageLoader>();

                // We found an endpoint instance that has a PageActionDescriptor, but not a
                // CompiledPageActionDescriptor. Update the CandidateSet.
                var compiled = _loader.LoadAsync(page, endpoint.Metadata);

                if (compiled.IsCompletedSuccessfully)
                {
                    candidates.ReplaceEndpoint(i, compiled.Result.Endpoint, candidate.Values);
                }
                else
                {
                    // In the most common case, GetOrAddAsync will return a synchronous result.
                    // Avoid going async since this is a fairly hot path.
                    return ApplyAsyncAwaited(_loader, candidates, compiled, i);
                }
            }
        }

        return Task.CompletedTask;
    }

    private static async Task ApplyAsyncAwaited(PageLoader pageLoader, CandidateSet candidates, Task<CompiledPageActionDescriptor> actionDescriptorTask, int index)
    {
        var compiled = await actionDescriptorTask;

        candidates.ReplaceEndpoint(index, compiled.Endpoint, candidates[index].Values);

        for (var i = index + 1; i < candidates.Count; i++)
        {
            if (!candidates.IsValidCandidate(i))
            {
                continue;
            }

            var candidate = candidates[i];
            var endpoint = candidate.Endpoint;

            var page = endpoint.Metadata.GetMetadata<PageActionDescriptor>();
            if (page != null)
            {
                compiled = await pageLoader.LoadAsync(page, endpoint.Metadata);

                candidates.ReplaceEndpoint(i, compiled.Endpoint, candidates[i].Values);
            }
        }
    }
}
