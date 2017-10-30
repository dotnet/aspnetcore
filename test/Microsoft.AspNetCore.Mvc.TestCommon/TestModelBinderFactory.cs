// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Mvc.Internal;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Mvc.ModelBinding
{
    public class TestModelBinderFactory : ModelBinderFactory
    {
        public static TestModelBinderFactory Create(params IModelBinderProvider[] providers)
        {
            return Create(null, providers);
        }

        public static TestModelBinderFactory Create(
            IModelMetadataProvider metadataProvider,
            params IModelBinderProvider[] providers)
        {
            if (metadataProvider == null)
            {
                metadataProvider = TestModelMetadataProvider.CreateDefaultProvider();
            }

            var options = Options.Create(new MvcOptions());
            foreach (var provider in providers)
            {
                options.Value.ModelBinderProviders.Add(provider);
            }
            return new TestModelBinderFactory(metadataProvider, options);
        }

        public static TestModelBinderFactory CreateDefault(params IModelBinderProvider[] providers)
        {
            return CreateDefault(null, providers);
        }

        public static TestModelBinderFactory CreateDefault(
            IModelMetadataProvider metadataProvider, 
            params IModelBinderProvider[] providers)
        {
            if (metadataProvider == null)
            {
                metadataProvider = TestModelMetadataProvider.CreateDefaultProvider();
            }

            var options = Options.Create(new MvcOptions());
            foreach (var provider in providers)
            {
                options.Value.ModelBinderProviders.Add(provider);
            }
            new MvcCoreMvcOptionsSetup(new TestHttpRequestStreamReaderFactory()).Configure(options.Value);
            return new TestModelBinderFactory(metadataProvider, options);
        }

        protected TestModelBinderFactory(IModelMetadataProvider metadataProvider, IOptions<MvcOptions> options) 
            : base(metadataProvider, options)
        {
        }
    }
}
