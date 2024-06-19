// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Analyzers.Startup;

internal static class SymbolNames
{
    public static class MvcServiceCollectionExtensions
    {
        public const string AddControllersMethodName = "AddControllers";

        public const string AddControllersWithViewsMethodName = "AddControllersWithViews";

        public const string AddMvcMethodName = "AddMvc";

        public const string AddRazorPagesMethodName = "AddRazorPages";
    }

    public static class ServiceCollectionServiceExtensions
    {
        public const string AddTransientMethodName = "AddTransient";

        public const string AddScopedMethodName = "AddScoped";

        public const string AddSingletonMethodName = "AddSingleton";
    }

    public static class IProblemDetailsWriter
    {
        public const string Name = "IProblemDetailsWriter";
    }
}
