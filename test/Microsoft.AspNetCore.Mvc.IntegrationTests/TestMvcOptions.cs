// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Buffers;
using Microsoft.AspNetCore.Mvc.DataAnnotations;
using Microsoft.AspNetCore.Mvc.DataAnnotations.Internal;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.Mvc.Formatters.Json.Internal;
using Microsoft.AspNetCore.Mvc.Internal;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Mvc.IntegrationTests
{
    public class TestMvcOptions : IOptions<MvcOptions>
    {
        public TestMvcOptions()
        {
            Value = new MvcOptions();
            var optionsSetup = new MvcCoreMvcOptionsSetup(new TestHttpRequestStreamReaderFactory());
            optionsSetup.Configure(Value);

            var collection = new ServiceCollection().AddOptions();
            collection.AddSingleton<ICompositeMetadataDetailsProvider, DefaultCompositeMetadataDetailsProvider>();
            collection.AddSingleton<IModelMetadataProvider, DefaultModelMetadataProvider>();
            collection.AddSingleton<IValidationAttributeAdapterProvider, ValidationAttributeAdapterProvider>();
            MvcDataAnnotationsMvcOptionsSetup.ConfigureMvc(
                Value,
                collection.BuildServiceProvider());

            var loggerFactory = new LoggerFactory();
            var serializerSettings = JsonSerializerSettingsProvider.CreateSerializerSettings();

            MvcJsonMvcOptionsSetup.ConfigureMvc(
                Value,
                serializerSettings,
                loggerFactory,
                ArrayPool<char>.Shared,
                new DefaultObjectPoolProvider());
        }

        public MvcOptions Value { get; }
    }
}