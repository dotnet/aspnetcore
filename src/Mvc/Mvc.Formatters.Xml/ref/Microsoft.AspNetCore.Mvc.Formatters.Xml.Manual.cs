// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Mvc.Formatters.Xml
{
    public partial class ProblemDetailsWrapper : Microsoft.AspNetCore.Mvc.Formatters.Xml.IUnwrappable, System.Xml.Serialization.IXmlSerializable
    {
        internal Microsoft.AspNetCore.Mvc.ProblemDetails ProblemDetails { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
    }
    public partial class ValidationProblemDetailsWrapper : Microsoft.AspNetCore.Mvc.Formatters.Xml.ProblemDetailsWrapper, Microsoft.AspNetCore.Mvc.Formatters.Xml.IUnwrappable
    {
        internal new Microsoft.AspNetCore.Mvc.ValidationProblemDetails ProblemDetails { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
    }
    internal partial class ProblemDetailsWrapperProviderFactory : Microsoft.AspNetCore.Mvc.Formatters.Xml.IWrapperProviderFactory
    {
        public ProblemDetailsWrapperProviderFactory() { }
        public Microsoft.AspNetCore.Mvc.Formatters.Xml.IWrapperProvider GetProvider(Microsoft.AspNetCore.Mvc.Formatters.Xml.WrapperProviderContext context) { throw null; }
    }
    internal static partial class FormattingUtilities
    {
        public static readonly int DefaultMaxDepth;
        public static readonly System.Runtime.Serialization.XsdDataContractExporter XsdDataContractExporter;
        public static System.Xml.XmlDictionaryReaderQuotas GetDefaultXmlReaderQuotas() { throw null; }
        public static System.Xml.XmlWriterSettings GetDefaultXmlWriterSettings() { throw null; }
    }
}

namespace Microsoft.Extensions.DependencyInjection
{
    internal sealed partial class XmlDataContractSerializerMvcOptionsSetup : Microsoft.Extensions.Options.IConfigureOptions<Microsoft.AspNetCore.Mvc.MvcOptions>
    {
        public XmlDataContractSerializerMvcOptionsSetup(Microsoft.Extensions.Logging.ILoggerFactory loggerFactory) { }
        public void Configure(Microsoft.AspNetCore.Mvc.MvcOptions options) { }
    }
    internal sealed partial class XmlSerializerMvcOptionsSetup : Microsoft.Extensions.Options.IConfigureOptions<Microsoft.AspNetCore.Mvc.MvcOptions>
    {
        public XmlSerializerMvcOptionsSetup(Microsoft.Extensions.Logging.ILoggerFactory loggerFactory) { }
        public void Configure(Microsoft.AspNetCore.Mvc.MvcOptions options) { }
    }
}