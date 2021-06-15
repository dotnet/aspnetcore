// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Reflection.Metadata;
using System.Threading;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using Microsoft.Extensions.Primitives;

[assembly: MetadataUpdateHandler(typeof(Microsoft.AspNetCore.Mvc.HotReload.HotReloadService))]

namespace Microsoft.AspNetCore.Mvc.HotReload
{
    internal sealed class HotReloadService : IActionDescriptorChangeProvider, IDisposable
    {
        private readonly DefaultModelMetadataProvider? _modelMetadataProvider;
        private readonly DefaultControllerPropertyActivator? _controllerPropertyActivator;
        private CancellationTokenSource _tokenSource = new();

        public HotReloadService(
            IModelMetadataProvider modelMetadataProvider,
            IControllerPropertyActivator controllerPropertyActivator)
        {
            ClearCacheEvent += NotifyClearCache;
            if (modelMetadataProvider.GetType() == typeof(DefaultModelMetadataProvider))
            {
                _modelMetadataProvider = (DefaultModelMetadataProvider)modelMetadataProvider;
            }

            if (controllerPropertyActivator is DefaultControllerPropertyActivator defaultControllerPropertyActivator)
            {
                _controllerPropertyActivator = defaultControllerPropertyActivator;
            }
        }

        public static event Action? ClearCacheEvent;

        public static void ClearCache(Type[]? _)
        {
            ClearCacheEvent?.Invoke();
        }

        IChangeToken IActionDescriptorChangeProvider.GetChangeToken() => new CancellationChangeToken(_tokenSource.Token);

        private void NotifyClearCache()
        {
            // Trigger the ActionDescriptorChangeProvider
            var current = Interlocked.Exchange(ref _tokenSource, new CancellationTokenSource());
            current.Cancel();

            // Clear individual caches
            _modelMetadataProvider?.ClearCache();
            _controllerPropertyActivator?.ClearCache();
        }

        public void Dispose()
        {
            ClearCacheEvent -= NotifyClearCache;
        }
    }
}
