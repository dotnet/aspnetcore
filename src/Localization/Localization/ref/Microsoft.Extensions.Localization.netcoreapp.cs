// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class LocalizationServiceCollectionExtensions
    {
        public static Microsoft.Extensions.DependencyInjection.IServiceCollection AddLocalization(this Microsoft.Extensions.DependencyInjection.IServiceCollection services) { throw null; }
        public static Microsoft.Extensions.DependencyInjection.IServiceCollection AddLocalization(this Microsoft.Extensions.DependencyInjection.IServiceCollection services, System.Action<Microsoft.Extensions.Localization.LocalizationOptions> setupAction) { throw null; }
    }
}
namespace Microsoft.Extensions.Localization
{
    public partial interface IResourceNamesCache
    {
        System.Collections.Generic.IList<string> GetOrAdd(string name, System.Func<string, System.Collections.Generic.IList<string>> valueFactory);
    }
    public partial class LocalizationOptions
    {
        public LocalizationOptions() { }
        public string ResourcesPath { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Assembly, AllowMultiple=false, Inherited=false)]
    public partial class ResourceLocationAttribute : System.Attribute
    {
        public ResourceLocationAttribute(string resourceLocation) { }
        public string ResourceLocation { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
    }
    public partial class ResourceManagerStringLocalizer : Microsoft.Extensions.Localization.IStringLocalizer
    {
        public ResourceManagerStringLocalizer(System.Resources.ResourceManager resourceManager, Microsoft.Extensions.Localization.Internal.AssemblyWrapper resourceAssemblyWrapper, string baseName, Microsoft.Extensions.Localization.IResourceNamesCache resourceNamesCache, Microsoft.Extensions.Logging.ILogger logger) { }
        public ResourceManagerStringLocalizer(System.Resources.ResourceManager resourceManager, Microsoft.Extensions.Localization.Internal.IResourceStringProvider resourceStringProvider, string baseName, Microsoft.Extensions.Localization.IResourceNamesCache resourceNamesCache, Microsoft.Extensions.Logging.ILogger logger) { }
        public ResourceManagerStringLocalizer(System.Resources.ResourceManager resourceManager, System.Reflection.Assembly resourceAssembly, string baseName, Microsoft.Extensions.Localization.IResourceNamesCache resourceNamesCache, Microsoft.Extensions.Logging.ILogger logger) { }
        public virtual Microsoft.Extensions.Localization.LocalizedString this[string name] { get { throw null; } }
        public virtual Microsoft.Extensions.Localization.LocalizedString this[string name, params object[] arguments] { get { throw null; } }
        public virtual System.Collections.Generic.IEnumerable<Microsoft.Extensions.Localization.LocalizedString> GetAllStrings(bool includeParentCultures) { throw null; }
        protected System.Collections.Generic.IEnumerable<Microsoft.Extensions.Localization.LocalizedString> GetAllStrings(bool includeParentCultures, System.Globalization.CultureInfo culture) { throw null; }
        protected string GetStringSafely(string name, System.Globalization.CultureInfo culture) { throw null; }
    }
    public partial class ResourceManagerStringLocalizerFactory : Microsoft.Extensions.Localization.IStringLocalizerFactory
    {
        public ResourceManagerStringLocalizerFactory(Microsoft.Extensions.Options.IOptions<Microsoft.Extensions.Localization.LocalizationOptions> localizationOptions, Microsoft.Extensions.Logging.ILoggerFactory loggerFactory) { }
        public Microsoft.Extensions.Localization.IStringLocalizer Create(string baseName, string location) { throw null; }
        public Microsoft.Extensions.Localization.IStringLocalizer Create(System.Type resourceSource) { throw null; }
        protected virtual Microsoft.Extensions.Localization.ResourceManagerStringLocalizer CreateResourceManagerStringLocalizer(System.Reflection.Assembly assembly, string baseName) { throw null; }
        protected virtual Microsoft.Extensions.Localization.ResourceLocationAttribute GetResourceLocationAttribute(System.Reflection.Assembly assembly) { throw null; }
        protected virtual string GetResourcePrefix(System.Reflection.TypeInfo typeInfo) { throw null; }
        protected virtual string GetResourcePrefix(System.Reflection.TypeInfo typeInfo, string baseNamespace, string resourcesRelativePath) { throw null; }
        protected virtual string GetResourcePrefix(string baseResourceName, string baseNamespace) { throw null; }
        protected virtual string GetResourcePrefix(string location, string baseName, string resourceLocation) { throw null; }
        protected virtual Microsoft.Extensions.Localization.RootNamespaceAttribute GetRootNamespaceAttribute(System.Reflection.Assembly assembly) { throw null; }
    }
    public partial class ResourceNamesCache : Microsoft.Extensions.Localization.IResourceNamesCache
    {
        public ResourceNamesCache() { }
        public System.Collections.Generic.IList<string> GetOrAdd(string name, System.Func<string, System.Collections.Generic.IList<string>> valueFactory) { throw null; }
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Assembly, AllowMultiple=false, Inherited=false)]
    public partial class RootNamespaceAttribute : System.Attribute
    {
        public RootNamespaceAttribute(string rootNamespace) { }
        public string RootNamespace { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
    }
}
namespace Microsoft.Extensions.Localization.Internal
{
    public partial class AssemblyWrapper
    {
        public AssemblyWrapper(System.Reflection.Assembly assembly) { }
        public System.Reflection.Assembly Assembly { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public virtual string FullName { get { throw null; } }
        public virtual System.IO.Stream GetManifestResourceStream(string name) { throw null; }
    }
    public partial interface IResourceStringProvider
    {
        System.Collections.Generic.IList<string> GetAllResourceStrings(System.Globalization.CultureInfo culture, bool throwOnMissing);
    }
    public partial class ResourceManagerStringProvider : Microsoft.Extensions.Localization.Internal.IResourceStringProvider
    {
        public ResourceManagerStringProvider(Microsoft.Extensions.Localization.IResourceNamesCache resourceCache, System.Resources.ResourceManager resourceManager, System.Reflection.Assembly assembly, string baseName) { }
        public System.Collections.Generic.IList<string> GetAllResourceStrings(System.Globalization.CultureInfo culture, bool throwOnMissing) { throw null; }
    }
}
