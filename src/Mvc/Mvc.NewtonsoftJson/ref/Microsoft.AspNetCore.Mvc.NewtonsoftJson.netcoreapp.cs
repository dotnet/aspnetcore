// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Mvc
{
    public static partial class JsonPatchExtensions
    {
        public static void ApplyTo<T>(this Microsoft.AspNetCore.JsonPatch.JsonPatchDocument<T> patchDoc, T objectToApplyTo, Microsoft.AspNetCore.Mvc.ModelBinding.ModelStateDictionary modelState) where T : class { }
        public static void ApplyTo<T>(this Microsoft.AspNetCore.JsonPatch.JsonPatchDocument<T> patchDoc, T objectToApplyTo, Microsoft.AspNetCore.Mvc.ModelBinding.ModelStateDictionary modelState, string prefix) where T : class { }
    }
    public partial class MvcNewtonsoftJsonOptions : System.Collections.Generic.IEnumerable<Microsoft.AspNetCore.Mvc.Infrastructure.ICompatibilitySwitch>, System.Collections.IEnumerable
    {
        public MvcNewtonsoftJsonOptions() { }
        public bool AllowInputFormatterExceptionMessages { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public Newtonsoft.Json.JsonSerializerSettings SerializerSettings { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        System.Collections.Generic.IEnumerator<Microsoft.AspNetCore.Mvc.Infrastructure.ICompatibilitySwitch> System.Collections.Generic.IEnumerable<Microsoft.AspNetCore.Mvc.Infrastructure.ICompatibilitySwitch>.GetEnumerator() { throw null; }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { throw null; }
    }
}
namespace Microsoft.AspNetCore.Mvc.Formatters
{
    public partial class NewtonsoftJsonInputFormatter : Microsoft.AspNetCore.Mvc.Formatters.TextInputFormatter, Microsoft.AspNetCore.Mvc.Formatters.IInputFormatterExceptionPolicy
    {
        public NewtonsoftJsonInputFormatter(Microsoft.Extensions.Logging.ILogger logger, Newtonsoft.Json.JsonSerializerSettings serializerSettings, System.Buffers.ArrayPool<char> charPool, Microsoft.Extensions.ObjectPool.ObjectPoolProvider objectPoolProvider, Microsoft.AspNetCore.Mvc.MvcOptions options, Microsoft.AspNetCore.Mvc.MvcNewtonsoftJsonOptions jsonOptions) { }
        public virtual Microsoft.AspNetCore.Mvc.Formatters.InputFormatterExceptionPolicy ExceptionPolicy { get { throw null; } }
        protected Newtonsoft.Json.JsonSerializerSettings SerializerSettings { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        protected virtual Newtonsoft.Json.JsonSerializer CreateJsonSerializer() { throw null; }
        protected virtual Newtonsoft.Json.JsonSerializer CreateJsonSerializer(Microsoft.AspNetCore.Mvc.Formatters.InputFormatterContext context) { throw null; }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public override System.Threading.Tasks.Task<Microsoft.AspNetCore.Mvc.Formatters.InputFormatterResult> ReadRequestBodyAsync(Microsoft.AspNetCore.Mvc.Formatters.InputFormatterContext context, System.Text.Encoding encoding) { throw null; }
        protected virtual void ReleaseJsonSerializer(Newtonsoft.Json.JsonSerializer serializer) { }
    }
    public partial class NewtonsoftJsonOutputFormatter : Microsoft.AspNetCore.Mvc.Formatters.TextOutputFormatter
    {
        public NewtonsoftJsonOutputFormatter(Newtonsoft.Json.JsonSerializerSettings serializerSettings, System.Buffers.ArrayPool<char> charPool, Microsoft.AspNetCore.Mvc.MvcOptions mvcOptions) { }
        protected Newtonsoft.Json.JsonSerializerSettings SerializerSettings { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        protected virtual Newtonsoft.Json.JsonSerializer CreateJsonSerializer() { throw null; }
        protected virtual Newtonsoft.Json.JsonSerializer CreateJsonSerializer(Microsoft.AspNetCore.Mvc.Formatters.OutputFormatterWriteContext context) { throw null; }
        protected virtual Newtonsoft.Json.JsonWriter CreateJsonWriter(System.IO.TextWriter writer) { throw null; }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public override System.Threading.Tasks.Task WriteResponseBodyAsync(Microsoft.AspNetCore.Mvc.Formatters.OutputFormatterWriteContext context, System.Text.Encoding selectedEncoding) { throw null; }
    }
    public partial class NewtonsoftJsonPatchInputFormatter : Microsoft.AspNetCore.Mvc.Formatters.NewtonsoftJsonInputFormatter
    {
        public NewtonsoftJsonPatchInputFormatter(Microsoft.Extensions.Logging.ILogger logger, Newtonsoft.Json.JsonSerializerSettings serializerSettings, System.Buffers.ArrayPool<char> charPool, Microsoft.Extensions.ObjectPool.ObjectPoolProvider objectPoolProvider, Microsoft.AspNetCore.Mvc.MvcOptions options, Microsoft.AspNetCore.Mvc.MvcNewtonsoftJsonOptions jsonOptions) : base (default(Microsoft.Extensions.Logging.ILogger), default(Newtonsoft.Json.JsonSerializerSettings), default(System.Buffers.ArrayPool<char>), default(Microsoft.Extensions.ObjectPool.ObjectPoolProvider), default(Microsoft.AspNetCore.Mvc.MvcOptions), default(Microsoft.AspNetCore.Mvc.MvcNewtonsoftJsonOptions)) { }
        public override Microsoft.AspNetCore.Mvc.Formatters.InputFormatterExceptionPolicy ExceptionPolicy { get { throw null; } }
        public override bool CanRead(Microsoft.AspNetCore.Mvc.Formatters.InputFormatterContext context) { throw null; }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public override System.Threading.Tasks.Task<Microsoft.AspNetCore.Mvc.Formatters.InputFormatterResult> ReadRequestBodyAsync(Microsoft.AspNetCore.Mvc.Formatters.InputFormatterContext context, System.Text.Encoding encoding) { throw null; }
    }
}
namespace Microsoft.AspNetCore.Mvc.NewtonsoftJson
{
    public static partial class JsonSerializerSettingsProvider
    {
        public static Newtonsoft.Json.JsonSerializerSettings CreateSerializerSettings() { throw null; }
    }
    public sealed partial class ProblemDetailsConverter : Newtonsoft.Json.JsonConverter
    {
        public ProblemDetailsConverter() { }
        public override bool CanConvert(System.Type objectType) { throw null; }
        public override object ReadJson(Newtonsoft.Json.JsonReader reader, System.Type objectType, object existingValue, Newtonsoft.Json.JsonSerializer serializer) { throw null; }
        public override void WriteJson(Newtonsoft.Json.JsonWriter writer, object value, Newtonsoft.Json.JsonSerializer serializer) { }
    }
    public sealed partial class ValidationProblemDetailsConverter : Newtonsoft.Json.JsonConverter
    {
        public ValidationProblemDetailsConverter() { }
        public override bool CanConvert(System.Type objectType) { throw null; }
        public override object ReadJson(Newtonsoft.Json.JsonReader reader, System.Type objectType, object existingValue, Newtonsoft.Json.JsonSerializer serializer) { throw null; }
        public override void WriteJson(Newtonsoft.Json.JsonWriter writer, object value, Newtonsoft.Json.JsonSerializer serializer) { }
    }
}
namespace Microsoft.AspNetCore.Mvc.Rendering
{
    public static partial class JsonHelperExtensions
    {
        public static Microsoft.AspNetCore.Html.IHtmlContent Serialize(this Microsoft.AspNetCore.Mvc.Rendering.IJsonHelper jsonHelper, object value, Newtonsoft.Json.JsonSerializerSettings serializerSettings) { throw null; }
    }
}
namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class MvcNewtonsoftJsonOptionsExtensions
    {
        public static Microsoft.AspNetCore.Mvc.MvcNewtonsoftJsonOptions UseCamelCasing(this Microsoft.AspNetCore.Mvc.MvcNewtonsoftJsonOptions options, bool processDictionaryKeys) { throw null; }
        public static Microsoft.AspNetCore.Mvc.MvcNewtonsoftJsonOptions UseMemberCasing(this Microsoft.AspNetCore.Mvc.MvcNewtonsoftJsonOptions options) { throw null; }
    }
    public static partial class NewtonsoftJsonMvcBuilderExtensions
    {
        public static Microsoft.Extensions.DependencyInjection.IMvcBuilder AddNewtonsoftJson(this Microsoft.Extensions.DependencyInjection.IMvcBuilder builder) { throw null; }
        public static Microsoft.Extensions.DependencyInjection.IMvcBuilder AddNewtonsoftJson(this Microsoft.Extensions.DependencyInjection.IMvcBuilder builder, System.Action<Microsoft.AspNetCore.Mvc.MvcNewtonsoftJsonOptions> setupAction) { throw null; }
    }
    public static partial class NewtonsoftJsonMvcCoreBuilderExtensions
    {
        public static Microsoft.Extensions.DependencyInjection.IMvcCoreBuilder AddNewtonsoftJson(this Microsoft.Extensions.DependencyInjection.IMvcCoreBuilder builder) { throw null; }
        public static Microsoft.Extensions.DependencyInjection.IMvcCoreBuilder AddNewtonsoftJson(this Microsoft.Extensions.DependencyInjection.IMvcCoreBuilder builder, System.Action<Microsoft.AspNetCore.Mvc.MvcNewtonsoftJsonOptions> setupAction) { throw null; }
    }
}
