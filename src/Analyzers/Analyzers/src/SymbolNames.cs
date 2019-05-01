// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;

namespace Microsoft.AspNetCore.Analyzers
{
    internal static class SymbolNames
    {
        public static readonly string ConfigureServicesMethodPrefix = "Configure";

        public static readonly string ConfigureServicesMethodSuffix = "Services";

        public static readonly string ConfigureMethodPrefix = "Configure";

        public static class IApplicationBuilder
        {
            public static readonly string MetadataName = "Microsoft.AspNetCore.Builder.IApplicationBuilder";
        }

        public static class IServiceCollection
        {
            public static readonly string MetadataName = "Microsoft.Extensions.DependencyInjection.IServiceCollection";
        }
    }
}
