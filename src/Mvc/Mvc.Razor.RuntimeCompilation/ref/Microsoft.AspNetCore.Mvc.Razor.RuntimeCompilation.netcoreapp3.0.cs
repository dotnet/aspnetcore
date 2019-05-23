// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Mvc.ApplicationParts
{
    public static partial class AssemblyPartExtensions
    {
        public static System.Collections.Generic.IEnumerable<string> GetReferencePaths(this Microsoft.AspNetCore.Mvc.ApplicationParts.AssemblyPart assemblyPart) { throw null; }
    }
}
namespace Microsoft.AspNetCore.Mvc.Razor.RuntimeCompilation
{
    public partial class FileProviderRazorProjectItem : Microsoft.AspNetCore.Razor.Language.RazorProjectItem
    {
        public FileProviderRazorProjectItem(Microsoft.Extensions.FileProviders.IFileInfo fileInfo, string basePath, string filePath, string root) { }
        public FileProviderRazorProjectItem(Microsoft.Extensions.FileProviders.IFileInfo fileInfo, string basePath, string filePath, string root, string fileKind) { }
        public override string BasePath { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public override bool Exists { get { throw null; } }
        public Microsoft.Extensions.FileProviders.IFileInfo FileInfo { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public override string FileKind { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public override string FilePath { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public override string PhysicalPath { get { throw null; } }
        public override string RelativePhysicalPath { get { throw null; } }
        public override System.IO.Stream Read() { throw null; }
    }
    public partial class MvcRazorRuntimeCompilationOptions
    {
        public MvcRazorRuntimeCompilationOptions() { }
        public System.Collections.Generic.IList<string> AdditionalReferencePaths { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public System.Collections.Generic.IList<Microsoft.Extensions.FileProviders.IFileProvider> FileProviders { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
    }
}
namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class RazorRuntimeCompilationMvcBuilderExtensions
    {
        public static Microsoft.Extensions.DependencyInjection.IMvcBuilder AddRazorRuntimeCompilation(this Microsoft.Extensions.DependencyInjection.IMvcBuilder builder) { throw null; }
        public static Microsoft.Extensions.DependencyInjection.IMvcBuilder AddRazorRuntimeCompilation(this Microsoft.Extensions.DependencyInjection.IMvcBuilder builder, System.Action<Microsoft.AspNetCore.Mvc.Razor.RuntimeCompilation.MvcRazorRuntimeCompilationOptions> setupAction) { throw null; }
    }
    public static partial class RazorRuntimeCompilationMvcCoreBuilderExtensions
    {
        public static Microsoft.Extensions.DependencyInjection.IMvcCoreBuilder AddRazorRuntimeCompilation(this Microsoft.Extensions.DependencyInjection.IMvcCoreBuilder builder) { throw null; }
        public static Microsoft.Extensions.DependencyInjection.IMvcCoreBuilder AddRazorRuntimeCompilation(this Microsoft.Extensions.DependencyInjection.IMvcCoreBuilder builder, System.Action<Microsoft.AspNetCore.Mvc.Razor.RuntimeCompilation.MvcRazorRuntimeCompilationOptions> setupAction) { throw null; }
    }
}
