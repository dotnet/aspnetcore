// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;

namespace Microsoft.AspNetCore.Analyzers
{
    internal static class SymbolNames
    {
        public const string ConfigureServicesMethodPrefix = "Configure";

        public const string ConfigureServicesMethodSuffix = "Services";

        public const string ConfigureMethodPrefix = "Configure";

        public static class IApplicationBuilder
        {
            public const string MetadataName = "Microsoft.AspNetCore.Builder.IApplicationBuilder";
        }

        public static class IServiceCollection
        {
            public const string MetadataName = "Microsoft.Extensions.DependencyInjection.IServiceCollection";
        }
    }
}
