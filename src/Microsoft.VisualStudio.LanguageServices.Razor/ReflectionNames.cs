// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#if RAZOR_EXTENSION_DEVELOPER_MODE
namespace Microsoft.VisualStudio.LanguageServices.Razor
{
    internal static class ReflectionNames
    {
        public static readonly string RazorAssemblyName = "Microsoft.AspNetCore.Razor.Language";

        public static readonly string CustomizationAttribute = RazorAssemblyName + ".RazorEngineCustomizationAttribute";

        public static readonly string DependencyAttribute = RazorAssemblyName + ".RazorEngineDependencyAttribute";
    }
}
#endif