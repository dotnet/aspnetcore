// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Mvc.Formatters
{
    public partial class XmlDataContractSerializerInputFormatter : Microsoft.AspNetCore.Mvc.Formatters.TextInputFormatter, Microsoft.AspNetCore.Mvc.Formatters.IInputFormatterExceptionPolicy
    {
        public XmlDataContractSerializerInputFormatter(Microsoft.AspNetCore.Mvc.MvcOptions options) { }
        public virtual Microsoft.AspNetCore.Mvc.Formatters.InputFormatterExceptionPolicy ExceptionPolicy { get { throw null; } }
        public int MaxDepth { get { throw null; } set { } }
        public System.Runtime.Serialization.DataContractSerializerSettings SerializerSettings { get { throw null; } set { } }
        public System.Collections.Generic.IList<Microsoft.AspNetCore.Mvc.Formatters.Xml.IWrapperProviderFactory> WrapperProviderFactories { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public System.Xml.XmlDictionaryReaderQuotas XmlDictionaryReaderQuotas { get { throw null; } }
        protected override bool CanReadType(System.Type type) { throw null; }
        protected virtual System.Runtime.Serialization.DataContractSerializer CreateSerializer(System.Type type) { throw null; }
        protected virtual System.Xml.XmlReader CreateXmlReader(System.IO.Stream readStream, System.Text.Encoding encoding) { throw null; }
        protected virtual System.Runtime.Serialization.DataContractSerializer GetCachedSerializer(System.Type type) { throw null; }
        protected virtual System.Type GetSerializableType(System.Type declaredType) { throw null; }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public override System.Threading.Tasks.Task<Microsoft.AspNetCore.Mvc.Formatters.InputFormatterResult> ReadRequestBodyAsync(Microsoft.AspNetCore.Mvc.Formatters.InputFormatterContext context, System.Text.Encoding encoding) { throw null; }
    }
    public partial class XmlDataContractSerializerOutputFormatter : Microsoft.AspNetCore.Mvc.Formatters.TextOutputFormatter
    {
        public XmlDataContractSerializerOutputFormatter() { }
        public XmlDataContractSerializerOutputFormatter(Microsoft.Extensions.Logging.ILoggerFactory loggerFactory) { }
        public XmlDataContractSerializerOutputFormatter(System.Xml.XmlWriterSettings writerSettings) { }
        public XmlDataContractSerializerOutputFormatter(System.Xml.XmlWriterSettings writerSettings, Microsoft.Extensions.Logging.ILoggerFactory loggerFactory) { }
        public System.Runtime.Serialization.DataContractSerializerSettings SerializerSettings { get { throw null; } set { } }
        public System.Collections.Generic.IList<Microsoft.AspNetCore.Mvc.Formatters.Xml.IWrapperProviderFactory> WrapperProviderFactories { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public System.Xml.XmlWriterSettings WriterSettings { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        protected override bool CanWriteType(System.Type type) { throw null; }
        protected virtual System.Runtime.Serialization.DataContractSerializer CreateSerializer(System.Type type) { throw null; }
        public virtual System.Xml.XmlWriter CreateXmlWriter(Microsoft.AspNetCore.Mvc.Formatters.OutputFormatterWriteContext context, System.IO.TextWriter writer, System.Xml.XmlWriterSettings xmlWriterSettings) { throw null; }
        public virtual System.Xml.XmlWriter CreateXmlWriter(System.IO.TextWriter writer, System.Xml.XmlWriterSettings xmlWriterSettings) { throw null; }
        protected virtual System.Runtime.Serialization.DataContractSerializer GetCachedSerializer(System.Type type) { throw null; }
        protected virtual System.Type GetSerializableType(System.Type type) { throw null; }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public override System.Threading.Tasks.Task WriteResponseBodyAsync(Microsoft.AspNetCore.Mvc.Formatters.OutputFormatterWriteContext context, System.Text.Encoding selectedEncoding) { throw null; }
    }
    public partial class XmlSerializerInputFormatter : Microsoft.AspNetCore.Mvc.Formatters.TextInputFormatter, Microsoft.AspNetCore.Mvc.Formatters.IInputFormatterExceptionPolicy
    {
        public XmlSerializerInputFormatter(Microsoft.AspNetCore.Mvc.MvcOptions options) { }
        public virtual Microsoft.AspNetCore.Mvc.Formatters.InputFormatterExceptionPolicy ExceptionPolicy { get { throw null; } }
        public int MaxDepth { get { throw null; } set { } }
        public System.Collections.Generic.IList<Microsoft.AspNetCore.Mvc.Formatters.Xml.IWrapperProviderFactory> WrapperProviderFactories { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public System.Xml.XmlDictionaryReaderQuotas XmlDictionaryReaderQuotas { get { throw null; } }
        protected override bool CanReadType(System.Type type) { throw null; }
        protected virtual System.Xml.Serialization.XmlSerializer CreateSerializer(System.Type type) { throw null; }
        protected virtual System.Xml.XmlReader CreateXmlReader(System.IO.Stream readStream, System.Text.Encoding encoding) { throw null; }
        protected virtual System.Xml.XmlReader CreateXmlReader(System.IO.Stream readStream, System.Text.Encoding encoding, System.Type type) { throw null; }
        protected virtual System.Xml.Serialization.XmlSerializer GetCachedSerializer(System.Type type) { throw null; }
        protected virtual System.Type GetSerializableType(System.Type declaredType) { throw null; }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public override System.Threading.Tasks.Task<Microsoft.AspNetCore.Mvc.Formatters.InputFormatterResult> ReadRequestBodyAsync(Microsoft.AspNetCore.Mvc.Formatters.InputFormatterContext context, System.Text.Encoding encoding) { throw null; }
    }
    public partial class XmlSerializerOutputFormatter : Microsoft.AspNetCore.Mvc.Formatters.TextOutputFormatter
    {
        public XmlSerializerOutputFormatter() { }
        public XmlSerializerOutputFormatter(Microsoft.Extensions.Logging.ILoggerFactory loggerFactory) { }
        public XmlSerializerOutputFormatter(System.Xml.XmlWriterSettings writerSettings) { }
        public XmlSerializerOutputFormatter(System.Xml.XmlWriterSettings writerSettings, Microsoft.Extensions.Logging.ILoggerFactory loggerFactory) { }
        public System.Collections.Generic.IList<Microsoft.AspNetCore.Mvc.Formatters.Xml.IWrapperProviderFactory> WrapperProviderFactories { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public System.Xml.XmlWriterSettings WriterSettings { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        protected override bool CanWriteType(System.Type type) { throw null; }
        protected virtual System.Xml.Serialization.XmlSerializer CreateSerializer(System.Type type) { throw null; }
        public virtual System.Xml.XmlWriter CreateXmlWriter(Microsoft.AspNetCore.Mvc.Formatters.OutputFormatterWriteContext context, System.IO.TextWriter writer, System.Xml.XmlWriterSettings xmlWriterSettings) { throw null; }
        public virtual System.Xml.XmlWriter CreateXmlWriter(System.IO.TextWriter writer, System.Xml.XmlWriterSettings xmlWriterSettings) { throw null; }
        protected virtual System.Xml.Serialization.XmlSerializer GetCachedSerializer(System.Type type) { throw null; }
        protected virtual System.Type GetSerializableType(System.Type type) { throw null; }
        protected virtual void Serialize(System.Xml.Serialization.XmlSerializer xmlSerializer, System.Xml.XmlWriter xmlWriter, object value) { }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public override System.Threading.Tasks.Task WriteResponseBodyAsync(Microsoft.AspNetCore.Mvc.Formatters.OutputFormatterWriteContext context, System.Text.Encoding selectedEncoding) { throw null; }
    }
}
namespace Microsoft.AspNetCore.Mvc.Formatters.Xml
{
    public partial class DelegatingEnumerable<TWrapped, TDeclared> : System.Collections.Generic.IEnumerable<TWrapped>, System.Collections.IEnumerable
    {
        public DelegatingEnumerable() { }
        public DelegatingEnumerable(System.Collections.Generic.IEnumerable<TDeclared> source, Microsoft.AspNetCore.Mvc.Formatters.Xml.IWrapperProvider elementWrapperProvider) { }
        public void Add(object item) { }
        public System.Collections.Generic.IEnumerator<TWrapped> GetEnumerator() { throw null; }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { throw null; }
    }
    public partial class DelegatingEnumerator<TWrapped, TDeclared> : System.Collections.Generic.IEnumerator<TWrapped>, System.Collections.IEnumerator, System.IDisposable
    {
        public DelegatingEnumerator(System.Collections.Generic.IEnumerator<TDeclared> inner, Microsoft.AspNetCore.Mvc.Formatters.Xml.IWrapperProvider wrapperProvider) { }
        public TWrapped Current { get { throw null; } }
        object System.Collections.IEnumerator.Current { get { throw null; } }
        public void Dispose() { }
        public bool MoveNext() { throw null; }
        public void Reset() { }
    }
    public partial class EnumerableWrapperProvider : Microsoft.AspNetCore.Mvc.Formatters.Xml.IWrapperProvider
    {
        public EnumerableWrapperProvider(System.Type sourceEnumerableOfT, Microsoft.AspNetCore.Mvc.Formatters.Xml.IWrapperProvider elementWrapperProvider) { }
        public System.Type WrappingType { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public object Wrap(object original) { throw null; }
    }
    public partial class EnumerableWrapperProviderFactory : Microsoft.AspNetCore.Mvc.Formatters.Xml.IWrapperProviderFactory
    {
        public EnumerableWrapperProviderFactory(System.Collections.Generic.IEnumerable<Microsoft.AspNetCore.Mvc.Formatters.Xml.IWrapperProviderFactory> wrapperProviderFactories) { }
        public Microsoft.AspNetCore.Mvc.Formatters.Xml.IWrapperProvider GetProvider(Microsoft.AspNetCore.Mvc.Formatters.Xml.WrapperProviderContext context) { throw null; }
    }
    public partial interface IUnwrappable
    {
        object Unwrap(System.Type declaredType);
    }
    public partial interface IWrapperProvider
    {
        System.Type WrappingType { get; }
        object Wrap(object original);
    }
    public partial interface IWrapperProviderFactory
    {
        Microsoft.AspNetCore.Mvc.Formatters.Xml.IWrapperProvider GetProvider(Microsoft.AspNetCore.Mvc.Formatters.Xml.WrapperProviderContext context);
    }
    public partial class MvcXmlOptions : System.Collections.Generic.IEnumerable<Microsoft.AspNetCore.Mvc.Infrastructure.ICompatibilitySwitch>, System.Collections.IEnumerable
    {
        public MvcXmlOptions() { }
        System.Collections.Generic.IEnumerator<Microsoft.AspNetCore.Mvc.Infrastructure.ICompatibilitySwitch> System.Collections.Generic.IEnumerable<Microsoft.AspNetCore.Mvc.Infrastructure.ICompatibilitySwitch>.GetEnumerator() { throw null; }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { throw null; }
    }
    [System.Xml.Serialization.XmlRootAttribute("problem", Namespace="urn:ietf:rfc:7807")]
    public partial class ProblemDetailsWrapper : Microsoft.AspNetCore.Mvc.Formatters.Xml.IUnwrappable, System.Xml.Serialization.IXmlSerializable
    {
        protected static readonly string EmptyKey;
        public ProblemDetailsWrapper() { }
        public ProblemDetailsWrapper(Microsoft.AspNetCore.Mvc.ProblemDetails problemDetails) { }
        public System.Xml.Schema.XmlSchema GetSchema() { throw null; }
        object Microsoft.AspNetCore.Mvc.Formatters.Xml.IUnwrappable.Unwrap(System.Type declaredType) { throw null; }
        protected virtual void ReadValue(System.Xml.XmlReader reader, string name) { }
        public virtual void ReadXml(System.Xml.XmlReader reader) { }
        public virtual void WriteXml(System.Xml.XmlWriter writer) { }
    }
    [System.Xml.Serialization.XmlRootAttribute("Error")]
    public sealed partial class SerializableErrorWrapper : Microsoft.AspNetCore.Mvc.Formatters.Xml.IUnwrappable, System.Xml.Serialization.IXmlSerializable
    {
        public SerializableErrorWrapper() { }
        public SerializableErrorWrapper(Microsoft.AspNetCore.Mvc.SerializableError error) { }
        public Microsoft.AspNetCore.Mvc.SerializableError SerializableError { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public System.Xml.Schema.XmlSchema GetSchema() { throw null; }
        public void ReadXml(System.Xml.XmlReader reader) { }
        public object Unwrap(System.Type declaredType) { throw null; }
        public void WriteXml(System.Xml.XmlWriter writer) { }
    }
    public partial class SerializableErrorWrapperProvider : Microsoft.AspNetCore.Mvc.Formatters.Xml.IWrapperProvider
    {
        public SerializableErrorWrapperProvider() { }
        public System.Type WrappingType { get { throw null; } }
        public object Wrap(object original) { throw null; }
    }
    public partial class SerializableErrorWrapperProviderFactory : Microsoft.AspNetCore.Mvc.Formatters.Xml.IWrapperProviderFactory
    {
        public SerializableErrorWrapperProviderFactory() { }
        public Microsoft.AspNetCore.Mvc.Formatters.Xml.IWrapperProvider GetProvider(Microsoft.AspNetCore.Mvc.Formatters.Xml.WrapperProviderContext context) { throw null; }
    }
    [System.Xml.Serialization.XmlRootAttribute("problem", Namespace="urn:ietf:rfc:7807")]
    public partial class ValidationProblemDetailsWrapper : Microsoft.AspNetCore.Mvc.Formatters.Xml.ProblemDetailsWrapper, Microsoft.AspNetCore.Mvc.Formatters.Xml.IUnwrappable
    {
        public ValidationProblemDetailsWrapper() { }
        public ValidationProblemDetailsWrapper(Microsoft.AspNetCore.Mvc.ValidationProblemDetails problemDetails) { }
        object Microsoft.AspNetCore.Mvc.Formatters.Xml.IUnwrappable.Unwrap(System.Type declaredType) { throw null; }
        protected override void ReadValue(System.Xml.XmlReader reader, string name) { }
        public override void WriteXml(System.Xml.XmlWriter writer) { }
    }
    public partial class WrapperProviderContext
    {
        public WrapperProviderContext(System.Type declaredType, bool isSerialization) { }
        public System.Type DeclaredType { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public bool IsSerialization { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
    }
    public static partial class WrapperProviderFactoriesExtensions
    {
        public static Microsoft.AspNetCore.Mvc.Formatters.Xml.IWrapperProvider GetWrapperProvider(this System.Collections.Generic.IEnumerable<Microsoft.AspNetCore.Mvc.Formatters.Xml.IWrapperProviderFactory> wrapperProviderFactories, Microsoft.AspNetCore.Mvc.Formatters.Xml.WrapperProviderContext wrapperProviderContext) { throw null; }
    }
}
namespace Microsoft.AspNetCore.Mvc.ModelBinding.Metadata
{
    public partial class DataMemberRequiredBindingMetadataProvider : Microsoft.AspNetCore.Mvc.ModelBinding.Metadata.IBindingMetadataProvider, Microsoft.AspNetCore.Mvc.ModelBinding.Metadata.IMetadataDetailsProvider
    {
        public DataMemberRequiredBindingMetadataProvider() { }
        public void CreateBindingMetadata(Microsoft.AspNetCore.Mvc.ModelBinding.Metadata.BindingMetadataProviderContext context) { }
    }
}
namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class MvcXmlMvcBuilderExtensions
    {
        public static Microsoft.Extensions.DependencyInjection.IMvcBuilder AddXmlDataContractSerializerFormatters(this Microsoft.Extensions.DependencyInjection.IMvcBuilder builder) { throw null; }
        public static Microsoft.Extensions.DependencyInjection.IMvcBuilder AddXmlDataContractSerializerFormatters(this Microsoft.Extensions.DependencyInjection.IMvcBuilder builder, System.Action<Microsoft.AspNetCore.Mvc.Formatters.Xml.MvcXmlOptions> setupAction) { throw null; }
        public static Microsoft.Extensions.DependencyInjection.IMvcBuilder AddXmlOptions(this Microsoft.Extensions.DependencyInjection.IMvcBuilder builder, System.Action<Microsoft.AspNetCore.Mvc.Formatters.Xml.MvcXmlOptions> setupAction) { throw null; }
        public static Microsoft.Extensions.DependencyInjection.IMvcBuilder AddXmlSerializerFormatters(this Microsoft.Extensions.DependencyInjection.IMvcBuilder builder) { throw null; }
        public static Microsoft.Extensions.DependencyInjection.IMvcBuilder AddXmlSerializerFormatters(this Microsoft.Extensions.DependencyInjection.IMvcBuilder builder, System.Action<Microsoft.AspNetCore.Mvc.Formatters.Xml.MvcXmlOptions> setupAction) { throw null; }
    }
    public static partial class MvcXmlMvcCoreBuilderExtensions
    {
        public static Microsoft.Extensions.DependencyInjection.IMvcCoreBuilder AddXmlDataContractSerializerFormatters(this Microsoft.Extensions.DependencyInjection.IMvcCoreBuilder builder) { throw null; }
        public static Microsoft.Extensions.DependencyInjection.IMvcCoreBuilder AddXmlDataContractSerializerFormatters(this Microsoft.Extensions.DependencyInjection.IMvcCoreBuilder builder, System.Action<Microsoft.AspNetCore.Mvc.Formatters.Xml.MvcXmlOptions> setupAction) { throw null; }
        public static Microsoft.Extensions.DependencyInjection.IMvcCoreBuilder AddXmlOptions(this Microsoft.Extensions.DependencyInjection.IMvcCoreBuilder builder, System.Action<Microsoft.AspNetCore.Mvc.Formatters.Xml.MvcXmlOptions> setupAction) { throw null; }
        public static Microsoft.Extensions.DependencyInjection.IMvcCoreBuilder AddXmlSerializerFormatters(this Microsoft.Extensions.DependencyInjection.IMvcCoreBuilder builder) { throw null; }
        public static Microsoft.Extensions.DependencyInjection.IMvcCoreBuilder AddXmlSerializerFormatters(this Microsoft.Extensions.DependencyInjection.IMvcCoreBuilder builder, System.Action<Microsoft.AspNetCore.Mvc.Formatters.Xml.MvcXmlOptions> setupAction) { throw null; }
    }
}
