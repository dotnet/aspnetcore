// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.Extensions.Configuration
{
    public static partial class KeyPerFileConfigurationBuilderExtensions
    {
        public static Microsoft.Extensions.Configuration.IConfigurationBuilder AddKeyPerFile(this Microsoft.Extensions.Configuration.IConfigurationBuilder builder, System.Action<Microsoft.Extensions.Configuration.KeyPerFile.KeyPerFileConfigurationSource> configureSource) { throw null; }
        public static Microsoft.Extensions.Configuration.IConfigurationBuilder AddKeyPerFile(this Microsoft.Extensions.Configuration.IConfigurationBuilder builder, string directoryPath) { throw null; }
        public static Microsoft.Extensions.Configuration.IConfigurationBuilder AddKeyPerFile(this Microsoft.Extensions.Configuration.IConfigurationBuilder builder, string directoryPath, bool optional) { throw null; }
        public static Microsoft.Extensions.Configuration.IConfigurationBuilder AddKeyPerFile(this Microsoft.Extensions.Configuration.IConfigurationBuilder builder, string directoryPath, bool optional, bool reloadOnChange) { throw null; }
    }
}
namespace Microsoft.Extensions.Configuration.KeyPerFile
{
    public partial class KeyPerFileConfigurationProvider : Microsoft.Extensions.Configuration.ConfigurationProvider, System.IDisposable
    {
        public KeyPerFileConfigurationProvider(Microsoft.Extensions.Configuration.KeyPerFile.KeyPerFileConfigurationSource source) { }
        public void Dispose() { }
        public override void Load() { }
        public override string ToString() { throw null; }
    }
    public partial class KeyPerFileConfigurationSource : Microsoft.Extensions.Configuration.IConfigurationSource
    {
        public KeyPerFileConfigurationSource() { }
        public Microsoft.Extensions.FileProviders.IFileProvider FileProvider { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public System.Func<string, bool> IgnoreCondition { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public string IgnorePrefix { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public bool Optional { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public int ReloadDelay { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public bool ReloadOnChange { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public Microsoft.Extensions.Configuration.IConfigurationProvider Build(Microsoft.Extensions.Configuration.IConfigurationBuilder builder) { throw null; }
    }
}
