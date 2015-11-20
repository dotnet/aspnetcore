// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Mvc.DataAnnotations;
using Microsoft.AspNet.Mvc.DataAnnotations.Internal;
using Microsoft.AspNet.Mvc.Formatters.Json.Internal;
using Microsoft.AspNet.Mvc.Internal;
using Microsoft.AspNet.Mvc.ModelBinding;
using Microsoft.AspNet.Mvc.ModelBinding.Metadata;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.OptionsModel;

namespace Microsoft.AspNet.Mvc.IntegrationTests
{
    public class TestMvcOptions : IOptions<MvcOptions>
    {
        public TestMvcOptions()
        {
            Value = new MvcOptions();
            MvcCoreMvcOptionsSetup.ConfigureMvc(Value, new TestHttpRequestStreamReaderFactory());
            var collection = new ServiceCollection().AddOptions();
            collection.AddSingleton<ICompositeMetadataDetailsProvider, DefaultCompositeMetadataDetailsProvider>();
            collection.AddSingleton<IModelMetadataProvider, DefaultModelMetadataProvider>();
            collection.AddSingleton<IValidationAttributeAdapterProvider, ValidationAttributeAdapterProvider>();
            MvcDataAnnotationsMvcOptionsSetup.ConfigureMvc(
                Value,
                collection.BuildServiceProvider());

            var loggerFactory = new LoggerFactory();
            var serializerSettings = SerializerSettingsProvider.CreateSerializerSettings();

            MvcJsonMvcOptionsSetup.ConfigureMvc(Value, serializerSettings, loggerFactory);
        }

        public MvcOptions Value { get; }
    }
}