// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Mvc.NewtonsoftJson
{
    internal partial class BsonTempDataSerializer : Microsoft.AspNetCore.Mvc.ViewFeatures.Infrastructure.TempDataSerializer
    {
        public BsonTempDataSerializer() { }
        public override bool CanSerializeType(System.Type type) { throw null; }
        public override System.Collections.Generic.IDictionary<string, object> Deserialize(byte[] value) { throw null; }
        public void EnsureObjectCanBeSerialized(object item) { }
        public override byte[] Serialize(System.Collections.Generic.IDictionary<string, object> values) { throw null; }
    }

    internal partial class NewtonsoftJsonResultExecutor : Microsoft.AspNetCore.Mvc.Infrastructure.IActionResultExecutor<Microsoft.AspNetCore.Mvc.JsonResult>
    {
        public NewtonsoftJsonResultExecutor(Microsoft.AspNetCore.Mvc.Infrastructure.IHttpResponseStreamWriterFactory writerFactory, Microsoft.Extensions.Logging.ILogger<Microsoft.AspNetCore.Mvc.NewtonsoftJson.NewtonsoftJsonResultExecutor> logger, Microsoft.Extensions.Options.IOptions<Microsoft.AspNetCore.Mvc.MvcOptions> mvcOptions, Microsoft.Extensions.Options.IOptions<Microsoft.AspNetCore.Mvc.MvcNewtonsoftJsonOptions> jsonOptions, System.Buffers.ArrayPool<char> charPool) { }
        public System.Threading.Tasks.Task ExecuteAsync(Microsoft.AspNetCore.Mvc.ActionContext context, Microsoft.AspNetCore.Mvc.JsonResult result) { throw null; }
    }

    internal sealed partial class JsonPatchOperationsArrayProvider : Microsoft.AspNetCore.Mvc.ApiExplorer.IApiDescriptionProvider
    {
        public JsonPatchOperationsArrayProvider(Microsoft.AspNetCore.Mvc.ModelBinding.IModelMetadataProvider modelMetadataProvider) { }
        public int Order { get { throw null; } }
        public void OnProvidersExecuted(Microsoft.AspNetCore.Mvc.ApiExplorer.ApiDescriptionProviderContext context) { }
        public void OnProvidersExecuting(Microsoft.AspNetCore.Mvc.ApiExplorer.ApiDescriptionProviderContext context) { }
    }

    internal partial class NewtonsoftJsonHelper : Microsoft.AspNetCore.Mvc.Rendering.IJsonHelper
    {
        public NewtonsoftJsonHelper(Microsoft.Extensions.Options.IOptions<Microsoft.AspNetCore.Mvc.MvcNewtonsoftJsonOptions> options, System.Buffers.ArrayPool<char> charPool) { }
        public Microsoft.AspNetCore.Html.IHtmlContent Serialize(object value) { throw null; }
        public Microsoft.AspNetCore.Html.IHtmlContent Serialize(object value, Newtonsoft.Json.JsonSerializerSettings serializerSettings) { throw null; }
    }

    internal static partial class Resources
    {
        internal static string ContractResolverCannotBeNull { get { throw null; } }
        internal static System.Globalization.CultureInfo Culture { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        internal static string InvalidContractResolverForJsonCasingConfiguration { get { throw null; } }
        internal static string JsonHelperMustBeAnInstanceOfNewtonsoftJson { get { throw null; } }
        internal static string ObjectResultExecutor_MaxEnumerationExceeded { get { throw null; } }
        internal static string Property_MustBeInstanceOfType { get { throw null; } }
        internal static System.Resources.ResourceManager ResourceManager { get { throw null; } }
        internal static string TempData_CannotDeserializeToken { get { throw null; } }
        internal static string TempData_CannotSerializeDictionary { get { throw null; } }
        internal static string TempData_CannotSerializeType { get { throw null; } }
        internal static string FormatContractResolverCannotBeNull(object p0) { throw null; }
        internal static string FormatInvalidContractResolverForJsonCasingConfiguration(object p0, object p1) { throw null; }
        internal static string FormatJsonHelperMustBeAnInstanceOfNewtonsoftJson(object p0, object p1, object p2, object p3) { throw null; }
        internal static string FormatObjectResultExecutor_MaxEnumerationExceeded(object p0, object p1) { throw null; }
        internal static string FormatProperty_MustBeInstanceOfType(object p0, object p1, object p2) { throw null; }
        internal static string FormatTempData_CannotDeserializeToken(object p0, object p1) { throw null; }
        internal static string FormatTempData_CannotSerializeDictionary(object p0, object p1, object p2) { throw null; }
        internal static string FormatTempData_CannotSerializeType(object p0, object p1) { throw null; }
    }

    internal sealed partial class AsyncEnumerableReader
    {
        public AsyncEnumerableReader(Microsoft.AspNetCore.Mvc.MvcOptions mvcOptions) { }
        public System.Threading.Tasks.Task<System.Collections.ICollection> ReadAsync(System.Collections.Generic.IAsyncEnumerable<object> value) { throw null; }
    }

    public static partial class JsonSerializerSettingsProvider
    {
        internal static Newtonsoft.Json.Serialization.DefaultContractResolver CreateContractResolver() { throw null; }
    }
}

namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class NewtonsoftJsonMvcCoreBuilderExtensions
    {
        internal static void AddServicesCore(Microsoft.Extensions.DependencyInjection.IServiceCollection services) { }
    }
    internal partial class NewtonsoftJsonMvcOptionsSetup : Microsoft.Extensions.Options.IConfigureOptions<Microsoft.AspNetCore.Mvc.MvcOptions>
    {
        public NewtonsoftJsonMvcOptionsSetup(Microsoft.Extensions.Logging.ILoggerFactory loggerFactory, Microsoft.Extensions.Options.IOptions<Microsoft.AspNetCore.Mvc.MvcNewtonsoftJsonOptions> jsonOptions, System.Buffers.ArrayPool<char> charPool, Microsoft.Extensions.ObjectPool.ObjectPoolProvider objectPoolProvider) { }
        public void Configure(Microsoft.AspNetCore.Mvc.MvcOptions options) { }
    }
}