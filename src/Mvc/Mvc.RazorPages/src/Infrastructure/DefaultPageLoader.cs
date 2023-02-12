// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.Razor.Compilation;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure;

internal sealed class DefaultPageLoader : PageLoader
{
    private readonly IViewCompilerProvider _viewCompilerProvider;
    private readonly CompiledPageActionDescriptorFactory _compiledPageActionDescriptorFactory;
    private readonly ActionEndpointFactory _endpointFactory;

    public DefaultPageLoader(
        IEnumerable<IPageApplicationModelProvider> applicationModelProviders,
        IViewCompilerProvider viewCompilerProvider,
        ActionEndpointFactory endpointFactory,
        IOptions<RazorPagesOptions> pageOptions,
        IOptions<MvcOptions> mvcOptions)
    {
        _viewCompilerProvider = viewCompilerProvider;
        _endpointFactory = endpointFactory;
        _compiledPageActionDescriptorFactory = new CompiledPageActionDescriptorFactory(applicationModelProviders, mvcOptions.Value, pageOptions.Value);
    }

    private IViewCompiler Compiler => _viewCompilerProvider.GetCompiler();

    [Obsolete]
    public override Task<CompiledPageActionDescriptor> LoadAsync(PageActionDescriptor actionDescriptor)
        => LoadAsync(actionDescriptor, EndpointMetadataCollection.Empty);

    public override Task<CompiledPageActionDescriptor> LoadAsync(PageActionDescriptor actionDescriptor, EndpointMetadataCollection endpointMetadata)
    {
        ArgumentNullException.ThrowIfNull(actionDescriptor);

        if (actionDescriptor is CompiledPageActionDescriptor compiledPageActionDescriptor)
        {
            // It's possible for some code paths of PageLoaderMatcherPolicy to invoke LoadAsync with an instance
            // of CompiledPageActionDescriptor. In that case, we'll return the instance as-is.
            compiledPageActionDescriptor.CompiledPageActionDescriptorTask ??= Task.FromResult(compiledPageActionDescriptor);
            return compiledPageActionDescriptor.CompiledPageActionDescriptorTask;
        }

        var task = actionDescriptor.CompiledPageActionDescriptorTask;

        if (task != null)
        {
            return task;
        }

        return actionDescriptor.CompiledPageActionDescriptorTask = LoadAsyncCore(actionDescriptor, endpointMetadata);
    }

    private async Task<CompiledPageActionDescriptor> LoadAsyncCore(PageActionDescriptor actionDescriptor, EndpointMetadataCollection endpointMetadata)
    {
        var viewDescriptor = await Compiler.CompileAsync(actionDescriptor.RelativePath);
        var compiled = _compiledPageActionDescriptorFactory.CreateCompiledDescriptor(actionDescriptor, viewDescriptor);

        var endpoints = new List<Endpoint>();
        _endpointFactory.AddEndpoints(
            endpoints,
            routeNames: new HashSet<string>(StringComparer.OrdinalIgnoreCase),
            action: compiled,
            routes: Array.Empty<ConventionalRouteEntry>(),
            conventions: new Action<EndpointBuilder>[]
            {
                    b =>
                    {
                        // Copy Endpoint metadata for PageActionActionDescriptor to the compiled one.
                        // This is particularly important for the runtime compiled scenario where endpoint metadata is added
                        // to the PageActionDescriptor, which needs to be accounted for when constructing the
                        // CompiledPageActionDescriptor as part of the one of the many matcher policies.
                        // Metadata from PageActionDescriptor is less significant than the one discovered from the compiled type.
                        // Consequently, we'll insert it at the beginning.
                        for (var i = endpointMetadata.Count - 1; i >=0; i--)
                        {
                            b.Metadata.Insert(0, endpointMetadata[i]);
                        }
                    },
            },
            groupConventions: Array.Empty<Action<EndpointBuilder>>(),
            finallyConventions: Array.Empty<Action<EndpointBuilder>>(),
            groupFinallyConventions: Array.Empty<Action<EndpointBuilder>>(),
            createInertEndpoints: false);

        // In some test scenarios there's no route so the endpoint isn't created. This is fine because
        // it won't happen for real.
        compiled.Endpoint = endpoints.SingleOrDefault();

        return compiled;
    }
}
